using UnityEngine;
using System.Collections;

public class InitAddColorPanel : MonoBehaviour {

    public GameObject addColorPanel;

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            addColorPanel.SetActive(false);
        }
	}
}
