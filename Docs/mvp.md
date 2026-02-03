# Shapeshifter Puzzle — MVP (Unity 2D)

## Goal
Build a playable grid-based puzzle prototype that runs in Unity Editor and can be built for Android later.
MVP = 1 scene, keyboard controls, 4 levels played sequentially, no menus.

## Controls
- Move: Arrow keys / WASD (one tile per key press)
- Shapeshift: Space (cycle available forms)
- Restart level: R (optional but helpful)

## Core Loop
1) Load level
2) Move on grid, interact with tiles/objects
3) Collect key → reach door → next level

## Player & Forms
The player is a Chameleon. It can learn forms by stepping on a creature token.

### Available in MVP
- Chameleon: normal move (no special powers)
- Frog: jump over 1 Water tile in a straight line
  - Rule: if adjacent tile is Water and the landing tile (2 tiles away) is walkable, allow move by 2.
- Gorilla: push Boulder (Sokoban-style)
  - Rule: if next tile is Boulder and the tile behind it is empty/walkable, push Boulder and move player.
- Mole: pass through Dirt walls
  - Rule: Dirt wall is blocked for other forms; Mole treats it as walkable.

### Shapeshift limit
- Each level starts with `ShapeshiftsRemaining = 5`.
- Each press of Space that successfully changes the current form decreases it by 1.
- If remaining = 0, Space does nothing.
- UI must show: current form + remaining.

## Tiles & Objects
### Tiles
- Floor: walkable
- Stone wall `#`: blocked
- Dirt wall `d`: blocked, except Mole
- Water `w`: blocked, except Frog can jump over (as described)

### Objects
- Player spawn `P`
- Boulder `b`: pushable only by Gorilla
- Key `k`: collectible
- Door `D`: closed until key collected; stepping onto open door completes level
- Creature tokens:
  - Frog token `F` → unlock Frog form
  - Gorilla token `G` → unlock Gorilla form
  - Mole token `M` → unlock Mole form

## Level Format (Text)
Levels are stored as text maps and loaded at runtime.

Location:
- `Assets/Resources/Levels/level1.txt` ... `level4.txt`

Legend:
- `.` Floor
- `#` Stone wall
- `d` Dirt wall
- `w` Water
- `b` Boulder
- `k` Key
- `D` Door
- `P` Player spawn
- `F` Frog token
- `G` Gorilla token
- `M` Mole token

## Levels (MVP)
Provide 4 small levels:
1) Movement + Key/Door
2) Frog jump over Water
3) Gorilla pushes Boulder
4) Mole passes through Dirt walls

Each level must be solvable with the s
