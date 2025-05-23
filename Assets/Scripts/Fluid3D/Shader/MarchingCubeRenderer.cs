using UnityEngine;
using CGFinal.Helpers;
using Unity.Mathematics;

public class MarchingCubeRenderer : MonoBehaviour
{
    [Header("Mesh settings")]
    public Material marchingCubeMaterial;
    
    [Header("Renderer settings")]
    public Vector3Int resolution;
    public int maxSamples = 1000000;
    public int numSamples {
        get {
            return Mathf.Min((resolution.x + 1) * (resolution.y + 1) * (resolution.z + 1), maxSamples) + 1; // +1 for manual query
        }
    }
    public float isoLevel = 0.5f;
    public ComputeShader marchingCubeComputeShader;
    public ComputeShader densityComputeShader; // SPHCompute

    private Vector3[] samplePositions;
    private float[] sampleDensities;
    
    private ComputeBuffer queryPositionsBuffer;
    private ComputeBuffer densityResultsBuffer;

    public float3 manualQueryPos;
    public void Init(SPH sph)
    {
        densityComputeShader = sph.computeShader;
        sph.onSimulationComplete.AddListener(SampleDensity);

        initSamplePos(); // Initialize positions to sample

        queryPositionsBuffer = ComputeHelper.CreateStructBuffer<Vector3>(numSamples);
        densityResultsBuffer = ComputeHelper.CreateStructBuffer<float>(numSamples);

        ComputeHelper.SetBuffer(densityComputeShader, queryPositionsBuffer, "_QueryPositions", SPH.calcDensityPos);
        ComputeHelper.SetBuffer(densityComputeShader, densityResultsBuffer, "_DensityResults", SPH.calcDensityPos);

    }


    void SampleDensity()
    {
        // Called by onSimulationComplete
        queryPositionsBuffer.SetData(samplePositions);
        densityComputeShader.SetInt("numQueryPositions", numSamples);
        ComputeHelper.Dispatch(densityComputeShader, numSamples, SPH.calcDensityPos);
        densityResultsBuffer.GetData(sampleDensities);
        //Debug.Log("Manual query density: Location: " + manualQueryPos + " Density: " + sampleDensities[sampleDensities.Length - 1]);
    }

    void initSamplePos()
    {
        samplePositions = new Vector3[numSamples];
        sampleDensities = new float[numSamples];
        int ind = 0;
        for (int x = 0; x < resolution.x + 1; x++)
        {
            for (int y = 0; y < resolution.y + 1; y++)
            {
                for (int z = 0; z < resolution.z + 1; z++)
                {
                    samplePositions[ind] = new Vector3(
                        x * (1f / resolution.x),
                        y * (1f / resolution.y),
                        z * (1f / resolution.z)
                    );
                    ind++;
                }
            }
        }

        //manual query
        samplePositions[ind++] = manualQueryPos;
        
    }

    void LateUpdate()
    {
        
    }

    void OnDestroy()
    {
        ComputeHelper.Release(queryPositionsBuffer, densityResultsBuffer);
    }

}
