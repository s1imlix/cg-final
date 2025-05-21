using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using CGFinal.Helpers;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

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
    public int _particleCount {
        get {
            return Mathf.Min(numToSpawn.x * numToSpawn.y * numToSpawn.z, MAX_PARTICLES);
        }
    }

    [Header("Simulation settings")]
    public bool pauseNextFrame = false;
    private bool isPaused;
    public int iterationPerFrame = 1;
    public float timeScale = 1f;

    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);   

    [Header("Spawner settings")]
    public Vector3Int numToSpawn = new Vector3Int(10, 10, 10);
    public float spawnBoundSize = 1f;
    public Vector3 spawnBasePosition = new Vector3(0f, 0f, 0f);
    public bool useParentPosition = true;
    public float jitLength = 0.5f;

    public Vector3 initVelocity = new Vector3(0f, 5f, 0f);  

    [Header("Rendering")]

    public ParticleRenderer particleRenderer;
    public UnityEvent onSimulationComplete;

    [Header("Compute Shader")]
    public ComputeShader computeShader;
    public Particle[] particles;
    public ComputeBuffer _particleBuffer;

    public FixedRadiusNeighbourSearch fixedRadiusNeighbourSearch = new FixedRadiusNeighbourSearch();
    // GPU buffer 用來傳 spatial lookup 結果給 compute shader
    private ComputeBuffer _spatialLookupBuffer;
    private ComputeBuffer _startIndicesBuffer;

    const int CSMain = 0;
    const int ExternalGravity = 1;
    const int HandleBoundingBoxCollision = 2;
    const int SpatialQueryKernel = 3;
    

    void InitializeParticles()
    {
        if (useParentPosition)
            spawnBasePosition = transform.position;

        int numParticlesX = numToSpawn.x;
        int numParticlesY = numToSpawn.y;
        int numParticlesZ = numToSpawn.z;

        List<Particle> _particles = new List<Particle>();

        // Spawn particles in a grid pattern, center is (0.5, 0.5, 0.5)
        for (int x = 0; x < numParticlesX; x++)
        {
            for (int y = 0; y < numParticlesY; y++)
            {
                for (int z = 0; z < numParticlesZ; z++)
                {
                    int index = x * numParticlesY * numParticlesZ + y * numParticlesZ + z;
                    if (index >= MAX_PARTICLES) break;

                    float dx = x / (numParticlesX - 1);
                    float dy = y / (numParticlesY - 1);
                    float dz = z / (numParticlesZ - 1);

                    float px = (dx - 0.5f) * spawnBoundSize;
                    float py = (dy - 0.5f) * spawnBoundSize;
                    float pz = (dz - 0.5f) * spawnBoundSize;

                    // Calculate the position of the particle
                    Vector3 _offset = new Vector3(px, py, pz) * particleRenderer.particleRadius;
                    Vector3 jit = UnityEngine.Random.insideUnitSphere * jitLength;
                    Vector3 _position = spawnBasePosition + _offset + jit;
                    
                    _particles.Add(new Particle{
                        position = _position,
                        velocity = initVelocity,
                    });
                }
            }
        }
        particles = _particles.ToArray();
    }

    void SetInitialBuffer()
    {
        InitializeParticles();
        // Set the initial data for the compute buffer
        _particleBuffer.SetData(particles);
    }
    void Start()
    {
        InitializeParticles();
        /*        
            ComputeBuffer will handle particle simulation
            Here, we only set _particleBuffer on CPU once, 
            later updates directly on GPU by computeShader.
            @stride: size of each element in the buffer, sizeof(Particle) = 44 bytes
        */
        _particleBuffer = ComputeHelper.CreateStructBuffer(particles);  
        ComputeHelper.SetBuffer(computeShader, _particleBuffer, "_ParticleBuffer", 
                                CSMain, ExternalGravity, HandleBoundingBoxCollision);

        // 預留初始大小（與粒子數同），之後可 Resize
        _spatialLookupBuffer = new ComputeBuffer(_particleCount, sizeof(uint) * 2); // Entry: uint + uint
        _startIndicesBuffer = new ComputeBuffer(_particleCount, sizeof(uint));

        particleRenderer.Init(this);
    }

    void SimulateFrame(float deltaTime)
    {
        if (isPaused) return;
        float timeStep = deltaTime / iterationPerFrame * timeScale;

        UpdateSettings();

        for (int i = 0; i<iterationPerFrame; i++) {
            Simulate();
            // onSimulationComplete?.Invoke();
        }
    }

    void UpdateSettings() {
        // Set global variables in compute shader
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetVector("_Gravity", gravity);
    }

    /*
    void Simulate() {
        // Dispatch your work here
        ComputeHelper.Dispatch(computeShader, _particleCount, CSMain); 
        Particle[] debug = ComputeHelper.DebugStructBuffer<Particle>(_particleBuffer, _particleCount);
        Debug.Log($"First particle v: {debug[0].velocity}");
    }
    */

    void Simulate()
    {
        // GPU 執行第一個 kernel（例如加重力 + 預測位置）
        ComputeHelper.Dispatch(computeShader, _particleCount, CSMain);

        // Step 1：從 GPU 擷取位置
        Vector3[] positions3D = ComputeHelper.DebugVector3Buffer(_particleBuffer, _particleCount);
        Vector2[] positions2D = positions3D.Select(p => new Vector2(p.x, p.y)).ToArray();

        // Step 2：更新 spatial lookup（在 CPU）
        float radius = particleRenderer.particleRadius;
        fixedRadiusNeighbourSearch.UpdateSpatialLookup(positions2D, radius);

        // Step 3：把資料傳回 GPU
        var entries = fixedRadiusNeighbourSearch.SpatialLookup;
        var startIndices = fixedRadiusNeighbourSearch.StartIndices;

        _spatialLookupBuffer.SetData(entries);
        _startIndicesBuffer.SetData(startIndices);

        // Step 4：SetBuffer + Dispatch SpatialQueryKernel
        int spatialQueryKernel = computeShader.FindKernel("SpatialQueryKernel");

        computeShader.SetBuffer(spatialQueryKernel, "_SpatialLookup", _spatialLookupBuffer);
        computeShader.SetBuffer(spatialQueryKernel, "_StartIndices", _startIndicesBuffer);
        computeShader.SetFloat("_Radius", radius);
        computeShader.SetInt("_NumPoints", _particleCount);
        computeShader.SetInt("_SpatialLookupLength", entries.Length);

        ComputeHelper.Dispatch(computeShader, _particleCount, spatialQueryKernel);
    }

    /*
    // predict position (but chatGPT say simulate() already do the job)
    void SimulationStep(float deltaTime)
    {
        // Apply gravity and predict next positions
        Parallel.For(0, numParticles, i => {
            velocities[i] += Vector2.down * gravity * deltaTime;
            predictedPositions[i] = positions[i] + velocities[i] * deltaTime;
        });

        // Update spatial lookup with predicted positions
        fixedRadiusNeighbourSearch.UpdateSpatialLookup(predictedPositions, smoothingRadius);

        // Calculate densities
        Parallel.For(0, numParticles, i => {
            densities[i] = CalculateDensity(predictedPositions[i]);
        });

        // Calculate and apply pressure forces
        Parallel.For(0, numParticles, i => {
            Vector2 pressureForce = CalculatePressureForce(i);
            Vector2 pressureAcceleration = pressureForce / densities[i];
            velocities[i] += pressureAcceleration * deltaTime;
        });

        // Update positions and resolve collisions
        Parallel.For(0, numParticles, i => {
            positions[i] += velocities[i] * deltaTime;
            ResolveCollisions(ref positions[i], ref velocities[i]);
        });
    }
    */


    void Update() {
        if (Time.frameCount > 10) {
            SimulateFrame(Time.deltaTime);
        }

        if (pauseNextFrame) {
            pauseNextFrame = false;
            isPaused = true;
        }

        HandleInput();
    }

    void HandleInput()
    {
        var keyboard = Keyboard.current;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isPaused = !isPaused;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            isPaused = false;
            SetInitialBuffer();
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            pauseNextFrame = true;
            isPaused = false;
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
                Gizmos.DrawSphere(particles[i].position, particleRenderer.particleRadius);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(spawnBasePosition, spawnBoundSize * Vector3.one);

        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnBasePosition, 0.1f);
        }
    }

    void OnDestroy(){
        ComputeHelper.Release(_particleBuffer);
        ComputeHelper.Release(_spatialLookupBuffer);
        ComputeHelper.Release(_startIndicesBuffer);
    }

}
