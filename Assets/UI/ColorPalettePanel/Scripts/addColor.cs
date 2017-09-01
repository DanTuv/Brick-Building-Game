using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class addColor : MonoBehaviour {

    public GameObject contentPanel;
    public Object colorPrefab;
    BrickMaterials brickMaterialsScript;
    SelectScript selectScript;
    Bricks bricksScript;
    int number_of_colors = 0;
    const int colors_per_row = 5;
    const int color_size = 30;
    const int color_spacing = 2;
    const int border_spacing = 10;
    Color colorPickerValue;
    public HSVPicker picker;
    public AddColorPanelToggle addColorPanel;
    public SelectedColor selectedColor;
    List<GameObject> colors;
    List<Vector3> color_position;

    Vector2 GetPositionFromIndex(int index)
    {
        float x = (float) border_spacing;
        float y = (float) -border_spacing;
        
        y -= ((float)color_size + (float)color_spacing) * (float)((int)((float)index / (float)colors_per_row));
        

        x += ((float)(index % colors_per_row)) * ((float)(color_size + color_spacing));
        return new Vector2(x, y);
    }

    bool isEdit()
    {
        if (addColorPanel == null)
        {
            return false;
        }
        return addColorPanel.edit_mode;
    }

    void check_and_fix_references()
    {
        if (addColorPanel == null)
        {
            GameObject addColorPanelObject = GameObject.Find("AddColorPanel");
            if (addColorPanelObject != null)
                addColorPanel = addColorPanelObject.GetComponent<AddColorPanelToggle>();
            addColorPanelObject.SetActive(false);
        }

        if (selectedColor == null)
        {
            GameObject selectedColorObject = GameObject.Find("selectedColor");
            if (selectedColorObject != null)
                selectedColor = selectedColorObject.GetComponent<SelectedColor>();
        }
    }
    
    bool colorAlreadyExists(Color color)
    {
        for (int i = 0; i < colors.Count; i++)
        {
            if (colors[i].gameObject.transform.Find("btnColor").GetComponent<Image>().color == color)
            {
                return true;
            }
        }

        return false;
    }

    public void AddColor()
    {
        if (!isEdit())
        {
            // Adding a new color.
            if (!colorAlreadyExists(colorPickerValue))
            {
                int newIndex = number_of_colors;
                number_of_colors++;
                Vector3 position = GetPositionFromIndex(newIndex);
                Object newColor = Instantiate(colorPrefab);
                ((GameObject)newColor).transform.SetParent(contentPanel.transform);
                ((GameObject)newColor).transform.localPosition = position;
                ((GameObject)newColor).transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                (((GameObject)newColor).transform.Find("btnColor")).GetComponent<Image>().color = colorPickerValue;
                contentPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, -position.y + color_size + color_spacing);
                colors.Add(((GameObject)newColor));
                color_position.Add(((GameObject)newColor).transform.localPosition);
                SelectColor(number_of_colors-1);
            } else
            {
                //EditorUtility.DisplayDialog("Can't add color.", "Can not add this color as it already exists in the palette.", "OK");
            }
        } else
        {
            // Edit existing color.
            if (!colorAlreadyExists(colorPickerValue))
            {
                // Deselect everything first since selected items cause problems when they are to be reverted to their old color.
                selectScript.RestoreOriginalColors();
                selectScript.ClearSelection();
                // Edit the color in the color palette icon.
                Image selectedColorImage = selectedColor.current_button_image.transform.Find("btnColor").GetComponent<Image>();
                Color oldColor = selectedColorImage.color;
                Color newColor = colorPickerValue;
                selectedColorImage.color = newColor;
                selectedColor.selected_color = newColor;
                // Change the material associated with this color (if any).
                brickMaterialsScript.ChangeMaterial(oldColor, newColor);
            }
            else
            {
                //EditorUtility.DisplayDialog("Can't change color.", "Can not change to this color as it already exists in the palette.", "OK");
            }
        }
    }

    void SelectColor(int index)
    {
        GameObject color = colors[index];
        var pointer = new PointerEventData(EventSystem.current);
        GameObject button = color.transform.Find("btnColor").GetComponent<Button>().gameObject;
        ExecuteEvents.Execute(button, pointer, ExecuteEvents.submitHandler);
    }

    public void RemoveColor()
    {
        // Deselect everything first since selected items cause problems when they are to be reverted to their old color.
        selectScript.RestoreOriginalColors();
        selectScript.ClearSelection();
        // Don't remove the color if its in use.
        List<Brick> bricks = bricksScript.GetBricks();
        bool colorInUse = false;
        // iterate all bricks to see if the color is in use by any.
        for (int i = 0; i < bricks.Count; i++)
        {
            if (BrickMaterials.IsColorInUse(bricks[i], selectedColor.selected_color))
            {
                colorInUse = true;
                break;
            }
        }

        if (!colorInUse)
        {

            int index = color_position.IndexOf(selectedColor.current_button_image.transform.localPosition);
            if (index != 0)
            {
                Destroy(selectedColor.current_button_image.transform.gameObject);
                colors.RemoveAt(index);
                color_position.RemoveAt(index);
                for (int i = 0; i < colors.Count; i++)
                {
                    Vector3 position = GetPositionFromIndex(i);
                    colors[i].transform.localPosition = position;
                    color_position[i] = position;
                }
                number_of_colors -= 1;
                SelectColor(0);
            }
        }
    }

	// Use this for initialization
	void Start () {
        brickMaterialsScript = GameObject.Find("BrickMaterialsScript").GetComponent<BrickMaterials>();
        selectScript = GameObject.Find("SelectScript").GetComponent<SelectScript>();
        bricksScript = GameObject.Find("BricksScript").GetComponent<Bricks>();
        colors = new List<GameObject>();
        color_position = new List<Vector3>();
        check_and_fix_references();
        colorPickerValue = new Color(0.75f, 0.0f, 0.0f, 1.0f);
        AddColor();
        colorPickerValue = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        picker.AssignColor(colorPickerValue);
        picker.onValueChanged.AddListener(color =>
        {
            colorPickerValue = color;
        });
        SelectColor(0);
    }

    bool is_default_selected_startup = false;

	// Update is called once per frame
	void Update () {
        if (!is_default_selected_startup)
        {
            SelectColor(0);
            is_default_selected_startup = true;
        }
	}
}
