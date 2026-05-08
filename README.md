# Portal Gun

A first-person portal-shooter sandbox built in Unity, inspired by Valve's *Portal*. Place a blue and an orange portal on tagged surfaces, walk through, and bring your momentum with you.

> **Status:** playable single-level prototype.

<!-- TODO: replace with a gameplay GIF or screenshot -->
<!-- ![Gameplay](docs/gameplay.gif) -->

## Features

- Real-time portal rendering (recursive view, oblique near-plane projection) ported from Sebastian Lague's open-source portal project.
- Portal gun with left/right click to fire blue/orange portals, beam VFX, open VFX, and a first-person view-model.
- Tile-based placement system: portals only stick to surfaces marked with `PortalTile` prefabs (wall, ground, ceiling), and snap onto a grid so they always fit.
- Custom FPS controller with walk/run/jump, mouse-look, and momentum carry-through portals.
- Portal-aware physics objects (props travel through portals correctly).
- Sliced shader so objects clipping a portal aren't visible on the wrong side.

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

The portal mechanic is decoupled from the included player — you can drop it into a shooter, a puzzle game, a third-person action game, or anything else. You only need to bring three things together: a portal grid, a traveller (your player), and a portal gun (anywhere that can call into the API).

### 1. Copy the assets you need

At minimum, copy these into your project:

- `Assets/Scripts/Portal Core/` — portal rendering, traveller, shaders. **Don't rename or modify**, this is the engine.
- `Assets/Scripts/Portal Game/` — the placement, gun, VFX, and tile system.
- `Assets/Prefabs/BluePortal.prefab`, `OrangePortal.prefab` — the portal instances.
- `Assets/Prefabs/PortalableTiles/` — surface markers used by the placement raycast.
- `Assets/Materials/Portal Screen.mat`, `Portal Stone.mat` — referenced by the portal prefabs.

The included `Player.prefab`, `Lamp.prefab`, `Physics Cube.prefab`, and `Level 1` scene are **examples**; you don't need them.

### 2. Set up the scene

1. Add an empty GameObject and attach a `PortalGrid` component. There must be exactly one in the scene.
2. Build any portal-able surfaces using prefabs from `Assets/Prefabs/PortalableTiles/` (`PortalTileWall`, `PortalTileGround`, `PortalTileCeiling`). Surfaces without a `PortalTile` will block the shot but reject the portal.
3. Place `BluePortal.prefab` and `OrangePortal.prefab` somewhere off-screen — they get teleported in when fired.

### 3. Make your player a portal traveller

Anything that should pass through a portal needs a `PortalTraveller` component. On your existing player root:

- Add `PortalTraveller` (or a subclass).
- Assign its `Graphics Object` field to the visual mesh root (used to clone a sliced copy on the other side).
- Make sure the player has a `Rigidbody` and a `Collider` so portals can detect entry.

If you want portal momentum to carry through (like the example FPS controller), override `PortalTraveller.Teleport()` in your own controller and reapply your stored velocity in the new portal's local space. See [`Assets/Scripts/Portal Game/FPSController.cs`](Assets/Scripts/Portal Game/FPSController.cs) for a worked example.

### 4. Attach the portal gun

Add a `PortalGun` component anywhere in the player hierarchy (commonly on the camera or a held weapon GameObject) and fill in the inspector:

- `Cam` — your aiming camera. Leave empty to default to `Camera.main`.
- `Portalable Mask` — the layer(s) your `PortalTile*` surfaces live on.
- `Blue Portal` / `Orange Portal` — drag your two portal instances here.
- Leave `Auto Create Visuals` and `Show Crosshair` on if you want the script to spawn the view-model, beam VFX, and crosshair for you. Turn them off if you have your own.

### 5. Drive firing from your own input system

`PortalGun` reads the left and right mouse buttons by default. To wire it to your own input system (Unity Input System, gamepad, mobile UI button, networked command, etc.):

```csharp
gun.inputEnabled = false;   // stop the built-in mouse handling
gun.FireBlue();             // call from your own input handler
gun.FireOrange();
```

Both methods evaluate the placement, run VFX, and update the crosshair the same way the built-in path does — so you get correct behavior regardless of how the call is triggered.

## Credits

- **Portal rendering core** — based on [Sebastian Lague's Portals project](https://github.com/SebLague/Portals), MIT-licensed (`Assets/Scripts/Portal Core/`).
- **Portal gun model** — ["Portal Gun" on Sketchfab](https://sketchfab.com/3d-models/portal-gun-b0260066ba2c4e80aba4d1d8717d9fd9).

## License

Released under the [MIT License](License). The original portal-rendering code is also MIT-licensed.
