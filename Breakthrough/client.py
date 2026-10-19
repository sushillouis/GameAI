#!/usr/bin/env python3
"""
Breakthrough client - starter code.

Connects to the tournament server, receives the board on every turn, and replies
with a move. Everything except choose_move() is done for you. Replace the body of
choose_move() with your minimax search with alpha-beta pruning.

    python client.py --host 192.168.1.20 --port 5555 --name YourTeam

Rules (8x8 by default): each side starts with two full rows of pieces. White
(rows 1-2) moves first and moves up the board; Black (rows 7-8) moves down.
A piece moves one square straight forward into an EMPTY square, or one square
diagonally forward into an empty square or onto an enemy piece (capturing it).
You win by reaching the opponent's home row or by capturing all their pieces.
An illegal move, a timeout, or disconnecting loses the game.
"""
import argparse
import random
import socket
import sys
import time

WHITE, BLACK, EMPTY = "W", "B", "."


# ------------------------------------------------------------------ board helpers
def parse_board(text, rows, cols):
    """Server string (top rank first) -> grid[r][c], with r = 0 as rank 1 (White's home row)."""
    grid = [list(text[i * cols:(i + 1) * cols]) for i in range(rows)]
    grid.reverse()
    return grid


def opponent(color):
    return BLACK if color == WHITE else WHITE


def legal_moves(grid, color):
    """All legal moves for color as (from_row, from_col, to_row, to_col) tuples."""
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
                    target = grid[nr][nc]
                    if (dc == 0 and target == EMPTY) or (dc != 0 and target != color):
                        moves.append((r, c, nr, nc))
    return moves


def apply_move(grid, move):
    """Return a NEW grid with move played (the original is unchanged)."""
    r1, c1, r2, c2 = move
    new = [row[:] for row in grid]
    new[r2][c2] = new[r1][c1]
    new[r1][c1] = EMPTY
    return new


def winner(grid):
    """WHITE, BLACK, or None if the game is not over."""
    if WHITE in grid[-1] or not any(BLACK in row for row in grid):
        return WHITE
    if BLACK in grid[0] or not any(WHITE in row for row in grid):
        return BLACK
    return None


def move_to_str(move):
    r1, c1, r2, c2 = move
    return f"{chr(97 + c1)}{r1 + 1}{chr(97 + c2)}{r2 + 1}"


def print_board(grid):
    for r in range(len(grid) - 1, -1, -1):
        print(f"{r + 1:>2} " + " ".join(grid[r]))
    print("   " + " ".join(chr(97 + c) for c in range(len(grid[0]))))


# ------------------------------------------------------------------ your AI goes here
def choose_move(grid, color, time_limit):
    """
    Return one move from legal_moves(grid, color).

    grid        board as grid[row][col]; row 0 is White's home row
    color       WHITE ("W") or BLACK ("B") - the side you are playing
    time_limit  seconds you have for this move; answer well before it runs out,
                because a late move loses the game

    TODO: replace this with minimax + alpha-beta pruning and your own
    evaluation function. Iterative deepening is a good way to use the time.
    """
    moves = legal_moves(grid, color)
    for mv in moves:                       # take an immediate win if there is one
        if winner(apply_move(grid, mv)) == color:
            return mv
    return random.choice(moves)


# ------------------------------------------------------------------ networking
def main():
    ap = argparse.ArgumentParser(description="Breakthrough client")
    ap.add_argument("--host", default="127.0.0.1", help="server IP address")
    ap.add_argument("--port", type=int, default=5555)
    ap.add_argument("--name", default="player", help="your team name (letters, digits, _ . -)")
    ap.add_argument("--quiet", action="store_true", help="don't print the board every turn")
    args = ap.parse_args()

    sock = socket.create_connection((args.host, args.port))
    sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
    inp = sock.makefile("r", encoding="utf-8", newline="\n")

    def send(line):
        sock.sendall((line + "\n").encode())

    send(f"NAME {args.name}")
    color, rows, cols = None, 8, 8
    for line in inp:
        fields = line.split()
        if not fields:
            continue
        cmd = fields[0]
        if cmd == "WELCOME":
            print(f"Connected as {fields[1]}. Waiting for the tournament to start...")
        elif cmd == "START":
            color = WHITE if fields[1] == "WHITE" else BLACK
            rows, cols = int(fields[2]), int(fields[3])
            print(f"\n=== New game: I am {fields[1]} vs {fields[5]}, {int(fields[4]) / 1000:g}s per move ===")
        elif cmd == "YOURTURN":
            grid = parse_board(fields[1], rows, cols)
            time_limit = int(fields[3]) / 1000
            t0 = time.time()
            move = choose_move(grid, color, time_limit)
            send(f"MOVE {move_to_str(move)}")
            if not args.quiet:
                print(f"\nOpponent played {fields[2]}; I play {move_to_str(move)} ({time.time() - t0:.2f}s)")
                print_board(apply_move(grid, move))
        elif cmd == "END":
            print(f"=== Game over: {fields[1]} - {' '.join(fields[2:])} ===")
        elif cmd == "ERROR":
            print("Server error:", " ".join(fields[1:]))
        elif cmd == "BYE":
            print("Tournament finished.")
            break
    sock.close()


if __name__ == "__main__":
    try:
        main()
    except ConnectionRefusedError:
        sys.exit("Could not connect: is the server running, and are --host/--port right?")
    except KeyboardInterrupt:
        pass
