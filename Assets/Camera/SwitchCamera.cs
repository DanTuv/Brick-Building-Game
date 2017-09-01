using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCamera : MonoBehaviour {
    public GameObject topCam;
    public GameObject fpsCam;
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.C))
        {
            if (topCam.gameObject.activeInHierarchy)
            {
                topCam.gameObject.SetActive(false);
                fpsCam.gameObject.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = true;
            } else
            {
                topCam.gameObject.SetActive(true);
                fpsCam.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
	}
}
