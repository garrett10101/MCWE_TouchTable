# Mobile Touch Test Project

This repository contains the source files for a simple Unity 2D mobile interaction test.  The goal of this project is to demonstrate basic multi‑touch handling, UI scrolling, and slider‑driven blending between backgrounds.  It is organized into four scenes and a handful of reusable scripts and sprites.

## Features

* **Main Menu**: A launcher with buttons to open the test scenes.  See `SceneLoader.cs` for the helper component that loads scenes by name.

* **Touch Pop Test**: Implements a whack‑a‑mole style interaction where coloured targets spawn randomly and disappear when touched.  Supports simultaneous touches via the new Input System.  See `TouchPopManager.cs` and `TouchTarget.cs` for implementation details.

* **Scrollable Text Test**: Demonstrates a scrollable text popup controlled by UI buttons.  See `ScrollableTextController.cs`.

* **Background Slider Test**: Demonstrates blending between multiple backgrounds using a slider.  See `BackgroundBlendController.cs`.

## Assets

* `Assets/Sprites/Background_*.png` – Four background images (morning, afternoon, evening, night) used by the slider test.
* `Assets/Sprites/Target.png` – A simple red circle sprite used for the touch pop targets.

## Scripts

| Script | Purpose |
| --- | --- |
| `SceneLoader.cs` | Helper for loading scenes and quitting the application from UI buttons. |
| `TouchPopManager.cs` | Spawns touch targets, handles multi‑touch input via overlap checks, despawns expired targets and updates the score. |
| `TouchTarget.cs` | Lightweight component attached to each target so it knows which manager spawned it. |
| `ScrollableTextController.cs` | Controls opening and closing of a popup panel containing scrollable text.  Resets scroll position on open. |
| `BackgroundBlendController.cs` | Drives a slider to interpolate between layered backgrounds and updates a label with the current time of day. |

## Editor Utilities

| Script | Purpose |
| --- | --- |
| `SceneBuilder.cs` | Programmatically creates all four scenes and the Target prefab. Run via **Tools > TouchTable > Build All Scenes**. |
| `AndroidBuilder.cs` | Headless Android APK build. Run via `-executeMethod AndroidBuilder.Build`. |
| `PCBuilder.cs` | Headless Linux standalone build. Run via `-executeMethod PCBuilder.Build`. |

---

## Build & Run

### Requirements

* **Unity 6000.3.11f1** (or later 6.x LTS)
* **New Input System** package (included in `Packages/manifest.json`)
* **Android Build Support** module (for Android builds) — install via Unity Hub → Installs → Add Modules

---

### Play in Editor (all platforms)

1. Open the project in Unity Hub.
2. In the menu bar run **Tools > TouchTable > Build All Scenes** to generate all scenes and the Target prefab.
3. Open `Assets/Scenes/MainMenu.unity`.
4. Press **Play**.

---

### PC Standalone — Linux

```bash
/home/<user>/Unity/Hub/Editor/6000.3.11f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod PCBuilder.Build \
  -quit \
  -logFile /tmp/unity_pc_build.log

# Run the build
/tmp/TouchTable-PC/TouchTable
```

### PC Standalone — macOS

```bash
/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod PCBuilder.Build \
  -quit \
  -logFile /tmp/unity_pc_build.log
```

> **Note:** `PCBuilder.cs` targets `StandaloneLinux64` by default.  Change `BuildTarget.StandaloneLinux64` to `BuildTarget.StandaloneWindows64` or `BuildTarget.StandaloneOSX` before building on other platforms.

### PC Standalone — Windows

```bat
"C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" ^
  -batchmode -nographics ^
  -projectPath C:\path\to\TouchTable ^
  -executeMethod PCBuilder.Build ^
  -quit ^
  -logFile C:\Temp\unity_pc_build.log
```

---

### Android

#### Requirements
* Android Build Support module installed in Unity Hub.
* A device with **USB Debugging** enabled (Settings → Developer Options → USB Debugging).

```bash
# Build APK
/home/<user>/Unity/Hub/Editor/6000.3.11f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod AndroidBuilder.Build \
  -quit \
  -logFile /tmp/unity_android_build.log

# Install on connected device
adb install /tmp/TouchTable.apk

# Launch on device
adb shell am start -n "com.TouchTable.TouchTable/com.unity3d.player.UnityPlayerGameActivity"
```

> **ADB location** (if not on PATH):
> `<UnityEditorRoot>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`

---

### iOS

> **Requires macOS + Xcode.**

1. In Unity open **File > Build Settings**, select **iOS**, click **Switch Platform**.
2. Click **Build** and choose an output folder (e.g. `Build/iOS`).
3. Open the generated `.xcodeproj` in Xcode.
4. Select your development team under **Signing & Capabilities**.
5. Connect your device and press **Run** (or **Product > Archive** to distribute).

---

## Notes

* The project uses the **New Input System** exclusively.  The `EventSystem` in each scene uses `InputSystemUIInputModule`; the legacy `StandaloneInputModule` is not used.
* Touch and mouse input are both handled in `TouchPopManager` using `Touchscreen.current` and `Mouse.current`, so all scenes work on desktop and mobile without code changes.
* Scene files and the Target prefab are generated by `SceneBuilder.cs` and are committed to the repository.  Re-run **Tools > TouchTable > Build All Scenes** any time scripts or layout changes are made.
