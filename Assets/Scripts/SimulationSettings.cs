using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Simulation Settings")]
public class SimulationSettings : ScriptableObject
{
    [Header("Bounds")]
    [SerializeField] public Vector3 emissionBox;
    [SerializeField] public Vector3 emissionBoxOffset;
    [SerializeField] public Vector3 bounds;

    [Header("Fluid Parameters")]
    public float h = 0.4f;
    public float gasConstant = 16f;
    public float restDensity = 1000f;
    public float particleMass = 0.1f;
    public float viscosityConstant = 2f;

    [Header("Simulation Parameters")]
    public float clickAndDragForce = 5f;
    public float deltaTime = 0.01f;
    [SerializeField] public Vector3 gravity;
    public float damping = 0;

}
