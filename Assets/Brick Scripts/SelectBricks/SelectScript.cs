using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class SelectedBrick
{
    Brick brick;
    Color originalColor;

    public SelectedBrick(Brick pBrick)
    {
        brick = pBrick;
        originalColor = brick.brickGO.GetComponent<Renderer>().sharedMaterial.color;
    }

    public Brick GetBrick()
    {
        return brick;
    }

    public Color GetOriginalColor()
    {
        return originalColor;
    }
}

public class SelectScript : MonoBehaviour {

    BrickMaterials brickMaterialsScript;
    PlacerBrick placerBrickScript;
    SelectedColor selectedColorScript;
    ConnectionClass connectionScript;
    VisibleConnectors visibleConnectorsScript;
    RotateInput rotateInputScript;
    RotateConnection rotateConnectionScript;
    List<SelectedBrick> selectedBricks;
    Color selectionColor;
    Button btnPaintBucket;
    public Button btnTrashCan;
    public Button btnFocusCamera;
    bool paintBucketActive = false;
    Bricks brickScript;
    public Button btnHideAndShowBricks;
    public Sprite imgHide;
    public Sprite imgShow;

    public void HideBricks()
    {
        if (selectedBricks.Count > 0)
        {
            List<Brick> bricksToHide = new List<Brick>();
            for (int i = 0; i < selectedBricks.Count; i++)
            {
                SelectedBrick selectedBrick = selectedBricks[i];
                bricksToHide.Add(selectedBrick.GetBrick());
            }
            RestoreOriginalColors();
            ClearSelection();
            brickScript.HideBricks(bricksToHide);
            btnHideAndShowBricks.image.sprite = imgShow;
            btnHideAndShowBricks.gameObject.SetActive(true);
        }
        else
        {
            brickScript.ShowHiddenBricks();
            btnHideAndShowBricks.gameObject.SetActive(false);
        }
    }

    public void MoveBrick()
    {
        if (selectedBricks.Count < 1)
        {
            return;
        }
        SelectedBrick brickToMove = selectedBricks[selectedBricks.Count - 1];
        placerBrickScript.MoveBrick(brickToMove.GetBrick());
    }

    public void cloneSelected()
    {
        if (selectedBricks.Count < 1)
        {
            return;
        }
        SelectedBrick brickToClone = selectedBricks[selectedBricks.Count - 1];
        placerBrickScript.CreatePlacer(brickToClone.GetBrick().brickType.GetFilename());
    }

    public void DeleteSelectedBricks()
    {
        rotateConnectionScript.RestoreOriginalColor();
        List<SelectedBrick> selectedBricks = GetSelection();
        for (int i = selectedBricks.Count - 1; i >= 0; i--)
        {
            Brick brick = selectedBricks[i].GetBrick();
            if (brick != null)
            {
                brick.FreeBrick(visibleConnectorsScript);
                brick.DeleteBrick();
                brickScript.RemoveBrick(brick);
                RemoveSelectedAtIndex(i);
            }
        }
    }

    // Removes brick from selection and restores it color to its original color.
    void RemoveAndRestoreColor(Brick brick)
    {
        RestoreOriginalColor(brick);
        RemoveFromSelection(brick);
    }

    // Restores the original color for brick. 
    void RestoreOriginalColor(Brick brick)
    {
        rotateConnectionScript.RestoreOriginalColor();
        for (int i = 0; i < selectedBricks.Count; i++)
        {
            if (selectedBricks[i].GetBrick().SameBrick(brick))
            {
                brickMaterialsScript.SetBrickAndStudColor(
                    selectedBricks[i].GetBrick(), selectedBricks[i].GetOriginalColor());
                return;
            }
        }
    }

    public void RemoveSelectedAtIndex(int index)
    {
        selectedBricks.RemoveAt(index);

        if (selectedBricks.Count == 0)
        {
            UpdateSelectedActionsMenu();
        }
    }

    // Removes this brick from the current selection.
    public void RemoveFromSelection(Brick brick)
    {
        for (int i = 0; i < selectedBricks.Count; i++)
        {
            if (selectedBricks[i].GetBrick().SameBrick(brick))
            {
                RemoveSelectedAtIndex(i);
                return;
            }
        }
    }

    // Returns true if the selected brick already is in the seleciton.
    bool IsInSelection(Brick brick)
    {
        for (int i = 0; i < selectedBricks.Count; i++)
        {
            if (selectedBricks[i].GetBrick().SameBrick(brick))
            {
                return true;
            }
        }
        return false;
    }

    // Clears the current selection (un-selects) without
    // restoring any materials.
    public void ClearSelection()
    {
        rotateConnectionScript.RestoreOriginalColor();
        selectedBricks.Clear();
        UpdateSelectedActionsMenu();
    }

    public List<SelectedBrick> GetSelection()
    {
        return selectedBricks;
    }

    // Returns the brick that the mouse is currently hovering over (if it's not in the
    // ignore raycast layer).
    public Brick GetPressedBrick(bool includeBaseplate = true)
    {
        RaycastHit hitInfo = new RaycastHit();
        int mask = 1 << Physics.IgnoreRaycastLayer;
        mask = ~mask;
        bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, float.MaxValue, mask);
        if (hit)
        {
            BrickTypeIdentifier brickIdentifier = hitInfo.transform.gameObject.GetComponentInParent<BrickTypeIdentifier>();
            if (brickIdentifier == null)
            {
                return null;
            }
            Brick brick = brickIdentifier.thisBrick;
            if (brick.brickGO.name == "basePlane" && !includeBaseplate)
            {
                return null;
            }
            return brick;
        }
        else {
            return null;
        }
    }

    // Restores all selected bricks to their original Color.
    public void RestoreOriginalColors()
    {
        rotateConnectionScript.RestoreOriginalColor();
        for (int i = 0; i < selectedBricks.Count; i++)
        {
            // Change the Color back to its original.
            brickMaterialsScript.SetBrickAndStudColor(selectedBricks[i].GetBrick(), 
                selectedBricks[i].GetOriginalColor());
        }
    }

    void ChangeToSelectionMaterial(Brick brick)
    {
        brickMaterialsScript.SetBrickAndStudColor(brick, selectionColor);
    }

    // Restores the current selection to its original color, clears the selection, and
    // adds any selected brick to a new selection.
    void NewSelection(Brick brick)
    {
        bool isInSelectionAlone = false;
        if (brick != null && selectedBricks.Count == 1)
        {
            isInSelectionAlone = IsInSelection(brick);
        }
        RestoreOriginalColors();
        selectedBricks.Clear();
        if (brick != null && !isInSelectionAlone)
        {
            AddToSelection(brick);
        }
    }

    // Adds brick to the list of selected bricks, saves its material and re-colour it 
    // with the selection material.
    void AddToSelection(Brick brick)
    {
        selectedBricks.Add(new SelectedBrick(brick));
        Color color = brick.brickGO.GetComponent<Renderer>().sharedMaterial.color;
        // Enable paint bucket if another color than the bricks color is active.
        if (!paintBucketActive) {
            if (!BrickMaterials.SameColor(color, selectedColorScript.selected_color)) {
                EnablePaintBucket();
            }
        }
        ChangeToSelectionMaterial(brick);
        UpdateSelectedActionsMenu();
    }

	// Use this for initialization
	void Start () {
        brickMaterialsScript = GameObject.Find("BrickMaterialsScript").GetComponent<BrickMaterials>();
        placerBrickScript = GameObject.Find("PlacerBrickScript").GetComponent<PlacerBrick>();
        selectionColor = (Resources.Load("Materials/SelectMaterial", typeof(Material)) as Material).color;
        btnPaintBucket = GameObject.Find("btnPaintBucket").GetComponent<Button>();
        selectedColorScript = GameObject.Find("selectedColor").GetComponent<SelectedColor>();
        connectionScript = GameObject.Find("ConnectionClass").GetComponent<ConnectionClass>();
        rotateInputScript = GameObject.Find("RotateInputScript").GetComponent<RotateInput>();
        visibleConnectorsScript = GameObject.Find("VisibleConnectorsScript").GetComponent<VisibleConnectors>();
        brickScript = GameObject.Find("BricksScript").GetComponent<Bricks>();
        rotateConnectionScript = GameObject.Find("RotateConnectionScript").GetComponent<RotateConnection>();
        selectedBricks = new List<SelectedBrick>();
        UpdateSelectedActionsMenu();
    }

    void AddSelectPressed()
    {
        Brick brick = GetPressedBrick(false);
        if (brick != null)
        {
            if (!IsInSelection(brick))
            {
                AddToSelection(brick);
            } else
            {
                RemoveAndRestoreColor(brick);
            }
        }
    }

    void SimpleSelectPressed()
    {
        Brick brick = GetPressedBrick(false);
        NewSelection(brick);
    }

    public void paintSelected()
    {
        for (int i = 0; i < selectedBricks.Count; i++) {
            brickMaterialsScript.SetBrickAndStudColor(selectedBricks[i].GetBrick(), 
                selectedColorScript.selected_color);
        }
        ClearSelection();
    }

    public void EnablePaintBucket()
    {
        btnPaintBucket.gameObject.SetActive(true);
        paintBucketActive = true;
    }

    void UpdateSelectedActionsMenu()
    {
        if (selectedBricks.Count == 0) {
            if (!brickScript.ExistsHiddenBricks())
            {
                btnHideAndShowBricks.gameObject.SetActive(false);
            } else
            {
                btnHideAndShowBricks.image.sprite = imgShow;
                btnHideAndShowBricks.gameObject.SetActive(true);
            }
            btnPaintBucket.gameObject.SetActive(false);
            paintBucketActive = false;
            rotateInputScript.HideRotationButtons();
            //btnFocusCamera.gameObject.SetActive(false);
            btnTrashCan.gameObject.SetActive(false);
        } else
        {
            btnHideAndShowBricks.image.sprite = imgHide;
            btnHideAndShowBricks.gameObject.SetActive(true);

            btnTrashCan.gameObject.SetActive(true);

            if (selectedBricks.Count == 1)
            {
                Brick rotateBrick = selectedBricks[selectedBricks.Count - 1].GetBrick();
                rotateConnectionScript.FindAndSetRotatingConnections(rotateBrick);

                if (rotateConnectionScript.GetNumberOfRotatingConnections() > 0)
                {
                    rotateConnectionScript.SetRotatingBrick(rotateBrick);
                    rotateInputScript.ShowRotationButtons();
                }
                else
                {
                    rotateInputScript.HideRotationButtons();
                }
            }
            else {
                rotateInputScript.HideRotationButtons();
            }
        } 
    }
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown(KeyCode.Mouse0) && 
            !placerBrickScript.PlacerExists() && 
            !Input.GetKey(KeyCode.LeftAlt) &&
            !rotateConnectionScript.isWaitingForCollisionCheck() &&
            !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            rotateConnectionScript.RestoreOriginalColor();
            if (Input.GetKey(KeyCode.LeftShift))
            {
                AddSelectPressed();
            } else
            {
                SimpleSelectPressed();
            }
            UpdateSelectedActionsMenu();
        }
	}
}
