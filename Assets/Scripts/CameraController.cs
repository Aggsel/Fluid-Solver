using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5.0f;
    void Update(){
        transform.rotation = Quaternion.Euler(new Vector3(0, -Input.GetAxis("Horizontal") * Time.deltaTime * rotationSpeed, 0) + transform.rotation.eulerAngles);        
    }
}