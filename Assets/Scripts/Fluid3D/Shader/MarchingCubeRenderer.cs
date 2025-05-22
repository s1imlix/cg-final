using UnityEngine;
using CGFinal.Helpers;

public class MarchingCubeRenderer : MonoBehaviour
{
    [Header("Mesh settings")]
    public Material marchingCubeMaterial;
    
    [Header("Renderer settings")]
    public Vector3Int resolution;
    public int maxSamples = 1000000;
    private int numSamples {
        get {
            return Mathf.Min(resolution.x * resolution.y * resolution.z, maxSamples);
        }
    }
    public float isoLevel = 0.5f;


    ComputeBuffer densitySampleBuffer;
    void Init(SPH sph) {
        densitySampleBuffer = ComputeHelper.CreateStructBuffer<float>(maxSamples);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
