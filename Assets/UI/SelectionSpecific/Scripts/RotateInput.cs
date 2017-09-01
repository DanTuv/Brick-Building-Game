using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class RotateInput : MonoBehaviour {

    float currentRotationInput = 0.0f;
    float currentRotationResult = 0.0f;
    public InputField txtRotateInput;
    float timerForRotateButtons = 0.0f;
    static float updateDelay = 0.1f;
    static float degreesPerSecond = 1.0f;
    float maxRotation;
    float minRotation;
    bool rotateLeftIsPressed = false;
    bool rotateRightIsPressed = false;
    public Button btnRotateLeft;
    public Button btnRotateRight;
    public Button btnRotationAxis;
    int numRotationAxises = 1;
    public RotateConnection rotateConnectionScript;
    float lastRotationResult;

    public void showRotationAxisBtnIfAvailable()
    {
        int numAxises = rotateConnectionScript.GetNumberOfRotatingConnections();
        if (numAxises > 1)
        {
            btnRotationAxis.gameObject.SetActive(true);
            numRotationAxises = numAxises;
        } else
        {
            btnRotationAxis.gameObject.SetActive(false);
            numRotationAxises = 1;
        }
    }

    public void nextRotationAxis()
    {
        rotateConnectionScript.NextConnIndex();
        ShowRotationButtons();
    }

    void UpdateIncreaseDecreaseButtons()
    {
        if (rotateConnectionScript.TestRotationNegative())
        {
            btnRotateLeft.interactable = true;
        }
        else
        {
            btnRotateLeft.interactable = false;
        }
        if (rotateConnectionScript.TestRotationPositive())
        {
            btnRotateRight.interactable = true;
        }
        else
        {
            btnRotateRight.interactable = false;
        }
    }

    public void ShowRotationButtons()
    {
        currentRotationInput = rotateConnectionScript.GetCurrentRootRotation();
        currentRotationResult = currentRotationInput;
        Vector2 limits = rotateConnectionScript.GetLimits();
        minRotation = limits[0];
        maxRotation = limits[1];
        txtRotateInput.text = string.Format("{0:0.#}", currentRotationResult);
        btnRotateLeft.gameObject.SetActive(true);
        btnRotateRight.gameObject.SetActive(true);
        UpdateIncreaseDecreaseButtons();
        txtRotateInput.gameObject.SetActive(true);
        showRotationAxisBtnIfAvailable();
    }

    public void HideRotationButtons()
    {
        btnRotateLeft.gameObject.SetActive(false);
        btnRotateRight.gameObject.SetActive(false);
        txtRotateInput.gameObject.SetActive(false);
        btnRotationAxis.gameObject.SetActive(false);
        txtRotateInput.text = "0";
        currentRotationInput = 0;
        currentRotationResult = 0;
    }

    public void SetLimits(float pMaxRotation, float pMinRotation)
    {
        maxRotation = pMaxRotation;
        minRotation = pMinRotation;
    }

    void AddRotation(bool positiveRotation, float rotationToAdd)
    {
        float sign = 1.0f;
        if (!positiveRotation)
        {
            sign = -1.0f;
        }
        NewRotateValue(rotationToAdd * sign + currentRotationInput, true, true);
    }

    void RotateValueChanged()
    {
        if (txtRotateInput.IsActive())
        {
            float rotateValue;
            if (float.TryParse(txtRotateInput.text, out rotateValue))
            {
                NewRotateValue(rotateValue, false, false);
            }
        }
    }

    void NewRotateValue(float rotateValue, bool round, bool display)
    {
        if (!rotateConnectionScript.isWaitingForCollisionCheck())
        {
            lastRotationResult = currentRotationResult;
            float lastRotationInput = currentRotationInput;
            currentRotationInput = rotateValue;
            if (currentRotationInput >= 360.0f)
            {
                currentRotationInput -= 360.0f;
            }

            if (currentRotationInput <= -360.0f)
            {
                currentRotationInput += 360.0f;
            }

            if (currentRotationInput > maxRotation)
            {
                currentRotationInput = maxRotation;
            }

            if (currentRotationInput < minRotation)
            {
                currentRotationInput = minRotation;
            }

            if (round)
            {
                // Round to nearest 5 degrees in the direction they pressed.
                // The user is trying to increase rotation.
                if (currentRotationInput > lastRotationInput)
                {
                    currentRotationResult = (float)((int)(Math.Ceiling(currentRotationInput / 5.0) * 5.0f));
                }
                // The user is trying to decrease rotation.
                else
                {
                    currentRotationResult = (float)((int)(Math.Floor(currentRotationInput / 5.0) * 5.0f));
                }
            }
            else
            {
                currentRotationResult = currentRotationInput;
            }
            if (display)
            {
                txtRotateInput.gameObject.SetActive(false);
                txtRotateInput.text = currentRotationResult.ToString();
                txtRotateInput.gameObject.SetActive(true);
            }

            if (currentRotationResult > maxRotation)
            {
                currentRotationResult = maxRotation;
            }

            if (currentRotationResult < minRotation)
            {
                currentRotationResult = minRotation;
            }

            if (currentRotationResult != lastRotationResult)
            {
                if (!rotateConnectionScript.SetRotation(currentRotationResult))
                {
                    // Change it back, nothing could rotate.
                    RevertRotation(lastRotationResult);
                    return;
                }
                else
                {
                    UpdateIncreaseDecreaseButtons();
                }
            }
        }
    }

    public void RevertRotation(float actualRotation)
    {
        txtRotateInput.gameObject.SetActive(false);
        currentRotationResult = actualRotation;
        currentRotationInput = actualRotation;
        txtRotateInput.text = string.Format("{0:0.#}", actualRotation);
        txtRotateInput.gameObject.SetActive(true);
    }

    public void rotateLeftPressed()
    {
        rotateLeftIsPressed = true;
        timerForRotateButtons = Time.time - updateDelay;
    }

    public void rotateLeftReleased()
    {
        rotateLeftIsPressed = false;
    }

    public void rotateRightPressed()
    {
        rotateRightIsPressed = true;
        timerForRotateButtons = Time.time - updateDelay;
    }

    public void rotateRightReleased()
    {
        rotateRightIsPressed = false;
    }

    // Use this for initialization
    void Start () {
        //connectionScript = GameObject.Find("ConnectionClass").GetComponent<ConnectionClass>();
        rotateConnectionScript = GameObject.Find("RotateConnectionScript").GetComponent<RotateConnection>();
        //Adds a listener to the main input field and invokes a method when the value changes.
        txtRotateInput.onEndEdit.AddListener(delegate { RotateValueChanged(); });
        HideRotationButtons();
    }
	
	// Update is called once per frame
	void Update () {
        if (rotateRightIsPressed || rotateLeftIsPressed) {
            float deltaTime = Time.time - timerForRotateButtons;
            if (deltaTime > updateDelay)
            {
                // Add some degrees per second.
                AddRotation(rotateRightIsPressed, deltaTime * degreesPerSecond);
            }
        }
	}
}
