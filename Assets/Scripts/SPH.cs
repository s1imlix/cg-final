using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 44)]
public struct Particle
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 currentForce;
    public float density;
    public float pressure;
}

public class SPH : MonoBehaviour
{
    [Header("Constants")]
    private const int MAX_PARTICLES = 1000;
    private int _particleCount {
        get {
            return Mathf.Min(numToSpawn.x * numToSpawn.y * numToSpawn.z, MAX_PARTICLES);
        }
    }

    [Header("Spawner settings")]
    public Vector3Int numToSpawn = new Vector3Int(10, 10, 10);
    public Vector3 spawnBounds = new Vector3(4f, 10f, 3f);
    public Vector3 spawnBasePosition = new Vector3(0f, 0f, 0f);
    public float particleRadius = 0.1f;

    [Header("Rendering")]
    public Mesh particleMesh;
    public Material particleMaterial;

    [Header("Compute Shader")]
    public ComputeShader computeShader;
    public Particle[] particles;

    private ComputeBuffer _particleBuffer;
    private GraphicsBuffer _argsBuffer;
    void InitializeParticles()
    {
        int numParticlesX = numToSpawn.x;
        int numParticlesY = numToSpawn.y;
        int numParticlesZ = numToSpawn.z;

        List<Particle> _particles = new List<Particle>();

        // Spawn particles in a grid pattern
        for (int x = 0; x < numParticlesX; x++)
        {
            for (int y = 0; y < numParticlesY; y++)
            {
                for (int z = 0; z < numParticlesZ; z++)
                {
                    int index = x * numParticlesY * numParticlesZ + y * numParticlesZ + z;
                    if (index >= MAX_PARTICLES) break;

                    // Calculate the position of the particle
                    Vector3 _offset = new Vector3(x*particleRadius*2, y*particleRadius*2, z*particleRadius*2);
                    Vector3 _position = spawnBasePosition + _offset;
                    
                    _particles.Add(new Particle {
                        position = _position,
                    });
                }
            }
        }
        particles = _particles.ToArray();
    }
    void Start()
    {
        // Initialize the SPH simulation
        InitializeParticles();

        _argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].indexCountPerInstance = particleMesh.GetIndexCount(0);
        args[0].instanceCount = (uint)_particleCount;
        args[0].startIndex = particleMesh.GetIndexStart(0);
        args[0].baseVertexIndex = particleMesh.GetBaseVertex(0);
        args[0].startInstance = 0;

        _argsBuffer.SetData(args);

        // Particle buffer is unchanged
        _particleBuffer = new ComputeBuffer(_particleCount, 44);
        _particleBuffer.SetData(particles);

    }

    void Update()
    {
        // Set shader parameters 
        computeShader.SetBuffer(0, "_ParticleBuffer", _particleBuffer);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetVector("_Gravity", new Vector3(0, -9.81f, 0));

        int threadGroupsX = Mathf.CeilToInt(_particleCount / 64.0f);
        computeShader.Dispatch(0, threadGroupsX, 1, 1);

    }

    void OnDrawGizmos()
    {
        // Draw the particles in the scene view
        if (particles != null)
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < _particleCount && i < particles.Length; i++)
            {
                Gizmos.DrawSphere(particles[i].position, particleRadius);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, spawnBounds);

        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnBasePosition, 0.1f);
        }
    }

    void OnRenderObject()
    {
        if (!Application.isPlaying || particles == null || particles.Length == 0 || _argsBuffer == null || _particleBuffer == null)
            return;

        particleMaterial.SetBuffer("_ParticleBuffer", _particleBuffer);
        particleMaterial.SetFloat("_ParticleRadius", particleRadius);

        particleMaterial.SetPass(0);
        Graphics.DrawMeshInstancedIndirect(
            particleMesh,
            0,
            particleMaterial,
            new Bounds(Vector3.zero, spawnBounds * 50),
            _argsBuffer
        );
    }

    void OnDisable(){
        if (_particleBuffer != null){
            _particleBuffer.Release();
            _particleBuffer = null;
        }

        if (_argsBuffer != null){
            _argsBuffer.Release();
            _argsBuffer = null;
        }
    }

}
