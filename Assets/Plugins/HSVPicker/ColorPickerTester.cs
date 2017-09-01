﻿using UnityEngine;
using System.Collections;

public class ColorPickerTester : MonoBehaviour 
{

    public new Renderer renderer;
    public HSVPicker picker;

	// Use this for initialization
	void Start () 
    {
        picker.onValueChanged.AddListener(color =>
        {
            renderer.sharedMaterial.color = color;
        });
		renderer.sharedMaterial.color = picker.currentColor;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
