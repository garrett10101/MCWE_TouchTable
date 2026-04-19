using UnityEngine;

namespace WatershedSim
{
    /// <summary>
    /// Procedurally generates a Unity Terrain heightmap and implements ITerrainProvider.
    /// Attach alongside Terrain and TerrainCollider components.
    /// Right-click header → "Regenerate Terrain" to rebuild in Edit mode.
    /// To use a custom terrain instead: implement ITerrainProvider on your own MonoBehaviour
    /// and assign it to WaterSimulation.terrainProvider.
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    [RequireComponent(typeof(TerrainCollider))]
    public class WatershedTerrain : MonoBehaviour, ITerrainProvider
    {
        [Header("Terrain Size")]
        [Tooltip("Heightmap resolution — must be 2^n+1 (e.g. 129, 257, 513).")]
        public int resolution = 129;
        [Tooltip("World-space width and depth of the terrain (metres).")]
        public float worldWidth = 500f;
        [Tooltip("Maximum terrain height (metres).")]
        public float heightScale = 80f;

        [Header("Noise")]
        [Range(0.001f, 0.1f)]
        public float noiseScale = 0.015f;
        public int seed = 42;

        [Header("Pond")]
        public Vector2 pondCenter = new Vector2(0.5f, 0.5f);
        [Range(0.01f, 0.5f)] public float pondRadius = 0.12f;
        [Range(0f, 1f)]      public float pondDepth  = 0.35f;

        [Header("Valley")]
        public Vector2 valleyStart = new Vector2(0.0f, 0.3f);
        public Vector2 valleyEnd   = new Vector2(1.0f, 0.6f);
        [Range(0.01f, 0.3f)] public float valleyWidth = 0.06f;
        [Range(0f, 1f)]      public float valleyDepth = 0.2f;

        [Header("Canal")]
        public Vector2 canalStart = new Vector2(0.2f, 0.0f);
        public Vector2 canalEnd   = new Vector2(0.8f, 1.0f);
        [Range(0.005f, 0.1f)] public float canalWidth = 0.02f;
        [Range(0f, 1f)]       public float canalDepth = 0.15f;

        // ── ITerrainProvider ──────────────────────────────────────────────────

        public float   WorldWidth  => worldWidth;
        public float   WorldDepth  => worldWidth;   // square terrain
        public Vector3 WorldOrigin => transform.position;
        public bool    IsReady     => _ready;

        public float SampleHeight(float normX, float normZ)
        {
            if (_terrain == null) return 0f;
            return _terrain.terrainData.GetInterpolatedHeight(normX, normZ);
        }

        // ─────────────────────────────────────────────────────────────────────

        private Terrain _terrain;
        private bool    _ready;

        void Awake()  => _terrain = GetComponent<Terrain>();
        void Start()  => GenerateTerrain();

        [ContextMenu("Regenerate Terrain")]
        public void GenerateTerrain()
        {
            _ready = false;
            if (_terrain == null) _terrain = GetComponent<Terrain>();

            TerrainData data = _terrain.terrainData;
            data.heightmapResolution = resolution;
            data.size = new Vector3(worldWidth, heightScale, worldWidth);
            data.SetHeights(0, 0, BuildHeights(resolution));
            _ready = true;
        }

        float[,] BuildHeights(int res)
        {
            float[,] h = new float[res, res];
            float offset = seed * 1000f;

            Vector2 vDir  = valleyEnd - valleyStart;
            float   vLen  = vDir.magnitude;
            Vector2 vDirN = vLen > 0.0001f ? vDir / vLen : Vector2.right;

            Vector2 cDir  = canalEnd - canalStart;
            float   cLen  = cDir.magnitude;
            Vector2 cDirN = cLen > 0.0001f ? cDir / cLen : Vector2.up;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float nx = (float)x / (res - 1);
                    float nz = (float)z / (res - 1);

                    // Perlin base (2 octaves)
                    float b = Mathf.PerlinNoise(nx * res * noiseScale + offset,
                                                nz * res * noiseScale + offset);
                    b += 0.5f * Mathf.PerlinNoise(nx * res * noiseScale * 2f + offset + 17f,
                                                   nz * res * noiseScale * 2f + offset + 17f);
                    b /= 1.5f;

                    // Pond Gaussian bowl
                    Vector2 dp = new Vector2(nx - pondCenter.x, nz - pondCenter.y);
                    float distPond   = dp.magnitude / pondRadius;
                    float pondCarve  = Mathf.Exp(-distPond * distPond * 2f) * pondDepth;

                    // Valley linear carve
                    Vector2 vRel    = new Vector2(nx - valleyStart.x, nz - valleyStart.y);
                    float   vT      = Mathf.Clamp01(Vector2.Dot(vRel, vDirN) / vLen);
                    float   vDist   = (new Vector2(nx, nz) - (valleyStart + vT * vDir)).magnitude;
                    float   vMask   = Mathf.Max(0f, 1f - (vDist / valleyWidth) * (vDist / valleyWidth));
                    float   valleyCarve = vMask * valleyDepth;

                    // Canal rectangular carve
                    Vector2 cRel    = new Vector2(nx - canalStart.x, nz - canalStart.y);
                    float   cT      = Mathf.Clamp01(Vector2.Dot(cRel, cDirN) / cLen);
                    float   cDist   = (new Vector2(nx, nz) - (canalStart + cT * cDir)).magnitude;
                    float   canalCarve = (cDist < canalWidth ? 1f : 0f) * canalDepth;

                    h[z, x] = Mathf.Clamp01(b - pondCarve - valleyCarve - canalCarve);
                }
            }
            return h;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.5f);
            Gizmos.DrawWireSphere(pos + new Vector3(pondCenter.x * worldWidth, heightScale * 0.5f, pondCenter.y * worldWidth), pondRadius * worldWidth);

            Gizmos.color = new Color(0f, 0.8f, 1f);
            Gizmos.DrawLine(pos + new Vector3(valleyStart.x * worldWidth, heightScale * 0.5f, valleyStart.y * worldWidth),
                            pos + new Vector3(valleyEnd.x   * worldWidth, heightScale * 0.5f, valleyEnd.y   * worldWidth));

            Gizmos.color = new Color(0.5f, 1f, 0.5f);
            Gizmos.DrawLine(pos + new Vector3(canalStart.x * worldWidth, heightScale * 0.5f, canalStart.y * worldWidth),
                            pos + new Vector3(canalEnd.x   * worldWidth, heightScale * 0.5f, canalEnd.y   * worldWidth));
        }
    }
}
