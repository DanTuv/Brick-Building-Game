using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisibleConnectors : MonoBehaviour {

    Object connectorPrefab;
    BrickMaterials brickMaterialsScript;
    ConnectionClass connectionClassScript;

    public void DeleteAllVisibleConnectors(Brick brick)
    {
        for (int i = brick.brickGO.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = brick.brickGO.transform.GetChild(i).gameObject;
            ConnectionTypeIdentifier childConnectionIdentifier = child.GetComponent<ConnectionTypeIdentifier>();
            if (childConnectionIdentifier != null)
            {
                DestroyImmediate(child);
            }
        }
    }

    public void CreateVisibleConnectorsFromCurrentColor(Brick brick)
    {
        Renderer renderer = brick.brickGO.GetComponent<Renderer>();
        Color color = renderer.sharedMaterial.color;
        CreateVisibleConnectors(brick, color);
        // Make sure it's not the basePlane.
        if (brick.brickGO.name != "basePlane")
        {
            // Recolor the brick to prevent the weird issue where the call to
            // renderer.material causes the material to be cloned instead of instanced.
            brickMaterialsScript.SetBrickMaterials(brick, color);
        }
    }

    public void CreateVisibleConnectors(Brick brick, Color color)
    {
        List<int> connTypesLists = brick.brickType.GetConnectionTypesIndices();

        for (int ctl = 0; ctl < connTypesLists.Count; ctl++)
        {
            ConnectionType connType =
            connectionClassScript.ConnectionTypeFromId(connTypesLists[ctl]);
            if (connType.HasConnectorMesh())
            {
                List<BrickTypeConnection> brickConnections = brick.GetFreeConnectionsOfType(connType);
                for (int i = 0; i < brickConnections.Count; i++)
                {
                    Vector3 startPos = brickConnections[i].GetStartPosition();
                    Vector3 endPos = brickConnections[i].GetEndPosition();

                    GameObject connector = Instantiate(connectorPrefab) as GameObject;
                    connector.transform.SetParent(brick.brickGO.transform);
                    Renderer connectorMaterialRenderer = connector.GetComponent<Renderer>();
                    MeshFilter meshFilter = connector.GetComponent<MeshFilter>();
                    meshFilter.sharedMesh = connType.GetMesh();

                    ConnectionTypeIdentifier identifierScript = connector.GetComponent<ConnectionTypeIdentifier>();
                    identifierScript.SetConnTypeIndex(connType.GetID());

                    // Check if this is a special connector (has texture or normal map) or just an
                    // ordinary one which we can use a material from the common pool on.
                    if (connType.HasTexture() || connType.HasNormalMap())
                    {
                        connectorMaterialRenderer.sharedMaterial =
                            brickMaterialsScript.GetSpecialMaterialForConnector(color, connType);
                    }
                    else
                    {
                        connectorMaterialRenderer.sharedMaterial =
                            brickMaterialsScript.GetCommonMaterial(color);
                    }

                    Vector3 rotationVector = endPos - startPos;
                    connector.transform.localRotation = Quaternion.LookRotation(rotationVector);
                    connector.transform.Rotate(90, 0, 0);
                    connector.transform.Rotate(0, 180, 0);
                    connector.transform.localPosition = (startPos / 100.0f);
                }
            }
        }
    }

	// Use this for initialization
	void Awake () {
        brickMaterialsScript = GameObject.Find("Scripts/BrickMaterialsScript").GetComponent<BrickMaterials>();
        connectionClassScript = GameObject.Find("Scripts/ConnectionClass").GetComponent<ConnectionClass>();
        connectorPrefab = Resources.Load("Bricks/Prefab/connectorPrefab");
	}
}
