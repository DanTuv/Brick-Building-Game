using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CreateBaseplate : MonoBehaviour
{ 
    float baseSizeX;
    float baseSizeY;
    float baseSizeZ;

    //public Object studPrefab;
    public int baseSize;
    GameObject plane;
    BrickClass brickClass;
    Bricks brickScript;
    VisibleConnectors visibleConnectorsScript;
    ConnectionClass connectionClassScript;

    Mesh CreateMesh(float width, float height)
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        m.vertices = new Vector3[] {
            new Vector3(-width, 0.00f, height),
            new Vector3(width, 0.00f, height),
            new Vector3(width, 0.00f, -height),
            new Vector3(-width, 0.00f, -height)
        };
        m.uv = new Vector2[] {
            new Vector2 (0, 0),
            new Vector2 (0, 1),
            new Vector2(1, 1),
            new Vector2 (1, 0)
        };
        m.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateNormals();

        return m;
    }

    void Awake()
    {
        plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "basePlane";
        //MeshFilter meshFilter = (MeshFilter)plane.AddComponent(typeof(MeshFilter));
        baseSizeX = 0.16f * 4.0f * baseSize;
        baseSizeY = 0.05f;
        baseSizeZ = 0.16f * 4.0f * baseSize;

        plane.transform.localScale = new Vector3(baseSizeX, baseSizeY, baseSizeZ);
        //meshFilter.mesh = CreateMesh(sizeX, sizeZ);
        Renderer renderer = plane.GetComponent<Renderer>(); ;
        renderer.sharedMaterial = Resources.Load("Materials/LegoBaseplateMaterial", typeof(Material)) as Material;
        renderer.enabled = true;
        plane.transform.position = new Vector3(0, 0, 0);
        BoxCollider planeCollider = plane.AddComponent<BoxCollider>();
        planeCollider.size = new Vector3(baseSizeX, baseSizeY, baseSizeZ);
    }

    // Use this for initialization
    void Start()
    {
        connectionClassScript = GameObject.Find("ConnectionClass").GetComponent<ConnectionClass>();
        brickClass = GameObject.Find("BrickClass").GetComponent<BrickClass>();
        brickScript = GameObject.Find("BricksScript").GetComponent<Bricks>();
        visibleConnectorsScript = GameObject.Find("VisibleConnectorsScript").GetComponent<VisibleConnectors>();
        List<v2x3> studVectors = new List<v2x3>();
        for (var z = -baseSize; z < baseSize; z++)
        {
            for (var x = -baseSize; x < baseSize; x++)
            {
                float posX = x * 0.32f + 0.16f;
                float posY = baseSizeY/2;
                float posZ = z * 0.32f + 0.16f;
                //GameObject newStud = Instantiate(studPrefab, new Vector3(posX, posY, posZ), Quaternion.Euler(0, 180, 0)) as GameObject;
                //newStud.transform.SetParent(plane.transform);
                v2x3 vectors = new v2x3();
                vectors.v1 = new Vector3((posX * 100.0f) / baseSizeX, (posY * 100.0f) / baseSizeY, (posZ * 100.0f) / baseSizeZ);
                vectors.v2 = new Vector3((posX * 100.0f) / baseSizeX, (posY * 100.0f) / baseSizeY + 1.0f, (posZ * 100.0f) / baseSizeZ);
                studVectors.Add(vectors);
            }
        }
        BrickType brickType = new BrickType();
        List<BrickTypeConnection> brickTypeConns = new List<BrickTypeConnection>();

        int connId = connectionClassScript.ConnectionIdFromName("stud_male");

        for (int i = 0; i < studVectors.Count; i++)
        {
            BrickTypeConnection brickTypeConn =
                new BrickTypeConnection(connId, studVectors[i].v1, studVectors[i].v2);
            brickTypeConn.SetBrickTypeConnArrayIndex(i);
            brickTypeConns.Add(brickTypeConn);
        }

        brickType.CreateBaseplateType(brickTypeConns);
        brickClass.AddBrickType(brickType);
        Brick basePlateBrick = new Brick();
        basePlateBrick.brickGO = plane;
        basePlateBrick.brickType = brickType;
        basePlateBrick.brickGO.AddComponent<BrickTypeIdentifier>();
        basePlateBrick.brickGO.GetComponent<BrickTypeIdentifier>().thisBrick = basePlateBrick;
        visibleConnectorsScript.CreateVisibleConnectors(basePlateBrick, 
            plane.GetComponent<Renderer>().sharedMaterial.color);
        basePlateBrick.local_position = new Vector3(0.0f, 0.0f);
        basePlateBrick.local_rotation = new Vector3(0.0f, 0.0f);
        brickScript.AddBrick(basePlateBrick);
    }
}
