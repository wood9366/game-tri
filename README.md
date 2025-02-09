# Toon Blast like Crash

## Blocks

- stay on one cell

Brick Types:
- key for eliminate check
- color
- shape

Block Behaviours:
- move
  - static, stick on cell until elimiate
  - drop, empty cell under, drop down
- (no)eliminate, whether or not be eliminate

Blocks:
- brick
  - move, drop
  - brick type, different color or shape
  - eliminate by click when linked brick >= 2
  - eliminate all linked bricks
  - generate special block depends on eliminate number
    - 5 - 7, rocket
    - 8 - 10, bomb
    - > 10, dimond
  - generate special block on click brick pos
- rocket
  - move, drop
  - rocket type, horizental or vertical
  - eliminate by click
  - eliminate all row or column blocks
- bomb
  - move, drop
  - eliminate by click
  - eliminate 3 x 3 rect area blocks
- dimond
  - move, drop
  - brick type
  - eliminate by click
  - eliminate all brick type blocks
- balloon
  - move, drop
  - eliminate when neighbor block eliminate
- wooden_box
  - move, static
  - eliminate when neighbor block eliminate
- magic_hat
  - move, static
  - generate carrot by when neighbor block eliminate

Special Blocks:
- rocket
- bomb
- dimond

Special Block Eliminate:
- eliminate all linked special block
- rocket + rocket, eliminate all row and column blocks
- rocket + bomb
- dimond + others (rocket, bomb)
  - change all same type brick to rocket(horizental or vertical) or bomb
  - eliminate all generate block one by one
- dimond + dimond 
  - eliminate all blocks on board
