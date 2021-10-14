using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateBackground : MonoBehaviour
{
    [SerializeField] Color[] colors;
    int currentColor = 0;
    int nextColor = 1;
    [SerializeField] private float lerpSpeed = 1.0f;
    float timer = 0.0f;

    Camera mainCamera;

    void Start(){
        mainCamera = Camera.main;
    }

    void Update(){
        Camera.main.backgroundColor = Color.Lerp(colors[currentColor], colors[nextColor], timer/lerpSpeed);
        timer += Time.deltaTime;

        if(timer > lerpSpeed){
            int temp = nextColor;
            nextColor = (nextColor + 1) % colors.Length;
            currentColor = temp;
            timer = 0.0f;
        }
    }
}
