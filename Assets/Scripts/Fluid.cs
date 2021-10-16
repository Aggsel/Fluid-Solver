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

public class Fluid : MonoBehaviour{

    [SerializeField] public SimulationSettings settings;
    [SerializeField] private ComputeShader shader;
    private Camera mainCamera;

    //Buffers to store our particle data.
    [SerializeField] private particle[] particles;
    ComputeBuffer particleBuffer;

    private int pressureKernel;
    private int forcesKernel;
    private int positionKernel;

    // [SerializeField] private GameObject obstacle;

    [Header("Particles")]
    [SerializeField] Material particleMaterial;
    [SerializeField] Mesh particleMesh;

    [Range(1, 65534)]
    [SerializeField] private int particleCount = 8192;

    private Vector3 actualBounds;

    [Header("Simulation Parameters")]
    [SerializeField] private float externalForceMagnitude = 10f;
    private Vector3 externalForcePoint;
    [SerializeField] private Vector3 gravity;

    //Buffers that contain data about our particle mesh.
    ComputeBuffer meshTriangles;
    ComputeBuffer meshVertices;
    ComputeBuffer meshNormals;

    void Awake(){
        mainCamera = Camera.main;
        Restart();
    }

    public void Restart(){
        if(shader == null){
            Debug.LogError("Please attach a compute shader.", this);
            this.enabled = false;
        }
        particles = new particle[particleCount];

        Vector3 tempEmissionBox = new Vector3(  Mathf.Min(settings.emissionBox.x, settings.bounds.x),
                                                Mathf.Min(settings.emissionBox.y, settings.bounds.y),
                                                Mathf.Min(settings.emissionBox.z, settings.bounds.z));

        for (int i = 0; i < particles.Length; i++){
            particles[i].position.x = Random.Range(-tempEmissionBox.x, tempEmissionBox.x) + settings.emissionBoxOffset.x;
            particles[i].position.y = Random.Range(-tempEmissionBox.y, tempEmissionBox.y) + settings.emissionBoxOffset.y;
            particles[i].position.z = Random.Range(-tempEmissionBox.z, tempEmissionBox.z) + settings.emissionBoxOffset.z;
            particles[i].density = 0.0f;
        }

        InitializeBuffers();
        CopySettingsToShader();
    }

    public void CopySettingsToShader(){
        //Initialize fluid specific parameters.
        shader.SetFloat("h", settings.h);
        shader.SetFloat("gasConstant", settings.gasConstant);
        shader.SetFloat("restDensity", settings.restDensity);
        shader.SetFloat("viscosityConstant", settings.viscosityConstant);

        //Initialize simulation parameters.
        shader.SetVector("bounds", settings.bounds);
        shader.SetInt("particleCount", particles.Length);
        shader.SetFloat("deltaTime", settings.deltaTime);
        shader.SetFloat("particleMass", settings.particleMass);
        shader.SetFloat("damping", settings.damping);
        shader.SetVector("gravity", settings.gravity);
    }

    private void InitializeBuffers(){
        pressureKernel = shader.FindKernel("CalculatePressure");
        forcesKernel = shader.FindKernel("CalculateForces");
        positionKernel = shader.FindKernel("UpdateParticlePositions");

        //ComputeBuffer(count, stride) (number of elements, size of one element)
        //Here we're creating a compute buffer with particles length, each particle struct contains 12 floats.
        particleBuffer = new ComputeBuffer(particles.Length, sizeof(float)*11);
        particleBuffer.SetData(particles);

        //Make sure all 3 shader kernels and the material can access the particle buffer.
        shader.SetBuffer(pressureKernel, "particleBuffer", particleBuffer);
        shader.SetBuffer(forcesKernel, "particleBuffer", particleBuffer);
        shader.SetBuffer(positionKernel, "particleBuffer", particleBuffer);
        particleMaterial.SetBuffer("particles", particleBuffer);

        //Precompute the constants in the smoothing kernel functions.
        shader.SetFloat("poly6Constant", 315 / (64*Mathf.PI*Mathf.Pow(settings.h,9)));
        shader.SetFloat("spikyConstant", 15 / (Mathf.PI * Mathf.Pow(settings.h, 6)));
        shader.SetFloat("laplaceConstant", 45 / (Mathf.PI * Mathf.Pow(settings.h, 6)));

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
            shader.SetVector("bounds", settings.bounds);
        }

        //Handle ability to click and drag particles around the screen.
        if(Input.GetMouseButton(0) || Input.GetMouseButton(1)){
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0;
            Plane raycastPlane = new Plane(forward.normalized, Vector3.zero);
            Ray ray = mainCamera.ViewportPointToRay(mainCamera.ScreenToViewportPoint(Input.mousePosition));
            float distance;
            if(raycastPlane.Raycast(ray, out distance)){
                externalForcePoint = ray.GetPoint(distance);
                externalForceMagnitude = Input.GetMouseButton(0) ? settings.clickAndDragForce : -settings.clickAndDragForce;

                shader.SetVector("externalForcePoint", externalForcePoint);
            }
        }
        else{
            externalForceMagnitude = 0.0f;
        }
        shader.SetFloat("externalForceMagnitude", externalForceMagnitude);
        //Dispatch shaders, updating the particle positions in the buffers.

        for (int i = 0; i < settings.substeps; i++){
            shader.Dispatch(pressureKernel, particleCount/1024, 1, 1);
            shader.Dispatch(forcesKernel, particleCount/1024, 1, 1);
            shader.Dispatch(positionKernel, particleCount/1024, 1, 1);
        }
        
        //Instead of passing the results from our compute shaders back to the CPU when done, our material
        //can directly access the particle positions from the same buffers.
        //https://forum.unity.com/threads/most-efficient-way-to-render-particle-sprites.487496/
        Graphics.DrawProcedural(material: particleMaterial, 
                                bounds: new Bounds(transform.position, settings.bounds*2), 
                                topology: MeshTopology.Triangles, 
                                vertexCount:meshTriangles.count, 
                                instanceCount: particleCount, 
                                castShadows: ShadowCastingMode.On, 
                                receiveShadows: true);
    }

    public void ChangeSimSettings(SimulationSettings newSettings){
        this.settings = newSettings;
        CopySettingsToShader();
    }

    public void SetShaderFloat(string variableName, float value){
        shader.SetFloat(variableName, value);
    }

    public void SetShaderVector(string variableName, Vector3 value){
        shader.SetVector(variableName, value);
    }

    void OnDestroy(){
        DisposeBuffers();
    }

    void OnDrawGizmos(){
        if(settings == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, settings.bounds*2);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(settings.emissionBoxOffset, settings.emissionBox*2);
    }
}