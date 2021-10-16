using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private SliderWithTextInput viscositySlider;
    [SerializeField] private SliderWithTextInput gravitySlider;
    [SerializeField] private SliderWithTextInput boundsX;
    [SerializeField] private SliderWithTextInput boundsZ;
    [SerializeField] private Fluid fluid;

    [SerializeField] private GameObject sidebar;
    [SerializeField] private GameObject sidebarButton;

    [SerializeField] private Material particleMaterial;
    [SerializeField] private SliderWithTextInput particleSizeSlider;

    private SimulationSettings settingsCopy;

    public void Start(){
        ChangeSimulationSettings(fluid.settings);
        AddSliderListener(viscositySlider, UpdateViscosity);
        AddSliderListener(gravitySlider, UpdateGravity);
        AddSliderListener(boundsX, UpdateBoundsX);
        AddSliderListener(boundsZ, UpdateBoundsZ);
        AddSliderListener(particleSizeSlider, UpdateParticleSize);
    }

    public void ToggleSidebar(){
        sidebar.SetActive(!sidebar.activeInHierarchy);
        sidebarButton.SetActive(!sidebarButton.activeInHierarchy);
    }

    private void AddSliderListener(Slider slider, UnityAction<float> action){
        slider.onValueChanged.AddListener(new UnityAction<float>(action));
    }

    public void ChangeSimulationSettings(SimulationSettings newSettings){
        settingsCopy = Instantiate(newSettings);
        fluid.ChangeSimSettings(settingsCopy);
        SetUIElementsFromSimSettings();
    }

    private void SetUIElementsFromSimSettings(){
        viscositySlider.value = settingsCopy.viscosityConstant;
        gravitySlider.value = settingsCopy.gravity.y;
        boundsX.value = settingsCopy.bounds.x;
        boundsZ.value = settingsCopy.bounds.z;
        particleSizeSlider.value = particleMaterial.GetFloat("_ParticleScale");
    }

    private void UpdateViscosity(float value){
        settingsCopy.viscosityConstant = value;
        fluid.SetShaderFloat("viscosityConstant", settingsCopy.viscosityConstant);
    }

    private void UpdateGravity(float value){
        settingsCopy.gravity = new Vector3(0, value, 0);
        fluid.SetShaderVector("gravity", settingsCopy.gravity);
    }

    private void UpdateBoundsX(float value){
        settingsCopy.bounds = new Vector3(value, settingsCopy.bounds.y, settingsCopy.bounds.z);
        fluid.SetShaderVector("bounds", settingsCopy.bounds);
    }

    private void UpdateBoundsZ(float value){
        settingsCopy.bounds = new Vector3(settingsCopy.bounds.x, settingsCopy.bounds.y, value);
        fluid.SetShaderVector("bounds", settingsCopy.bounds);
    }

    private void UpdateParticleSize(float value){
        particleMaterial.SetFloat("_ParticleScale", value);
    }
}
