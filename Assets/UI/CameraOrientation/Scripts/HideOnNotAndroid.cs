using UnityEngine;
using System.Collections;

public class HideOnNotAndroid : MonoBehaviour {

	// Use this for initialization
	void Start () {
	    if (Application.platform != RuntimePlatform.Android)
        {
            transform.gameObject.SetActive(false);
        }
	}
}
