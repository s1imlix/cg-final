using UnityEngine;
using CGFinal.Helpers;

public class MarchingCubeRenderer : MonoBehaviour
{
    [Header("Mesh settings")]
    public Material marchingCubeMaterial;
    
    [Header("Renderer settings")]
    public Vector3Int resolution;
    public int maxSamples = 1000000;
    public int numSamples {
        get {
            return Mathf.Min(resolution.x * resolution.y * resolution.z, maxSamples);
        }
    }
    public float isoLevel = 0.5f;
    public ComputeShader marchingCubeComputeShader;
    public ComputeShader densityComputeShader; // SPHCompute

    private ComputeBuffer densitySampleBuffer;
    private ComputeBuffer sampleParticleBuffer;
    public void Init(SPH sph) {
        densityComputeShader = sph.computeShader;
        densitySampleBuffer = ComputeHelper.CreateStructBuffer<float>(maxSamples);
        sampleParticleBuffer = ComputeHelper.CreateStructBuffer<Particle>(maxSamples);
    }


    void SampleDensity() {
        // Called by onSimulationComplete
        densityComputeShader.SetInt("numParticles", numSamples);
        
        ComputeHelper.Dispatch(densityComputeShader, numSamples, SPH.calcDensity);
    }

    void LateUpdate()
    {
        
    }

    void OnDestroy()
    {
        ComputeHelper.Release(densitySampleBuffer);
    }

}
