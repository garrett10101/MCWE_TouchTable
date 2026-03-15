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
3. Open **Tools → TouchTable → Build Manager** to configure scenes and trigger builds directly from the Editor.
4. Open any scene from `Assets/Scenes/` and press **Play** to test in the editor.

> **Input:** all scenes support both mouse/keyboard (PC) and multi-touch (mobile) via the New Input System.

---

## Build & Run

### Using the Build Manager (Editor GUI)

Open via **Tools → TouchTable → Build Manager**.

| Tab | Purpose |
|-----|---------|
| **Scene Setup** | Discover scenes, toggle which are included in builds, apply to File → Build Settings |
| **PC Build** | Select Linux / Windows / macOS target, choose output folder, click Build |
| **Mobile Build** | Android: refresh connected devices, build APK, optionally install via adb. iOS: build Xcode project (macOS only) |

> The headless CLI scripts (`PCBuilder.cs`, `AndroidBuilder.cs`) are still available for CI/automated use — see sections below.

---

### Play in Editor (all platforms)

1. Open the project in Unity Hub.
2. Open **Tools → TouchTable → Build Manager → Scene Setup** and click **Refresh**, then **Apply to Build Settings**.
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

## Map Markers

Map markers use the `PopupText_With_Picture` component — no code required to add or update content.

**To create a marker:**
1. Right-click in Hierarchy → **Create Empty** → rename (e.g. `SpringLake-Video`).
2. **Add Component → Sprite Renderer**
3. **Add Component → Box Collider 2D**
4. **Add Component → PopupText_With_Picture**
5. In the Inspector set:
   - **Marker Sprite** — drag any circle/pin sprite (falls back to Unity's built-in Knob if empty)
   - **Marker Color** — color picker
   - **Popup Picture** — optional image sprite shown at the top of the popup
   - **Text File** — drag a `.txt` TextAsset from Project (takes priority over Popup Text)
   - **Popup Text** — or type content directly in the text area
6. Position the marker on the map in the Scene view.
7. Press **Play** — clicking the marker shows a scrollable popup near the tap point.

The popup panel is built at runtime and shared across all markers. Clicking the background or the X button closes it.

> Run **Tools → TouchTable → Setup Map Popup** once to remove any legacy `PopupPanel`/`PopupManager` objects from an older scene.

### Video Map Markers

Video markers use the `PopupVideo` component — no code required.

**To create a video marker:**
1. Right-click in Hierarchy → **Create Empty** → rename (e.g. `SpringLake-Video`)
2. **Add Component → Sprite Renderer**
3. **Add Component → Box Collider 2D**
4. **Add Component → PopupVideo**
5. In the Inspector set:
   - **Marker Sprite** — drag any circle/pin sprite (falls back to Unity's built-in Knob)
   - **Marker Color** — color picker
   - **Popup Width / Height** — size of the dark popup panel (default 640 × 520)
   - **Position Mode** — `NearMarker` opens popup near tap point; `Centered` always centers on screen
   - **Tap Offset** — nudge the popup away from the finger (NearMarker only; default 20, 20)
   - **Video Clip** — drag a `VideoClip` asset from the Project window
   - **Video Width / Height** — bounding box for the video inside the popup (default 616 × 346 for 16:9).
     Set these to match your clip's native resolution for best quality (e.g. 1280 × 720 for 720p).
   - **Loop Video** — if enabled, video loops until the user closes the popup
   - **Close On End** — if enabled (and Loop Video is off), popup auto-closes when playback finishes
   - **Show Title** — toggle a header text strip above the video
   - **Title Text** — text for the header (leave empty to suppress it even if Show Title is on)
   - **Title Font Size / Color / Header Height** — appearance tweaks
6. Position the marker GameObject on the map in the Scene view.
7. Press **Play** — clicking/tapping the marker shows the video popup.

> **Note:** `Close On End` has no effect when `Loop Video` is also enabled — Unity does not fire
> the end-of-video event for looping clips. A warning is logged in the Console if both are set.
>
> Each video marker owns its own hidden popup panel built in `Start()`.
> The `VideoPlayer` and `RenderTexture` are created once per marker — not per click.
> Recommended maximum `RenderTexture` size: 1280 × 720 per marker.

### Image Slider Map Markers

Image slider markers use the `PopupImageSlider` component — no code required.

**To create an image slider marker:**
1. Right-click in Hierarchy → **Create Empty** → rename (e.g. `SpringLake-Photos`)
2. **Add Component → Sprite Renderer**
3. **Add Component → Box Collider 2D**
4. **Add Component → PopupImageSlider**
5. In the Inspector set:
   - **Marker Sprite** — drag any circle/pin sprite (falls back to Unity's built-in Knob)
   - **Marker Color** — color picker
   - **Popup Width / Height** — size of the dark popup panel (default 620 × 680)
   - **Position Mode** — `NearMarker` opens near tap; `Centered` always centers on screen
   - **Tap Offset** — nudge popup away from the finger (NearMarker only; default 20, 20)
   - **Slides** — expand the array; set **Size** to the number of images; for each element:
     - Drag a **Sprite** (photo/image asset from Project window) into **Picture**
     - Optionally type a **Caption** in the text area — shown below the image when the slider is near that image
   - **Show Captions** — global toggle; when off, the caption area is hidden for all slides
   - **Caption Font Size / Color / Height** — appearance of the caption strip
   - **Slider Height / Colors** — visual style of the slider bar and handle
6. Position the marker GameObject on the map in the Scene view.
7. Press **Play** — clicking/tapping the marker opens the popup. Drag the slider left/right to crossfade between images.

> The slider crossfades smoothly between adjacent images. The caption updates to the nearest slide's
> text as the slider moves. A slide with no caption hides the caption box automatically.
> When only one image is assigned, the slider is hidden.
> Each marker owns its own hidden popup panel built at startup — not per click.

---

## Project Structure

```
Assets/
  Editor/
    TouchTableBuildWindow.cs  # Interactive Build Manager (Tools → TouchTable → Build Manager)
    AndroidBuilder.cs         # Headless Android APK build (-executeMethod AndroidBuilder.Build)
    PCBuilder.cs              # Headless PC standalone build (-executeMethod PCBuilder.Build)
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
