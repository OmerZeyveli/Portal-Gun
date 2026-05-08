# Portal Gun

A first-person portal-shooter sandbox built in Unity, inspired by Valve's *Portal*. Shoot a blue and an orange portal on tagged surfaces, walk through, and carry your momentum.

<!-- TODO: replace with a gameplay GIF or screenshot -->
<!-- ![Gameplay](docs/gameplay.gif) -->

## Features

- Real-time portal rendering ported from Sebastian Lague's open-source portal project.
- Portal gun with beam VFX, open VFX, and a first-person view-model.
- Tile-based, grid-snapped portal placement.
- FPS controller with walk/run/jump and momentum carry-through portals.
- Portal-aware physics objects and a sliced shader for objects clipping a portal.

## Requirements

- **Unity 2022.3.62f3**.
- Built-in render pipeline.


## Controls

| Action            | Key                |
| ----------------- | ------------------ |
| Move              | `W` `A` `S` `D`    |
| Look              | Mouse              |
| Run               | `Left Shift`       |
| Jump              | `Space`            |
| Fire blue portal  | `Left Mouse`       |
| Fire orange portal| `Right Mouse`      |
| Toggle player input | `O`              |
| Pause editor (debug break) | `P`       |

Portals will only land on surfaces built from the `PortalTile*` prefabs found under `Assets/Prefabs/PortalableTiles/`.

## Project Structure

```
Assets/
├── Materials/        Shared materials (portal screens, palette colors, props)
├── Models/           FBX meshes — portal frames and the portal-gun model
├── Prefabs/          Player, portals, physics cube, lamp, portalable tiles
├── Scenes/Level 1    The playable scene
├── Scripts/
│   ├── Portal Core/  Portal rendering, traveller, camera, shaders (Lague-derived)
│   └── Portal Game/  Portal gun, FPS controller, VFX, tile/grid, physics objects
└── Settings/         Lighting settings asset
```

## Using in Your Own Game

The portal mechanic is decoupled from the included player. Three pieces: a portal grid, a traveller (your player), and a portal gun.

**1. Copy what you need**

- `Assets/Scripts/Portal Core/` and `Assets/Scripts/Portal Game/`
- `Assets/Prefabs/BluePortal.prefab`, `OrangePortal.prefab`, `PortalableTiles/`
- `Assets/Materials/Portal Screen.mat`, `Portal Stone.mat`

The included `Player.prefab`, `Lamp.prefab`, `Physics Cube.prefab`, and `Level 1` are examples — skip them if you don't need them.

**2. Set up the scene**

- Add a `PortalGrid` component on an empty GameObject (one per scene).
- Build portal-able surfaces with the `PortalTile*` prefabs. Other surfaces block shots but reject portals.
- Drop `BluePortal.prefab` and `OrangePortal.prefab` off-screen — they're teleported in on fire.

**3. Make your player a traveller**

- Add `PortalTraveller` to the player root and assign `Graphics Object` to its visual mesh root.
- Player needs a `Rigidbody` and `Collider`.
- For momentum carry, override `PortalTraveller.Teleport()` — see [`FPSController.cs`](Assets/Scripts/Portal Game/FPSController.cs).

**4. Attach the portal gun**

Add `PortalGun` anywhere in the player hierarchy and assign:

- `Cam` (defaults to `Camera.main`)
- `Portalable Mask` — layer of your `PortalTile*` surfaces
- `Blue Portal` / `Orange Portal` — your two portal instances

Turn off `Auto Create Visuals` / `Show Crosshair` if you have your own.

**5. Drive firing from your own input**

```csharp
gun.inputEnabled = false;   // disable built-in mouse handling
gun.FireBlue();
gun.FireOrange();
```

## Credits

- **Portal rendering core** — [Sebastian Lague's Portals project](https://github.com/SebLague/Portals), MIT.
- **Portal gun model** — ["Portal Gun" on Sketchfab](https://sketchfab.com/3d-models/portal-gun-b0260066ba2c4e80aba4d1d8717d9fd9).

## License

[MIT](License).
