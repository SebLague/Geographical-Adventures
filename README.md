# Planetary Explorer & Interaction Controller

This is a transformed version of Sebastian Lague's Geographical Adventures project, converting it from an airplane-based delivery simulator into a **third-person planetary exploration game**.

## Features Implemented
* **Spherical Locomotion**: A physics-aligned, surface-aware character controller that allows smooth movement across the globe, traversing both the equator and poles without gimbal-lock or rotation suddenness.
* **Planetary Camera System**: A custom third-person orbit camera system using SmoothDamp interpolation for stability at the poles. Supports drag controls for free look.
* **Artifact Interaction System**: A reusable framework supporting artifact pickup, socket attachment, weight-based speed reduction, dropping, and terrain surface normal alignment.
* **Complete UI/UX Redesign**: A futuristic dark HUD showing current region/country, precise coordinates (Lat/Lon), dynamic compass, and status of carried artifacts.
* **Architecture Integrity**: Clean separation of concerns using ScriptableObjects, interfaces, and decoupling techniques.

---

## Setup & Run Instructions
1. Open Unity Hub and add the project folder.
2. Ensure you are using **Unity 2021.3 LTS** or newer.
3. Open the main scene located at `Assets/Scenes/Game.unity`.
4. Press **Play** in the Unity Editor to test.

## Controls
* **W, A, S, D / Arrow Keys**: Move the character relative to the camera direction.
* **Mouse Drag (Left/Right Click)**: Orbit the camera around the character.
* **Space / Drop Action**: Drop quest delivery packages.
* **E**: Interact with objects (Pick up / Drop artifacts).
* **Esc**: Pause Menu.
* **M**: Map View.

---

## Architecture Overview
* **Player.cs**: Manages spherical third-person movement, state, and interaction handling.
* **GameCamera.cs**: Smoothly orbits and follows the player.
* **Artifact.cs & ArtifactData.cs**: Define interaction physics and ScriptableObject architecture.
* **PlanetaryHUD.cs**: Implements the Futuristic Surveyor UI overlay.
* **PlanetaryExplorerSetup.cs**: Procedurally instantiates the HUD and spawns test artifacts in the world.
