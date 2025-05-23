using UnityEngine;
using CGFinal.Helpers;
using Unity.Mathematics;

public class MarchingCubeRenderer : MonoBehaviour
{
    [Header("Mesh settings")]
    public Material marchingCubeMaterial;
    
    [Header("Renderer settings")]

    public bool isRendering;
    public Vector3Int resolution;
    public int Resolution;
    public int maxSamples = 1000000;
    public int numSamples {
        get {
            return Mathf.Min((resolution.x + 1) * (resolution.y + 1) * (resolution.z + 1), maxSamples) + 1; // +1 for manual query
        }
    }

    [SerializeField] private bool needUpdate = true; // set this whenever numSamples changes
    public float isoLevel = 0.5f;
    public ComputeShader marchingCubeComputeShader;

    private Vector3[] samplePositions;
    private float[] sampleDensities;
    
    private ComputeBuffer queryPositionsBuffer;
    private ComputeBuffer densityResultsBuffer;

    private SPH sphSystem;

    public float3 manualQueryPos;
    public void Init(SPH sph)
    {
        sphSystem = sph;
    }

    void LateUpdate()
    {
        if (!isRendering || sphSystem.DensityTexture == null) return;
        RenderFluid();
    }

    void RenderFluid() {
        // Launch marching cube compute shader
        
    }

    void OnDestroy()
    {
        ComputeHelper.Release(queryPositionsBuffer, densityResultsBuffer);
    }

}
