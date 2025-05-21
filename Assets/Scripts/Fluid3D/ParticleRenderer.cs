using UnityEngine;
using CGFinal.Helpers;

public class ParticleRenderer : MonoBehaviour
{
    [Header("Mesh settings")]
    public Mesh particleMesh;
    public Material particleMaterial;

    public float particleRadius = 0.1f;

    [Header("Renderer settings")]
    Bounds bounds;
    GraphicsBuffer _argsBuffer;

    public void Init(SPH sph) {
           
        /*
            GraphicsBuffer will handle particle mesh rendering
            @indexCountPerInstance: number of indices per mesh
            @instanceCount: number of mesh instances to draw
            @startIndex: index of the first index in the mesh (index = triple of vertices = triangles)
            @baseVertexIndex: index of the first vertex in the mesh
        */

        _argsBuffer = ComputeHelper.CreateArgsBuffer(particleMesh, sph._particleCount);
        particleMaterial.SetBuffer("_ParticleBuffer", sph._particleBuffer);

        bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
    }

    void LateUpdate() {
        UpdateSettings();
        if (particleMaterial.SetPass(0)) {
            // Dispatch rendering request, go check Resources/ParticleShader.shader
            // Instanced: no GameObject for these meshes
            Graphics.DrawMeshInstancedIndirect(
                particleMesh,
                0,
                particleMaterial,
                bounds,
                _argsBuffer
            );
        }
    }

    private void UpdateSettings() {
        particleMaterial.SetFloat("_ParticleRadius", particleRadius);
    }

    void OnDestroy() {
        ComputeHelper.Release(_argsBuffer);
    }
}
