using UnityEngine;
using CGFinal.Helpers;

public class ParticleRenderer : MonoBehaviour
{
    [Header("Mesh settings")]
    public Mesh particleMesh;
    public Material particleMaterial;

    public float particleRadius = 0.1f;

    [Header("Renderer settings")]

    public bool isRendering = true;

    public bool useMaterialColor;

    public Gradient speedGradient;
    Texture2D colorTexture;

    public float maxGradientVelocity;
    Bounds bounds;
    GraphicsBuffer _argsBuffer;

    public void Init(SPH sph) {
        
        GenerateGradientTexture(ref colorTexture);

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
        if (!isRendering) return;
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

    void GenerateGradientTexture(ref Texture2D gradientTex) {
        // Create a gradient texture for the speed gradient
        if (gradientTex != null)
        {
            Destroy(gradientTex);
        }
        // Create a new texture
        int width = 256;
        gradientTex = new Texture2D(width, 1);
        gradientTex.wrapMode = TextureWrapMode.Clamp;
        gradientTex.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[width];
        for (int i = 0; i < width; i++)
        {
            float t = i / (float)(width - 1);
            colors[i] = speedGradient.Evaluate(t);
        }
        gradientTex.SetPixels(colors);
        gradientTex.Apply();
        particleMaterial.SetTexture("_GradientTex", gradientTex);
        // Debug.Log($"First color: {colors[0]}, Mid color: {colors[width / 2]}, Last color: {colors[width - 1]}");
    }


    private void UpdateSettings() {
        particleMaterial.SetFloat("_ParticleRadius", particleRadius);
        particleMaterial.SetInt("useMaterialColor", useMaterialColor ? 1 : 0);
        particleMaterial.SetFloat("Vmax", maxGradientVelocity);    
    }

    void OnDestroy() {
        ComputeHelper.Release(_argsBuffer);
    }
}
