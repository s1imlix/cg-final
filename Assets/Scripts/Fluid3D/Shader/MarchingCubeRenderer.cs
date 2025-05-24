using UnityEngine;
using CGFinal.Helpers;
using Unity.Mathematics;
using System.Linq;
public class MarchingCubeRenderer : MonoBehaviour
{

    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
    } 

    public struct Triangle
    {
        public Vertex vertexA;
        public Vertex vertexB;
        public Vertex vertexC;
    }

    [Header("Mesh settings")]
    public Material marchingCubeMaterial;
    
    [Header("Renderer settings")]

    public bool isRendering;
    public int Resolution;

    public float isoLevel = 0.5f;
    private SPH sphSystem;
    private ComputeShader marchingCubeComputeShader;
    private ComputeBuffer edgeLUTBuffer;
    private ComputeBuffer vertexBuffer;

    private ComputeBuffer renderArgs;
    ComputeBuffer triangleBuffer;

    private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
    private const int MarchCube = 0;
    private const int UpdateRenderArgs = 1; 
    const uint maxBytes = 2147483648; // 2GB
    

    public void Init(SPH sph)
    {
        sphSystem = sph;
        marchingCubeComputeShader = Resources.Load<ComputeShader>("MarchingCube");
        string lut = Resources.Load<TextAsset>("MarchingCubeLUT").text;
        int[] edgeLUT = lut.Trim().Split(',').Select(x => int.Parse(x)).ToArray();
        edgeLUTBuffer = ComputeHelper.CreateStructBuffer(edgeLUT);
        renderArgs = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        ComputeHelper.SetBuffer(marchingCubeComputeShader, renderArgs, "RenderArgs", UpdateRenderArgs);
    }

    void LateUpdate()
    {
        if (!isRendering || sphSystem.DensityTexture == null) return;
        RenderFluid();
    }

    void UpdateMarchingCubeSettings() {
        // Generate triangle buffer and clamp at max byte size
        if (triangleBuffer != null) {
            triangleBuffer.Release();
        }
        int numCubePerAxis = Resolution - 1;
        int numCubes = numCubePerAxis * numCubePerAxis * numCubePerAxis;
        int maxTriangles = numCubes * 5; // 5 is the maximum number of triangles per cube (see offset[])
        int stride = ComputeHelper.GetStride<Triangle>();
        uint maxEntry = maxBytes / (uint)stride;

        if (maxTriangles > maxEntry) {
            Debug.LogWarning("MarchingCubeRenderer: Triangle buffer exceeds max size, reducing resolution.");
        }

        ComputeHelper.CreateAppendBuffer<Triangle>(ref triangleBuffer, Mathf.Min(maxTriangles, (int)maxEntry));
        
        // Update variables
        marchingCubeComputeShader.SetBuffer(MarchCube, "edgeLUT", edgeLUTBuffer);
        marchingCubeComputeShader.SetBuffer(MarchCube, "OutputBuffer", triangleBuffer);
        marchingCubeComputeShader.SetTexture(MarchCube, "DensityTex", sphSystem.DensityTexture);
        marchingCubeComputeShader.SetInts("DensityTexSize", sphSystem.DensityTexture.width, sphSystem.DensityTexture.height, sphSystem.DensityTexture.depth);
        marchingCubeComputeShader.SetFloat("IsoValue", isoLevel);
        marchingCubeComputeShader.SetVector("localScale", sphSystem.transform.localScale);
    }

    void RenderFluid() {
        // Launch marching cube compute shader
        UpdateMarchingCubeSettings();
        marchingCubeMaterial.SetBuffer("VertexBuffer", triangleBuffer);

        // (triangle index count, instance count, sub-mesh index, base vertex index, byte offset)
        ComputeBuffer.CopyCount(triangleBuffer, renderArgs, 0);
        marchingCubeComputeShader.Dispatch(UpdateRenderArgs, 1, 1, 1);

        Graphics.DrawProceduralIndirect(marchingCubeMaterial, bounds, MeshTopology.Triangles, renderArgs);
    }


    void OnDestroy()
    {
        ComputeHelper.Release(edgeLUTBuffer, renderArgs, triangleBuffer);
    }

}
