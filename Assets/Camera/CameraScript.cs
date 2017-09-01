using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraScript : MonoBehaviour {

    SelectScript selectScript;

	public float distance = 15.0f;
	public float xSpeed = 120.0f;
	public float ySpeed = 120.0f;

	public float orbitSpeed = 0.08f;

	public float yMinLimit = -200f;
	public float yMaxLimit = 300f;

	public float distanceMin = 1.5f;
	public float distanceMax = 300f;
    public float distanceNormal = 15.0f;
    public float distanceZoomed = 5.0f;
    KeyCode rotateKey = KeyCode.Mouse2;
    float normalRotationX;
    float normalRotationY;

	bool isRotateCursor = false;
	bool isRotating = false;

	float x = 0.0f;
	float y = 0.0f;

	// Use this for initialization
	void Start () 
	{
		SetMainCursor ();
		Vector3 angles = transform.eulerAngles;
		x = angles.y;
		y = angles.x;
        normalRotationX = angles.y;
        normalRotationY = angles.x;
		basePlate = GameObject.Find ("basePlane");
        selectScript = GameObject.Find("SelectScript").GetComponent<SelectScript>();
		cameraObject = basePlate;
        QualitySettings.antiAliasing = 8;
    }

    bool basePlaneHidden = false;

    void HideShowBasePlane()
    {
        if (y < 0.0f) {
            if (!basePlaneHidden)
            {
                var renderers = basePlate.GetComponentsInChildren<Renderer>();
                basePlaneHidden = true;
                foreach (var r in renderers)
                {
                    r.enabled = false;
                }
            }
        } else {
            if (basePlaneHidden)
            {
                basePlate.gameObject.GetComponent<Renderer>().enabled = true;
                basePlaneHidden = false;
                var renderers = basePlate.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    r.enabled = true;
                }
            }
        }
    }

	void SetRotateCursor() {
		CursorMode cursorMode = CursorMode.Auto;
		Vector2 hotSpot = new Vector2(16, 16);
		Texture2D rotateTexture = (Texture2D)Resources.Load ("rotate");
		Cursor.SetCursor (rotateTexture, hotSpot, cursorMode);
	}

	void SetMainCursor() {
		CursorMode cursorMode = CursorMode.Auto;
		Vector2 hotSpot = new Vector2(0, 0);
		Texture2D rotateTexture = (Texture2D)Resources.Load ("construction");
		Cursor.SetCursor (rotateTexture, hotSpot, cursorMode);
	}

	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		return Mathf.Clamp(angle, min, max);
	}

    void ResetRotation()
    {
        x = normalRotationX;
        y = normalRotationY;
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Camera.main.transform.rotation = rotation;
    }

    public void ZoomIn()
    {
        distance = distance - 1.0f;
        UpdateZoom(distance);
    }

    public void ZoomOut()
    {
        distance = distance + 1.0f;
        UpdateZoom(distance);
    }

    void UpdateZoom(float distance)
    {
        RaycastHit hit;

        if (Physics.Linecast(Camera.main.transform.position, Camera.main.transform.position, out hit))
        {
            distance -= hit.distance;
        }
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * negDistance + cameraObject.transform.position;
        Camera.main.transform.position = position;
    }

    void resetZoom()
    {
        distance = distanceNormal;
        UpdateZoom(distance);
    }

    void zoomIn()
    {
        distance = distanceZoomed;
        UpdateZoom(distance);
    }

	public void focusOnSelected() {
        List<SelectedBrick> selectedBricks = selectScript.GetSelection();
        selectedObject = null;
        if (selectedBricks.Count > 0)
        {
            selectedObject = selectedBricks[selectedBricks.Count - 1].GetBrick().brickGO;
        }
		if (selectedObject == null) {
			cameraObject = basePlate;
		} else {
            cameraObject = selectedObject;
        }
        if (cameraObject == selectedObject || (cameraObject == basePlate && selectedObject == null)) {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                ResetRotation();
            }
            else {
                if (distance == distanceNormal)
                {
                    zoomIn();
                }
                else
                {
                    resetZoom();
                }
            }

        } 
		//gameCamera.transform.SetParent (cameraObject.transform);
        if (cameraObject != null)
            Camera.main.transform.LookAt(cameraObject.transform);
	}

	void LateUpdate () 
	{
        if (cameraObject == null)
        {
            cameraObject = basePlate;
        }
		isRotating = false;
		
		// Orbit camera.
		if ((Input.GetKey (KeyCode.LeftAlt) && Input.GetKey(KeyCode.Mouse0)) || Input.GetKey (rotateKey) || Input.touchCount > 1) {
			isRotating = true;
			if (!isRotateCursor) {
				SetRotateCursor ();
				isRotateCursor = true;
			}
            if (Input.touchCount < 2)
            {
                x += Input.GetAxis("Mouse X") * xSpeed * orbitSpeed;
                y -= Input.GetAxis("Mouse Y") * ySpeed * orbitSpeed;
            } else
            {
                x += Input.touches[1].deltaPosition.x * xSpeed * orbitSpeed/6.0f;
                y -= Input.touches[1].deltaPosition.y * ySpeed * orbitSpeed/6.0f;
            }
			y = ClampAngle (y, yMinLimit, yMaxLimit);
		}

		Quaternion rotation = Quaternion.Euler(y, x, 0);

		// Zoom
		distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel")*20, distanceMin, distanceMax);

        UpdateZoom(distance);

        Camera.main.transform.rotation = rotation;

		if (isRotateCursor && !isRotating) {
			SetMainCursor();
			isRotateCursor = false;
		}
	}

	public GameObject basePlate;
	public GameObject cameraObject = null;
	public GameObject selectedObject = null;

	void updateSelection() {
		if (Input.GetMouseButtonDown(0))
		{
			RaycastHit hitInfo = new RaycastHit();
            int mask = 1 << Physics.IgnoreRaycastLayer;
            mask = ~mask;
            bool hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, float.MaxValue, mask);
			if (hit) 
			{
				selectedObject = hitInfo.transform.gameObject;
			} else {
				selectedObject = null;
			}

		} 
	}

	// Update is called once per frame
	void Update () {
		updateSelection ();
		if (Input.GetKeyDown (KeyCode.F)) {
			focusOnSelected ();
		}
        //HideShowBasePlane();
	}
}

