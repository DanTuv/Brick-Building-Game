using UnityEngine;
using System.Collections;

public class RotationTrigger : MonoBehaviour {

    RotateConnection rotateConnectionScript;

    void OnTriggerEnter(Collider other)
    {
        rotateConnectionScript.Colliding();
    }

    // Use this for initialization
    void Awake()
    {
        if (rotateConnectionScript == null)
        {
            rotateConnectionScript = GameObject.Find("RotateConnectionScript").GetComponent<RotateConnection>();
        }
    }
}
