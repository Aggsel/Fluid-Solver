using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct particle{
    public float x;
    public float y;
    public float z;
    float dummy;
}

public class ComputeShaderDispatch : MonoBehaviour{

    [SerializeField] private ComputeShader shader;

    //Buffers to store our particle data.
    [SerializeField] private particle[] particles;  //On the CPU
    ComputeBuffer particleBuffer;                   //On the GPU

    private int CSMainKernel;

    [Header("Particles")]
    [SerializeField] Material particleMaterial;
    [SerializeField] Mesh particleMesh;

    [Range(1, 65534)]
    [SerializeField] private int particleCount = 1024;

    //Buffers that contain data about our particle mesh.
    ComputeBuffer meshTriangles;
    ComputeBuffer meshVertices;
    ComputeBuffer meshNormals;

    void Start(){
        if(shader == null){
            Debug.LogError("Please attach a compute shader.", this);
            this.enabled = false;
        }
        particles = new particle[particleCount];

        InitializeBuffers();
    }

    private void InitializeBuffers(){
        CSMainKernel = shader.FindKernel("CSMain");

        //ComputeBuffer(count, stride) (number of elements, size of one element)
        //Here we're creating a compute buffer with particles length, each particle struct contains 4 floats (each float being 4 bytes).
        particleBuffer = new ComputeBuffer(particles.Length, sizeof(float)*4);
        particleBuffer.SetData(particles);
        shader.SetBuffer(CSMainKernel, "particleBuffer", particleBuffer);
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
        shader.SetFloat("time", Time.time);
        //Basicly "run" the CSMain function (kernel) in the compute shader using 16x16x1 threads.
        //Keep in mind that this is multiplied with the numthreads in the actual shader file.
        //The total number of threads must be more than the number of particles in our simulation, since
        //we're directly manipulating only one particle per thread.
        shader.Dispatch(CSMainKernel, particleCount, 1, 1);
        
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
}