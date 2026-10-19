# Breakthrough Tournament Server

A game server for a classroom Breakthrough competition (minimax / alpha-beta assignment).
Students' programs connect over the local network; the server runs a round-robin tournament,
enforces the rules and time limit, logs every game, and shows a live, animated board you can project.
People can join too, with a graphical client.

Python 3.8+, standard library only. Nothing to install.

| File | For | Purpose |
|---|---|---|
| `server.py` | instructor | tournament server and live viewer |
| `viewer.html` | instructor | live board page (served by `server.py`, keep it next to it) |
| `human_client.py` | anyone | play against the AIs yourself, in a window with a clickable board |
| `client.py` | students | starter client: networking, board parsing and move generation done; plays random moves |
| `STUDENT_GUIDE.md` | students | rules, how to use the starter client, and the protocol for other languages |

Give students `client.py`, `STUDENT_GUIDE.md`, and also `server.py` + `viewer.html` so they can test at home.

## Running a competition

1. On the instructor machine:

   ```
   python server.py --players 12 --games 2 --time 5
   ```

   The server prints the address students should use, e.g. `--host 192.168.1.20 --port 5555`,
   and the viewer URL, e.g. `http://192.168.1.20:8080/`. Open the viewer on the projector and click
   **Sound: off** in its top-right corner to turn the sound effects on.

2. Each student runs their client:

   ```
   python client.py --host 192.168.1.20 --port 5555 --name TeamName
   ```

3. When the requested number of players has connected, the tournament starts automatically.
   Every pair plays `--games` games, alternating who is White. Results are printed at the end, and
   saved in `results/<date_time>/` (one log per game plus `results.json`). The viewer stays up
   showing the final standings until you press Ctrl+C.

## The live viewer

Moves slide across the board with a wooden "tock"; captures shatter the taken piece with a burst and
a heavier sound; the winner gets a banner and a short fanfare. The player to move is outlined in gold.
Moves are shown at most once every `--min-move-time` seconds (3 by default) so the audience can follow.
Players' thinking time overlaps with that wait, so a fast program is not slowed down by more than the
display, and a slow one loses nothing.

## Playing as a human

```
python human_client.py
```

A window asks for the server address and your name (or pass `--host`, `--port`, `--name`).
Count yourself in `--players` when starting the server. Click one of your pieces to see where it can
go (green dots, red rings for captures), then click the destination. Your pieces are always at the
bottom of the board. Humans get `--human-time` per move (2 minutes by default) and a countdown clock;
sounds play on Windows. Human players are marked "(human)" in the server log and on the viewer.
Because any client can ask for human time, start the official student competition with `--no-humans`.

### Options

| Option | Default | Meaning |
|---|---|---|
| `--players N` | 2 | how many clients (programs and humans) to wait for before starting |
| `--games N` | 2 | games per pairing (colors alternate) |
| `--time S` | 5 | seconds per move for programs |
| `--human-time S` | 120 | seconds per move for human players |
| `--no-humans` | off | treat every client as a program, so nobody can claim human time; use it for the official competition |
| `--min-move-time S` | 3 | show at most one move every S seconds so an audience can follow; use 0 for fast testing |
| `--grace S` | 0.5 | extra seconds allowed for network delay before a move counts as late |
| `--pause S` | 2 | pause between games |
| `--rows R --cols C` | 8 8 | board size (smaller boards are handy for testing) |
| `--port P` | 5555 | game port |
| `--http-port P` | 8080 | live viewer port (0 turns it off) |
| `--log-dir DIR` | results | where logs go (`""` turns logging off) |
| `--verbose` | off | print the board after every move |
| `--exit-when-done` | off | quit right after the tournament instead of keeping the viewer up |

Round-robin length grows quickly: 12 players × 2 games = 132 games, and with the 3-second minimum
move time a 60-move game takes at least 3 minutes. For a class session, consider fewer games per
pairing, a group stage, or a lower `--min-move-time` for early rounds and the default 3 s for the final.

### Rules enforced

- Standard Breakthrough: one step forward, straight (into an empty square) or diagonally (empty or capture).
  White moves first. Win by reaching the far row or capturing every enemy piece; no legal moves loses.
- An illegal or malformed move, exceeding the time limit (+ grace), or disconnecting loses that game.
  A disconnected player forfeits all remaining games.
- Late replies are handled safely: if a client times out and answers late, that late move is
  discarded and never counts in the next game.

## Network notes

- All machines must be on the same network (and the campus Wi-Fi may block traffic between
  devices; a lab network or a personal router/hotspot is more reliable).
- The first time `server.py` runs, Windows Firewall asks whether to allow Python on the network.
  Allow it on **private** networks, or ports 5555 and 8080 will be blocked.
- If students cannot connect, test from the instructor machine first with
  `python client.py --host 127.0.0.1`, then from a second laptop.
