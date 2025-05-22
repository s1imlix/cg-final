using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using CGFinal.Helpers;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 44)]
public struct Particle
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 predictPosition;
    public float density;
    public float nearDensity;
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
struct Entry
{
    uint pointIndex;
	uint cellHash;
    uint cellKey;
};

public class SPH : MonoBehaviour
{
    [Header("Constants")]
    public int MAX_PARTICLES = 100000;
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
    public float targetDensity = 1000f;  
    public float pressureMultiplier = 0.1f;
    public float ngbPressureMultiplier = 0.1f;

    public float particleMass = 1f;
    public float particleRadius = 0.1f; // radius of the particle

    public float viscosityStrength = 0.1f;

    public float collisionDamping = 0.1f;

    [Header("Spawner settings")]
    public Vector3Int numToSpawn = new Vector3Int(10, 10, 10);
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

    const int ExternalGravity = 0;
    const int UpdatePositions = 1;

    const int calcDensity = 2;
    const int calcPressureForce = 3;
    const int calcViscosityForce = 4;
    const int UpdateSpatialHash = 5;

    GPUSort gpuBMS;
    private ComputeBuffer _spatialLookupBuffer;
    private ComputeBuffer _startIndicesBuffer;

    private ComputeBuffer _debug;
    private uint[] _debugInit = new uint[1]{0};

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

                    float px = (dx - 0.5f) * transform.localScale.x;
                    float py = (dy - 0.5f) * transform.localScale.y;
                    float pz = (dz - 0.5f) * transform.localScale.z;

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
        Debug.Log("Graphics Device: " + SystemInfo.graphicsDeviceName);
        Debug.Log("Graphics Vendor: " + SystemInfo.graphicsDeviceVendor);
        Debug.Log("Graphics Type: " + SystemInfo.graphicsDeviceType);

        /*        
            ComputeBuffer will handle particle simulation
            Here, we only set _particleBuffer on CPU once, 
            later updates directly on GPU by computeShader.
            @stride: size of each element in the buffer, sizeof(Particle) = 44 bytes
        */
        _particleBuffer = ComputeHelper.CreateStructBuffer(particles); 
        _spatialLookupBuffer = ComputeHelper.CreateStructBuffer<Entry>(_particleCount);
        _startIndicesBuffer = ComputeHelper.CreateStructBuffer<uint>(_particleCount);
        // _debug = ComputeHelper.CreateStructBuffer<uint>(1);
        ComputeHelper.SetBuffer(computeShader, _particleBuffer, "_ParticleBuffer", 
                                ExternalGravity, UpdatePositions, calcDensity, calcPressureForce, calcViscosityForce, UpdateSpatialHash);
        ComputeHelper.SetBuffer(computeShader, _spatialLookupBuffer, "_SpatialLookupBuffer",
                                calcDensity, calcPressureForce, calcViscosityForce, UpdateSpatialHash);
        ComputeHelper.SetBuffer(computeShader, _startIndicesBuffer, "_startIndicesBuffer",
                                calcDensity, calcPressureForce, calcViscosityForce, UpdateSpatialHash);
        // ComputeHelper.SetBuffer(computeShader, _debug, "_DebugBuffer", calcPressureForce);
        gpuBMS = new();
        gpuBMS.SetBuffers(_spatialLookupBuffer, _startIndicesBuffer);   

        particleRenderer.Init(this);

    }

    void SimulateFrame(float deltaTime)
    {
        if (isPaused) return;
        float timeStep = deltaTime / iterationPerFrame * timeScale;

        UpdateSettings(timeStep);

        for (int i = 0; i<iterationPerFrame; i++) {
            Simulate();
            // onSimulationComplete?.Invoke();
        }
    }

    void UpdateSettings(float timeStep) {
        // Set global variables in compute shader
        computeShader.SetFloat("_DeltaTime", timeStep);
        computeShader.SetVector("halfBoxScale", transform.localScale * 0.5f);
        computeShader.SetFloat("targetDensity", targetDensity);
        computeShader.SetFloat("pressureMultiplier", pressureMultiplier);
        computeShader.SetFloat("ngbPressureMultiplier", ngbPressureMultiplier);
        computeShader.SetFloat("viscosityStrength", viscosityStrength);
        computeShader.SetFloat("mass", particleMass);
        computeShader.SetFloat("radius", particleRadius);
        computeShader.SetFloat("squaredRadius", particleRadius * particleRadius);
        computeShader.SetFloat("collisionDamping", collisionDamping);
        computeShader.SetInt("numParticles", _particleCount);
        computeShader.SetVector("_Gravity", gravity);
        computeShader.SetMatrix("worldToLocal", transform.worldToLocalMatrix);
        computeShader.SetMatrix("localToWorld", transform.localToWorldMatrix);

        // _debug.SetData(_debugInit); // Counts total time of interaction
    }

    void Simulate() {
        // Dispatch your work here
        // Particle[] _particles = ComputeHelper.DebugStructBuffer<Particle>(_particleBuffer, 1);
        // Debug.Log($"Particle velocity: {_particles[0].velocity}, density: {_particles[0].density}");
        ComputeHelper.Dispatch(computeShader, _particleCount, ExternalGravity);
        ComputeHelper.Dispatch(computeShader, _particleCount, UpdateSpatialHash); 
        gpuBMS.SortAndCalculateOffsets();
        ComputeHelper.Dispatch(computeShader, _particleCount, calcDensity);
        ComputeHelper.Dispatch(computeShader, _particleCount, calcPressureForce);
        ComputeHelper.Dispatch(computeShader, _particleCount, calcViscosityForce);
        ComputeHelper.Dispatch(computeShader, _particleCount, UpdatePositions);

        // Debug.Log($"DebugBuffer: {ComputeHelper.DebugStructBuffer<uint>(_debug, 1)[0]}");
    }

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

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale*2);

        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnBasePosition, 0.1f);
        }
    }

    void OnDestroy(){
        ComputeHelper.Release(_particleBuffer, _spatialLookupBuffer, _startIndicesBuffer);
    }

}
