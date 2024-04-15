# Snek the game
This repository contains a game prepared by the REVERSED section to showcase during PutCyberCONF 2024.

### Description
The game is an implementation of the all-time classic snake game. It consists of levels impossible to beat without modifying the program's code appropriately.

------------
### Walkthrough

#### 1. Level one
The snake aims to collect a fruit with a minuscule chance of appearing. The player must increase the probability of this event or hardcode it as permanent.

#### 2. Level two
The level is beaten once the snake has eaten an impossibly large amount of fruit. The requirement must be lowered or the number of points accredited by each gathered fruit must be increased.

#### 3. Level three
Victory is achieved after eating the fruit hidden behind an impenetrable wall. The wall must be removed, the fruit displaced, or collisions disabled.

------------
### Known bugs

- Closing the victory message immediately after its appearance causes the game to freeze.
- The game **MUST NOT** be closed using the X in the title bar!!! The "abandon snek" button is used to close the program.
