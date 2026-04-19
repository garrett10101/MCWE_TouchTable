using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace WatershedSim
{
    /// <summary>
    /// Routes all touch/mouse input to either camera control or simulation interaction.
    ///
    /// Modes:
    ///   Camera    — 1-finger orbit, 2-finger pinch zoom (default)
    ///   Water     — tap/hold to inject water at the terrain hit point
    ///   Dam       — tap to raise an obstacle; tap again in Remove mode to clear it
    ///   Pollutant — tap to inject a pollution plume; configurable color
    ///   Remove    — tap to remove the nearest obstacle or pollutant marker
    ///
    /// Attach to the same GameObject as WaterSimulation (or any active GameObject).
    /// </summary>
    public class WatershedInteraction : MonoBehaviour
    {
        public enum InteractionMode { Camera, Water, Dam, Pollutant, Remove }

        [Header("References")]
        public WaterSimulation waterSim;
        public WatershedCamera watershedCamera;

        [Tooltip("Camera used for raycasting to terrain. Usually the Main Camera child of CameraRig.")]
        public Camera mainCamera;

        [Tooltip("LayerMask for terrain raycasting. Set to include your Terrain layer.")]
        public LayerMask terrainLayerMask = ~0;

        [Header("Interaction Settings")]
        [Tooltip("Current interaction mode. Set via UI buttons or the SetMode() method.")]
        public InteractionMode currentMode = InteractionMode.Camera;

        [Tooltip("Radius in grid cells affected by a single touch/click (brush size).")]
        [Range(1, 8)] public int brushRadius = 2;

        [Tooltip("Water depth added per second when holding in Water mode.")]
        [Range(0.1f, 5f)] public float waterFlowRate = 1f;

        [Tooltip("Height of dams placed in Dam mode (metres above terrain).")]
        [Range(5f, 50f)] public float damHeight = 15f;

        [Tooltip("Pollution mass injected per second when holding in Pollutant mode.")]
        [Range(0.1f, 3f)] public float pollutionRate = 0.5f;

        [Tooltip("Color used when injecting pollution via touch.")]
        public Color pollutionColor = new Color(0.6f, 0.2f, 0.0f);

        // ── Private State ──────────────────────────────────────────────────────

        // Touch tracking for camera delegation
        int     _touch0Id = -1, _touch1Id = -1;
        Vector2 _prevTouch0, _prevTouch1;
        float   _prevPinchDist;

        // Mouse tracking for camera delegation
        Vector2 _prevMousePos;
        bool    _mouseDown;

        bool _interactionActive; // true while touch/mouse is held for sim interaction

        // ── Public API ────────────────────────────────────────────────────────

        public void SetMode(InteractionMode mode)
        {
            currentMode = mode;
            // Let camera handle its own orbit only in Camera mode
            if (watershedCamera != null)
                watershedCamera.handleOwnInput = (mode == InteractionMode.Camera);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        void Start()
        {
            if (watershedCamera != null)
                watershedCamera.handleOwnInput = (currentMode == InteractionMode.Camera);
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        void Update()
        {
            if (currentMode == InteractionMode.Camera)
                return; // WatershedCamera handles its own input

            HandleMouseInteraction();
            HandleTouchInteraction();
        }

        // ── Mouse Input ────────────────────────────────────────────────────────

        void HandleMouseInteraction()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                if (IsPointerOverUI(screenPos)) return;
                _mouseDown = true;
                _prevMousePos = screenPos;
                OnInteractBegin(screenPos);
            }

            if (_mouseDown && mouse.leftButton.isPressed)
            {
                OnInteractHold(mouse.position.ReadValue(), Time.deltaTime);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
                _mouseDown = false;

            // Right mouse → always orbit (shortcut, regardless of mode)
            HandleMouseOrbit(mouse);

            // Scroll → zoom
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && watershedCamera != null)
                watershedCamera.ExternalZoom(-scroll * watershedCamera.zoomSpeed * 0.05f);
        }

        void HandleMouseOrbit(Mouse mouse)
        {
            if (mouse.rightButton.wasPressedThisFrame) _prevMousePos = mouse.position.ReadValue();

            if (mouse.rightButton.isPressed && watershedCamera != null)
            {
                Vector2 pos   = mouse.position.ReadValue();
                Vector2 delta = pos - _prevMousePos;
                _prevMousePos = pos;
                watershedCamera.ExternalOrbit(delta);
            }
        }

        // ── Touch Input ────────────────────────────────────────────────────────

        void HandleTouchInteraction()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;

            Vector2 t0Pos = Vector2.zero, t1Pos = Vector2.zero;
            bool t0Active = false, t1Active = false;

            foreach (var t in ts.touches)
            {
                var phase = t.phase.ReadValue();
                int id    = t.touchId.ReadValue();
                Vector2 pos = t.position.ReadValue();

                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (_touch0Id < 0) { _touch0Id = id; _prevTouch0 = pos; }
                    else if (_touch1Id < 0 && id != _touch0Id) { _touch1Id = id; _prevTouch1 = pos; _prevPinchDist = Vector2.Distance(_prevTouch0, pos); }
                }
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    if (id == _touch0Id) { _touch0Id = -1; _touch1Id = -1; _interactionActive = false; }
                    else if (id == _touch1Id) _touch1Id = -1;
                }

                bool active = phase == UnityEngine.InputSystem.TouchPhase.Began
                           || phase == UnityEngine.InputSystem.TouchPhase.Moved
                           || phase == UnityEngine.InputSystem.TouchPhase.Stationary;

                if (!t0Active && active && id == _touch0Id) { t0Pos = pos; t0Active = true; }
                else if (t0Active && !t1Active && active && id == _touch1Id) { t1Pos = pos; t1Active = true; }
            }

            if (t0Active && t1Active)
            {
                // 2-finger pinch → always zoom camera
                float dist = Vector2.Distance(t0Pos, t1Pos);
                if (watershedCamera != null)
                    watershedCamera.ExternalZoom(-(dist - _prevPinchDist) * watershedCamera.zoomSpeed * 0.01f);
                _prevPinchDist = dist;
                _prevTouch0 = t0Pos;
                _prevTouch1 = t1Pos;
                _interactionActive = false;
            }
            else if (t0Active)
            {
                // Single finger → interact with simulation
                if (!_interactionActive)
                {
                    if (!IsPointerOverUI(t0Pos))
                    {
                        _interactionActive = true;
                        OnInteractBegin(t0Pos);
                    }
                }
                else
                {
                    OnInteractHold(t0Pos, Time.deltaTime);
                }
                _prevTouch0 = t0Pos;
            }
        }

        // ── Interaction Logic ─────────────────────────────────────────────────

        void OnInteractBegin(Vector2 screenPos)
        {
            if (!TryRaycastTerrain(screenPos, out Vector3 worldHit)) return;
            if (waterSim == null) return;

            var (gx, gz) = waterSim.WorldToGrid(worldHit);

            switch (currentMode)
            {
                case InteractionMode.Dam:
                    ApplyBrush(gx, gz, (cx, cz) => waterSim.AddObstacle(cx, cz, damHeight));
                    break;

                case InteractionMode.Remove:
                    ApplyBrush(gx, gz, (cx, cz) =>
                    {
                        waterSim.RemoveObstacle(cx, cz);
                    });
                    break;

                // Water and Pollutant also handled continuously in Hold
            }
        }

        void OnInteractHold(Vector2 screenPos, float dt)
        {
            if (!TryRaycastTerrain(screenPos, out Vector3 worldHit)) return;
            if (waterSim == null) return;

            var (gx, gz) = waterSim.WorldToGrid(worldHit);

            switch (currentMode)
            {
                case InteractionMode.Water:
                    ApplyBrush(gx, gz, (cx, cz) => waterSim.AddWater(cx, cz, waterFlowRate * dt));
                    break;

                case InteractionMode.Pollutant:
                    ApplyBrush(gx, gz, (cx, cz) =>
                    {
                        waterSim.AddWater(cx, cz, 0.02f * dt);           // tiny carrier
                        waterSim.InjectPollution(cx, cz, pollutionRate * dt, pollutionColor);
                    });
                    break;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        bool TryRaycastTerrain(Vector2 screenPos, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (mainCamera == null) return false;

            Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 10000f, terrainLayerMask))
            {
                hitPoint = hit.point;
                return true;
            }
            return false;
        }

        void ApplyBrush(int centerX, int centerZ, System.Action<int, int> action)
        {
            for (int dz = -brushRadius; dz <= brushRadius; dz++)
                for (int dx = -brushRadius; dx <= brushRadius; dx++)
                    if (dx * dx + dz * dz <= brushRadius * brushRadius)
                        action(centerX + dx, centerZ + dz);
        }

        static bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            return results.Count > 0;
        }
    }
}
