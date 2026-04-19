using UnityEngine;

namespace WatershedSim
{
    /// <summary>
    /// Water injection point. Position over terrain; WaterSimulation finds all
    /// instances at startup via FindObjectsByType.
    /// </summary>
    public class WaterSource : MonoBehaviour
    {
        public enum WaterType { River, Pond, Canal, Spring }

        [Tooltip("Water depth added per second to the nearest simulation cell (metres).")]
        [Range(0f, 2f)] public float flowRate = 0.05f;

        [Tooltip("Semantic type — affects gizmo colour only.")]
        public WaterType waterType = WaterType.Spring;

        void OnDrawGizmos()
        {
            Gizmos.color = waterType switch
            {
                WaterType.River  => new Color(0.0f, 0.4f, 1.0f),
                WaterType.Pond   => new Color(0.0f, 0.6f, 0.8f),
                WaterType.Canal  => new Color(0.2f, 0.8f, 0.5f),
                WaterType.Spring => new Color(0.5f, 0.8f, 1.0f),
                _                => Color.blue
            };
            Vector3 pos = transform.position;
            float len = 4f;
            Gizmos.DrawWireSphere(pos, 1.5f);
            Gizmos.DrawLine(pos, pos + Vector3.down * len);
            Gizmos.DrawLine(pos + Vector3.down * len,
                            pos + Vector3.down * len + new Vector3( 1f, 1f, 0f).normalized * len * 0.3f);
            Gizmos.DrawLine(pos + Vector3.down * len,
                            pos + Vector3.down * len + new Vector3(-1f, 1f, 0f).normalized * len * 0.3f);
        }
    }
}
