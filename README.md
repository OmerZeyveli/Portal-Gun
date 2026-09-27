# Portal Gun

A first-person portal-shooter sandbox built in Unity, inspired by Valve's *Portal*. Shoot a blue and an orange portal on surfaces built from `PortalTile` prefabs on the portalable layer, walk through, and carry your momentum.

<!-- TODO: replace with a gameplay GIF or screenshot -->
<!-- ![Gameplay](docs/gameplay.gif) -->

## Features

- Real-time portal rendering ported from Sebastian Lague's open-source portal project.
- Split into independent assemblies (`PortalKit.Core`, `.Gun`, `.VFX`, `.Samples`); Core has no dependencies, so you can take only the pieces you need.
- Portal gun with beam VFX, open VFX, and a first-person view-model.
- Tile-based, grid-snapped portal placement.
- FPS controller with walk/run/jump and momentum carry-through portals.
- Portal-aware physics objects and a sliced shader for objects clipping a portal.

## Requirements

- **Unity 2022.3.62f3**.
- Built-in render pipeline.

## Getting Started

1. Clone the repository and open the folder with Unity 2022.3.62f3.
2. Open `Assets/PortalKit/Samples/Scenes/Level 1.unity` and press Play.

## Controls

| Action            | Key                |
| ----------------- | ------------------ |
| Move              | `W` `A` `S` `D`    |
| Look              | Mouse              |
| Run               | `Left Shift`       |
| Jump              | `Space`            |
| Fire blue portal  | `Left Mouse`       |
| Fire orange portal| `Right Mouse`      |
| Restart scene     | `R`                |
| Toggle player input | `O`              |
| Pause editor (debug break) | `P`       |

Portals will only land on surfaces built from the `PortalTile*` prefabs found under `Assets/PortalKit/Gun/Prefabs/PortalableTiles/`.

## Project Structure

```
Assets/PortalKit/
├── Core/     Portals only: rendering, travel, slicing, PortalAperture, PortalTransformUtility
├── Gun/      Portal gun + tile/grid placement (depends on Core)
├── VFX/      Optional effects: rim, open burst, beam, gun view model, crosshair (depends on Core, Gun)
├── Samples/  Level 1, FPS player, physics cube (depends on everything)
└── Tests/    EditMode tests for the travel math and aperture mesh
```

Each folder is its own assembly. Dependencies only point up this list (Gun uses Core, VFX uses Core and Gun), so Core never depends on the gun or the effects.

## Using PortalKit in Your Own Game

### Portals only

1. Copy `Assets/PortalKit/Core`.
2. Drop `Core/Prefabs/Portal.prefab` into the scene twice and set each one's **Linked Portal** to the other.
3. Add **Portal Renderer** to your main camera.
4. Add **Portal Traveller** (or a subclass) to every object that should pass through, and set its **Graphics Object**. Use the `Slice` shader on its materials so it is cut cleanly at the portal.
5. Moving a portal from code: call `portal.PlaceAt(position, rotation)`.

- A traveller also needs a `Collider` (or `CharacterController`) so the portal's trigger can detect it.
- Portals find the player camera via `Camera.main`, so that camera needs the `MainCamera` tag and a `PortalRenderer` component.

### Your own character controller

Override `PortalTraveller.Teleport` and use `PortalTransformUtility`:

```csharp
public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
{
    transform.position = pos;
    Quaternion mapped = PortalTransformUtility.TransformRotation(fromPortal, toPortal, cameraTransform.rotation);
    UprightLook look = PortalTransformUtility.ClosestUprightLook(mapped, pitchLimits);
    // Apply look.yaw / look.pitch to your controller; ease look.residual to identity over a few frames.
    velocity = PortalTransformUtility.TransformDirection(fromPortal, toPortal, velocity);
}
```

`Samples/Scripts/FPSController.cs` is a complete example.

### Adding the portal gun

1. Also copy `Assets/PortalKit/Gun`.
2. Add a `PortalGrid` to the scene and build portal-able surfaces from `Gun/Prefabs/PortalableTiles`.
3. Add `PortalGun` under your camera and assign the two portals, `Portalable Mask` and `Shot Mask`.
4. React to shots from your own code with `gun.Fired += shot => ...`, or poll `gun.CanPlace(slot)`.

- To drive firing from your own input instead of the built-in mouse handling, set `gun.inputEnabled = false` and call `gun.FireBlue()` / `gun.FireOrange()` (or `gun.Fire(PortalSlot.Blue)`).

### Adding the effects

1. Also copy `Assets/PortalKit/VFX`.
2. Put `PortalShotVfx` and `PortalCrosshair` on the gun's GameObject, `PortalGunViewModel` on the gun model under it, and `PortalRim` + `PortalOpenVfx` on each portal. They find the gun/portal themselves.

- `PortalGunViewModel` expects a layer named `ViewModel`; without it, it logs a warning and the gun model can clip into walls.

### Removing the effects

Delete `Assets/PortalKit/VFX` (and `Samples`, which uses them). Core and Gun still compile and work.

## Running the Tests

Open **Window → General → Test Runner**, select **EditMode** and press **Run All**. The tests cover the portal travel math (`PortalTransformUtility`), the aperture mesh and `Portal.PlaceAt`.

## Credits

- **Portal rendering core** — [Sebastian Lague's Portals project](https://github.com/SebLague/Portals), MIT.
- **Portal gun model** — ["Portal Gun" on Sketchfab](https://sketchfab.com/3d-models/portal-gun-b0260066ba2c4e80aba4d1d8717d9fd9).

## License

[MIT](License).
