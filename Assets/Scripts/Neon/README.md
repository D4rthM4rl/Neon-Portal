# Neon Block Outline System

Draws a single continuous neon line around groups of adjacent blocks / platforms and lets you
assign both the **inside (fill) colour** and the **outline (neon) colour** of each block.

## Components

- **`NeonBlock`** — add to any block or platform that has a square `SpriteRenderer` and a
  `BoxCollider2D` (or an axis-aligned `PolygonCollider2D` used as a platform collider).
  - `Inside Color` — applied to the block's `SpriteRenderer`.
  - `Outline Color` — colour of the neon line traced around the block's exposed surface.
  - `Cell Size` — world size of one grid cell (usually `1` to match a 1x1 sprite). Blocks must
    line up to this grid. A block stretched via x/y scaling simply covers a run of cells.
- **`NeonOutlineManager`** — one per scene (auto-created). Rasterises every block onto a shared
  grid, merges adjacent same-outline-colour blocks, traces the perimeters and draws them with
  `LineRenderer`s. Configure `Line Width`, `Line Material`, sorting layer/order and `Z Offset`.

## How merging works

Each block fills one or more unit cells (stretched blocks fill several). A cell edge becomes a
neon segment **only if the neighbouring cell is not filled with the same outline colour**.

- **Same outline colour + adjacent** → the shared edge has a same-colour neighbour, so it is not
  drawn. The blocks act as one big block with a single outline and no interior lines. For a
  T made of two blocks, only the silhouette of the T is drawn.
- **Different outline colour + adjacent** → each block keeps its own full outline (the shared
  edge is drawn once per colour).

Corners are kept only where the outline actually turns, i.e. where the two edges around the
corner are both part of the neon line; collinear pass-through points are removed so lines stay
continuous.

## Usage

1. Select one or more block GameObjects in a scene.
2. Run **Tools ▸ Neon ▸ Add Neon Block To Selection** (`Ctrl/Cmd+Shift+N`).
3. Set the Inside / Outline colours per block in the Inspector.
4. Outlines rebuild automatically on change. Use **Tools ▸ Neon ▸ Rebuild Outlines** to force a
   rebuild (e.g. after moving blocks).

At runtime the manager rebuilds when blocks register/unregister; call
`NeonOutlineManager.RequestRebuild()` after moving blocks (e.g. moving platforms) to refresh.
