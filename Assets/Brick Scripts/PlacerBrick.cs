using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public static class RectTransformExtension
{

    public static Rect GetScreenRect(this RectTransform rectTransform, Canvas canvas)
    {

        Vector3[] corners = new Vector3[4];
        Vector3[] screenCorners = new Vector3[2];
        
        rectTransform.GetWorldCorners(corners);

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
        {
            screenCorners[0] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[1]);
            screenCorners[1] = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[3]);
        }
        else
        {
            screenCorners[0] = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            screenCorners[1] = RectTransformUtility.WorldToScreenPoint(null, corners[3]);
        }

        screenCorners[0].y = screenCorners[0].y;
        screenCorners[1].y = screenCorners[1].y;

        Rect newRect =  new Rect(new Vector2(screenCorners[0].x, screenCorners[1].y),  screenCorners[1]-screenCorners[0]);
        newRect.height = Mathf.Abs(newRect.height);
        newRect.width = Mathf.Abs(newRect.width);
        return newRect;
    }

}

public class PlacerBrick : MonoBehaviour {

    const float collider_size_decrease = 0.005f;
    Object brickPrefab;
    Bricks bricksScript;
    BrickMaterials brickMaterialsScript;
    BrickClass brickClassScript;
    SelectedColor selectedColorScript;
    RotateConnection rotateConnectionScript;
    VisibleConnectors visibleConnectorsScript;
    SelectScript selectScript;
    string lastFilename;
    Quaternion lastRotation = Quaternion.identity;
    Quaternion previousRotation = Quaternion.identity;
    Brick placerBrick = null;
    bool canPlaceHere = true;
    Material cantPlaceMaterial;
    Material placerLastMaterial;
    List<Material> studLastMaterial;
    public Button putButton;
    public Canvas uiCanvas;
    Vector3 lastPlacePosition = new Vector3();
    bool hasMoved = false;
    Quaternion rotationBeforeMoving;

    public void DeletePlacer()
    {
        if (placerBrick != null)
        {
            GameObject.Destroy(placerBrick.brickGO);
            canPlaceHere = true;
            collisionCount = 0;
            placerBrick = null;
        }
    }

    public void MoveBrick(Brick brick)
    {
        selectScript.RestoreOriginalColors();
        selectScript.ClearSelection();
        if (!hasMoved)
        {
            rotationBeforeMoving = lastRotation;
        }
        lastRotation = brick.brickGO.gameObject.transform.rotation;
        DeletePlacer();
        brick.FreeBrick(visibleConnectorsScript);
        placerBrick = brick;
        foreach (Transform child in brick.brickGO.transform)
        {
            if (child.gameObject.GetComponent<ConnectionTypeIdentifier>() != null)
            {
                Destroy(child.gameObject);
            }
        }
        Renderer renderer = placerBrick.brickGO.GetComponent<Renderer>();
        visibleConnectorsScript.CreateVisibleConnectors(placerBrick, renderer.sharedMaterial.color);
        placerLastMaterial = renderer.sharedMaterial;

        Collider [] colliders = brick.brickGO.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].transform.gameObject.layer = Physics.IgnoreRaycastLayer;
        }
        Rigidbody rigid = placerBrick.brickGO.AddComponent<Rigidbody>();
        rigid.detectCollisions = true;
        rigid.useGravity = false;
        rigid.isKinematic = true;
        placerBrick.brickGO.AddComponent<PlacerTrigger>();
        placerBrick.brickGO.transform.rotation = lastRotation;
        SetStudsLastColor();
        UpdatePlacerPosition();
        hasMoved = true;
    }

    public void CreatePlacer(string filename)
    {
        // Unselect everything first.
        selectScript.RestoreOriginalColors();
        selectScript.ClearSelection();

        if (hasMoved)
        {
            hasMoved = false;
            lastRotation = rotationBeforeMoving;
            return;
        }

        DeletePlacer();
        Brick newPlacerBrick = new Brick();
        newPlacerBrick.brickGO = Instantiate(brickPrefab) as GameObject;
        newPlacerBrick.brickType = brickClassScript.GetBrickType(filename);
        newPlacerBrick.brickGO.GetComponent<BrickTypeIdentifier>().thisBrick = newPlacerBrick;
        placerBrick = newPlacerBrick;
        placerBrick.brickGO.transform.position = lastPlacePosition;
        BrickMesh brickMeshStruct = placerBrick.brickType.GetMesh();
        Mesh brickMesh = brickMeshStruct.mesh;
        placerBrick.brickGO.GetComponent<MeshFilter>().sharedMesh = brickMesh;
        for (int i = 0; i < brickMeshStruct.colliderMeshes.Count; i++)
        {
            GameObject meshColliderObject = new GameObject("meshCollider");
            MeshCollider meshCollider = meshColliderObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = brickMeshStruct.colliderMeshes[i];
            meshCollider.convex = true;
            meshCollider.transform.gameObject.layer = Physics.IgnoreRaycastLayer;
            meshCollider.isTrigger = true;
            meshColliderObject.transform.SetParent(placerBrick.brickGO.transform);
            if (brickMeshStruct.scaleColliders)
            {
                Vector3 extents = brickMesh.bounds.extents;
                Vector3 adjustedExtents = extents - new Vector3(collider_size_decrease, 
                                                                collider_size_decrease, 
                                                                collider_size_decrease);
                Vector3 percentage = new Vector3();
                // Don't allow extents less than 0.
                for (int e = 0; e < 3; e++)
                {
                    if (adjustedExtents[i] < 0)
                    {
                        adjustedExtents[i] = 0.001f;
                    }
                    // Get the percentage we need to scale the object down by in current axis.
                    percentage[e] = adjustedExtents[e] / extents[e];
                }

                meshColliderObject.transform.localScale = new Vector3(percentage.x, percentage.y, percentage.z);
            } else
            {
                meshColliderObject.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            if (i < brickMeshStruct.colliderPositions.Count)
            {
                meshColliderObject.transform.localPosition = brickMeshStruct.colliderPositions[i] / 100.0f;
            } else
            {
                meshColliderObject.transform.localPosition = new Vector3(0.0f, 0.0f);
            }
        }

        PlacerMaterialChanged();
        Color selectedColor = selectedColorScript.selected_color;
        visibleConnectorsScript.CreateVisibleConnectors(placerBrick, selectedColor);
        lastFilename = filename;
        // Add a rigidbody.
        Rigidbody rigid = placerBrick.brickGO.AddComponent<Rigidbody>();
        rigid.detectCollisions = true;
        rigid.useGravity = false;
        rigid.isKinematic = true;
        placerBrick.brickGO.AddComponent<PlacerTrigger>();
        placerBrick.brickGO.transform.rotation = lastRotation;
        SetStudsLastColor();
        UpdatePlacerPosition();
    }

    public void PlacerMaterialChanged()
    {
        if (placerBrick != null)
        {
            Color selectedColor = selectedColorScript.selected_color;
            brickMaterialsScript.SetBrickMaterials(placerBrick, selectedColor);
            placerLastMaterial = placerBrick.brickGO.GetComponent<Renderer>().sharedMaterial;
        }
    }

    public void StudMaterialChanged()
    {
        if (placerBrick != null)
        {
            Color selectedColor = selectedColorScript.selected_color;
            brickMaterialsScript.SetStudMaterials(placerBrick, selectedColor);
            SetStudsLastColor();
        }
    }

    public void RotatePlacerY()
    {
        if (placerBrick != null)
        {
            placerBrick.brickGO.transform.rotation = lastRotation;
            placerBrick.brickGO.transform.Rotate(0.0f, 90.0f, 0.0f, Space.World);
            lastRotation = placerBrick.brickGO.transform.rotation;
        }
    }

    public void RotatePlacerX()
    {
        if (placerBrick != null)
        {
            placerBrick.brickGO.transform.rotation = lastRotation;
            placerBrick.brickGO.transform.Rotate(90.0f, 0.0f, 0.0f, Space.World);
            lastRotation = placerBrick.brickGO.transform.rotation;
        }
    }

    public void RotatePlacerZ()
    {
        if (placerBrick != null)
        {
            placerBrick.brickGO.transform.rotation = lastRotation;
            placerBrick.brickGO.transform.Rotate(0.0f, 0.0f, 90.0f, Space.World);
            lastRotation = placerBrick.brickGO.transform.rotation;
        }
    }

    void SetStudsLastColor()
    {
        studLastMaterial.Clear();
        int i = 0;
        foreach (Transform child in placerBrick.brickGO.transform)
        {
            Renderer childRenderer = child.gameObject.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                studLastMaterial.Add(childRenderer.sharedMaterial);
                i++;
            }
        }
    }

    void SetStudsCantPlaceMaterial()
    {
        foreach (Transform child in placerBrick.brickGO.transform)
        {
            Renderer childRenderer = child.gameObject.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                if (childRenderer.GetComponent<ConnectionTypeIdentifier>() != null) { 
                    childRenderer.sharedMaterial = cantPlaceMaterial;
                }
            }
        }
    }

    void UpdatePlaceableColor()
    {
        if (placerBrick != null)
        {
            Renderer renderer = placerBrick.brickGO.GetComponent<Renderer>();
            if (canPlaceHere)
            {
                renderer.sharedMaterial = placerLastMaterial;
                int i = 0;
                foreach (Transform child in placerBrick.brickGO.transform)
                {
                    Renderer childRenderer = child.gameObject.GetComponent<Renderer>();
                    if (childRenderer != null)
                    {
                        childRenderer.sharedMaterial = studLastMaterial[i];
                        i++;
                    }
                }
            }
            else {
                placerLastMaterial = renderer.sharedMaterial;
                renderer.sharedMaterial = cantPlaceMaterial;
                SetStudsLastColor();
                SetStudsCantPlaceMaterial();
            }
        }
    }

    void UpdatePlacerPosition()
    {
        RectTransform transform = putButton.GetComponent<RectTransform>();
        Rect rect = RectTransformExtension.GetScreenRect(transform, uiCanvas);
        bool insideUI = rect.Contains(Input.mousePosition);
        if (!insideUI || Application.platform != RuntimePlatform.Android)
        {
            placerBrick.brickGO.transform.rotation = lastRotation;
            GameObject hitObject;
            RaycastHit hitInfo = new RaycastHit();
            int mask = 1 << Physics.IgnoreRaycastLayer;
            mask = ~mask;

            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, float.MaxValue, mask);
            if (hit)
            {
                hitObject = hitInfo.transform.gameObject;
            }
            else {
                hitObject = null;
            }

            if (hitObject != null)
            {
                Vector3 point = hitInfo.point;

                placerBrick.brickGO.transform.position = new Vector3();
                Bounds bounds = placerBrick.brickGO.GetComponent<Renderer>().bounds;

                point.y += -bounds.min.y;

                placerBrick.brickGO.transform.position = point;
            }

            Vector3 originalPosition = placerBrick.brickGO.transform.position;
            if (!bricksScript.SetToSnapPoint(placerBrick))
            {
                placerBrick.brickGO.transform.position = originalPosition;
            }
        }
    }

    public void CheckAndPlaceBrick()
    {
        if (placerBrick != null)
        {
            if (canPlaceHere)
            {
                bricksScript.PlaceBrick(placerBrick);
                lastPlacePosition = placerBrick.brickGO.transform.position;
                placerBrick = null;
                CreatePlacer(lastFilename);
            }
        }
    }

    int collisionCount = 0;

    public void Colliding()
    {
        if (!rotateConnectionScript.isWaitingForCollisionCheck())
        {
            collisionCount++;
            canPlaceHere = false;
            if (collisionCount == 1)
            {
                UpdatePlaceableColor();
            }
        }
    }

    public void StoppedColliding()
    {
        if (!rotateConnectionScript.isWaitingForCollisionCheck())
        {
            collisionCount--;
            if (collisionCount == 0)
            {
                canPlaceHere = true;
                UpdatePlaceableColor();
            }
        }
    }

    public bool PlacerExists()
    {
        return placerBrick != null;
    }

    void Start () {
        bricksScript = GameObject.Find("BricksScript").GetComponent<Bricks>();
        visibleConnectorsScript = GameObject.Find("VisibleConnectorsScript").GetComponent<VisibleConnectors>();
        brickPrefab = Resources.Load("Bricks/Prefab/brickPrefab");
        brickClassScript = GameObject.Find("BrickClass").GetComponent<BrickClass>();
        selectedColorScript = GameObject.Find("selectedColor").GetComponent<SelectedColor>();
        selectScript = GameObject.Find("SelectScript").GetComponent<SelectScript>();
        brickMaterialsScript = GameObject.Find("BrickMaterialsScript").GetComponent<BrickMaterials>();
        cantPlaceMaterial = Resources.Load("Materials/CantPlaceMaterial") as Material;
        rotateConnectionScript = GameObject.Find("RotateConnectionScript").GetComponent<RotateConnection>();
        studLastMaterial = new List<Material>();
	}

    bool buttonPressed = false;

    void CheckButtonPressed()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) && 
            !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && 
            !Input.GetKey(KeyCode.LeftAlt))
        {
            buttonPressed = true;
        }
    }

    Vector3 lastMousePosition;

    bool MousePositionChanged()
    {
        bool hasChanged = false;
        Vector3 currentMousePosition = Input.mousePosition;
        hasChanged = currentMousePosition != lastMousePosition;
        lastMousePosition = currentMousePosition;
        return hasChanged;
    }

	// Update is called once per frame
	void Update () {
        if (Application.platform != RuntimePlatform.Android)
        {
            CheckButtonPressed();
        }
        if (buttonPressed)
        {
            buttonPressed = false;
            if (placerBrick != null)
            {
                CheckAndPlaceBrick();
            }
        }
        if (placerBrick != null /* && (  MousePositionChanged()  || 
            previousRotation != lastRotation) */)
        {
            
            UpdatePlacerPosition();
        }
        previousRotation = lastRotation;
    }
}
