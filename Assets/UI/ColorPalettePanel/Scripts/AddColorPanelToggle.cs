using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AddColorPanelToggle : MonoBehaviour {

    public GameObject addColorPanel;
    public Text add_edit_text;
    public bool edit_mode = false;

    public void TogglePanel()
    {
        addColorPanel.SetActive(!addColorPanel.activeSelf);
    }

    public void SetAddText()
    {
        add_edit_text.text = "Add Color";
        edit_mode = false;
    }

    public void SetEditText()
    {
        add_edit_text.text = "Save Color";
        edit_mode = true;
    }

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
