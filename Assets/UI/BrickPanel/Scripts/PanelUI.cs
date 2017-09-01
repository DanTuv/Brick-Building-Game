using UnityEngine;
using System.Collections;

public class PanelUI : MonoBehaviour {

    public GameObject panel;

    public void toggleBrickPanel()
    {
        panel.SetActive(!panel.activeSelf);
    }

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
