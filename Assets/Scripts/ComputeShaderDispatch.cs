using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct particle{
    //If changed, remember to update the shader, the buffer size
    //and keep it divisible by 128.
    //https://developer.nvidia.com/content/understanding-structured-buffer-performance
    public Vector3 position;
    public float density;
    public float pressure;
    public float mass;
    public Vector3 forces;
    public Vector3 velocity;
}

public class ComputeShaderDispatch : MonoBehaviour{

    [SerializeField] private ComputeShader shader;

    //Buffers to store our particle data.
    [SerializeField] private particle[] particles;  //On the CPU
    ComputeBuffer particleBuffer;                   //On the GPU

    private int pressureKernel;
    private int accelerationKernel;
    private int positionKernel;

    [Header("Particles")]
    [SerializeField] Material particleMaterial;
    [SerializeField] Mesh particleMesh;

    [Range(1, 65534)]
    [SerializeField] private int particleCount = 1024;

    [Header("Spawning Volume")]
    [SerializeField] private Vector3 emissionBox;
    [SerializeField] private Vector3 emissionBoxOffset;
    [SerializeField] private Vector3 bounds;

    [Header("Fluid Properties")]
    [SerializeField] private float h = 1.0f;
    [SerializeField] private float pressureConstant = 250.0f;
    [SerializeField] private float referenceDensity = 1.0f;
    [SerializeField] private float defaultMass = 1.0f;
    [SerializeField] private float viscosityConstant = 0.018f;
    [SerializeField] private float externalForce = 5;
    [SerializeField] private float deltaTime = 0.01f;

    private float defaultDensity = 0.0f;

    //Buffers that contain data about our particle mesh.
    ComputeBuffer meshTriangles;
    ComputeBuffer meshVertices;
    ComputeBuffer meshNormals;

    void Start(){
        Restart();
    }

    void Restart(){
        if(shader == null){
            Debug.LogError("Please attach a compute shader.", this);
            this.enabled = false;
        }
        particles = new particle[particleCount];

        for (int i = 0; i < particles.Length; i++){
            particles[i].position.x = Random.Range(-emissionBox.x, emissionBox.x) + emissionBoxOffset.x;
            particles[i].position.y = Random.Range(-emissionBox.y, emissionBox.y) + emissionBoxOffset.y;
            particles[i].position.z = Random.Range(-emissionBox.z, emissionBox.z) + emissionBoxOffset.z;
            particles[i].mass = defaultMass;
            particles[i].density = defaultDensity;
        }

        InitializeBuffers();
    }

    private void InitializeBuffers(){
        pressureKernel = shader.FindKernel("CalculatePressure");
        accelerationKernel = shader.FindKernel("CalculateAcceleration");
        positionKernel = shader.FindKernel("UpdateParticlePositions");

        //ComputeBuffer(count, stride) (number of elements, size of one element)
        //Here we're creating a compute buffer with particles length, each particle struct contains 8 floats.
        particleBuffer = new ComputeBuffer(particles.Length, sizeof(float)*12);
        particleBuffer.SetData(particles);

        shader.SetBuffer(pressureKernel, "particleBuffer", particleBuffer);
        shader.SetBuffer(accelerationKernel, "particleBuffer", particleBuffer);
        shader.SetBuffer(positionKernel, "particleBuffer", particleBuffer);

        shader.SetFloat("h", h);
        shader.SetFloat("pressureConstant", pressureConstant);
        shader.SetFloat("referenceDensity", referenceDensity);
        shader.SetFloat("viscosityConstant", viscosityConstant);

        shader.SetVector("bounds", bounds);
        shader.SetInt("particleCount", particles.Length);

        particleMaterial.SetBuffer("particles", particleBuffer);

        Vector3[] vertices = particleMesh.vertices;
        meshVertices = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        meshVertices.SetData(vertices);
        particleMaterial.SetBuffer("vertices", meshVertices);

        int[] triangles = particleMesh.triangles;
        meshTriangles = new ComputeBuffer(triangles.Length, sizeof(int));
        meshTriangles.SetData(triangles);
        particleMaterial.SetBuffer("triangles", meshTriangles);

        Vector3[] normals = particleMesh.normals;
        meshNormals = new ComputeBuffer(normals.Length, sizeof(float) * 3);
        meshNormals.SetData(normals);
        particleMaterial.SetBuffer("normals", meshNormals);
    }

    private void DisposeBuffers(){
        particleBuffer.Dispose();
        meshTriangles.Dispose();
        meshVertices.Dispose();
        meshNormals.Dispose();
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.Q)){
            DisposeBuffers();
            Restart();
            return;
        }

        shader.SetFloat("deltaTime", deltaTime);

        shader.SetFloat("h", h);
        shader.SetFloat("pressureConstant", pressureConstant);
        shader.SetFloat("referenceDensity", referenceDensity);
        shader.SetFloat("viscosityConstant", viscosityConstant);
        shader.SetVector("externalForcePoint", bounds);

        shader.SetFloat("poly6Constant", 315 / (64*Mathf.PI*Mathf.Pow(h,9)));
        shader.SetFloat("spikyConstant", 15 / (Mathf.PI * Mathf.Pow(h, 6)));
        shader.SetFloat("laplaceConstant", 45 / (Mathf.PI * Mathf.Pow(h, 6)));
        shader.SetFloat("particleMass", defaultMass);

        if(Input.GetKey(KeyCode.Space))
            shader.SetFloat("externalForceMagnitude", externalForce);
        else
            shader.SetFloat("externalForceMagnitude", 0);
        //Basicly "run" the CSMain function (kernel) in the compute shader using 16x16x1 threads.
        //Keep in mind that this is multiplied with the numthreads in the actual shader file.
        //The total number of threads must be more than the number of particles in our simulation, since
        //we're directly manipulating only one particle per thread.
        //We want this number to be as low as possible and the numthreads to be as large as possible.
        shader.Dispatch(pressureKernel, particleCount/1024, 1, 1);
        shader.Dispatch(accelerationKernel, particleCount/1024, 1, 1);
        shader.Dispatch(positionKernel, particleCount/1024, 1, 1);
        
        //Instead of actually having game objects represent each particle, we just take the results from 
        //our compute shader and directly renders that data.
        //https://forum.unity.com/threads/most-efficient-way-to-render-particle-sprites.487496/
        Graphics.DrawProcedural(material: particleMaterial, 
                                bounds: new Bounds(new Vector3(0,0,0), new Vector3(80,80,80)), 
                                topology: MeshTopology.Triangles, 
                                vertexCount:meshTriangles.count, 
                                instanceCount: particleCount, 
                                castShadows: ShadowCastingMode.Off, 
                                receiveShadows: false);
    }

    void OnDestroy(){
        DisposeBuffers();
    }

    void OnDrawGizmos(){
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, bounds*2);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(emissionBoxOffset, emissionBox*2);
    }
}