using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct particle{
    float x;
    float y;
    float z;
    float dummy;
}

public class ComputeShaderDispatch : MonoBehaviour{

    [SerializeField] private ComputeShader compute;

    //Buffers to store our particle data.
    private particle[] particles;   //On the CPU
    ComputeBuffer particlesBuffer;  //On the GPU

    [SerializeField] private int particleCount = 5000; 

    void Start(){
        if(compute == null){
            Debug.LogError("Please attach a compute shader.", this);
            this.enabled = false;
        }

        particles = new particle[particleCount];

        //ComputeBuffer(count, stride) (number of elements, size of one element)
        //Here we're creating a compute buffer with particles length, each particle struct contains 4 floats (each float being 4 bytes).
        particlesBuffer = new ComputeBuffer(particles.Length, 4*4);
        particlesBuffer.SetData(particles);
    }
}