using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class SliderWithTextInput : Slider
{
    [SerializeField] private TextMeshProUGUI displayValue;
    [SerializeField] private TMP_InputField inputField;
    private UnityAction<string> onTextInputChange;
    private float defaultMin, defaultMax;

    protected override void Start(){
        base.Start();
        UpdateHandleText();
        UpdateInputField();
        onTextInputChange += OnTextInputChange;
        inputField.onEndEdit.AddListener(onTextInputChange);
        defaultMin = minValue;
        defaultMax = maxValue;
    }

    public override void OnPointerUp(PointerEventData eventData){
        UpdateHandleText();
        UpdateInputField();
        minValue = defaultMin;
        maxValue = defaultMax;
    }

    public override void OnDrag(PointerEventData eventData){
        base.OnDrag(eventData);
        UpdateHandleText();
        UpdateInputField();
        minValue = defaultMin;
        maxValue = defaultMax;
    }

    private void UpdateHandleText(){
        if(displayValue != null)
            displayValue.text = this.value.ToString("0.00");
    }

    private void UpdateInputField(){
        if(inputField != null)
            inputField.text = this.value.ToString("0.00");
    }

    private void OnTextInputChange(string inputFieldText){
        float newValue = float.Parse(inputFieldText);
        minValue = Mathf.Min(defaultMin, newValue);
        maxValue = Mathf.Max(defaultMax, newValue);
        this.value = newValue;
        UpdateHandleText();
    }
}
