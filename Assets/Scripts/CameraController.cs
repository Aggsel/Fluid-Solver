using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5.0f;

    void Update(){
        transform.rotation = Quaternion.Euler(new Vector3(Input.GetAxis("Vertical"), -Input.GetAxis("Horizontal"), 0) * Time.deltaTime * rotationSpeed + transform.rotation.eulerAngles);
        Camera.main.transform.position += Camera.main.transform.TransformVector(new Vector3(0, 0, Input.mouseScrollDelta.y));
    }
}