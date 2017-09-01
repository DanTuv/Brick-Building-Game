using UnityEngine;
using System.Collections;

public class PlacerTrigger : MonoBehaviour {

    PlacerBrick placerScript;
    
    void OnTriggerEnter(Collider other)
    {
        placerScript.Colliding();
    }

    void OnTriggerExit(Collider other)
    {
        placerScript.StoppedColliding();
    }
    
    // Use this for initialization
    void Awake () {
        if (placerScript == null)
        {
            placerScript = GameObject.Find("PlacerBrickScript").GetComponent<PlacerBrick>();
        }
    }
}
