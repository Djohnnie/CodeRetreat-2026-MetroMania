# MetroMania Tile Assets

This folder contains all SVG tile assets used for the top-down, tile-based level designer in MetroMania. Each level is composed of a grid of square tiles. The level editor and renderer use these tiles to paint the map.

Source files (`.afdesign`) are Affinity Designer projects. The `.svg` exports are what the application uses at runtime.

---

## Tile System Overview

The level grid is made up of **background** tiles, **water** tiles, and **station** tiles. Water tiles use an **auto-tiling** system: the correct water tile variant is selected based on which of the **eight surrounding neighbors** are also water tiles.

### Compass Directions

Each tile has up to 8 neighbors, named using compass directions in clockwise order:

```
 NW  N  NE
  W  ·  E
 SW  S  SE
```

| Direction | Position | Type |
|-----------|----------|------|
| **N** | Above | Cardinal |
| **NE** | Above-right | Diagonal |
| **E** | Right | Cardinal |
| **SE** | Below-right | Diagonal |
| **S** | Below | Cardinal |
| **SW** | Below-left | Diagonal |
| **W** | Left | Cardinal |
| **NW** | Above-left | Diagonal |

### Naming Convention

Water tiles are named by listing **which neighbors are water**, in clockwise order:

- `water.svg` — no water neighbors at all
- `water-N.svg` — only the tile above is water
- `water-N-NE-E-SE-S-SW-W-NW.svg` — all 8 neighbors are water

**Rule for diagonals:** A diagonal direction (NE, SE, SW, NW) is only relevant when **both** of its adjacent cardinal neighbors are water. For example, NE is only included when both N and E are water. If either N or E is land, the NE diagonal has no visual effect and is omitted from the name.

This yields **47 visually distinct water tiles**.

### Tile Categories

| Category | Description | Count |
|----------|-------------|-------|
| Background | Land/grass tile | 1 |
| Water — no cardinals | Isolated water tile | 1 |
| Water — 1 cardinal | Single-edge water | 4 |
| Water — 2 opposite cardinals | Straight channel | 2 |
| Water — 2 adjacent cardinals | Outer corners (±diagonal) | 8 |
| Water — 3 cardinals | T-junctions (±diagonals) | 16 |
| Water — 4 cardinals | Interior water (±diagonals) | 16 |
| Stations | Player station shapes | 6 |
| **Total** | | **54** |

---

## Background Tile

### `background.svg`

<img src="background.svg" width="64">
The default land/grass tile. Drawn first in every grid cell. All water tiles are layered on top of this tile. The embedded color `rgb(255,227,214)` is replaced at runtime with the level's custom background color.

---

## Station Tiles

Station tiles are overlaid on top of background (or water) tiles. Each station type has a distinct geometric shape.

### `station-circle.svg`

<img src="station-circle.svg" width="64">
**Circle station** — Maps to `StationType.Circle`. The most basic station shape. A filled circle centered in the tile.

### `station-square.svg`

<img src="station-square.svg" width="64">
**Square station** — Maps to `StationType.Rectangle`. A filled square/rectangle centered in the tile.

### `station-triangle.svg`

<img src="station-triangle.svg" width="64">
**Triangle station** — Maps to `StationType.Triangle`. A filled equilateral triangle centered in the tile.

### `station-diamond.svg`

<img src="station-diamond.svg" width="64">
**Diamond station** — Maps to `StationType.Diamond`. A 45°-rotated square (diamond) centered in the tile.

### `station-pentagon.svg`

<img src="station-pentagon.svg" width="64">

**Pentagon station** — Maps to `StationType.Pentagon`. A regular pentagon shape centered in the tile.

### `station-star.svg`

<img src="station-star.svg" width="64">
**Star station** — Maps to `StationType.Star`. A five-pointed star shape centered in the tile.

---

## Water Tiles

Water tiles use the embedded color `rgb(182,227,243)` which is replaced at runtime with the level's custom water color. Transparent areas show the background tile underneath.

Each tile's name lists which neighbors are water. The auto-tiling system checks all 8 neighbors and selects the matching tile. If the exact tile doesn't exist yet, it falls back to the version with all relevant diagonals treated as water.

### No Cardinal Water Neighbors (1 tile)

#### `water.svg`

<img src="water.svg" width="64">
**Isolated water tile.** No neighboring tiles are water. This tile is completely surrounded by land on all sides. Appears as a small rounded water body in the center of the tile.

**Neighbors:** None are water.

---

### 1 Cardinal Water Neighbor (4 tiles)

#### `water-N.svg`

<img src="water-N.svg" width="64">
**Water extends north.** Only the tile directly above (N) is water. The water body opens upward and is enclosed on the east, south, and west sides by land.

**Neighbors:** N = water. E, S, W = land. Diagonals irrelevant.

#### `water-E.svg`

<img src="water-E.svg" width="64">
**Water extends east.** Only the tile to the right (E) is water. The water body opens rightward and is enclosed on the north, south, and west sides by land.

**Neighbors:** E = water. N, S, W = land. Diagonals irrelevant.

#### `water-S.svg`

<img src="water-S.svg" width="64">
**Water extends south.** Only the tile directly below (S) is water. The water body opens downward and is enclosed on the north, east, and west sides by land.

**Neighbors:** S = water. N, E, W = land. Diagonals irrelevant.

#### `water-W.svg`

<img src="water-W.svg" width="64">
**Water extends west.** Only the tile to the left (W) is water. The water body opens leftward and is enclosed on the north, east, and south sides by land.

**Neighbors:** W = water. N, E, S = land. Diagonals irrelevant.

---

### 2 Opposite Cardinal Water Neighbors (2 tiles)

#### `water-N-S.svg`

<img src="water-N-S.svg" width="64">
**Vertical water channel.** The tiles above (N) and below (S) are water. Water flows through the tile vertically, with land on the east and west sides. Used for north-south river stretches.

**Neighbors:** N, S = water. E, W = land. Diagonals irrelevant (no adjacent cardinal pairs both water).

#### `water-E-W.svg`

<img src="water-E-W.svg" width="64">
**Horizontal water channel.** The tiles to the right (E) and left (W) are water. Water flows through the tile horizontally, with land on the north and south sides. Used for east-west river stretches.

**Neighbors:** E, W = water. N, S = land. Diagonals irrelevant (no adjacent cardinal pairs both water).

---

### 2 Adjacent Cardinal Water Neighbors (8 tiles)

When two adjacent cardinal neighbors are water, the diagonal between them becomes relevant. If the diagonal is also water, the corner has a smooth fill. If the diagonal is land, there is a concave inner corner notch.

#### `water-N-NE-E.svg` ✅

<img src="water-N-NE-E.svg" width="64">
**Smooth outer corner — top-right.** The tiles above (N), diagonally above-right (NE), and to the right (E) are water. Water fills the top-right quadrant smoothly. Land is to the south and west.

**Neighbors:** N, NE, E = water. SE, S, SW, W, NW = land.

#### `water-N-E.svg` ✅

<img src="water-N-E.svg" width="64">
**Inner corner — top-right.** The tiles above (N) and to the right (E) are water, but the diagonal between them (NE) is land. This creates a concave notch at the top-right corner where land intrudes between two water bodies.

**Neighbors:** N, E = water. NE = land. SE, S, SW, W, NW = land.

#### `water-E-SE-S.svg` ✅

<img src="water-E-SE-S.svg" width="64">
**Smooth outer corner — bottom-right.** The tiles to the right (E), diagonally below-right (SE), and below (S) are water. Water fills the bottom-right quadrant smoothly. Land is to the north and west.

**Neighbors:** E, SE, S = water. N, NE, SW, W, NW = land.

#### `water-E-S.svg` ✅

<img src="water-E-S.svg" width="64">
**Inner corner — bottom-right.** The tiles to the right (E) and below (S) are water, but the diagonal between them (SE) is land. This creates a concave notch at the bottom-right corner.

**Neighbors:** E, S = water. SE = land. N, NE, SW, W, NW = land.

#### `water-S-SW-W.svg` ✅

<img src="water-S-SW-W.svg" width="64">
**Smooth outer corner — bottom-left.** The tiles below (S), diagonally below-left (SW), and to the left (W) are water. Water fills the bottom-left quadrant smoothly. Land is to the north and east.

**Neighbors:** S, SW, W = water. N, NE, E, SE, NW = land.

#### `water-S-W.svg` ✅

<img src="water-S-W.svg" width="64">
**Inner corner — bottom-left.** The tiles below (S) and to the left (W) are water, but the diagonal between them (SW) is land. This creates a concave notch at the bottom-left corner.

**Neighbors:** S, W = water. SW = land. N, NE, E, SE, NW = land.

#### `water-N-W-NW.svg` ✅

<img src="water-N-W-NW.svg" width="64">
**Smooth outer corner — top-left.** The tiles above (N), to the left (W), and diagonally above-left (NW) are water. Water fills the top-left quadrant smoothly. Land is to the east and south.

**Neighbors:** N, W, NW = water. NE, E, SE, S, SW = land.

#### `water-N-W.svg` ✅

<img src="water-N-W.svg" width="64">
**Inner corner — top-left.** The tiles above (N) and to the left (W) are water, but the diagonal between them (NW) is land. This creates a concave notch at the top-left corner.

**Neighbors:** N, W = water. NW = land. NE, E, SE, S, SW = land.

---

### 3 Cardinal Water Neighbors (16 tiles)

T-junction tiles where water flows in three cardinal directions. Each T-junction has two relevant diagonals (between each pair of adjacent water cardinals). The presence or absence of each diagonal creates 4 variants per T-junction orientation.

#### Missing S (water flows N, E, W)

##### `water-N-NE-E-W-NW.svg` ✅

<img src="water-N-NE-E-W-NW.svg" width="64">
**T-junction open to north, east, west — all diagonals smooth.** Water fills the top portion of the tile. Land is only to the south. Both diagonal corners (NE, NW) are smoothly filled.

**Neighbors:** N, NE, E, W, NW = water. SE, S, SW = land.

##### `water-N-NE-E-W.svg` ✅

<img src="water-N-NE-E-W.svg" width="64">
**T-junction open to N, E, W — NW corner notch.** Same as above but the NW diagonal is land, creating a concave corner at top-left.

**Neighbors:** N, NE, E, W = water. NW, SE, S, SW = land.

##### `water-N-E-W-NW.svg` ✅

<img src="water-N-E-W-NW.svg" width="64">
**T-junction open to N, E, W — NE corner notch.** Same as base but the NE diagonal is land, creating a concave corner at top-right.

**Neighbors:** N, E, W, NW = water. NE, SE, S, SW = land.

##### `water-N-E-W.svg` ✅

<img src="water-N-E-W.svg" width="64">
**T-junction open to N, E, W — both corner notches.** Both NE and NW diagonals are land. Both top corners have concave notches.

**Neighbors:** N, E, W = water. NE, SE, S, SW, NW = land.

#### Missing W (water flows N, E, S)

##### `water-N-NE-E-SE-S.svg` ✅

<img src="water-N-NE-E-SE-S.svg" width="64">
**T-junction open to north, east, south — all diagonals smooth.** Water fills the right portion of the tile. Land is only to the west. Both diagonal corners (NE, SE) are smoothly filled.

**Neighbors:** N, NE, E, SE, S = water. SW, W, NW = land.

##### `water-N-NE-E-S.svg` ✅

<img src="water-N-NE-E-S.svg" width="64">
**T-junction open to N, E, S — SE corner notch.** The SE diagonal is land, creating a concave corner at bottom-right.

**Neighbors:** N, NE, E, S = water. SE, SW, W, NW = land.

##### `water-N-E-SE-S.svg` ✅

<img src="water-N-E-SE-S.svg" width="64">
**T-junction open to N, E, S — NE corner notch.** The NE diagonal is land, creating a concave corner at top-right.

**Neighbors:** N, E, SE, S = water. NE, SW, W, NW = land.

##### `water-N-E-S.svg` ✅

<img src="water-N-E-S.svg" width="64">
**T-junction open to N, E, S — both corner notches.** Both NE and SE diagonals are land.

**Neighbors:** N, E, S = water. NE, SE, SW, W, NW = land.

#### Missing N (water flows E, S, W)

##### `water-E-SE-S-SW-W.svg` ✅

<img src="water-E-SE-S-SW-W.svg" width="64">
**T-junction open to east, south, west — all diagonals smooth.** Water fills the bottom portion of the tile. Land is only to the north. Both diagonal corners (SE, SW) are smoothly filled.

**Neighbors:** E, SE, S, SW, W = water. N, NE, NW = land.

##### `water-E-SE-S-W.svg` ✅

<img src="water-E-SE-S-W.svg" width="64">
**T-junction open to E, S, W — SW corner notch.** The SW diagonal is land.

**Neighbors:** E, SE, S, W = water. N, NE, SW, NW = land.

##### `water-E-S-SW-W.svg` ✅

<img src="water-E-S-SW-W.svg" width="64">
**T-junction open to E, S, W — SE corner notch.** The SE diagonal is land.

**Neighbors:** E, S, SW, W = water. N, NE, SE, NW = land.

##### `water-E-S-W.svg` ✅

<img src="water-E-S-W.svg" width="64">
**T-junction open to E, S, W — both corner notches.** Both SE and SW diagonals are land.

**Neighbors:** E, S, W = water. N, NE, SE, SW, NW = land.

#### Missing E (water flows N, S, W)

##### `water-N-S-SW-W-NW.svg` ✅

<img src="water-N-S-SW-W-NW.svg" width="64">
**T-junction open to north, south, west — all diagonals smooth.** Water fills the left portion of the tile. Land is only to the east. Both diagonal corners (SW, NW) are smoothly filled.

**Neighbors:** N, S, SW, W, NW = water. NE, E, SE = land.

##### `water-N-S-SW-W.svg` ✅

<img src="water-N-S-SW-W.svg" width="64">
**T-junction open to N, S, W — NW corner notch.** The NW diagonal is land.

**Neighbors:** N, S, SW, W = water. NE, E, SE, NW = land.

##### `water-N-S-W-NW.svg` ✅

<img src="water-N-S-W-NW.svg" width="64">
**T-junction open to N, S, W — SW corner notch.** The SW diagonal is land.

**Neighbors:** N, S, W, NW = water. NE, E, SE, SW = land.

##### `water-N-S-W.svg` ✅

<img src="water-N-S-W.svg" width="64">
**T-junction open to N, S, W — both corner notches.** Both SW and NW diagonals are land.

**Neighbors:** N, S, W = water. NE, E, SE, SW, NW = land.

---

### 4 Cardinal Water Neighbors (16 tiles)

All four cardinal neighbors are water. The tile is "interior" water. Each of the four diagonals can independently be water or land, creating 16 distinct tiles. When a diagonal is land, it creates a small concave inner corner notch at that position.

#### All diagonals water

##### `water-N-NE-E-SE-S-SW-W-NW.svg` ✅

<img src="water-N-NE-E-SE-S-SW-W-NW.svg" width="64">
**Full water — all 8 neighbors are water.** The tile is completely filled with water. No land is visible. Used for interior water body areas.

**Neighbors:** All 8 = water.

#### 3 diagonals water (1 land)

##### `water-N-E-SE-S-SW-W-NW.svg` ✅

<img src="water-N-E-SE-S-SW-W-NW.svg" width="64">
**All water except NE diagonal.** A small concave inner corner notch appears at the top-right where a single land tile intrudes diagonally.

**Neighbors:** N, E, SE, S, SW, W, NW = water. **NE = land.**

##### `water-N-NE-E-S-SW-W-NW.svg` ✅

<img src="water-N-NE-E-S-SW-W-NW.svg" width="64">
**All water except SE diagonal.** A small concave inner corner notch appears at the bottom-right.

**Neighbors:** N, NE, E, S, SW, W, NW = water. **SE = land.**

##### `water-N-NE-E-SE-S-W-NW.svg` ✅

<img src="water-N-NE-E-SE-S-W-NW.svg" width="64">
**All water except SW diagonal.** A small concave inner corner notch appears at the bottom-left.

**Neighbors:** N, NE, E, SE, S, W, NW = water. **SW = land.**

##### `water-N-NE-E-SE-S-SW-W.svg` ✅

<img src="water-N-NE-E-SE-S-SW-W.svg" width="64">
**All water except NW diagonal.** A small concave inner corner notch appears at the top-left.

**Neighbors:** N, NE, E, SE, S, SW, W = water. **NW = land.**

#### 2 diagonals water (2 land)

##### `water-N-NE-E-SE-S-W.svg` ✅

<img src="water-N-NE-E-SE-S-W.svg" width="64">
**NE and SE water, SW and NW land.** Inner corner notches at both the bottom-left and top-left.

**Neighbors:** N, NE, E, SE, S, W = water. **SW, NW = land.**

##### `water-N-NE-E-S-SW-W.svg` ✅

<img src="water-N-NE-E-S-SW-W.svg" width="64">
**NE and SW water, SE and NW land.** Inner corner notches at the bottom-right and top-left (opposite corners).

**Neighbors:** N, NE, E, S, SW, W = water. **SE, NW = land.**

##### `water-N-NE-E-S-W-NW.svg` ✅

<img src="water-N-NE-E-S-W-NW.svg" width="64">
**NE and NW water, SE and SW land.** Inner corner notches at both the bottom-right and bottom-left.

**Neighbors:** N, NE, E, S, W, NW = water. **SE, SW = land.**

##### `water-N-E-SE-S-SW-W.svg` ✅

<img src="water-N-E-SE-S-SW-W.svg" width="64">
**SE and SW water, NE and NW land.** Inner corner notches at both the top-right and top-left.

**Neighbors:** N, E, SE, S, SW, W = water. **NE, NW = land.**

##### `water-N-E-SE-S-W-NW.svg` ✅

<img src="water-N-E-SE-S-W-NW.svg" width="64">
**SE and NW water, NE and SW land.** Inner corner notches at the top-right and bottom-left (opposite corners).

**Neighbors:** N, E, SE, S, W, NW = water. **NE, SW = land.**

##### `water-N-E-S-SW-W-NW.svg` ✅

<img src="water-N-E-S-SW-W-NW.svg" width="64">
**SW and NW water, NE and SE land.** Inner corner notches at both the top-right and bottom-right.

**Neighbors:** N, E, S, SW, W, NW = water. **NE, SE = land.**

#### 1 diagonal water (3 land)

##### `water-N-NE-E-S-W.svg` ✅

<img src="water-N-NE-E-S-W.svg" width="64">
**Only NE water.** Inner corner notches at SE, SW, and NW. Only the top-right corner is smooth.

**Neighbors:** N, NE, E, S, W = water. **SE, SW, NW = land.**

##### `water-N-E-SE-S-W.svg` ✅

<img src="water-N-E-SE-S-W.svg" width="64">
**Only SE water.** Inner corner notches at NE, SW, and NW. Only the bottom-right corner is smooth.

**Neighbors:** N, E, SE, S, W = water. **NE, SW, NW = land.**

##### `water-N-E-S-SW-W.svg` ✅

<img src="water-N-E-S-SW-W.svg" width="64">
**Only SW water.** Inner corner notches at NE, SE, and NW. Only the bottom-left corner is smooth.

**Neighbors:** N, E, S, SW, W = water. **NE, SE, NW = land.**

##### `water-N-E-S-W-NW.svg` ✅

<img src="water-N-E-S-W-NW.svg" width="64">
**Only NW water.** Inner corner notches at NE, SE, and SW. Only the top-left corner is smooth.

**Neighbors:** N, E, S, W, NW = water. **NE, SE, SW = land.**

#### 0 diagonals water (all 4 land)

##### `water-N-E-S-W.svg` ✅

<img src="water-N-E-S-W.svg" width="64">
**All four cardinals water, all four diagonals land.** Inner corner notches at all four corners. This creates a distinctive "pinched" water tile. Each corner has a small land intrusion.

**Neighbors:** N, E, S, W = water. **NE, SE, SW, NW = all land.**

---

## Quick Reference Table

### All Tiles (47 water + 1 background + 6 stations = 54)

| Preview | Filename | Water Neighbors |
|---------|----------|-----------------|
| <img src="background.svg" width="32"> | `background.svg` | — (land tile) |
| <img src="water.svg" width="32"> | `water.svg` | None |
| <img src="water-N.svg" width="32"> | `water-N.svg` | N |
| <img src="water-E.svg" width="32"> | `water-E.svg` | E |
| <img src="water-S.svg" width="32"> | `water-S.svg` | S |
| <img src="water-W.svg" width="32"> | `water-W.svg` | W |
| <img src="water-N-S.svg" width="32"> | `water-N-S.svg` | N, S |
| <img src="water-E-W.svg" width="32"> | `water-E-W.svg` | E, W |
| <img src="water-N-NE-E.svg" width="32"> | `water-N-NE-E.svg` | N, NE, E |
| <img src="water-N-E.svg" width="32"> | `water-N-E.svg` | N, E (NE land) |
| <img src="water-E-SE-S.svg" width="32"> | `water-E-SE-S.svg` | E, SE, S |
| <img src="water-E-S.svg" width="32"> | `water-E-S.svg` | E, S (SE land) |
| <img src="water-S-SW-W.svg" width="32"> | `water-S-SW-W.svg` | S, SW, W |
| <img src="water-S-W.svg" width="32"> | `water-S-W.svg` | S, W (SW land) |
| <img src="water-N-W-NW.svg" width="32"> | `water-N-W-NW.svg` | N, W, NW |
| <img src="water-N-W.svg" width="32"> | `water-N-W.svg` | N, W (NW land) |
| <img src="water-N-NE-E-W-NW.svg" width="32"> | `water-N-NE-E-W-NW.svg` | N, NE, E, W, NW |
| <img src="water-N-NE-E-W.svg" width="32"> | `water-N-NE-E-W.svg` | N, NE, E, W |
| <img src="water-N-E-W-NW.svg" width="32"> | `water-N-E-W-NW.svg` | N, E, W, NW |
| <img src="water-N-E-W.svg" width="32"> | `water-N-E-W.svg` | N, E, W |
| <img src="water-N-NE-E-SE-S.svg" width="32"> | `water-N-NE-E-SE-S.svg` | N, NE, E, SE, S |
| <img src="water-N-NE-E-S.svg" width="32"> | `water-N-NE-E-S.svg` | N, NE, E, S |
| <img src="water-N-E-SE-S.svg" width="32"> | `water-N-E-SE-S.svg` | N, E, SE, S |
| <img src="water-N-E-S.svg" width="32"> | `water-N-E-S.svg` | N, E, S |
| <img src="water-E-SE-S-SW-W.svg" width="32"> | `water-E-SE-S-SW-W.svg` | E, SE, S, SW, W |
| <img src="water-E-SE-S-W.svg" width="32"> | `water-E-SE-S-W.svg` | E, SE, S, W |
| <img src="water-E-S-SW-W.svg" width="32"> | `water-E-S-SW-W.svg` | E, S, SW, W |
| <img src="water-E-S-W.svg" width="32"> | `water-E-S-W.svg` | E, S, W |
| <img src="water-N-S-SW-W-NW.svg" width="32"> | `water-N-S-SW-W-NW.svg` | N, S, SW, W, NW |
| <img src="water-N-S-SW-W.svg" width="32"> | `water-N-S-SW-W.svg` | N, S, SW, W |
| <img src="water-N-S-W-NW.svg" width="32"> | `water-N-S-W-NW.svg` | N, S, W, NW |
| <img src="water-N-S-W.svg" width="32"> | `water-N-S-W.svg` | N, S, W |
| <img src="water-N-NE-E-SE-S-SW-W-NW.svg" width="32"> | `water-N-NE-E-SE-S-SW-W-NW.svg` | All 8 |
| <img src="water-N-E-SE-S-SW-W-NW.svg" width="32"> | `water-N-E-SE-S-SW-W-NW.svg` | All except NE |
| <img src="water-N-NE-E-S-SW-W-NW.svg" width="32"> | `water-N-NE-E-S-SW-W-NW.svg` | All except SE |
| <img src="water-N-NE-E-SE-S-W-NW.svg" width="32"> | `water-N-NE-E-SE-S-W-NW.svg` | All except SW |
| <img src="water-N-NE-E-SE-S-SW-W.svg" width="32"> | `water-N-NE-E-SE-S-SW-W.svg` | All except NW |
| <img src="water-N-NE-E-SE-S-W.svg" width="32"> | `water-N-NE-E-SE-S-W.svg` | NE+SE, SW+NW land |
| <img src="water-N-NE-E-S-SW-W.svg" width="32"> | `water-N-NE-E-S-SW-W.svg` | NE+SW, SE+NW land |
| <img src="water-N-NE-E-S-W-NW.svg" width="32"> | `water-N-NE-E-S-W-NW.svg` | NE+NW, SE+SW land |
| <img src="water-N-E-SE-S-SW-W.svg" width="32"> | `water-N-E-SE-S-SW-W.svg` | SE+SW, NE+NW land |
| <img src="water-N-E-SE-S-W-NW.svg" width="32"> | `water-N-E-SE-S-W-NW.svg` | SE+NW, NE+SW land |
| <img src="water-N-E-S-SW-W-NW.svg" width="32"> | `water-N-E-S-SW-W-NW.svg` | SW+NW, NE+SE land |
| <img src="water-N-NE-E-S-W.svg" width="32"> | `water-N-NE-E-S-W.svg` | Only NE diag |
| <img src="water-N-E-SE-S-W.svg" width="32"> | `water-N-E-SE-S-W.svg` | Only SE diag |
| <img src="water-N-E-S-SW-W.svg" width="32"> | `water-N-E-S-SW-W.svg` | Only SW diag |
| <img src="water-N-E-S-W-NW.svg" width="32"> | `water-N-E-S-W-NW.svg` | Only NW diag |
| <img src="water-N-E-S-W.svg" width="32"> | `water-N-E-S-W.svg` | All 4 diags land |
| <img src="station-circle.svg" width="32"> | `station-circle.svg` | — (Circle) |
| <img src="station-square.svg" width="32"> | `station-square.svg` | — (Rectangle) |
| <img src="station-triangle.svg" width="32"> | `station-triangle.svg` | — (Triangle) |
| <img src="station-diamond.svg" width="32"> | `station-diamond.svg` | — (Diamond) |
| <img src="station-pentagon.svg" width="32"> | `station-pentagon.svg` | — (Pentagon) |
| <img src="station-star.svg" width="32"> | `station-star.svg` | — (Star) |

> **All 47 water tile variants are now included.** The auto-tiling system selects the correct tile based on all 8 neighbors — no fallback needed.
