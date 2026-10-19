# Breakthrough Competition: Student Guide

Your minimax player will compete against your classmates' players in a Breakthrough tournament.
The instructor runs a game server on the local network; your program connects to it, receives the
board whenever it is your turn, and answers with a move.

## The game

- 8×8 board. Each player starts with two full rows of pieces: **White** on rows 1–2, **Black** on rows 7–8.
- **White moves first.** White moves up the board (toward row 8), Black moves down (toward row 1).
- On your turn, move one of your pieces one square forward:
  - **straight forward**, only into an **empty** square (you cannot capture straight ahead), or
  - **diagonally forward**, into an empty square **or onto an enemy piece, capturing it**.
- You **win** by getting any piece onto the opponent's home row, or by capturing all of their pieces.
  (A player with no legal move also loses, though this almost never happens.)
- You **lose the game immediately** if you send an illegal or badly formatted move, take longer than
  the time limit, or disconnect.

## Using the starter client (Python)

`client.py` already does all the networking, parses the board, and generates legal moves.
It plays random moves. **Your job is to replace `choose_move()` with minimax and alpha-beta pruning.**

```
python client.py --host <server IP> --port 5555 --name YourTeamName
```

Helpers you can use inside `choose_move(grid, color, time_limit)`:

| Function | What it does |
|---|---|
| `legal_moves(grid, color)` | list of moves `(from_row, from_col, to_row, to_col)` |
| `apply_move(grid, move)` | returns a **new** grid with the move played |
| `winner(grid)` | `WHITE`, `BLACK`, or `None` |
| `opponent(color)` | the other color |
| `print_board(grid)` | prints the board for debugging |

`grid[row][col]` holds `"W"`, `"B"` or `"."`; row 0 is White's home row (rank 1), col 0 is file *a*.

**Watch the clock.** `time_limit` is the number of seconds you have for the move. Leave a margin for
network delay. Iterative deepening (search depth 1, 2, 3, … and stop when time is nearly up) is the
safest way to use the time.

You may write your client in another language. It only has to follow the protocol below.

## Testing on your own computer

Run the server and two copies of your client in three terminals:

```
python server.py --players 2 --min-move-time 0
python client.py --name me
python client.py --name me_too
```

`--min-move-time 0` lets games run at full speed (by default the server shows at most one move every
3 seconds, for the audience in class). Open http://localhost:8080/ to watch the games. To test against
the random player, run the starter `client.py` unchanged as the second player.

**Play against your own AI** with `python human_client.py`: it opens a window where you click a piece
and then its destination. It is a good way to find weaknesses in your evaluation function.

## Protocol (for writing your own client)

Plain text over TCP, one message per line, ending in `\n`. Words are separated by single spaces.

**You send**

| Message | When |
|---|---|
| `NAME <name>` | once, right after connecting. Letters, digits, `_ . -`, at most 20 characters. (`human_client.py` sends `NAME <name> HUMAN` to get a longer time limit and `UPDATE` messages; in the class competition this is switched off and everyone gets the program time limit.) |
| `MOVE <from><to>` | only in reply to `YOURTURN`, for example `MOVE b2c3` |

**The server sends**

| Message | Meaning |
|---|---|
| `WELCOME <name>` | you are registered (your name may get a suffix if it was taken) |
| `START <WHITE\|BLACK> <rows> <cols> <time_ms> <opponent>` | a new game begins; this is your color |
| `YOURTURN <board> <lastmove> <time_ms>` | your move. `lastmove` is the opponent's last move, or `none` |
| `END <WIN\|LOSS> <reason…>` | the game is over |
| `ERROR <message>` | something you sent was rejected |
| `BYE` | the tournament is over; the server closes the connection |

**Board string:** `rows × cols` characters (`W`, `B`, `.`), row by row, starting with the **top row
(row 8, Black's home row)** and reading left to right (file *a* to *h*). So character `i` is the square
on row `rows − i // cols`, file `i % cols`. The starting position on 8×8 is:

```
BBBBBBBBBBBBBBBB................................WWWWWWWWWWWWWWWW
```

**Squares** are written as a file letter and a row number: `a1` is White's bottom-left corner and
`h8` is Black's top-right. A move is the two squares joined together: `e2e3` (straight), `d2e3`
(diagonal).

Example session (`>` = server, `<` = you):

```
< NAME Team7
> WELCOME Team7
> START WHITE 8 8 5000 Team3
> YOURTURN BBBBBBBBBBBBBBBB................................WWWWWWWWWWWWWWWW none 5000
< MOVE d2d3
> YOURTURN BBBBBBBBBBBB.BBB....B......................W....WWW.WWWWWWWWWWWW e7e6 5000
< MOVE c2c3
...
> END WIN Team7 broke through
> START BLACK 8 8 5000 Team3
...
> BYE
```

You play each opponent twice, once as White and once as Black (the instructor may change this).
