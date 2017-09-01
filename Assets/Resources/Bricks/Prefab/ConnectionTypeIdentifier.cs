using UnityEngine;
using System.Collections;

public class ConnectionTypeIdentifier : MonoBehaviour {

    int connTypeIndex;

    public int GetConnTypeIndex()
    {
        return connTypeIndex;
    }

    public void SetConnTypeIndex(int connectionTypeIndex)
    {
        connTypeIndex = connectionTypeIndex;
    }

    // Use this for initialization
    void Start () {
	
	}
}
