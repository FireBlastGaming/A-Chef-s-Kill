# Player Grounding Notes

This document captures an overview of how the current `PlayerMovement` script handles
movement and ground detection, plus in-editor setup steps to ensure the ground layer
is configured correctly.

## Script overview

* `PlayerMovement` drives all horizontal locomotion through `_rb` (a `Rigidbody2D`).
* Horizontal velocity is applied in `Move`, which interpolates `_moveVelocity`
  toward either the walking or sprinting target speeds depending on `InputManager.SprintIsHeld`.
* Jump logic tracks buffered jumps, coyote time, multi-jump counts, apex hang
  calculations, and fast-fall behaviour. Vertical velocity is ultimately written back
  to `_rb.linearVelocity` each physics frame.
* Ground checks originate from the feet collider (`_feetColl`), casting a box down
  using `MoveStats.GroundDetectionRayLength` and restricted to `MoveStats.GroundLayer`.
* Head checks start from the body collider to detect ceiling collisions, using the
  same layer mask.

Because the checks use `MoveStats.GroundLayer`, every surface the character should
stand on must be assigned to that layer (or included in the layer mask).

## Unity setup checklist

1. **Player**
   * Ensure the player object owns a `Rigidbody2D` (gravity enabled, not kinematic) and
     the foot/body colliders referenced in the script.
   * Verify the colliders are sized so the feet collider extends slightly below the
     sprite; this gives the box cast a reliable origin.

2. **Ground objects**
   * Create or select a dedicated layer (e.g., `Ground`) under *Edit ▸ Project Settings ▸ Tags and Layers*.
   * Assign that layer to every walkable surface: tilemaps, platforms, sloped meshes, etc.
   * Confirm the colliders on those objects are **not** marked *Is Trigger*.

3. **Layer mask asset**
   * Open the `Player Movement Stats` asset and make sure `GroundLayer` includes the
     chosen `Ground` layer. Multiple layers can be combined if the player must stand on
     more than one category of surface.

4. **Physics matrix**
   * In *Edit ▸ Project Settings ▸ Physics 2D*, confirm the collision matrix allows the
     player's layer to interact with the `Ground` layer.

5. **Testing**
   * Enter play mode and watch the `PlayerMovement` component in the Inspector. When
     standing still on the floor, `_isGrounded` should report `true`. If it does not,
     verify the ground object's layer assignment and that the ground collider intersects
     the `GroundLayer` mask.

These steps align the scene configuration with the expectations baked into the script,
preventing the character from falling through walkable surfaces.
