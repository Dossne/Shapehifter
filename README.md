# Shapeshifter Puzzle (MVP)

## Controls
- Move: Arrow keys / WASD
- Shapeshift: Space (cycles available forms)
- Restart level: R

## Level Legend
- `.` Floor (walkable)
- `#` Stone wall (blocked)
- `d` Dirt wall (blocked unless Mole)
- `w` Water (Frog can jump over one water tile)
- `b` Boulder (Gorilla can push)
- `k` Key (collect to open door)
- `D` Door (reach after key to finish level)
- `P` Player spawn
- `F` Frog token (unlock Frog form)
- `G` Gorilla token (unlock Gorilla form)
- `M` Mole token (unlock Mole form)

## Sprite Art Resources (Optional)
The game can load sprites at runtime from the Resources folders. If a sprite is missing, the game falls back to the current colored squares so the levels still run.

### Where to put PNGs
- Tiles: `Assets/Resources/Art/Tiles/`
- Objects: `Assets/Resources/Art/Objects/`
- Tokens/forms: `Assets/Resources/Art/Tokens/`

### Expected PNG names (exact strings)
**Tiles**
- `floor`
- `water_shallow`
- `water_deep`
- `stone_wall`
- `dirt_wall`

**Objects**
- `boulder`
- `key`
- `door`
- `spikes`

**Tokens/forms**
- `chameleon`
- `frog`
- `gorilla`
- `mole`

### How to verify in the Editor
1. Drop PNGs into the folders above with the exact names listed.
2. In the Inspector, set **Texture Type** to **Sprite (2D and UI)** if Unity does not auto-detect it.
3. Enter Play Mode and confirm sprites appear (missing ones will continue to use colored squares).
