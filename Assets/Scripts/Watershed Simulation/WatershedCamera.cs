using UnityEngine;
using UnityEngine.InputSystem;

namespace WatershedSim
{
    /// <summary>
    /// Orbit + zoom camera for the Watershed Simulation scene.
    /// Attach to a CameraRig (empty parent). The Main Camera is a child offset on local -Z.
    ///
    /// WatershedInteraction drives this camera when the user is in Camera mode.
    /// Call ExternalOrbit / ExternalZoom from WatershedInteraction to move the camera.
    /// </summary>
    public class WatershedCamera : MonoBehaviour
    {
        [Header("Orbit")]
        [Range(0.05f, 2f)]  public float orbitSpeed   = 0.25f;
        [Range(0f,   89f)]  public float minPitch      = 15f;
        [Range(1f,   90f)]  public float maxPitch      = 75f;

        [Header("Zoom")]
        [Range(0.5f, 20f)]  public float zoomSpeed    = 3f;
        [Range(1f,   200f)] public float minDistance   = 30f;
        [Range(50f,  800f)] public float maxDistance   = 400f;

        [Header("Pivot")]
        [Tooltip("World-space point the camera orbits around.")]
        public Vector3 pivotPoint = Vector3.zero;

        /// <summary>When false, this script's own input handling is skipped.
        /// WatershedInteraction sets this to false when in non-camera modes
        /// and calls ExternalOrbit / ExternalZoom directly.</summary>
        [HideInInspector] public bool handleOwnInput = true;

        // ── State ──────────────────────────────────────────────────────────────

        float _yaw      = 0f;
        float _pitch    = 45f;
        float _distance;

        // Touch tracking (used when handleOwnInput = true)
        int     _touch0Id = -1, _touch1Id = -1;
        Vector2 _prevTouch0, _prevTouch1;
        float   _prevPinchDist;
        Vector2 _prevMousePos;
        bool    _mouseDown;

        Transform _cam;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        void Awake()
        {
            if (transform.childCount > 0)
                _cam = transform.GetChild(0);
            _distance = (minDistance + maxDistance) * 0.5f;
            ApplyTransform();
        }

        void Update()
        {
            if (handleOwnInput)
            {
                HandleMouse();
                HandleTouch();
            }
            ApplyTransform();
        }

        // ── Public API (called by WatershedInteraction) ────────────────────────

        public void ExternalOrbit(Vector2 pixelDelta) => Orbit(pixelDelta);
        public void ExternalZoom(float delta)          => Zoom(delta);

        public void SetPivot(Vector3 worldPoint)
        {
            pivotPoint = worldPoint;
            transform.position = worldPoint;
        }

        // ── Own Input Handling (Camera mode only) ──────────────────────────────

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)  { _mouseDown = true;  _prevMousePos = mouse.position.ReadValue(); }
            if (mouse.leftButton.wasReleasedThisFrame)   _mouseDown = false;

            if (_mouseDown)
            {
                Vector2 pos = mouse.position.ReadValue();
                Orbit(pos - _prevMousePos);
                _prevMousePos = pos;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                Zoom(-scroll * zoomSpeed * 0.05f);
        }

        void HandleTouch()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;

            Vector2 t0Pos = Vector2.zero, t1Pos = Vector2.zero;
            bool t0Active = false, t1Active = false;

            foreach (var t in ts.touches)
            {
                var phase = t.phase.ReadValue();
                bool active = phase == UnityEngine.InputSystem.TouchPhase.Began
                           || phase == UnityEngine.InputSystem.TouchPhase.Moved
                           || phase == UnityEngine.InputSystem.TouchPhase.Stationary;
                int     id  = t.touchId.ReadValue();
                Vector2 pos = t.position.ReadValue();

                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (_touch0Id < 0)                       { _touch0Id = id; _prevTouch0 = pos; }
                    else if (_touch1Id < 0 && id != _touch0Id) { _touch1Id = id; _prevTouch1 = pos; _prevPinchDist = Vector2.Distance(_prevTouch0, pos); }
                }
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    if (id == _touch0Id) { _touch0Id = -1; _touch1Id = -1; }
                    else if (id == _touch1Id) _touch1Id = -1;
                }

                if (!t0Active && active && id == _touch0Id) { t0Pos = pos; t0Active = true; }
                else if (t0Active && !t1Active && active && id == _touch1Id) { t1Pos = pos; t1Active = true; }
            }

            if (t0Active && !t1Active)
            {
                Orbit(t0Pos - _prevTouch0);
                _prevTouch0 = t0Pos;
            }
            else if (t0Active && t1Active)
            {
                float dist  = Vector2.Distance(t0Pos, t1Pos);
                Zoom(-(dist - _prevPinchDist) * zoomSpeed * 0.01f);
                _prevPinchDist = dist;
                _prevTouch0 = t0Pos;
                _prevTouch1 = t1Pos;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void Orbit(Vector2 delta)
        {
            _yaw   += delta.x * orbitSpeed;
            _pitch  = Mathf.Clamp(_pitch - delta.y * orbitSpeed, minPitch, maxPitch);
        }

        void Zoom(float delta)
        {
            _distance = Mathf.Clamp(_distance + delta, minDistance, maxDistance);
        }

        void ApplyTransform()
        {
            transform.position = pivotPoint;
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            if (_cam != null)
                _cam.localPosition = new Vector3(0f, 0f, -_distance);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
            float sz = 5f;
            Gizmos.DrawLine(pivotPoint - Vector3.right   * sz, pivotPoint + Vector3.right   * sz);
            Gizmos.DrawLine(pivotPoint - Vector3.forward * sz, pivotPoint + Vector3.forward * sz);
            Gizmos.DrawWireSphere(pivotPoint, sz * 0.4f);
        }
    }
}
