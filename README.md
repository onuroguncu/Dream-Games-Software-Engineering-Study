# Dream Games – Software Engineering Study

A level-based mobile puzzle game built in Unity 6000.3.10f1 and C#, submitted as part of the Dream Games Software Engineering Case Study.

## Overview

The game is a blast-style puzzle where the player clears obstacles by tapping colored cube groups or triggering special items. It consists of 10 levels, each loaded from a JSON file, and is playable in portrait (9:16) orientation directly from the Unity Editor.

## Flow

- **Main Scene**: Displays a Level Button showing the current level. Once all levels are completed, it shows "Finished".
- **Level Scene**: Loaded on tap. The player interacts with a grid of cubes and obstacles.
- **Win**: Celebration animation and particles are shown, then the Main Scene is loaded.
- **Fail**: A popup appears with options to return to Main Scene or retry the level.
- The current level is persisted locally via PlayerPrefs. A Unity Editor menu item is available to manually set the level number.

## Gameplay

- A rectangular grid (6–10 cells wide/tall) where each cell holds one item.
- Tapping a group of 2+ same-colored adjacent cubes blasts them.
- Blasting 4 cubes creates a Rocket; blasting 6+ creates a TNT.
- Eligible groups display a hint icon showing the special item they would create.
- Fall mechanics and animations are implemented entirely in code (no Physics or Unity animations).

## Special Items

| Item | Explosion |
|---|---|
| Rocket (H/V) | Sweeps entire row or column, damaging cells one by one |
| TNT | Explodes in a 5×5 area |
| Rocket + Rocket combo | Cross sweep (+ shape) |
| TNT + TNT combo | 7×7 area explosion |
| TNT + Rocket combo | 3×3 array of rockets in both directions |

## Obstacles

| Obstacle | Behavior |
|---|---|
| Vase | Takes damage from adjacent blasts or special items. 2 hits to clear. Falls down. |
| Stone | Only damaged by special items. 1 hit to clear. Does not fall. |
| Chalice Box | 2×2 obstacle. Door phase: 1 damage per source. Chalice phase: damage = cells hit. Goal: collect 10 chalices. Does not fall. |

## Level Files

Levels are defined as JSON files under `Assets/Resources/`. Each file specifies the grid dimensions, move count, and a flat grid array read from bottom-left to top-right.

### Note on Levels 3 and 9

Levels 3 and 9 contain minor layout inconsistencies that likely stem from the JSON files being authored manually. Corrected versions are included in the repository as:

- `Assets/Resources/level_03_fixed.json`
- `Assets/Resources/level_09_fixed.json`

## Technical Notes

- **Engine**: Unity 6000.3.10f1, built-in renderer
- **Language**: C#
- **Animations**: DOTween
- **Architecture**: OOP — encapsulation, inheritance, polymorphism
- **No** third-party dependency injection libraries used
