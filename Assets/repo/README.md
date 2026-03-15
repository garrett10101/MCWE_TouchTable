# Mobile Touch Test Project

This repository contains the source files for a simple Unity 2D mobile interaction test.  The goal of this project is to demonstrate basic multi‑touch handling, UI scrolling, and slider‑driven blending between backgrounds.  It is organized into four scenes (not included as `.unity` files) and a handful of reusable scripts and sprites.

## Features

* **Main Menu**: A launcher with buttons to open the test scenes.  See `SceneLoader.cs` for the helper component that loads scenes by name.

* **Touch Pop Test**: Implements a whack‑a‑mole style interaction where coloured targets spawn randomly and disappear when touched.  Supports simultaneous touches via the new Input System.  See `TouchPopManager.cs` and `TouchTarget.cs` for implementation details.

* **Scrollable Text Test**: Demonstrates a scrollable text popup controlled by UI buttons.  The long body text can be configured in the inspector.  See `ScrollableTextController.cs`.

* **Background Slider Test**: Demonstrates blending between multiple backgrounds using a slider.  The nearest background is fully opaque while its neighbours fade in and out smoothly.  See `BackgroundBlendController.cs`.

## Assets

* `Assets/Sprites/Background_*.png` – Four background images (morning, afternoon, evening, night) used by the slider test.
* `Assets/Sprites/Target.png` – A simple red circle sprite used for the touch pop targets.

## Scripts

| Script | Purpose |
| --- | --- |
| `SceneLoader.cs` | Helper for loading scenes and quitting the application from UI buttons. |
| `TouchPopManager.cs` | Spawns touch targets, handles multi‑touch input via raycasts, despawns expired targets and updates the score. |
| `TouchTarget.cs` | Lightweight component attached to each target so it knows which manager spawned it. |
| `ScrollableTextController.cs` | Controls opening and closing of a popup panel containing a scrollable text.  Resets scroll position on open. |
| `BackgroundBlendController.cs` | Drives a slider to interpolate between layered backgrounds and updates a label with the current time of day. |

## Usage

1. **Import the project** into Unity (version 2020.3 or later recommended) by placing the `Assets` folder into your project.  Ensure that the Input System package is installed and enabled if you want multi‑touch support.
2. **Create scenes** corresponding to the Main Menu, Touch Pop Test, Scrollable Text Test and Background Slider Test.  Add them to the build settings.
3. **Assign scripts and assets**:
   - Add the `SceneLoader` component to an empty GameObject on the Main Menu and wire up your menu buttons’ `OnClick` events to call `LoadSceneByName` with the appropriate scene names.  You can also assign a `Quit` button to exit play mode.
   - In the Touch Pop Test scene, create a prefab for the target using `Sprites/Target.png` and a `Collider2D`.  Then add a `TouchPopManager` to an empty GameObject, assign the prefab, configure spawn area/lifetimes and link a UI Text for the score.
   - In the Scrollable Text Test scene, set up a popup panel with a `ScrollRect` and long text, then attach a `ScrollableTextController` to a convenient GameObject and assign the panel, scroll rect and text.  Hook up open/close buttons to call `OpenPopup` and `ClosePopup`.
   - In the Background Slider Test scene, create a Canvas with four `Image` objects for the backgrounds and a `Slider`.  Attach `BackgroundBlendController` to a GameObject and assign the images, slider and an optional label.  Provide human‑readable labels via the inspector if desired.

4. **Test** on a mobile device or in the editor (multi‑touch can be simulated with multiple touches in the device or with separate mouse clicks in the editor).

## Notes

This repository does not include prebuilt `.unity` scene files or package definitions.  It is intended as a starting point that you can integrate into an existing Unity project.  Feel free to modify the scripts or assets to suit your particular needs.