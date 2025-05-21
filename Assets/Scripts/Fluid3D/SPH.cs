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

    public bool useParentPosition = false;
    public float particleRadius = 0.1f;

    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);    

    [Header("Rendering")]
    public Mesh particleMesh;
    public Material particleMaterial;

    [Header("Compute Shader")]
    public ComputeShader computeShader;
    public Particle[] particles;
    private ComputeBuffer _particleBuffer;
    private GraphicsBuffer _argsBuffer;
    
    // private float[] gravityBuffer = new float[4 * 4];

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
        if (useParentPosition)
            spawnBasePosition = transform.position;
        // Initialize the SPH simulation particles
        InitializeParticles();

        /*
            GraphicsBuffer will handle particle mesh rendering
            @indexCountPerInstance: number of indices per mesh
            @instanceCount: number of mesh instances to draw
            @startIndex: index of the first index in the mesh (index = triple of vertices = triangles)
            @baseVertexIndex: index of the first vertex in the mesh
        */

        _argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        args[0].indexCountPerInstance = particleMesh.GetIndexCount(0);
        args[0].instanceCount = (uint)_particleCount;
        args[0].startIndex = particleMesh.GetIndexStart(0);
        args[0].baseVertexIndex = particleMesh.GetBaseVertex(0);
        args[0].startInstance = 0;

        _argsBuffer.SetData(args);

        
        /*        
            ComputeBuffer will handle particle simulation
            Here, we only set _particleBuffer on CPU once, 
            later updates directly on GPU by computeShader.
            @stride: size of each element in the buffer, sizeof(Particle) = 44 bytes
        */
        _particleBuffer = new ComputeBuffer(_particleCount, 44);
        _particleBuffer.SetData(particles);     
    }

    void Update()
    {
        /*
            In each frame, we would:
            1. Bind the variables, kernel index=0 is CSMain
            2. 64 particles share a thread group, each group has 64 threads
            3. dispatch work to computeShader, which directly updates on GPU.
        */
        computeShader.SetBuffer(0, "_ParticleBuffer", _particleBuffer);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);

        /* Use const in computeShader seems to fix the issue of not updating gravity */

        computeShader.SetVector("_Gravity", gravity);

        int threadGroupsX = Mathf.CeilToInt(_particleCount / 64.0f);
        
        computeShader.Dispatch(0, threadGroupsX, 1, 1);

        // Uncomment to debug particle data
        /*
            Particle[] debug = new Particle[_particleCount];
            _particleBuffer.GetData(debug);
            Debug.Log($"Particle 0 position: {debug[0].position}");
        */


        // Bind ComputeBuffer with the latest particle data (no copy)
        particleMaterial.SetBuffer("_ParticleBuffer", _particleBuffer);
        particleMaterial.SetFloat("_ParticleRadius", particleRadius);
        
        if (particleMaterial.SetPass(0)) {
            // Dispatch rendering request, go check Resources/ParticleShader.shader
            // Instanced: no GameObject for these meshes
            Graphics.DrawMeshInstancedIndirect(
                particleMesh,
                0,
                particleMaterial,
                new Bounds(Vector3.zero, spawnBounds * 50),
                _argsBuffer
            );
        }
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

    void OnRenderObject() // Called each frame, after all cameras have rendered, after Update
    {
        if (!Application.isPlaying || particles == null || particles.Length == 0 || _argsBuffer == null || _particleBuffer == null)
            return;
        // STOP VIBE CODING 
        // at least look for OnRenderObject() >:(
        // you render after camera renders, of course you see nothing
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
