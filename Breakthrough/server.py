#!/usr/bin/env python3
"""
Breakthrough game server for classroom AI competitions.

Student clients connect over TCP and speak a simple line-based text protocol
(see STUDENT_GUIDE.md). The server runs a round-robin tournament, enforces the
rules and the per-move time limit, logs every game, and serves a live board at
http://<server-ip>:<http-port>/ that can be projected in class.

Python 3.8+, standard library only.

    python server.py --players 2 --games 2 --time 5
"""
import argparse
import asyncio
import json
import os
import re
import socket
import time
from datetime import datetime

WHITE, BLACK, EMPTY = "W", "B", "."
COLOR_NAME = {WHITE: "WHITE", BLACK: "BLACK"}
MOVE_RE = re.compile(r"^([a-z])(\d+)-?([a-z])(\d+)$")
HERE = os.path.dirname(os.path.abspath(__file__))


def other(color):
    return BLACK if color == WHITE else WHITE


# --------------------------------------------------------------------- rules
class Board:
    """grid[r][c]: r = 0 is rank 1 (White's home row), c = 0 is file a."""

    def __init__(self, rows=8, cols=8, home_rows=2):
        self.rows, self.cols = rows, cols
        self.grid = [[EMPTY] * cols for _ in range(rows)]
        for r in range(home_rows):
            for c in range(cols):
                self.grid[r][c] = WHITE
                self.grid[rows - 1 - r][c] = BLACK

    def encode(self):
        """Row-major string, starting with the top rank (Black's home row)."""
        return "".join("".join(self.grid[r]) for r in range(self.rows - 1, -1, -1))

    @staticmethod
    def square(r, c):
        return f"{chr(ord('a') + c)}{r + 1}"

    def move_str(self, mv):
        r1, c1, r2, c2 = mv
        return self.square(r1, c1) + self.square(r2, c2)

    def parse_move(self, text):
        m = MOVE_RE.match(text.strip().lower())
        if not m:
            return None
        return (int(m.group(2)) - 1, ord(m.group(1)) - 97, int(m.group(4)) - 1, ord(m.group(3)) - 97)

    def legal_moves(self, color):
        d = 1 if color == WHITE else -1
        moves = []
        for r in range(self.rows):
            nr = r + d
            if not 0 <= nr < self.rows:
                continue
            for c in range(self.cols):
                if self.grid[r][c] != color:
                    continue
                for dc in (-1, 0, 1):
                    nc = c + dc
                    if not 0 <= nc < self.cols:
                        continue
                    target = self.grid[nr][nc]
                    # straight ahead: only into an empty square; diagonal: empty or capture
                    if (dc == 0 and target == EMPTY) or (dc != 0 and target != color):
                        moves.append((r, c, nr, nc))
        return moves

    def apply(self, mv):
        r1, c1, r2, c2 = mv
        captured = self.grid[r2][c2] != EMPTY
        self.grid[r2][c2] = self.grid[r1][c1]
        self.grid[r1][c1] = EMPTY
        return captured

    def count(self, color):
        return sum(row.count(color) for row in self.grid)

    def winner(self):
        if WHITE in self.grid[self.rows - 1] or self.count(BLACK) == 0:
            return WHITE
        if BLACK in self.grid[0] or self.count(WHITE) == 0:
            return BLACK
        return None

    def pretty(self):
        lines = []
        for r in range(self.rows - 1, -1, -1):
            lines.append(f"{r + 1:>2} " + " ".join(self.grid[r]))
        lines.append("   " + " ".join(chr(97 + c) for c in range(self.cols)))
        return "\n".join(lines)


# ------------------------------------------------------------------- players
class Player:
    def __init__(self, name, reader, writer, human=False):
        self.name, self.reader, self.writer = name, reader, writer
        self.human = human  # human players get --human-time per move and live board updates
        self.addr = writer.get_extra_info("peername")
        self.connected = True
        self.owed = 0  # replies still expected for turns that timed out
        self.inbox = asyncio.Queue()
        self._task = asyncio.ensure_future(self._pump())

    async def _pump(self):
        """Read lines in the background so late or unsolicited lines never leak into the next turn."""
        try:
            while True:
                line = await self.reader.readline()
                if not line:
                    break
                await self.inbox.put(line.decode("utf-8", "replace").strip())
        except (ConnectionError, OSError, asyncio.IncompleteReadError):
            pass
        self.connected = False
        await self.inbox.put(None)

    async def send(self, msg):
        if not self.connected:
            return
        try:
            self.writer.write((msg + "\n").encode())
            await self.writer.drain()
        except (ConnectionError, OSError):
            self.connected = False

    def discard_pending(self):
        while not self.inbox.empty():
            if self.inbox.get_nowait() is None:
                self.connected = False
            self.owed = max(0, self.owed - 1)

    async def settle(self, timeout):
        """Before a new turn, swallow the late answer to a turn that timed out, so it
        can't be mistaken for the answer to this one."""
        self.discard_pending()
        while self.owed and self.connected:
            try:
                if await self.recv(timeout) is None:
                    self.connected = False
            except asyncio.TimeoutError:
                pass
            self.owed -= 1

    async def recv(self, timeout):
        """Next line, None if disconnected; raises asyncio.TimeoutError."""
        return await asyncio.wait_for(self.inbox.get(), timeout)

    async def close(self):
        try:
            self.writer.close()
        except Exception:
            pass


class Lobby:
    def __init__(self, needed, allow_humans=True):
        self.needed = needed
        self.allow_humans = allow_humans
        self.players = []
        self.full = asyncio.Event()
        self.started = False

    def unique(self, name):
        taken = {p.name for p in self.players}
        base, n = name, 2
        while name in taken:
            name = f"{base}_{n}"; n += 1
        return name


# -------------------------------------------------------------- live viewer
STATE = {
    "status": "Waiting for players", "players": [], "game": None, "standings": [],
    "results": [], "gameNo": 0, "totalGames": 0, "settings": {},
}


async def http_handler(reader, writer):
    try:
        request = await asyncio.wait_for(reader.readline(), 5)
        parts = request.decode("latin-1").split()
        path = parts[1] if len(parts) > 1 else "/"
        while True:
            h = await asyncio.wait_for(reader.readline(), 5)
            if not h or h in (b"\r\n", b"\n"):
                break
        if path.startswith("/state"):
            body, ctype, code = json.dumps(STATE).encode(), "application/json", "200 OK"
        elif path in ("/", "/index.html"):
            with open(os.path.join(HERE, "viewer.html"), "rb") as f:
                body = f.read()
            ctype, code = "text/html; charset=utf-8", "200 OK"
        else:
            body, ctype, code = b"not found", "text/plain", "404 Not Found"
        writer.write(f"HTTP/1.1 {code}\r\nContent-Type: {ctype}\r\nContent-Length: {len(body)}\r\n"
                     "Cache-Control: no-store\r\nAccess-Control-Allow-Origin: *\r\n"  # lets a viewer.html opened from disk poll
                     "Connection: close\r\n\r\n".encode() + body)
        await writer.drain()
    except Exception:
        pass
    finally:
        writer.close()


# ---------------------------------------------------------------------- game
def log(msg):
    print(f"[{datetime.now():%H:%M:%S}] {msg}", flush=True)


async def play_game(white, black, args, game_no, log_dir):
    board = Board(args.rows, args.cols)
    players = {WHITE: white, BLACK: black}
    history, last, winner, reason = [], "none", None, ""

    def limit(p):
        return args.human_time if p.human else args.time

    async def update_watchers(turn):
        # human clients get every position so their window stays current
        for q in players.values():
            if q.human:
                await q.send(f"UPDATE {board.encode()} {last} {COLOR_NAME[turn]}")

    STATE["game"] = {"white": white.name, "black": black.name, "board": board.encode(),
                     "rows": args.rows, "cols": args.cols, "last": None, "captured": False,
                     "turn": WHITE, "moveNo": 0, "winner": None, "reason": "",
                     "humans": [p.name for p in players.values() if p.human]}
    for color, p in players.items():
        p.discard_pending()
        await p.send(f"START {COLOR_NAME[color]} {args.rows} {args.cols} {int(limit(p) * 1000)} "
                     f"{players[other(color)].name}")
    last_shown = time.monotonic()

    turn = WHITE
    while True:
        p, opp = players[turn], other(turn)
        if not p.connected:
            winner, reason = opp, f"{p.name} disconnected"
            break
        legal = board.legal_moves(turn)
        if not legal:
            winner, reason = opp, f"{p.name} has no legal moves"
            break
        await p.settle(limit(p) + args.grace)
        if not p.connected:
            winner, reason = opp, f"{p.name} disconnected"
            break
        await p.send(f"YOURTURN {board.encode()} {last} {int(limit(p) * 1000)}")
        t0 = time.monotonic()
        try:
            line = await p.recv(limit(p) + args.grace)
        except asyncio.TimeoutError:
            p.owed += 1
            winner, reason = opp, f"{p.name} exceeded the {limit(p):g}s time limit"
            break
        elapsed = time.monotonic() - t0
        if line is None:
            winner, reason = opp, f"{p.name} disconnected"
            break
        fields = line.split()
        mv = board.parse_move(fields[1]) if len(fields) == 2 and fields[0].upper() == "MOVE" else None
        if mv is None or mv not in legal:
            winner, reason = opp, f"{p.name} sent an illegal move: {line[:40]!r}"
            await p.send(f"ERROR illegal move {line[:40]}")
            break
        # Minimum move time: moves appear no faster than one per --min-move-time seconds so an
        # audience can follow. Thinking time overlaps with this wait, so it costs players nothing.
        wait = args.min_move_time - (time.monotonic() - last_shown)
        if wait > 0:
            await asyncio.sleep(wait)
        captured = board.apply(mv)
        last_shown = time.monotonic()
        last = board.move_str(mv)
        history.append((COLOR_NAME[turn], last, round(elapsed, 3)))
        STATE["game"].update(board=board.encode(), last=last, captured=captured, turn=opp,
                             moveNo=len(history))
        await update_watchers(opp)
        if args.verbose:
            log(f"  {len(history):>3}. {p.name} ({COLOR_NAME[turn]}) {last}  [{elapsed:.2f}s]\n{board.pretty()}")
        w = board.winner()
        if w:
            winner, reason = w, f"{players[w].name} " + (
                "broke through" if board.count(other(w)) else "captured every enemy piece")
            break
        turn = opp

    STATE["game"].update(winner=COLOR_NAME[winner], reason=reason)
    for color, p in players.items():
        await p.send(f"END {'WIN' if color == winner else 'LOSS'} {reason}")

    if log_dir:
        fname = os.path.join(log_dir, f"game_{game_no:03d}_{white.name}_vs_{black.name}.txt")
        with open(fname, "w") as f:
            f.write(f"White: {white.name}\nBlack: {black.name}\nBoard: {args.rows}x{args.cols}  "
                    f"Time/move: {args.time:g}s\n\n")
            for i, (col, mv, secs) in enumerate(history, 1):
                f.write(f"{i:>3}. {col:<5} {mv:<6} {secs:.3f}s\n")
            f.write(f"\nWinner: {players[winner].name} ({COLOR_NAME[winner]}) - {reason}\n\n{board.pretty()}\n")
    return players[winner], players[other(winner)], reason, len(history)


async def run_tournament(args, lobby, log_dir):
    await lobby.full.wait()
    lobby.started = True
    players = lobby.players[:args.players]
    pairings = [(a, b) for i, a in enumerate(players) for b in players[i + 1:]]
    total = len(pairings) * args.games
    table = {p.name: {"name": p.name, "wins": 0, "losses": 0, "played": 0} for p in players}
    STATE.update(totalGames=total, status="Tournament running")
    log(f"Tournament: {len(players)} players, {len(pairings)} pairings x {args.games} games = {total} games")

    game_no = 0
    for a, b in pairings:
        for g in range(args.games):
            white, black = (a, b) if g % 2 == 0 else (b, a)
            game_no += 1
            STATE["gameNo"] = game_no
            log(f"Game {game_no}/{total}: {white.name} (WHITE) vs {black.name} (BLACK)")
            win, lose, reason, n = await play_game(white, black, args, game_no, log_dir)
            table[win.name]["wins"] += 1
            table[lose.name]["losses"] += 1
            for p in (win, lose):
                table[p.name]["played"] += 1
            log(f"  -> {win.name} wins in {n} moves ({reason})")
            STATE["results"].append({"no": game_no, "white": white.name, "black": black.name,
                                     "winner": win.name, "moves": n, "reason": reason})
            STATE["standings"] = sorted(table.values(), key=lambda s: (-s["wins"], s["losses"], s["name"]))
            await asyncio.sleep(args.pause)

    STATE["status"] = "Tournament finished"
    for p in players:
        await p.send("BYE")
        await p.close()

    print("\nFinal standings")
    print(f"{'#':>2}  {'Player':<20} {'W':>3} {'L':>3} {'Played':>6}")
    for i, s in enumerate(STATE["standings"], 1):
        print(f"{i:>2}  {s['name']:<20} {s['wins']:>3} {s['losses']:>3} {s['played']:>6}")
    if log_dir:
        with open(os.path.join(log_dir, "results.json"), "w") as f:
            json.dump({"settings": STATE["settings"], "standings": STATE["standings"],
                       "games": STATE["results"]}, f, indent=2)
        print(f"\nGame logs and results.json saved in {log_dir}")


def make_client_handler(lobby):
    async def handle(reader, writer):
        addr = writer.get_extra_info("peername")
        try:
            first = await asyncio.wait_for(reader.readline(), 30)
        except (asyncio.TimeoutError, ConnectionError, OSError):
            writer.close(); return
        fields = first.decode("utf-8", "replace").split()
        if len(fields) < 2 or fields[0].upper() != "NAME":
            writer.write(b"ERROR expected: NAME <yourname>\n"); writer.close(); return
        if lobby.started or len(lobby.players) >= lobby.needed:
            writer.write(b"ERROR tournament is full or already started\n"); writer.close(); return
        name = re.sub(r"[^A-Za-z0-9_.-]", "", fields[1])[:20] or "player"
        name = lobby.unique(name)
        wants_human = len(fields) > 2 and fields[2].upper() == "HUMAN"
        p = Player(name, reader, writer, human=wants_human and lobby.allow_humans)
        lobby.players.append(p)
        STATE["players"] = [x.name for x in lobby.players]
        STATE["status"] = f"Waiting for players ({len(lobby.players)}/{lobby.needed})"
        await p.send(f"WELCOME {name}")
        log(f"{name}{' (human)' if p.human else ''} connected from {addr[0]}  "
            f"({len(lobby.players)}/{lobby.needed})")
        if len(lobby.players) >= lobby.needed:
            lobby.full.set()
    return handle


def lan_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("10.255.255.255", 1))  # no packets are sent; this just picks the outbound interface
        return s.getsockname()[0]
    except OSError:
        return "127.0.0.1"
    finally:
        s.close()


async def main():
    ap = argparse.ArgumentParser(description="Breakthrough tournament server")
    ap.add_argument("--host", default="0.0.0.0", help="interface to listen on (default: all)")
    ap.add_argument("--port", type=int, default=5555, help="game port (default 5555)")
    ap.add_argument("--http-port", type=int, default=8080, help="live viewer port, 0 to disable (default 8080)")
    ap.add_argument("--players", type=int, default=2, help="number of clients to wait for (default 2)")
    ap.add_argument("--games", type=int, default=2, help="games per pairing, colors alternate (default 2)")
    ap.add_argument("--time", type=float, default=5.0, help="seconds per move (default 5)")
    ap.add_argument("--grace", type=float, default=0.5, help="extra seconds allowed for network delay (default 0.5)")
    ap.add_argument("--rows", type=int, default=8)
    ap.add_argument("--cols", type=int, default=8)
    ap.add_argument("--pause", type=float, default=2.0, help="seconds between games (default 2)")
    ap.add_argument("--min-move-time", type=float, default=3.0,
                    help="show at most one move every this many seconds so an audience can follow; "
                         "thinking time overlaps with it (default 3, use 0 for fast testing)")
    ap.add_argument("--human-time", type=float, default=120.0,
                    help="seconds per move for human players (default 120)")
    ap.add_argument("--no-humans", action="store_true",
                    help="treat every client as a program (no extra time), e.g. for the official competition")
    ap.add_argument("--log-dir", default="results", help="where game logs go; '' to disable")
    ap.add_argument("--verbose", action="store_true", help="print the board after every move")
    ap.add_argument("--exit-when-done", action="store_true", help="quit right after the tournament")
    args = ap.parse_args()
    if args.players < 2 or not (3 <= args.rows <= 26) or not (2 <= args.cols <= 26):
        ap.error("need at least 2 players and a board between 3x2 and 26x26")

    log_dir = ""
    if args.log_dir:
        log_dir = os.path.join(args.log_dir, datetime.now().strftime("%Y-%m-%d_%H%M%S"))
        os.makedirs(log_dir, exist_ok=True)
    STATE["settings"] = {"rows": args.rows, "cols": args.cols, "time": args.time,
                         "games": args.games, "players": args.players}
    STATE["status"] = f"Waiting for players (0/{args.players})"

    lobby = Lobby(args.players, allow_humans=not args.no_humans)
    server = await asyncio.start_server(make_client_handler(lobby), args.host, args.port)
    ip = lan_ip()
    log(f"Breakthrough server listening on {ip}:{args.port}  (clients use --host {ip} --port {args.port})")
    if args.http_port:
        await asyncio.start_server(http_handler, args.host, args.http_port)
        log(f"Live viewer: http://{ip}:{args.http_port}/   (or http://localhost:{args.http_port}/ on this machine)")
    log(f"Waiting for {args.players} players; {args.games} games per pairing; {args.time:g}s per move "
        f"({args.human_time:g}s for humans); moves shown at most every {args.min_move_time:g}s; "
        f"{args.rows}x{args.cols} board")
    async with server:
        tournament = asyncio.ensure_future(run_tournament(args, lobby, log_dir))
        while not tournament.done():
            await asyncio.wait([tournament], timeout=1)  # wake up regularly so Ctrl+C works on Windows
        tournament.result()
    if args.http_port and not args.exit_when_done:
        log("Viewer is still up so the final standings can be shown. Press Ctrl+C to quit.")
        while True:
            await asyncio.sleep(1)  # short sleeps so Ctrl+C is handled promptly on Windows


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nServer stopped.")
