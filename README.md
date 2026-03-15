# TouchTable

A Unity 2D test project demonstrating multi-touch and mouse input on both **PC** and **Android/iOS mobile**. Contains four scenes:

| Scene | Description |
|-------|-------------|
| **Main Menu** | Navigation hub with buttons to each scene |
| **Touch Pop** | Whack-a-mole — tap/click red circles before they expire, score tracked in real time |
| **Scrollable Text** | Popup panel with long scrollable text, controllable via mouse wheel, drag, or touch |
| **Background Blend** | Slider blends between four time-of-day backgrounds (morning → night) |

---

## Requirements

- **Unity 6000.3.11f1** (or later 6.x LTS) — install via [Unity Hub](https://unity.com/download)
- **New Input System** package — included in `Packages/manifest.json`
- **Android Build Support** module — required for Android builds only (Unity Hub → Installs → your version → Add Modules)

---

## Editing the Project

1. Open **Unity Hub** and click **Open → Add project from disk**, select this folder.
2. Unity opens the project and compiles scripts automatically.
3. In the menu bar run **Tools → TouchTable → Build All Scenes** to (re)generate all scene files and the Target prefab after any script or layout change.
4. Open any scene from `Assets/Scenes/` and press **Play** to test in the editor.

> **Input:** all scenes support both mouse/keyboard (PC) and multi-touch (mobile) via the New Input System.

---

## Build & Run

### Play in Editor (all platforms)

1. Open the project in Unity Hub.
2. Run **Tools → TouchTable → Build All Scenes**.
3. Open `Assets/Scenes/MainMenu.unity`.
4. Press **Play**.

---

### PC Standalone

#### Linux
```bash
~/Unity/Hub/Editor/6000.3.11f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod PCBuilder.Build \
  -quit \
  -logFile /tmp/unity_pc_build.log

# Run the build
/tmp/TouchTable-PC/TouchTable
```

#### macOS
```bash
/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod PCBuilder.Build \
  -quit \
  -logFile /tmp/unity_pc_build.log
```

#### Windows
```bat
"C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" ^
  -batchmode -nographics ^
  -projectPath C:\path\to\TouchTable ^
  -executeMethod PCBuilder.Build ^
  -quit ^
  -logFile C:\Temp\unity_pc_build.log
```

> `PCBuilder.cs` targets `StandaloneLinux64` by default. Change `BuildTarget.StandaloneLinux64` in `Assets/Editor/PCBuilder.cs` to `StandaloneWindows64` or `StandaloneOSX` before building on other platforms.

---

### Android

#### Requirements
- Android Build Support module installed in Unity Hub.
- Android device with **USB Debugging** enabled (Settings → Developer Options → USB Debugging).

```bash
# Build APK (close Unity editor first)
~/Unity/Hub/Editor/6000.3.11f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /path/to/TouchTable \
  -executeMethod AndroidBuilder.Build \
  -quit \
  -logFile /tmp/unity_android_build.log

# Install on connected device
adb install -r /tmp/TouchTable.apk

# Launch on device
adb shell monkey -p com.touchtable.demo -c android.intent.category.LAUNCHER 1
```

> **ADB location** if not on PATH:
> `<UnityEditorRoot>/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`

---

### iOS

> Requires **macOS + Xcode**.

1. In Unity open **File → Build Settings**, select **iOS**, click **Switch Platform**.
2. Click **Build** and choose an output folder (e.g. `Build/iOS`).
3. Open the generated `.xcodeproj` in Xcode.
4. Under **Signing & Capabilities** select your development team.
5. Connect your device and press **Run** (or **Product → Archive** to distribute).

---

## Project Structure

```
Assets/
  Editor/
    SceneBuilder.cs       # Generates all scenes — run via Tools → TouchTable → Build All Scenes
    AndroidBuilder.cs     # Headless Android APK build (-executeMethod AndroidBuilder.Build)
    PCBuilder.cs          # Headless PC standalone build (-executeMethod PCBuilder.Build)
  Prefabs/
    Target.prefab         # Red circle used in Touch Pop scene
  Resources/
    ScrollableText.txt    # Text content displayed in the Scrollable Text scene
  Scenes/
    MainMenu.unity
    TouchPop.unity
    ScrollableText.unity
    BackgroundBlend.unity
  Scripts/
    TouchPop/
      TouchPopManager.cs  # Spawns targets, handles multi-touch and mouse input, tracks score
      TouchTarget.cs      # Component on each target
    ScrollableText/
      ScrollableTextController.cs  # Opens/closes popup, loads text content
    BackgroundBlend/
      BackgroundBlendController.cs # Drives slider to blend background images
    SceneLoader.cs        # Loads scenes by name and quits the app
  Sprites/
    Target.png
    Background_Morning.png
    Background_Afternoon.png
    Background_Evening.png
    Background_Night.png
```
