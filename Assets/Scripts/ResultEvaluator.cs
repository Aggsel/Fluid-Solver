using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class ResultEvaluator : MonoBehaviour
{
    private int particles = 0;
    [SerializeField] private string filename = "results.txt";
    private string path = "";

    [SerializeField] private int framesPerEvaluation = 2000;
    private float deltaTimes = 0.0f;
    private int frameCounter = 0;
    [SerializeField] private float timeBetweenTests = 1.0f;
    private float timer = 0.0f;
    [SerializeField] private int maxParticles = 68000;

    [SerializeField] private Fluid fluid;

    void Start(){
        Debug.Log(SystemInfo.graphicsDeviceVersion);
        path = $"{Application.dataPath}/{filename}";
        if (!File.Exists(path))
            File.Delete(path);

        StreamWriter sw = File.CreateText(path);
        sw.Close();
    }

    void Update(){
        timer += Time.deltaTime;
        if(timer > timeBetweenTests){
            //Gather data
            deltaTimes += Time.deltaTime;
            frameCounter++;
            if(frameCounter >= framesPerEvaluation){
                WriteResults();
                NextTest();
            }
        }
    }

    private void WriteResults(){
        using (StreamWriter sw = File.AppendText(path)){
            sw.WriteLine($"Particles: {particles}, Frames: {frameCounter}, Total duration: {deltaTimes}, Avg. time per frame: {deltaTimes/frameCounter}");
        }
    }

    private void NextTest(){
        particles += 1024;
        if(particles >= maxParticles){
            Debug.Log("Testing complete.");
            this.enabled = false;
        }
        timer = 0.0f;
        deltaTimes = 0.0f;
        frameCounter = 0;

        fluid.particleCount = particles;
        fluid.DisposeBuffers();
        fluid.Restart();
    }
}
