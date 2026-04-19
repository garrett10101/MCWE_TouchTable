using UnityEngine;

namespace WatershedSim
{
    /// <summary>
    /// Interface for terrain height sampling. Implement on any MonoBehaviour to use
    /// as the terrain source for WaterSimulation — swap in custom terrain without
    /// changing simulation code.
    /// </summary>
    public interface ITerrainProvider
    {
        /// <summary>World-space size of the terrain (X and Z).</summary>
        float WorldWidth { get; }
        float WorldDepth { get; }

        /// <summary>World-space origin (bottom-left corner) of the terrain.</summary>
        Vector3 WorldOrigin { get; }

        /// <summary>
        /// Sample terrain height at normalized coordinates [0,1] x [0,1].
        /// Returns world-space Y height.
        /// </summary>
        float SampleHeight(float normX, float normZ);

        /// <summary>
        /// Returns true once the terrain has finished generating and is ready to sample.
        /// WaterSimulation will wait for this before initializing its grid.
        /// </summary>
        bool IsReady { get; }
    }
}
