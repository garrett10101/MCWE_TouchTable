# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6000.3.11f1 2D project demonstrating multi-touch/mouse input on PC and Android. Four interactive scenes: MainMenu, TouchPop (whack-a-mole), ScrollableText (scrollable popup), and BackgroundBlend (slider-driven day/night transitions).

## Build Commands

### Build Manager (in Unity Editor)
Open **Tools → TouchTable → Build Manager** — an interactive EditorWindow with three tabs:
- **Scene Setup** — discover/toggle scenes, apply to EditorBuildSettings
- **PC Build** — select Linux x64 / Windows x64 / macOS, set output folder, click Build
- **Mobile Build** — Android: refresh adb devices, build APK, optionally install; iOS: build Xcode project (macOS only)

### PC Standalone (headless)
```bash
# Linux
~/Unity/Hub/Editor/6000.3.11f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod PCBuilder.Build \
  -quit \
  -logFile /tmp/unity_pc_build.log

# Run the built binary
/tmp/TouchTable-PC/TouchTable
```
Change `BuildTarget.StandaloneLinux64` in `Assets/Editor/PCBuilder.cs` for Windows/macOS targets.

### Android APK (headless)
```bash
~/Unity/Hub/Editor/6000.3.11f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod AndroidBuilder.Build \
  -quit \
  -logFile /tmp/unity_android_build.log

adb install -r /tmp/TouchTable.apk
```
SDK/NDK/JDK paths in `Assets/Editor/AndroidBuilder.cs` are hardcoded to Linux Unity installation — adjust for macOS/Windows.

### iOS
Manual: File → Build Settings → iOS → Switch Platform → Build → open in Xcode.

## Architecture

### Scene Management
Scenes are maintained manually in the Unity Editor. Use the **Build Manager → Scene Setup** tab to manage which scenes are registered in EditorBuildSettings. Edit scenes directly in the Unity Editor and save them as usual.

### Input System
Uses the **New Input System exclusively** (package: `com.unity.inputsystem`). EventSystem uses `InputSystemUIInputModule`. Touch input via `Touchscreen.current.touches`, mouse via `Mouse.current`. No legacy `Input` class.

### Canvas Configuration
All canvases: Screen Space Overlay, reference resolution **1080×1920** (portrait), Scale With Screen Size at 50% width/height match.

### Script Roles
- `TouchTableBuildWindow.cs` — Interactive EditorWindow (Tools → TouchTable → Build Manager). Three tabs: Scene Setup (scan/toggle scenes, apply to EditorBuildSettings), PC Build (Linux/Windows/macOS standalone), Mobile Build (Android APK with adb device list + install; iOS Xcode project on macOS).
- `SceneLoader.cs` — Reusable component on every scene's canvas. Call `LoadSceneByName(name)` or `Quit()` from buttons.
- `TouchPopManager.cs` — Spawns/tracks targets, processes all touch/mouse input via `Physics2D.OverlapPoint` raycasts. `TouchTarget.cs` is a thin delegate that calls back into the manager.
- `ScrollableTextController.cs` — Shows/hides popup via `CanvasGroup` (not SetActive). Water conservation text is hardcoded as a constant in the file.
- `BackgroundBlendController.cs` — Alpha-blends an array of `Image` components based on slider value: `alpha = Clamp01(1 - |sliderValue - index|)`.
- `MapCameraFit.cs` / `MapMarker.cs` — Camera fitting and data holder for map markers.
- `PopupText_With_Picture.cs` — Self-contained marker + popup script (in `Assets/Scripts/Map/`). Add to any map marker GameObject; exposes `markerSprite`, `markerColor`, `popupPicture`, `textFile`, and `popupText` as Inspector fields. Builds the shared popup panel lazily at runtime (class `SharedPopupPanel` lives in the same file). Uses `IPointerClickHandler` + `Physics2DRaycaster` (auto-added if missing). One panel is shared across all markers in the scene. **Do not use `MarkerPopupController.cs`** — it has been deleted.
- `PopupVideo.cs` — Self-contained marker + video popup (in `Assets/Scripts/Map/`). Add alongside `SpriteRenderer` and `BoxCollider2D` on any map marker. Each instance owns its own hidden `VideoPopupPanel` (built in `Start()`), containing a `VideoPlayer` targeting a runtime-created `RenderTexture` displayed in a `RawImage`. Supports optional title header, configurable popup/video dimensions, `NearMarker` or `Centered` positioning, loop toggle, and close-on-end. No scene setup or editor script required. `closeOnEnd` has no effect when `loopVideo = true`.
- `PopupImageSlider.cs` — Self-contained marker + image-crossfade popup (in `Assets/Scripts/Map/`). Add alongside `SpriteRenderer` and `BoxCollider2D` on any map marker. Exposes an `ImageSlide[]` array (each entry: `picture` Sprite + `caption` string) and a global `showCaptions` bool. A draggable Unity `Slider` crossfades between adjacent images using two stacked `Image` components (bottom layer always alpha=1; top layer fades in). Caption text updates to the nearest slide as the slider moves. Supports `NearMarker` or `Centered` positioning. Slider hidden when only one slide is present. No scene setup or editor script required.

### Map Scene — Adding Markers
1. Create Empty GameObject, rename it (e.g. `SpringLake-Video`).
2. Add `Sprite Renderer`, `Box Collider 2D`, `PopupText_With_Picture`.
3. Set Inspector fields: Marker Sprite/Color, Popup Picture (optional), Text File or Popup Text.
4. Position on map in Scene view. Press Play — click marker to show popup.
5. Run **Tools → TouchTable → Setup Map Popup** only to clean up old `PopupPanel`/`PopupManager` objects.
- For video markers: use `PopupVideo` instead of `PopupText_With_Picture`. Assign a `VideoClip` asset and configure layout, title, and close behavior via the Inspector.
- For image-slider markers: use `PopupImageSlider` instead of `PopupText_With_Picture`. Assign `ImageSlide` array entries (Sprite + optional caption) and configure layout via the Inspector.

### Key Packages
- `com.unity.render-pipelines.universal` (URP 17.3.0) — required for 2D rendering
- `com.unity.inputsystem` (1.19.0) — required for all input
- `com.unity.ugui` (2.0.0) — UI toolkit
