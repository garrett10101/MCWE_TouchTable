using UnityEngine;

namespace WatershedSim
{
    /// <summary>
    /// Builds and updates a dynamic mesh representing the water surface.
    /// Vertex color blends between cleanWaterColor and the cell's pollution color
    /// based on pollution concentration.
    ///
    /// Requires MeshFilter and MeshRenderer on the same GameObject.
    /// A URP transparent vertex-color material is created automatically at runtime
    /// if none is assigned.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class WaterRenderer : MonoBehaviour
    {
        [Header("References")]
        public WaterSimulation waterSim;

        [Tooltip("A MonoBehaviour that implements ITerrainProvider (used for world-space sizing).")]
        [SerializeField] MonoBehaviour terrainProvider;

        [Header("Water Appearance")]
        [Tooltip("Cells shallower than this (metres) are hidden.")]
        [Range(0.001f, 0.5f)] public float depthThreshold = 0.02f;

        [Tooltip("Base water color when there is no pollution and water is still.")]
        public Color cleanWaterColor = new Color(0.13f, 0.47f, 0.71f, 0.80f);

        [Tooltip("Color at maximum flow speed (blended with cleanWaterColor).")]
        public Color fastFlowColor  = new Color(0.55f, 0.85f, 1.00f, 0.90f);

        [Tooltip("Flow speed (m/s) that maps to fastFlowColor.")]
        [Range(0.1f, 10f)] public float maxSpeedForColor = 1.5f;

        [Header("Obstacle Display")]
        [Tooltip("Color used to tint the mesh where an obstacle (dam) has been placed.")]
        public Color obstacleColor = new Color(0.45f, 0.30f, 0.10f, 1.00f);

        // ── Private State ──────────────────────────────────────────────────────

        Mesh      _mesh;
        Vector3[] _vertices;
        Color[]   _colors;
        int[]     _triangles;
        int       _cachedRes;
        ITerrainProvider _terrain;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        void Awake()
        {
            _terrain = terrainProvider as ITerrainProvider;

            _mesh = new Mesh { name = "WaterMesh" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().mesh = _mesh;

            // Create a runtime material if none assigned
            var mr = GetComponent<MeshRenderer>();
            if (mr.sharedMaterial == null)
                mr.sharedMaterial = CreateWaterMaterial();
        }

        void LateUpdate()
        {
            if (waterSim == null) return;
            int res = waterSim.Resolution;
            if (res < 2) return;

            if (res != _cachedRes) RebuildTopology(res);

            float worldWidth = _terrain != null ? _terrain.WorldWidth : 500f;
            Vector3 origin   = _terrain != null ? _terrain.WorldOrigin : Vector3.zero;
            float cellSize   = worldWidth / (res - 1);

            int vi = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float depth    = waterSim.GetDepth(x, z);
                    float terrainY = waterSim.GetTerrainHeight(x, z);
                    float obstH    = waterSim.GetObstacleHeight(x, z);

                    if (depth < depthThreshold && obstH < 0.1f)
                    {
                        _vertices[vi] = new Vector3(origin.x + x * cellSize,
                                                    terrainY - 1f,
                                                    origin.z + z * cellSize);
                        _colors[vi] = Color.clear;
                    }
                    else if (obstH >= 0.1f)
                    {
                        // Render obstacle (dam) as a colored block
                        float blockY = terrainY + obstH;
                        _vertices[vi] = new Vector3(origin.x + x * cellSize, blockY, origin.z + z * cellSize);
                        _colors[vi]   = obstacleColor;
                    }
                    else
                    {
                        float worldY = terrainY + depth;
                        _vertices[vi] = new Vector3(origin.x + x * cellSize, worldY, origin.z + z * cellSize);

                        // Flow-based color component
                        Vector2 flow  = waterSim.GetFlow(x, z);
                        float   speed = flow.magnitude;
                        float   t     = Mathf.Clamp01(speed / maxSpeedForColor);
                        Color   baseColor = Color.Lerp(cleanWaterColor, fastFlowColor, t);

                        // Pollution blend
                        float pollConc  = waterSim.GetPollutionConcentration(x, z);
                        Color pollColor = waterSim.GetPollutionColor(x, z);
                        _colors[vi] = Color.Lerp(baseColor, pollColor, pollConc);
                    }

                    vi++;
                }
            }

            _mesh.vertices = _vertices;
            _mesh.colors   = _colors;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        // ── Mesh Topology ──────────────────────────────────────────────────────

        void RebuildTopology(int res)
        {
            _cachedRes = res;
            int vCount = res * res;
            int qCount = (res - 1) * (res - 1);

            _vertices  = new Vector3[vCount];
            _colors    = new Color[vCount];
            _triangles = new int[qCount * 6];

            int ti = 0;
            for (int z = 0; z < res - 1; z++)
            {
                for (int x = 0; x < res - 1; x++)
                {
                    int bl = z * res + x;
                    int br = bl + 1;
                    int tl = bl + res;
                    int tr = tl + 1;

                    _triangles[ti++] = bl; _triangles[ti++] = tl; _triangles[ti++] = br;
                    _triangles[ti++] = br; _triangles[ti++] = tl; _triangles[ti++] = tr;
                }
            }

            _mesh.Clear();
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _mesh.vertices    = _vertices;
            _mesh.colors      = _colors;
            _mesh.triangles   = _triangles;
        }

        // ── Material Creation ──────────────────────────────────────────────────

        static Material CreateWaterMaterial()
        {
            // Try URP Particles/Unlit first (supports vertex colors + transparency)
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var mat = new Material(shader) { name = "WaterMaterial_Runtime" };

            // Enable transparency and vertex color
            mat.SetFloat("_Surface",    1f);  // 0=Opaque 1=Transparent
            mat.SetFloat("_Blend",      0f);  // Alpha blend
            mat.SetFloat("_AlphaClip",  0f);
            mat.SetFloat("_VertexColorMode", 2f); // Multiply vertex color
            mat.enableInstancing = false;
            mat.renderQueue = 3000;

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return mat;
        }
    }
}
