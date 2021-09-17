using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct particle{
    public float x;
    public float y;
    public float z;
    float dummy;
}

public class ComputeShaderDispatch : MonoBehaviour{

    [SerializeField] private ComputeShader shader;

    //Buffers to store our particle data.
    [SerializeField] private particle[] particles;   //On the CPU
    ComputeBuffer particleBuffer;  //On the GPU

    private GameObject[] particlesObjects;

    [SerializeField] private int particleCount = 1024;
    private int CSMainKernel;

    [Header("Draw Procedural")]
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;

    // ComputeBuffer resultBuffer;

    //Contains triangle data for the particle mesh.
    ComputeBuffer meshTriangles;
    //Contains vertices data for the particle mesh.
    ComputeBuffer meshVertices;

    Vector3[] vertices;
    int[] triangles;

    void Start(){
        if(shader == null){
            Debug.LogError("Please attach a compute shader.", this);
            this.enabled = false;
        }
        particles = new particle[particleCount];


        //ComputeBuffer(count, stride) (number of elements, size of one element)
        //Here we're creating a compute buffer with particles length, each particle struct contains 4 floats (each float being 4 bytes).
        particleBuffer = new ComputeBuffer(particles.Length, sizeof(float)*4);
        particleBuffer.SetData(particles);

        CSMainKernel = shader.FindKernel("CSMain");
        shader.SetBuffer(CSMainKernel, "particleBuffer", particleBuffer);
        material.SetBuffer("particles", particleBuffer);

        vertices = mesh.vertices;
        meshVertices = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        meshVertices.SetData(vertices);
        material.SetBuffer("vertices", meshVertices);

        triangles = mesh.triangles;
        meshTriangles = new ComputeBuffer(triangles.Length, sizeof(int));
        meshTriangles.SetData(triangles);
        material.SetBuffer("triangles", meshTriangles);

    }

    void Update(){
        shader.SetFloat("time", Time.time);
        //Basicly "run" the CSMain function (kernel) in the compute shader using 16x16x1 threads.
        //Keep in mind that this is multiplied with the numthreads in the actual shader file.
        //The total number of threads must be more than the number of particles in our simulation, since
        //we're directly manipulating only one particle per thread.
        shader.Dispatch(CSMainKernel, 32, 1, 1);
        
        //Instead of actually having game objects represent each particle, we just take the results from 
        //our compute shader and directly renders that data.
        //https://forum.unity.com/threads/most-efficient-way-to-render-particle-sprites.487496/
        Graphics.DrawProcedural(material, new Bounds(new Vector3(0,0,0), new Vector3(80,80,80)), MeshTopology.Triangles, meshTriangles.count, 1024);

        //DEBUG!!
        particleBuffer.GetData(particles);
        Debug.Log(string.Format("({0}, {1}, {2})", particles[0].x, particles[0].y, particles[0].z));
        
    }

    void OnDestroy(){
        particleBuffer.Dispose();
        meshTriangles.Dispose();
        meshVertices.Dispose();
    }
}