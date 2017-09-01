using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class KeyboardShortcuts : MonoBehaviour {

    GameObject brickPanel;
    GameObject rotateButton;
    GameObject rotateXbtn;
    GameObject rotateZbtn;
    PlacerBrick placerBrickScript;
    SelectScript selectScript;
    KeyCode rotateBrickKey1 = KeyCode.R;
    KeyCode rotateBrickKey2 = KeyCode.Mouse1;
    KeyCode rotateXKey = KeyCode.E;
    KeyCode rotateZKey = KeyCode.Q;

    // Use this for initialization
    void Start () {
        rotateButton = GameObject.Find("btnRotateY");
        rotateXbtn = GameObject.Find("btnRotateX");
        rotateZbtn = GameObject.Find("btnRotateZ");
        placerBrickScript = GameObject.Find("PlacerBrickScript").GetComponent<PlacerBrick>();
        selectScript = GameObject.Find("SelectScript").GetComponent<SelectScript>();
        brickPanel = GameObject.Find("BrickPanel");
    }

    public void SetSelectTool()
    {
        placerBrickScript.DeletePlacer();
    }
	
	// Update is called once per frame
	void Update () {
        var pointer = new PointerEventData(EventSystem.current); // pointer event for Execute
        if (Input.GetKeyDown(rotateXKey)) {
            ExecuteEvents.Execute(rotateXbtn, pointer, ExecuteEvents.submitHandler);
        }
        else if (Input.GetKeyDown(rotateZKey))
        {
            ExecuteEvents.Execute(rotateZbtn, pointer, ExecuteEvents.submitHandler);
        }
        if (Input.GetKeyDown(rotateBrickKey1)  || (Input.GetKeyDown(rotateBrickKey2) && 
            Application.platform != RuntimePlatform.Android))
        {
            ExecuteEvents.Execute(rotateButton, pointer, ExecuteEvents.submitHandler);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetSelectTool();
            selectScript.RestoreOriginalColors();
            selectScript.ClearSelection();
            brickPanel.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            selectScript.DeleteSelectedBricks();
        }
    }
}
