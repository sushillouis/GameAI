#!/usr/bin/env python3
"""
Breakthrough client for a human player, with a graphical board.

    python human_client.py                       (asks for server address and name)
    python human_client.py --host 192.168.1.20 --name Sushil

Click one of your pieces to see where it can go, then click a highlighted square.
The board is turned so your pieces are always at the bottom. Human players get the
server's --human-time per move (2 minutes by default) instead of the program time limit.

Python 3.8+ standard library only (tkinter). Sounds play on Windows.
"""
import argparse
import math
import os
import queue
import re
import socket
import struct
import sys
import tempfile
import threading
import time
import tkinter as tk
import wave

WHITE, BLACK, EMPTY = "W", "B", "."

# colours
BG, PANEL, TEXT, MUTED = "#14171c", "#1d2128", "#e8eaed", "#9aa0a6"
LIGHT, DARK, FRAME = "#ecd6ae", "#b5814f", "#4a2f1b"
SEL, MOVE_DOT, CAPTURE, LASTMOVE = "#f2c94c", "#2f7d4f", "#d9452b", "#e9c46a"
PIECE_COLORS = {  # side, top, ring, highlight, outline
    WHITE: ("#b9ae98", "#f4efe4", "#d6ccb8", "#ffffff", "#6f6553"),
    BLACK: ("#0a0a0c", "#2d3139", "#474d58", "#69717e", "#000000"),
}


# ---------------------------------------------------------------- rules
def opponent(color):
    return BLACK if color == WHITE else WHITE


def parse_board(text, rows, cols):
    grid = [list(text[i * cols:(i + 1) * cols]) for i in range(rows)]
    grid.reverse()  # grid[0] is rank 1 (White's home row)
    return grid


def start_grid(rows, cols):
    g = [[EMPTY] * cols for _ in range(rows)]
    for r in range(2):
        g[r] = [WHITE] * cols
        g[rows - 1 - r] = [BLACK] * cols
    return g


def legal_moves(grid, color):
    rows, cols = len(grid), len(grid[0])
    d = 1 if color == WHITE else -1
    moves = []
    for r in range(rows):
        nr = r + d
        if not 0 <= nr < rows:
            continue
        for c in range(cols):
            if grid[r][c] != color:
                continue
            for dc in (-1, 0, 1):
                nc = c + dc
                if 0 <= nc < cols:
                    t = grid[nr][nc]
                    if (dc == 0 and t == EMPTY) or (dc != 0 and t != color):
                        moves.append((r, c, nr, nc))
    return moves


def move_str(m):
    return f"{chr(97 + m[1])}{m[0] + 1}{chr(97 + m[3])}{m[2] + 1}"


def parse_move(s):
    """'b2c3' -> (1, 1, 2, 2), or None if s is not a move."""
    m = re.match(r"^([a-z])(\d+)([a-z])(\d+)$", s or "")
    if not m:
        return None
    return (int(m.group(2)) - 1, ord(m.group(1)) - 97, int(m.group(4)) - 1, ord(m.group(3)) - 97)


# ---------------------------------------------------------------- sound
class Sounds:
    """Short synthesized sounds, written once to temporary .wav files (played with winsound on Windows)."""

    def __init__(self):
        self.files = {}
        try:
            import winsound
            self.winsound = winsound
        except ImportError:
            self.winsound = None
            return
        rate = 22050

        def write(name, samples):
            path = os.path.join(tempfile.gettempdir(), f"breakthrough_{name}.wav")
            with wave.open(path, "wb") as w:
                w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate)
                w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32000)) for s in samples))
            self.files[name] = path

        import random
        rnd = random.Random(7)

        def tock(f0, f1, dur, noise_amt, gain):
            n = int(rate * dur)
            out, phase = [], 0.0
            for i in range(n):
                t = i / n
                f = f0 * (f1 / f0) ** t
                phase += 2 * math.pi * f / rate
                env = (1 - t) ** 3
                out.append(gain * env * (math.sin(phase) + noise_amt * (rnd.random() * 2 - 1) * (1 - t) ** 6))
            return out

        def notes(freqs, each):
            out = []
            for k, f in enumerate(freqs):
                n = int(rate * (each * (2.2 if k == len(freqs) - 1 else 1)))
                for i in range(n):
                    t = i / n
                    out.append(0.35 * min(1, i / 200) * (1 - t) ** 1.5 *
                               (math.sin(2 * math.pi * f * i / rate) + 0.3 * math.sin(4 * math.pi * f * i / rate)))
            return out

        write("move", tock(420, 180, 0.13, 0.6, 0.7))
        write("capture", tock(230, 60, 0.35, 1.2, 0.9))
        write("turn", notes([880, 1175], 0.09))
        write("win", notes([523, 659, 784, 1047], 0.13))
        write("lose", notes([392, 330, 262], 0.18))

    def play(self, name):
        if self.winsound and name in self.files:
            try:
                self.winsound.PlaySound(self.files[name], self.winsound.SND_FILENAME | self.winsound.SND_ASYNC)
            except RuntimeError:
                pass


# ---------------------------------------------------------------- network
class Connection:
    def __init__(self, host, port, name, inbox):
        self.sock = socket.create_connection((host, port), timeout=10)
        self.sock.settimeout(None)
        self.sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        self.inbox = inbox
        self.send(f"NAME {name} HUMAN")
        threading.Thread(target=self._reader, daemon=True).start()

    def _reader(self):
        try:
            for line in self.sock.makefile("r", encoding="utf-8", newline="\n"):
                self.inbox.put(line.strip())
        except OSError:
            pass
        self.inbox.put(None)

    def send(self, line):
        try:
            self.sock.sendall((line + "\n").encode())
        except OSError:
            self.inbox.put(None)


# ---------------------------------------------------------------- GUI
class App:
    SIZE = 640      # board area in pixels
    BORDER = 26     # wooden frame width (holds the coordinates)

    def __init__(self, root, args):
        self.root, self.args = root, args
        self.sounds = Sounds()
        self.inbox = queue.Queue()
        self.conn = None
        self.rows, self.cols = 8, 8
        self.grid = start_grid(8, 8)
        self.color = WHITE
        self.my_turn = False
        self.deadline = None
        self.selected = None
        self.last = None
        self.anim = None
        self.opp_name = ""
        self.games = {"W": 0, "L": 0}
        self.finished = False

        root.title("Breakthrough")
        root.configure(bg=BG)
        root.resizable(False, False)
        self.show_connect()

    # ------------------------------------------------------------ connect screen
    def show_connect(self):
        f = self.connect_frame = tk.Frame(self.root, bg=BG, padx=40, pady=30)
        f.pack()
        tk.Label(f, text="Breakthrough", font=("Segoe UI", 26, "bold"), fg=TEXT, bg=BG).grid(row=0, column=0, columnspan=2, pady=(0, 4))
        tk.Label(f, text="Play against the AIs", font=("Segoe UI", 12), fg=MUTED, bg=BG).grid(row=1, column=0, columnspan=2, pady=(0, 20))
        self.entries = {}
        for i, (label, key, val) in enumerate((("Server address", "host", self.args.host),
                                               ("Port", "port", str(self.args.port)),
                                               ("Your name", "name", self.args.name))):
            tk.Label(f, text=label, font=("Segoe UI", 11), fg=TEXT, bg=BG, anchor="w").grid(row=2 + i, column=0, sticky="w", pady=6, padx=(0, 12))
            e = tk.Entry(f, font=("Segoe UI", 12), width=22, bg="#262b34", fg=TEXT, insertbackground=TEXT,
                         relief="flat", highlightthickness=1, highlightbackground="#3a4352", highlightcolor=SEL)
            e.insert(0, val); e.grid(row=2 + i, column=1, pady=6, ipady=4)
            self.entries[key] = e
        self.connect_msg = tk.Label(f, text="", font=("Segoe UI", 10), fg=CAPTURE, bg=BG, wraplength=320)
        self.connect_msg.grid(row=5, column=0, columnspan=2, pady=(8, 0))
        b = tk.Button(f, text="Connect", font=("Segoe UI", 12, "bold"), bg="#2f6b4c", fg="white", activebackground="#3b8560",
                      activeforeground="white", relief="flat", padx=24, pady=6, command=self.connect, cursor="hand2")
        b.grid(row=6, column=0, columnspan=2, pady=(14, 0))
        self.root.bind("<Return>", lambda e: self.connect())
        self.entries["name"].focus_set()

    def connect(self):
        host = self.entries["host"].get().strip()
        name = self.entries["name"].get().strip() or "human"
        try:
            port = int(self.entries["port"].get())
            self.conn = Connection(host, port, name, self.inbox)
        except (OSError, ValueError) as e:
            self.connect_msg.config(text=f"Could not connect to {host}: {e}")
            return
        self.root.unbind("<Return>")
        self.connect_frame.destroy()
        self.build_game_screen()
        self.set_status("Connected. Waiting for the tournament to start...")
        self.root.after(30, self.pump)
        self.root.after(200, self.tick)

    # ------------------------------------------------------------ game screen
    def build_game_screen(self):
        top = tk.Frame(self.root, bg=BG, padx=16, pady=10)
        top.pack(fill="x")
        self.opp_label = tk.Label(top, text="", font=("Segoe UI", 15, "bold"), fg=TEXT, bg=BG, anchor="w")
        self.opp_label.pack(side="left")
        self.score_label = tk.Label(top, text="", font=("Segoe UI", 11), fg=MUTED, bg=BG)
        self.score_label.pack(side="right")
        total = self.SIZE + 2 * self.BORDER
        self.canvas = tk.Canvas(self.root, width=total, height=total, bg=BG, highlightthickness=0)
        self.canvas.pack(padx=16)
        self.canvas.bind("<Button-1>", self.on_click)
        bottom = tk.Frame(self.root, bg=BG, padx=16, pady=10)
        bottom.pack(fill="x")
        self.me_label = tk.Label(bottom, text="", font=("Segoe UI", 15, "bold"), fg=TEXT, bg=BG, anchor="w")
        self.me_label.pack(side="left")
        self.clock = tk.Label(bottom, text="", font=("Consolas", 18, "bold"), fg=TEXT, bg=BG)
        self.clock.pack(side="right")
        self.status = tk.Label(self.root, text="", font=("Segoe UI", 12), fg=TEXT, bg=PANEL, pady=10)
        self.status.pack(fill="x")
        self.draw()

    def set_status(self, text, color=TEXT):
        self.status.config(text=text, fg=color)

    # coordinates ---------------------------------------------------------
    @property
    def cell(self):
        return self.SIZE / max(self.rows, self.cols)

    def to_screen(self, r, c):
        """Board square -> top-left pixel. Your own pieces are always at the bottom."""
        if self.color == WHITE:
            sr, sc = self.rows - 1 - r, c
        else:
            sr, sc = r, self.cols - 1 - c
        return self.BORDER + sc * self.cell, self.BORDER + sr * self.cell

    def from_screen(self, x, y):
        sc, sr = int((x - self.BORDER) // self.cell), int((y - self.BORDER) // self.cell)
        if not (0 <= sr < self.rows and 0 <= sc < self.cols):
            return None
        return (self.rows - 1 - sr, sc) if self.color == WHITE else (sr, self.cols - 1 - sc)

    # drawing ------------------------------------------------------------
    def draw_piece(self, x, y, color, tag="piece", lift=0.0):
        side, topc, ring, hi, outline = PIECE_COLORS[color]
        s = self.cell
        cx, cy = x + s / 2, y + s / 2 - lift
        rx, ry, th = s * 0.36, s * 0.30, s * 0.09
        cv = self.canvas
        cv.create_oval(cx - rx * 0.95, cy - ry + th + lift * 0.7 + 4, cx + rx * 0.95, cy + ry + th + lift * 0.7 + 6,
                       fill="#3b2614", outline="", tags=tag)                                     # shadow
        cv.create_oval(cx - rx, cy - ry + th, cx + rx, cy + ry + th, fill=side, outline=outline, width=1.5, tags=tag)
        cv.create_rectangle(cx - rx + 0.8, cy, cx + rx - 0.8, cy + th, fill=side, outline="", tags=tag)
        cv.create_line(cx - rx, cy, cx - rx, cy + th, fill=outline, width=1.5, tags=tag)
        cv.create_line(cx + rx, cy, cx + rx, cy + th, fill=outline, width=1.5, tags=tag)
        cv.create_oval(cx - rx, cy - ry, cx + rx, cy + ry, fill=topc, outline=outline, width=1.5, tags=tag)
        cv.create_oval(cx - rx * 0.66, cy - ry * 0.66, cx + rx * 0.66, cy + ry * 0.66, outline=ring, width=2, tags=tag)
        cv.create_oval(cx - rx * 0.55, cy - ry * 0.62, cx - rx * 0.15, cy - ry * 0.32, fill=hi, outline="", tags=tag)

    def draw(self, skip=None):
        cv = self.canvas
        cv.delete("all")
        total = self.SIZE + 2 * self.BORDER
        cv.create_rectangle(0, 0, total, total, fill=FRAME, outline="")
        cv.create_rectangle(3, 3, total - 3, total - 3, outline="#6b4428", width=2)
        s = self.cell
        last = self.last
        targets = {}
        if self.selected and self.my_turn:
            for m in legal_moves(self.grid, self.color):
                if (m[0], m[1]) == self.selected:
                    targets[(m[2], m[3])] = self.grid[m[2]][m[3]] != EMPTY
        for r in range(self.rows):
            for c in range(self.cols):
                x, y = self.to_screen(r, c)
                cv.create_rectangle(x, y, x + s, y + s, fill=LIGHT if (r + c) % 2 else DARK, outline="")
                if last and (r, c) in ((last[0], last[1]), (last[2], last[3])):
                    cv.create_rectangle(x + 2, y + 2, x + s - 2, y + s - 2, outline=LASTMOVE, width=3)
                if self.selected == (r, c):
                    cv.create_rectangle(x + 2, y + 2, x + s - 2, y + s - 2, outline=SEL, width=4)
        # coordinates on the frame
        for c in range(self.cols):
            x, _ = self.to_screen(0, c)
            cv.create_text(x + s / 2, total - self.BORDER / 2, text=chr(97 + c), fill="#e3c9a0", font=("Segoe UI", 10, "bold"))
        for r in range(self.rows):
            _, y = self.to_screen(r, 0)
            cv.create_text(self.BORDER / 2, y + s / 2, text=str(r + 1), fill="#e3c9a0", font=("Segoe UI", 10, "bold"))
        # pieces, drawn top row first so lower pieces overlap correctly
        order = sorted(((self.to_screen(r, c), r, c) for r in range(self.rows) for c in range(self.cols)),
                       key=lambda t: t[0][1])
        for (x, y), r, c in order:
            if self.grid[r][c] != EMPTY and (r, c) != skip:
                self.draw_piece(x, y, self.grid[r][c], lift=6 if self.selected == (r, c) else 0)
        for (r, c), cap in targets.items():
            x, y = self.to_screen(r, c)
            if cap:
                cv.create_oval(x + 4, y + 4, x + s - 4, y + s - 4, outline=CAPTURE, width=4)
            else:
                cv.create_oval(x + s * 0.38, y + s * 0.38, x + s * 0.62, y + s * 0.62, fill=MOVE_DOT, outline="#1c4d30")

    def animate(self, prev_grid, move, then=None):
        """Slide the moved piece from its old square to the new one."""
        r1, c1, r2, c2 = move
        captured = prev_grid[r2][c2] != EMPTY
        color = self.grid[r2][c2]
        x1, y1 = self.to_screen(r1, c1)
        x2, y2 = self.to_screen(r2, c2)
        frames, start = 14, time.time()
        if self.anim:
            self.root.after_cancel(self.anim)

        def step(i=0):
            t = i / frames
            e = 1 - (1 - t) ** 3
            self.draw(skip=(r2, c2))
            if captured and t < 0.85:
                self.draw_piece(x2, y2, prev_grid[r2][c2], tag="victim")
            lift = math.sin(math.pi * t) * self.cell * 0.18
            self.draw_piece(x1 + (x2 - x1) * e, y1 + (y2 - y1) * e, color, tag="mover", lift=lift)
            if i < frames:
                self.anim = self.root.after(22, step, i + 1)
            else:
                self.anim = None
                self.draw()
                if captured:
                    self.flash(x2, y2)
                self.sounds.play("capture" if captured else "move")
                if then:
                    then()
        step()

    def flash(self, x, y):
        s, cv = self.cell, self.canvas
        cx, cy = x + s / 2, y + s / 2

        def ring(i=0):
            cv.delete("flash")
            if i > 8:
                return
            rad = s * (0.3 + i * 0.09)
            cv.create_oval(cx - rad, cy - rad, cx + rad, cy + rad, outline=CAPTURE, width=max(1, 5 - i // 2), tags="flash")
            for k in range(10):
                a = k * math.pi / 5 + 0.3
                d = s * (0.25 + i * 0.07)
                cv.create_oval(cx + math.cos(a) * d - 3, cy + math.sin(a) * d - 3, cx + math.cos(a) * d + 3,
                               cy + math.sin(a) * d + 3, fill="#ffd27a", outline="", tags="flash")
            self.root.after(35, ring, i + 1)
        ring()

    def banner(self, text, sub, color):
        cv, total = self.canvas, self.SIZE + 2 * self.BORDER
        cv.create_rectangle(self.BORDER, total / 2 - 60, total - self.BORDER, total / 2 + 60, fill="#101317", outline=color, width=3, tags="banner")
        cv.create_text(total / 2, total / 2 - 14, text=text, fill=color, font=("Segoe UI", 30, "bold"), tags="banner")
        cv.create_text(total / 2, total / 2 + 30, text=sub, fill=TEXT, font=("Segoe UI", 12), tags="banner", width=self.SIZE - 40)

    # ------------------------------------------------------------ input
    def on_click(self, event):
        if not self.my_turn or self.anim:
            return
        sq = self.from_screen(event.x, event.y)
        if sq is None:
            return
        r, c = sq
        if self.selected:
            for m in legal_moves(self.grid, self.color):
                if (m[0], m[1]) == self.selected and (m[2], m[3]) == (r, c):
                    self.play_move(m)
                    return
        if self.grid[r][c] == self.color and any((m[0], m[1]) == (r, c) for m in legal_moves(self.grid, self.color)):
            self.selected = (r, c)
        else:
            self.selected = None
        self.draw()

    def play_move(self, m):
        self.my_turn, self.selected, self.deadline = False, None, None
        prev = [row[:] for row in self.grid]
        self.grid[m[2]][m[3]] = self.grid[m[0]][m[1]]
        self.grid[m[0]][m[1]] = EMPTY
        self.last = m
        self.conn.send(f"MOVE {move_str(m)}")
        self.clock.config(text="")
        self.set_status(f"You played {move_str(m)}. Waiting for {self.opp_name}...", MUTED)
        self.animate(prev, m)

    # ------------------------------------------------------------ server messages
    def pump(self):
        try:
            while True:
                line = self.inbox.get_nowait()
                if line is None:
                    self.my_turn = False
                    if not self.finished:
                        self.set_status("Disconnected from the server.", CAPTURE)
                    return
                self.handle(line)
        except queue.Empty:
            pass
        self.root.after(30, self.pump)

    def apply_board(self, text, last):
        """Show a position from the server, animating it if it is one move after what is drawn."""
        new = parse_board(text, self.rows, self.cols)
        if new == self.grid:
            return
        mv = parse_move(last)
        prev = self.grid
        self.grid = new
        self.last = mv
        self.selected = None
        if mv and prev[mv[0]][mv[1]] != EMPTY and new[mv[2]][mv[3]] == prev[mv[0]][mv[1]]:
            self.animate(prev, mv)
        else:
            self.draw()

    def handle(self, line):
        f = line.split()
        if not f:
            return
        cmd = f[0]
        if cmd == "WELCOME":
            self.me_name = f[1]
            self.root.title(f"Breakthrough - {f[1]}")
        elif cmd == "START":
            self.color = WHITE if f[1] == "WHITE" else BLACK
            self.rows, self.cols = int(f[2]), int(f[3])
            self.opp_name = f[5]
            self.grid, self.last, self.selected, self.my_turn = start_grid(self.rows, self.cols), None, None, False
            other = "Black" if self.color == WHITE else "White"
            self.opp_label.config(text=f"● {self.opp_name}  ({other})")
            self.me_label.config(text=f"● {getattr(self, 'me_name', 'You')}  ({f[1].title()})",
                                 fg="#f4efe4" if self.color == WHITE else "#9aa3b2")
            self.opp_label.config(fg="#9aa3b2" if self.color == WHITE else "#f4efe4")
            self.draw()
            self.set_status(f"New game against {self.opp_name}. You are {f[1].title()}"
                            + (" and move first." if self.color == WHITE else ". White moves first."))
        elif cmd == "UPDATE":
            self.apply_board(f[1], f[2])
        elif cmd == "YOURTURN":
            self.apply_board(f[1], f[2])
            self.my_turn = True
            self.deadline = time.time() + int(f[3]) / 1000
            self.set_status("Your move: click a piece, then a highlighted square.", SEL)
            self.root.after(450, lambda: self.sounds.play("turn"))
        elif cmd == "END":
            self.my_turn, self.deadline, self.selected = False, None, None
            won = f[1] == "WIN"
            self.games["W" if won else "L"] += 1
            self.score_label.config(text=f"Your record: {self.games['W']} W - {self.games['L']} L")
            reason = " ".join(f[2:])
            self.clock.config(text="")

            def show():
                self.draw()
                self.banner("You win!" if won else "You lose", reason, "#5cc98b" if won else CAPTURE)
                self.sounds.play("win" if won else "lose")
            self.root.after(700 if self.anim else 100, show)
            self.set_status(("You won: " if won else "You lost: ") + reason, "#5cc98b" if won else CAPTURE)
        elif cmd == "ERROR":
            self.set_status("Server: " + " ".join(f[1:]), CAPTURE)
        elif cmd == "BYE":
            self.finished = True
            self.set_status(f"Tournament finished. Your record: {self.games['W']} wins, {self.games['L']} losses.")

    def tick(self):
        if self.my_turn and self.deadline:
            left = max(0, self.deadline - time.time())
            self.clock.config(text=f"{int(left // 60)}:{int(left % 60):02d}", fg=CAPTURE if left < 10 else TEXT)
        self.root.after(200, self.tick)


def main():
    ap = argparse.ArgumentParser(description="Breakthrough client for a human player")
    ap.add_argument("--host", default="127.0.0.1", help="server IP address")
    ap.add_argument("--port", type=int, default=5555)
    ap.add_argument("--name", default="Human", help="your name")
    args = ap.parse_args()
    if sys.platform == "win32":
        try:  # draw crisply on high-DPI screens instead of letting Windows stretch a blurry bitmap
            import ctypes
            ctypes.windll.shcore.SetProcessDpiAwareness(1)
        except Exception:
            pass
    root = tk.Tk()
    # size the board to the screen: about 60% of its height, in whole squares
    App.SIZE = max(400, min(900, int(root.winfo_screenheight() * 0.6) // 8 * 8))
    App.BORDER = max(22, App.SIZE // 24)
    App(root, args)
    root.mainloop()


if __name__ == "__main__":
    main()
