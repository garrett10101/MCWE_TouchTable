using System.Collections.Generic;
using UnityEngine;

namespace WatershedSim
{
    /// <summary>
    /// Virtual-pipe shallow-water simulation with pollution advection and obstacle layer.
    ///
    /// Assign a MonoBehaviour that implements ITerrainProvider to terrainProvider.
    /// WatershedTerrain does this out of the box; swap in any custom terrain by
    /// implementing the interface on your own script.
    /// </summary>
    public class WaterSimulation : MonoBehaviour
    {
        [Header("Simulation Grid")]
        [Range(32, 128)] public int simResolution = 64;

        [Header("Physics")]
        public float gravity = 9.8f;
        [Range(0.1f, 2.0f)] public float pipeLength = 0.5f;
        [Range(0.9f, 1.0f)] public float damping    = 0.99f;
        [Range(1, 10)]      public int   stepsPerFrame = 3;

        [Header("Rain")]
        [Tooltip("Water depth added per second across all cells when rain is active.")]
        public float rainRate = 0f;

        [Header("References")]
        [Tooltip("A MonoBehaviour that implements ITerrainProvider (e.g. WatershedTerrain).")]
        [SerializeField] MonoBehaviour terrainProvider;

        // ── Public Read-Only Properties ───────────────────────────────────────

        public int   Resolution => _res;
        public float CellSize   => _cellSize;

        // ── Private State ─────────────────────────────────────────────────────

        ITerrainProvider _terrain;
        List<WaterSource>    _sources    = new();
        List<PollutionSource> _pollSources = new();

        // Core water arrays [z, x]
        float[,] _depth;
        float[,] _flowX;       // flow from (x,z) → (x+1,z)
        float[,] _flowZ;       // flow from (x,z) → (x,z+1)
        float[,] _terrainH;    // cached terrain heights (world Y)
        float[,] _obstacleH;   // added obstacle height per cell

        // Pollution arrays [z, x]
        float[,] _pollMass;      // pollution "depth" per cell
        Color[,] _pollColor;     // blended pollution color per cell

        // Pre-allocated work buffers for UpdatePollution — avoids per-frame GC
        float[,] _pollDelta;
        Color[,] _pollColorIn;
        float[,] _pollMassIn;

        bool  _rainOn;
        int   _res;
        float _cellSize;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        void Awake()
        {
            _terrain = terrainProvider as ITerrainProvider;
        }

        void Start()
        {
            _sources.AddRange(FindObjectsByType<WaterSource>(FindObjectsSortMode.None));
            _pollSources.AddRange(FindObjectsByType<PollutionSource>(FindObjectsSortMode.None));

            if (_terrain != null && !_terrain.IsReady)
                StartCoroutine(WaitForTerrainThenInit());
            else
                Initialise();
        }

        System.Collections.IEnumerator WaitForTerrainThenInit()
        {
            while (_terrain != null && !_terrain.IsReady)
                yield return null;
            Initialise();
        }

        void Initialise()
        {
            _res      = simResolution;
            _cellSize = (_terrain != null ? _terrain.WorldWidth : 500f) / (_res - 1);

            _depth     = new float[_res, _res];
            _flowX     = new float[_res, _res];
            _flowZ     = new float[_res, _res];
            _terrainH  = new float[_res, _res];
            _obstacleH = new float[_res, _res];
            _pollMass    = new float[_res, _res];
            _pollColor   = new Color[_res, _res];
            _pollDelta   = new float[_res, _res];
            _pollColorIn = new Color[_res, _res];
            _pollMassIn  = new float[_res, _res];

            for (int z = 0; z < _res; z++)
                for (int x = 0; x < _res; x++)
                    _pollColor[z, x] = Color.clear;

            if (_terrain != null)
            {
                for (int z = 0; z < _res; z++)
                    for (int x = 0; x < _res; x++)
                        _terrainH[z, x] = _terrain.SampleHeight(
                            (float)x / (_res - 1), (float)z / (_res - 1));
            }
        }

        void FixedUpdate()
        {
            if (_depth == null) return;

            float dt = Time.fixedDeltaTime / stepsPerFrame;
            for (int s = 0; s < stepsPerFrame; s++)
            {
                InjectSources(dt);
                InjectPollutionSources(dt);
                if (_rainOn) ApplyRain(dt);
                UpdateFlow(dt);
                UpdateDepth(dt);
                UpdatePollution(dt);
                DrainEdges(dt);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Add water at a grid cell (e.g. from touch input).</summary>
        public void AddWater(int gx, int gz, float amount)
        {
            if (_depth == null) return;
            gx = Mathf.Clamp(gx, 0, _res - 1);
            gz = Mathf.Clamp(gz, 0, _res - 1);
            _depth[gz, gx] += amount;
        }

        /// <summary>Raise an obstacle layer at this cell (simulates a dam or barrier).</summary>
        public void AddObstacle(int gx, int gz, float height = 15f)
        {
            if (_obstacleH == null) return;
            gx = Mathf.Clamp(gx, 0, _res - 1);
            gz = Mathf.Clamp(gz, 0, _res - 1);
            _obstacleH[gz, gx] = Mathf.Max(_obstacleH[gz, gx], height);
        }

        /// <summary>Remove obstacle at this grid cell.</summary>
        public void RemoveObstacle(int gx, int gz)
        {
            if (_obstacleH == null) return;
            gx = Mathf.Clamp(gx, 0, _res - 1);
            gz = Mathf.Clamp(gz, 0, _res - 1);
            _obstacleH[gz, gx] = 0f;
        }

        /// <summary>Inject a slug of pollution at this cell with the given color.</summary>
        public void InjectPollution(int gx, int gz, float amount, Color color)
        {
            if (_pollMass == null) return;
            gx = Mathf.Clamp(gx, 0, _res - 1);
            gz = Mathf.Clamp(gz, 0, _res - 1);
            float newMass = _pollMass[gz, gx] + amount;
            if (newMass > 0.001f)
                _pollColor[gz, gx] = Color.Lerp(_pollColor[gz, gx], color, amount / newMass);
            _pollMass[gz, gx] = newMass;
        }

        /// <summary>Clears all water, obstacles, and pollution. Optionally regenerates terrain.</summary>
        public void ClearAll(bool regenerateTerrain = false)
        {
            if (_depth == null) return;
            System.Array.Clear(_depth,     0, _depth.Length);
            System.Array.Clear(_flowX,     0, _flowX.Length);
            System.Array.Clear(_flowZ,     0, _flowZ.Length);
            System.Array.Clear(_obstacleH, 0, _obstacleH.Length);
            System.Array.Clear(_pollMass,  0, _pollMass.Length);
            for (int z = 0; z < _res; z++)
                for (int x = 0; x < _res; x++)
                    _pollColor[z, x] = Color.clear;

            if (regenerateTerrain && terrainProvider != null)
            {
                if (terrainProvider is WatershedTerrain wt)
                {
                    wt.GenerateTerrain();
                    for (int z = 0; z < _res; z++)
                        for (int x = 0; x < _res; x++)
                            _terrainH[z, x] = _terrain.SampleHeight(
                                (float)x / (_res - 1), (float)z / (_res - 1));
                }
            }
        }

        /// <summary>Convert a world position to the nearest grid indices.</summary>
        public (int gx, int gz) WorldToGrid(Vector3 worldPos)
        {
            Vector3 origin = _terrain != null ? _terrain.WorldOrigin : Vector3.zero;
            float   width  = _terrain != null ? _terrain.WorldWidth  : 500f;
            int gx = Mathf.RoundToInt((worldPos.x - origin.x) / width * (_res - 1));
            int gz = Mathf.RoundToInt((worldPos.z - origin.z) / width * (_res - 1));
            return (Mathf.Clamp(gx, 0, _res - 1), Mathf.Clamp(gz, 0, _res - 1));
        }

        /// <summary>Convert grid indices to the world-space centre of that cell.</summary>
        public Vector3 GridToWorld(int gx, int gz)
        {
            Vector3 origin = _terrain != null ? _terrain.WorldOrigin : Vector3.zero;
            float   width  = _terrain != null ? _terrain.WorldWidth  : 500f;
            float wx = origin.x + (float)gx / (_res - 1) * width;
            float wz = origin.z + (float)gz / (_res - 1) * width;
            float wy = GetSurfaceHeight(gx, gz);
            return new Vector3(wx, wy, wz);
        }

        // ── Per-cell queries ──────────────────────────────────────────────────

        public float   GetDepth(int x, int z)    => _depth == null ? 0f : _depth[Clamp(z), Clamp(x)];
        public Vector2 GetFlow(int x, int z)      => _flowX == null ? Vector2.zero
                                                    : new Vector2(_flowX[Clamp(z), ClampM(x)], _flowZ[ClampM(z), Clamp(x)]);
        public float   GetTerrainHeight(int x, int z) => _terrainH == null ? 0f : _terrainH[Clamp(z), Clamp(x)];
        public float   GetObstacleHeight(int x, int z) => _obstacleH == null ? 0f : _obstacleH[Clamp(z), Clamp(x)];

        public float GetPollutionConcentration(int x, int z)
        {
            if (_pollMass == null) return 0f;
            int gz = Clamp(z), gx = Clamp(x);
            float d = _depth[gz, gx];
            return d > 0.001f ? Mathf.Clamp01(_pollMass[gz, gx] / d) : 0f;
        }

        public Color GetPollutionColor(int x, int z)
        {
            if (_pollColor == null) return Color.clear;
            return _pollColor[Clamp(z), Clamp(x)];
        }

        /// <summary>Returns 0–1 overall pollution level (fraction of total water that is polluted).</summary>
        public float GetOverallPollutionLevel()
        {
            if (_depth == null) return 0f;
            double totalWater = 0, totalPoll = 0;
            for (int z = 0; z < _res; z++)
                for (int x = 0; x < _res; x++)
                {
                    totalWater += _depth[z, x];
                    totalPoll  += _pollMass[z, x];
                }
            return totalWater < 0.001 ? 0f : Mathf.Clamp01((float)(totalPoll / totalWater));
        }

        public bool HasObstacle(int gx, int gz)
        {
            if (_obstacleH == null) return false;
            return _obstacleH[Clamp(gz), Clamp(gx)] > 0.1f;
        }

        public void SetRain(bool on) => _rainOn = on;
        public void SetRainRate(float rate) => rainRate = Mathf.Max(0f, rate);
        public void SetDamping(float d) => damping = Mathf.Clamp(d, 0.9f, 1.0f);

        // ── Simulation Steps ──────────────────────────────────────────────────

        void InjectSources(float dt)
        {
            foreach (var src in _sources)
            {
                if (src == null) continue;
                var (gx, gz) = WorldToGrid(src.transform.position);
                _depth[gz, gx] += src.flowRate * dt;
            }
        }

        void InjectPollutionSources(float dt)
        {
            foreach (var src in _pollSources)
            {
                if (src == null) continue;
                var (gx, gz) = WorldToGrid(src.transform.position);
                InjectPollution(gx, gz, src.pollutionRate * dt, src.pollutionColor);
                // Also add a trickle of water to carry the pollution
                _depth[gz, gx] += src.carrierFlowRate * dt;
            }
        }

        void ApplyRain(float dt)
        {
            float add = rainRate * dt;
            for (int z = 0; z < _res; z++)
                for (int x = 0; x < _res; x++)
                    _depth[z, x] += add;
        }

        void UpdateFlow(float dt)
        {
            float denom = Mathf.Max(_cellSize, 0.001f);
            float A = _cellSize * _cellSize;

            for (int z = 0; z < _res; z++)
            {
                for (int x = 0; x < _res; x++)
                {
                    float h0 = _terrainH[z, x] + _depth[z, x] + _obstacleH[z, x];

                    if (x < _res - 1)
                    {
                        float h1 = _terrainH[z, x+1] + _depth[z, x+1] + _obstacleH[z, x+1];
                        _flowX[z, x] = (_flowX[z, x] + dt * gravity * (h0 - h1) / denom * A) * damping;
                    }

                    if (z < _res - 1)
                    {
                        float h1 = _terrainH[z+1, x] + _depth[z+1, x] + _obstacleH[z+1, x];
                        _flowZ[z, x] = (_flowZ[z, x] + dt * gravity * (h0 - h1) / denom * A) * damping;
                    }
                }
            }
        }

        void UpdateDepth(float dt)
        {
            float area = _cellSize * _cellSize;
            for (int z = 0; z < _res; z++)
            {
                for (int x = 0; x < _res; x++)
                {
                    float net = 0f;
                    if (x > 0)       net += _flowX[z, x-1];
                    if (x < _res-1)  net -= _flowX[z, x];
                    if (z > 0)       net += _flowZ[z-1, x];
                    if (z < _res-1)  net -= _flowZ[z, x];

                    _depth[z, x] = Mathf.Max(0f, _depth[z, x] + (net * dt) / Mathf.Max(area, 0.001f));
                }
            }
        }

        void UpdatePollution(float dt)
        {
            // Advect pollution mass proportionally to water flow.
            // For each flow edge, the fraction of the source cell's water that moved
            // carries the same fraction of pollution.
            float area = _cellSize * _cellSize;

            // Clear pre-allocated work arrays
            System.Array.Clear(_pollDelta,   0, _pollDelta.Length);
            System.Array.Clear(_pollMassIn,  0, _pollMassIn.Length);
            for (int z2 = 0; z2 < _res; z2++)
                for (int x2 = 0; x2 < _res; x2++)
                    _pollColorIn[z2, x2] = Color.clear;

            void Transfer(int fromZ, int fromX, int toZ, int toX, float flowVol)
            {
                if (flowVol <= 0f) return;
                float srcDepth = Mathf.Max(_depth[fromZ, fromX], 0.001f);
                float ratio    = Mathf.Min(1f, flowVol / srcDepth);
                float pm       = _pollMass[fromZ, fromX] * ratio;
                if (pm < 0.0001f) return;

                _pollDelta[fromZ, fromX] -= pm;
                _pollDelta[toZ,   toX]   += pm;

                // Weighted color blend
                float existing = _pollMassIn[toZ, toX];
                float total    = existing + pm;
                if (total > 0.0001f)
                    _pollColorIn[toZ, toX] = Color.Lerp(_pollColorIn[toZ, toX], _pollColor[fromZ, fromX], pm / total);
                _pollMassIn[toZ, toX] += pm;
            }

            for (int z = 0; z < _res; z++)
            {
                for (int x = 0; x < _res; x++)
                {
                    if (x < _res - 1)
                    {
                        float vol = _flowX[z, x] * dt / area;
                        if (vol > 0) Transfer(z, x,   z, x+1, vol);
                        else         Transfer(z, x+1,  z, x,  -vol);
                    }
                    if (z < _res - 1)
                    {
                        float vol = _flowZ[z, x] * dt / area;
                        if (vol > 0) Transfer(z,   x, z+1, x, vol);
                        else         Transfer(z+1,  x, z,  x, -vol);
                    }
                }
            }

            // Apply deltas and blend colors
            for (int z = 0; z < _res; z++)
            {
                for (int x = 0; x < _res; x++)
                {
                    _pollMass[z, x] = Mathf.Max(0f, _pollMass[z, x] + _pollDelta[z, x]);

                    // Blend incoming color into cell color if there was inflow
                    if (_pollMassIn[z, x] > 0.0001f)
                    {
                        float t = _pollMassIn[z, x] / Mathf.Max(_pollMass[z, x], 0.001f);
                        _pollColor[z, x] = Color.Lerp(_pollColor[z, x], _pollColorIn[z, x], Mathf.Clamp01(t));
                    }

                    // Clamp pollution to available water
                    if (_depth[z, x] < 0.001f)
                        _pollMass[z, x] = 0f;
                    else
                        _pollMass[z, x] = Mathf.Min(_pollMass[z, x], _depth[z, x]);
                }
            }
        }

        void DrainEdges(float dt)
        {
            float rate = 0.8f * dt;
            for (int i = 0; i < _res; i++)
            {
                DrainCell(0,      i, rate);
                DrainCell(_res-1, i, rate);
                DrainCell(i,      0, rate);
                DrainCell(i, _res-1, rate);
            }
        }

        void DrainCell(int z, int x, float rate)
        {
            float d = _depth[z, x] * rate;
            _depth[z, x]    = Mathf.Max(0f, _depth[z, x]    - d);
            _pollMass[z, x] = Mathf.Max(0f, _pollMass[z, x] - _pollMass[z, x] * rate);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        float GetSurfaceHeight(int gx, int gz)
        {
            if (_terrainH == null) return 0f;
            return _terrainH[Clamp(gz), Clamp(gx)] + _depth[Clamp(gz), Clamp(gx)];
        }

        int Clamp(int v)  => Mathf.Clamp(v, 0, _res - 1);
        int ClampM(int v) => Mathf.Clamp(v, 0, _res - 2);
    }
}
