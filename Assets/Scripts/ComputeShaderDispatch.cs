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
    public Vector3 forces;
    public Vector3 velocity;
}

public class ComputeShaderDispatch : MonoBehaviour{

    [SerializeField] private ComputeShader shader;

    //Buffers to store our particle data.
    [SerializeField] private particle[] particles;
    ComputeBuffer particleBuffer;

    private int pressureKernel;
    private int accelerationKernel;
    private int positionKernel;

    // [SerializeField] private GameObject obstacle;

    [Header("Particles")]
    [SerializeField] Material particleMaterial;
    [SerializeField] Mesh particleMesh;

    [Range(1, 65534)]
    [SerializeField] private int particleCount = 8192;

    [Header("Bounds")]
    [SerializeField] private Vector3 emissionBox;
    [SerializeField] private Vector3 emissionBoxOffset;
    [SerializeField] private Vector3 bounds;
    private Vector3 actualBounds;

    [Header("Fluid Parameters")]
    [SerializeField] private float h = 1.0f;
    [SerializeField] private float pressureConstant = 250.0f;
    [SerializeField] private float referenceDensity = 1.0f;
    [SerializeField] private float particleMass = 0.1f;
    [SerializeField] private float viscosityConstant = 0.018f;

    [Header("Simulation Parameters")]
    [SerializeField] private float externalForceMagnitude = 10;
    private Vector3 externalForcePoint;
    [SerializeField] private float deltaTime = 0.01f;
    [SerializeField] private Vector3 gravity;
    [SerializeField] private float dampening;

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
            particles[i].density = 0.0f;
        }

        InitializeBuffers();
    }

    private void InitializeBuffers(){
        pressureKernel = shader.FindKernel("CalculatePressure");
        accelerationKernel = shader.FindKernel("CalculateAcceleration");
        positionKernel = shader.FindKernel("UpdateParticlePositions");

        //ComputeBuffer(count, stride) (number of elements, size of one element)
        //Here we're creating a compute buffer with particles length, each particle struct contains 12 floats.
        particleBuffer = new ComputeBuffer(particles.Length, sizeof(float)*11);
        particleBuffer.SetData(particles);

        //Make sure all 3 shader kernels and the material can access the particle buffer.
        shader.SetBuffer(pressureKernel, "particleBuffer", particleBuffer);
        shader.SetBuffer(accelerationKernel, "particleBuffer", particleBuffer);
        shader.SetBuffer(positionKernel, "particleBuffer", particleBuffer);
        particleMaterial.SetBuffer("particles", particleBuffer);

        //Initialize fluid specific parameters.
        shader.SetFloat("h", h);
        shader.SetFloat("pressureConstant", pressureConstant);
        shader.SetFloat("referenceDensity", referenceDensity);
        shader.SetFloat("viscosityConstant", viscosityConstant);
        
        //Initialize simulation parameters.
        shader.SetVector("bounds", bounds);
        shader.SetInt("particleCount", particles.Length);
        shader.SetFloat("deltaTime", deltaTime);
        shader.SetFloat("particleMass", particleMass);
        shader.SetFloat("dampening", dampening);
        shader.SetVector("gravity", gravity);

        //Precompute the constants in the smoothing kernel functions.
        shader.SetFloat("poly6Constant", 315 / (64*Mathf.PI*Mathf.Pow(h,9)));
        shader.SetFloat("spikyConstant", 15 / (Mathf.PI * Mathf.Pow(h, 6)));
        shader.SetFloat("laplaceConstant", 45 / (Mathf.PI * Mathf.Pow(h, 6)));

        //Initialize vertex, triangle and normal buffers for our rendering.
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
        if(Input.GetKeyDown(KeyCode.R)){
            DisposeBuffers();
            Restart();
            return;
        }

        if(Input.GetKey(KeyCode.B)){
            shader.SetVector("bounds", new Vector3(300,300,300));
        }
        else{
            shader.SetVector("bounds", bounds);
        }

        //Handle ability to click and drag particles around the screen.
        if(Input.GetMouseButton(0)){
            Plane raycastPlane = new Plane(-transform.forward, transform.position);
            Ray ray = Camera.main.ViewportPointToRay(Camera.main.ScreenToViewportPoint(Input.mousePosition));
            float distance;
            if(raycastPlane.Raycast(ray, out distance)){
                externalForcePoint = ray.GetPoint(distance);
                externalForceMagnitude = 5.0f;

                shader.SetVector("externalForcePoint", externalForcePoint);
            }
        }
        else{
            externalForceMagnitude = 0.0f;
        }
        shader.SetFloat("externalForceMagnitude", externalForceMagnitude);

        //Dispatch shaders, updating the particle positions in the buffers.
        shader.Dispatch(pressureKernel, particleCount/1024, 1, 1);
        shader.Dispatch(accelerationKernel, particleCount/1024, 1, 1);
        shader.Dispatch(positionKernel, particleCount/1024, 1, 1);
        
        //Instead of passing the results from our compute shaders back to the CPU when done, our material
        //can directly access the particle positions from the same buffers.
        //https://forum.unity.com/threads/most-efficient-way-to-render-particle-sprites.487496/
        Graphics.DrawProcedural(material: particleMaterial, 
                                bounds: new Bounds(transform.position, bounds*2), 
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