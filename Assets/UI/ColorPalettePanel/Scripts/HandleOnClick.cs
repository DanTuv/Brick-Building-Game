using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class HandleOnClick : MonoBehaviour {

    public GameObject colorObject;
    PlacerBrick placerBrickScript;
    SelectScript selectScript;

    void DisablePreviousHighlight(SelectedColor selectedScript)
    {
        if (selectedScript.current_button_image != null)
        {
            // Turn off the current image color if there is one.
            Color current_image_color = selectedScript.current_button_image.color;
            selectedScript.current_button_image.color = new Color(current_image_color.r, current_image_color.g, current_image_color.b, 0.0f);
        }
    }

    void EnableNewHighlight(Button btn, SelectedColor selectedScript)
    {
        Image parent_image = btn.transform.parent.gameObject.GetComponent<Image>();
        Color current_parent_color = parent_image.color;
        parent_image.color = new Color(current_parent_color.r, current_parent_color.g, current_parent_color.b, 1.0f);
        selectedScript.current_button_image = parent_image;
    }

    // Use this for initialization
    void Awake () {
        selectScript = GameObject.Find("SelectScript").GetComponent<SelectScript>();
        placerBrickScript = GameObject.Find("PlacerBrickScript").GetComponent<PlacerBrick>();
        selectScript = GameObject.Find("SelectScript").GetComponent<SelectScript>();
        colorObject = GameObject.Find("selectedColor");
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(() => {
            Image img = btn.GetComponent<Image>();
            Color color = img.color;
            SelectedColor selectedScript = colorObject.GetComponent<SelectedColor>();
            selectedScript.selected_color = color;
            // Show the paint bucket button for selected items.
            List<SelectedBrick> selectedBricks = selectScript.GetSelection();
            if (selectedBricks.Count > 0)
            {
                selectScript.EnablePaintBucket();
            }
            
            DisablePreviousHighlight(selectedScript);
            EnableNewHighlight(btn, selectedScript);
            placerBrickScript.PlacerMaterialChanged();
            placerBrickScript.StudMaterialChanged();
        });
    }
}
