using UnityEngine;

namespace WatershedSim
{
    /// <summary>
    /// Marks a pollution injection point on the terrain.
    /// WaterSimulation collects all instances at startup.
    /// Add to any GameObject and position it over the terrain.
    /// </summary>
    public class PollutionSource : MonoBehaviour
    {
        [Tooltip("Pollution mass added per second to the nearest simulation cell.")]
        [Range(0f, 2f)] public float pollutionRate = 0.3f;

        [Tooltip("A small water trickle to carry the pollution downstream.")]
        [Range(0f, 0.5f)] public float carrierFlowRate = 0.02f;

        [Tooltip("Color of the pollution plume in the water.")]
        public Color pollutionColor = new Color(0.6f, 0.2f, 0.0f, 1f); // brownish default

        void OnDrawGizmos()
        {
            Gizmos.color = pollutionColor;
            Gizmos.DrawWireSphere(transform.position, 2f);
            // Skull-like downward arrow
            Vector3 p = transform.position;
            Gizmos.DrawLine(p, p + Vector3.down * 5f);
            Gizmos.DrawLine(p + Vector3.down * 5f, p + Vector3.down * 5f + new Vector3( 1f, 1.5f, 0f).normalized * 1.5f);
            Gizmos.DrawLine(p + Vector3.down * 5f, p + Vector3.down * 5f + new Vector3(-1f, 1.5f, 0f).normalized * 1.5f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(pollutionColor.r, pollutionColor.g, pollutionColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, 4f);
        }
    }
}
