using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public struct v2x3
{
    public Vector3 v1;
    public Vector3 v2;
};

public class BrickMesh
{
    public Mesh mesh;
    public List<Mesh> colliderMeshes;
    public List<Vector3> colliderPositions;
    public bool scaleColliders;
    public string filename;
}

public class BrickType
{
    // String identifier for this bricktype.
    string name;
    string filename;
    List<BrickTypeConnectionList> connLists;
    BrickMesh brickMesh;
    Texture texture;
    Texture normalMap;
    List<Material> materials;

    public bool SameType(BrickType otherType)
    {
        return otherType.GetName() == GetName();
    }

    public List<BrickTypeConnectionList> GetAllConnLists()
    {
        return connLists;
    }

    public List<BrickTypeConnection> GetBrickTypeConnectionsOfType(int connTypeIndex)
    {
        List<int> connTypesIndices = new List<int>();
        for (int i = 0; i < connLists.Count; i++)
        {
            if (connLists[i].GetConnTypeIndex() == connTypeIndex)
            {
                return connLists[i].GetBrickTypeConnections();
            }
        }
        return null;
    }

    public List<int> GetConnectionTypesIndices()
    {
        List<int> connTypesIndices = new List<int>();
        for (int i = 0; i < connLists.Count; i++)
        {
            connTypesIndices.Add(connLists[i].GetConnTypeIndex());
        }
        return connTypesIndices;
    }

    public List<Material> GetMaterials()
    {
        return materials;
    }

    public Texture GetTexture()
    {
        return texture;
    }

    public Texture GetNormalMap()
    {
        return normalMap;
    }

    void LoadFixedColors()
    {
        Object colorTextObject = Resources.Load("Bricks/Colors/" + filename + "-color");
        materials = new List<Material>();
        if (colorTextObject != null)
        {
            string text = (colorTextObject as TextAsset).text;

            using (StringReader reader = new StringReader(text))
            {
                string line = string.Empty;
                do
                {
                    line = Tools.readNext(reader);
                    if (line != null)
                    {
                        if (line != "")
                        {
                            if (line != "can_change")
                            {
                                Vector4 colorVector = Tools.StringToVec4(line);
                                Material newMaterial = Resources.Load("Materials/defaultMaterial") as Material;
                                newMaterial.color = new Color(colorVector[0], colorVector[1], colorVector[2], colorVector[3]);
                                materials.Add(newMaterial);
                            } else {
                                materials.Add(null);
                            }
                        }
                    }
                } while (line != null);
            }
        }
        if (materials.Count == 0)
        {
            materials.Add(null);
        }
    }

    // This method has to be called before adding any other connections to this brick type.
    public void CreateBaseplateType(List<BrickTypeConnection> studs)
    {
        name = "base-plate";
        filename = "BasePlate";
        BrickTypeConnectionList connList = new BrickTypeConnectionList(studs[0].GetConnTypeIndex(), 
            studs);
        connLists = new List<BrickTypeConnectionList>();
        connLists.Add(connList);
    }

    public BrickMesh GetMesh()
    {
        if (brickMesh != null)
        {
            return brickMesh;
        }

        BrickMesh newBrickMesh = new BrickMesh();
        Object resource = Resources.Load("Bricks/Meshes/" + filename, typeof(Mesh));
        newBrickMesh.mesh = resource as Mesh;
        newBrickMesh.filename = filename;

        newBrickMesh.scaleColliders = false;
        // Add the original mesh as collider mesh if there are no defined collider meshes.
        List<Mesh> colliders = GetColliderList();
        if (colliders.Count == 0)
        {
            colliders.Add(Resources.Load("Bricks/Meshes/" + filename, typeof(Mesh)) as Mesh);
            newBrickMesh.scaleColliders = true;
        }
        List<Vector3> colliderPositions = LoadColliderPositions();
        newBrickMesh.colliderMeshes = colliders;
        newBrickMesh.colliderPositions = colliderPositions;
        brickMesh = newBrickMesh;
        return newBrickMesh;
    }

    List<Mesh> GetColliderList()
    {
        List<Mesh> colliderMeshes = new List<Mesh>();
        int fileIndex = 0;

        Object resource = Resources.Load("Bricks/Colliders/" + filename + "-collider" +
            fileIndex.ToString("D2"), typeof(Mesh));

        while (resource != null)
        {
            colliderMeshes.Add(resource as Mesh);
            fileIndex++;
            resource = Resources.Load("Bricks/Colliders/" + filename + "-collider" +
                fileIndex.ToString("D2"), typeof(Mesh));
        }

        return colliderMeshes;
    }

    List<Vector3> LoadColliderPositions()
    {
        List<Vector3> colliderPositions = new List<Vector3>();
        Object resource = Resources.Load("Bricks/ColliderPositions/" + filename +
            "-collider-positions", typeof(TextAsset));

        if (resource != null)
        {
            string text = (resource as TextAsset).text;

            using (StringReader reader = new StringReader(text))
            {
                string line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        Vector3 pos = Tools.StringToVec3(line);
                        colliderPositions.Add(pos);
                    }

                } while (line != null);
            }
        }

        if (colliderPositions.Count == 0)
        {
            colliderPositions.Add(new Vector3(0.0f, 0.0f));
        }

        return colliderPositions;
    }

    public List<BrickTypeConnection> GetConnectionList(int connTypeIndex)
    {
        for (int i = 0; i < connLists.Count; i++)
        {
            if (connLists[i].GetConnTypeIndex() == connTypeIndex)
            {
                return connLists[i].GetBrickTypeConnections();
            }
        }

        return null;
    }


    public BrickTypeConnection GetConnectionOfTypeAndIndex(int connTypeIndex, int brickTypeConnIndex)
    {
        for (int i = 0; i < connLists.Count; i++)
        {
            if (connLists[i].GetConnTypeIndex() == connTypeIndex)
            {
                return connLists[i].GetBrickTypeConnections()[brickTypeConnIndex];
            }
        }

        return null;
    }

    public string GetName()
    {
        return name;
    }

    public string GetFilename()
    {
        return filename;
    }

    void AddToConnLists(BrickTypeConnection brickTypeConn)
    {
        for (int i = 0; i < connLists.Count; i++)
        {
            if (connLists[i].GetConnTypeIndex() == brickTypeConn.GetConnTypeIndex())
            {
                connLists[i].AddBrickTypeConnection(brickTypeConn);
                return;
            }
        }
        BrickTypeConnectionList newBrickTypeConnList = new BrickTypeConnectionList(
            brickTypeConn.GetConnTypeIndex());
        newBrickTypeConnList.AddBrickTypeConnection(brickTypeConn);
        connLists.Add(newBrickTypeConnList);
    }

    public bool LoadBrickType(TextAsset textData, List<ConnectionType> connTypes)
    {
        string text = textData.text;
        filename = textData.name;
        bool nextLineRead = false;
        LoadFixedColors();
        connLists = new List<BrickTypeConnectionList>();

        using (StringReader reader = new StringReader(text))
        {
            string line = string.Empty;
            do
            {
                if (!nextLineRead) {
                    line = Tools.readNext(reader);
                }
                nextLineRead = false;
                if (line != null)
                {
                    switch (line)
                    {
                        case "name":
                            name = Tools.readNext(reader);
                            break;
                        default:
                            for(int i = 0; i < connTypes.Count; i++)
                            {
                                if (connTypes[i].GetStringIdentifier() == line)
                                {
                                    ConnectionType connType = connTypes[i];
                                    Vector3 startPos = Tools.ReadVector3(reader);
                                    Vector3 endPos = Tools.ReadVector3(reader);
                                    BrickTypeConnection brickTypeConn = new BrickTypeConnection(connType.GetID(),
                                        startPos, endPos);
                                    /*
                                    for (int f = 0; f <  connType.NumberOfFacingVectors(); f++) { 
                                        Vector3 facing = Tools.ReadVector3(reader);
                                        brickTypeConn.AddFacing(facing);
                                    }
                                    */
                                    line = Tools.readNext(reader);
                                    nextLineRead = true;
                                    // Check if this connection has facing vectors.
                                    while (line == "facing")
                                    {
                                        // Read the facing vector.
                                        brickTypeConn.AddFacing(Tools.ReadVector3(reader));
                                        //  Read the next line.
                                        line = Tools.readNext(reader);
                                    }
                                    if (line == "visible_connector")
                                    {
                                        brickTypeConn.SetVisibleConnector(true);
                                    }
                                    AddToConnLists(brickTypeConn);
                                    break;
                                }
                            }
                            break;
                    }
                }

            } while (line != null);
        }
        return true;
    }
}

public class Brick
{
    public GameObject brickGO;
    public BrickType brickType;
    public Vector3 local_position;
    public Vector3 local_rotation;
    List<BrickConnectionList> connections;
    List<Brick> connectedBricks;
    // Used for Depth-First Search.
    public bool visited;

    public List<BrickConnectionList> CopyAllBrickConnectionLists()
    {
        List<BrickConnectionList> connLists = new List<BrickConnectionList>();
        for (int cl = 0; cl < connections.Count; cl++)
        {
            BrickConnectionList brickConnList = new BrickConnectionList(connections[cl].GetConnectionTypeIndex());
            brickConnList.SetConnections(new List<BrickConnection>(connections[cl].GetBrickConnections()));
            connLists.Add(brickConnList);
        }
        return connLists;
    }

    public List<BrickConnectionList> GetAllBrickConnectionLists()
    {
        return new List<BrickConnectionList>(connections);
    }

    public void SetAllBrickConnectionLists(List<BrickConnectionList> brickConnectionLists)
    {
        connections = brickConnectionLists;
    }

    public bool IsBaseplate()
    {
        return brickType.GetName() == "base-plate";
    }

    public bool SameBrick(Brick otherBrick)
    {
        //return brickGO.GetInstanceID() == otherBrick.brickGO.GetInstanceID();
        return otherBrick == this;
    }

    public void SetConnectedBricks(List<Brick> pConnectedBricks)
    {
        connectedBricks = pConnectedBricks;
    }
 
    public List<Brick> CopyConnectedBricksList()
    {
        return new List<Brick>(connectedBricks);
    }

    public List<Brick> GetConnectedBricks()
    {
        return connectedBricks;
    }

    // Removes the connection with specific connection index from the brick 
    // (does not affect other bricks).
    public void RemoveConnectionWithIndex(int connIndex, int connTypeIndex)
    {
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].GetConnectionTypeIndex() == connTypeIndex)
            {
                // Now we have found the right type of conneciton. Now we search 
                // the actual connections for one with the correct index.
                List<BrickConnection> brickConns = connections[i].GetBrickConnections();
                for (int c = 0; c < brickConns.Count; c++)
                {
                    if (brickConns[c].GetThisBrickConnIndex() == connIndex)
                    {
                        // Remove from brick list.
                        for (int b = 0; b < connectedBricks.Count; b++)
                        {
                            if (connectedBricks[b].SameBrick(
                                brickConns[c].GetOtherBrick()))
                            {
                                connectedBricks.RemoveAt(b);
                                break;
                            }
                        }
                        // Remove connection.
                        brickConns.RemoveAt(c);
                        break;
                    }
                }

                if (brickConns.Count == 0)
                {
                    connections.RemoveAt(i);
                }
            }
        }
    }

    // Removes any visible connectors and creates new ones.
    public void RecreateVisibleConnectors(VisibleConnectors visibleConnectorsScript)
    {
        visibleConnectorsScript.DeleteAllVisibleConnectors(this);
        visibleConnectorsScript.CreateVisibleConnectorsFromCurrentColor(this);
    }

    // Breaks the connections between this brick and all other bricks. The connection
    // information will be updated also on the bricks connected to the removed brick.
    public void DisconnectFromAll(VisibleConnectors visibleConnectorsScript)
    {
        if (connections != null)
        {
            for (int l = 0; l < connections.Count; l++)
            {
                //int typeIndex = connections[l].GetConnectionTypeIndex();
                List<BrickConnection> brickConns = connections[l].GetBrickConnections();
                for (int i = 0; i < brickConns.Count; i++)
                {
                    BrickConnection conn = brickConns[i];
                    Brick otherBrick = brickConns[i].GetOtherBrick();
                    int otherBrickConnIndex = conn.GetOtherBrickConnIndex();
                    int otherConnTypeIndex = conn.GetOtherConnTypeIndex();
                    otherBrick.RemoveConnectionWithIndex(otherBrickConnIndex,
                        otherConnTypeIndex);
                }
            }

            connections.Clear();
            for (int i = 0; i < connectedBricks.Count; i++)
            {
                if (connectedBricks[i] != null && connectedBricks[i].brickGO != null)
                {
                    connectedBricks[i].RecreateVisibleConnectors(visibleConnectorsScript);
                }
            }
        }
    }

    // Disconnects the brick from any other brick, as well as removes the connections found on other bricks
    // referencing this brick. Also removes the GameObjects associated with this brick.
    public void FreeBrick(VisibleConnectors visibleConnectorsScript)
    {
        DisconnectFromAll(visibleConnectorsScript);
    }

    public void DeleteBrick()
    {
        GameObject.DestroyImmediate(brickGO);
    }

    // Adds a brick to the list of bricks that's connected to this brick.
    public void AddBrickToBrickListIfNew(Brick newBrick)
    {
        // Add to the bricklist if it's not there.
        if (connectedBricks == null)
        {
            connectedBricks = new List<Brick>();
        }

        for (int i = 0; i < connectedBricks.Count; i++)
        {
            if (newBrick.SameBrick(connectedBricks[i]))
            {
                return;
            }
        }

        // The new brick was not found in the connected bricks list. Add it to the list.
        connectedBricks.Add(newBrick);
    }

    // Remove a brick in the connected bricks list.
    public void RemoveBrickFromConnectedListAtIndex(int index)
    {
        connectedBricks.RemoveAt(index);
    }

    public void AddConnection(BrickConnection connection, ConnectionType connType)
    {
        if (connections == null)
        {
            connections = new List<BrickConnectionList>();
        }
        int connTypeIndex = connType.GetID();
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].GetConnectionTypeIndex() == connTypeIndex)
            {
                connections[i].AddConnection(connection);
                AddBrickToBrickListIfNew(connection.GetOtherBrick());
                return;
            }
        }
        BrickConnectionList newConnList = new BrickConnectionList(connType.GetID());
        newConnList.AddConnection(connection);
        connections.Add(newConnList);
        AddBrickToBrickListIfNew(connection.GetOtherBrick());
    }

    public List<BrickConnection> GetUsedConnectionsOfType(int connTypeIndex)
    {
        if (connections == null)
        {
            return null;
        }
        for (int c = 0; c < connections.Count; c++)
        {
            if (connections[c].GetConnectionTypeIndex() == connTypeIndex)
            {
                return connections[c].GetBrickConnections();
            }
        }
        return null;
    }

    public List<BrickTypeConnection> GetFreeConnectionsOfType(ConnectionType connType)
    {
        int connTypeIndex = connType.GetID();
        // If this connection type allows multiple connection then we don't need to check
        // if they're used, we just need to check how many connections of this type exist
        // for this BrickType and return them.
        if (connType.HasMultipleConnections())
        {
            List<BrickTypeConnection> brickTypeConns = brickType.GetBrickTypeConnectionsOfType(connTypeIndex);
            return brickTypeConns;
        }

        List<BrickTypeConnection> allConnsForBrickType = brickType.GetConnectionList(connTypeIndex);

        if (allConnsForBrickType == null)
        {
            return null;
        }

        if (connections  != null) {
            List<BrickTypeConnection> freeList = new List<BrickTypeConnection>();

            List<int> removeList = new List<int>();

            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].GetConnectionTypeIndex() == connTypeIndex)
                {
                    List<BrickConnection> brickConns = connections[i].GetBrickConnections();
                    if (brickConns != null)
                    {
                        for (int c = 0; c < brickConns.Count; c++)
                        {
                            removeList.Add(brickConns[c].GetThisBrickConnIndex());
                        }
                    }

                    for (int c = 0; c < allConnsForBrickType.Count; c++)
                    {
                        if (!removeList.Contains(c))
                        {
                            freeList.Add(allConnsForBrickType[c]);
                        }
                    }
                    
                    return freeList;
                }
            }
        }
        return allConnsForBrickType;
    }
}

public class BrickClass : MonoBehaviour {

    private List<BrickType> brickTypes = new List<BrickType>();
    private ConnectionClass connectionClassScript;
    private AddBrickToPanel addBrickToPanel;

    public void AddBrickType(BrickType brickType)
    {
        brickTypes.Add(brickType);
    }

    public BrickType GetBrickType(string filename)
    {
        for (int i = 0; i < brickTypes.Count; i++)
        {
            if (brickTypes[i].GetFilename() == filename)
            {
                return brickTypes[i];
            }
        }

        return null;
    }

    void LoadAllBrickTypes()
    {
        addBrickToPanel = GameObject.Find("BrickPanel").gameObject.GetComponent<AddBrickToPanel>();
        Object[] brickTypesTextfiles = Resources.LoadAll("Bricks/BrickTypes/");
        foreach (Object obj in brickTypesTextfiles)
        {
            BrickType newType = new BrickType();
            newType.LoadBrickType(obj as TextAsset, connectionClassScript.GetAllConnectionTypes());
            brickTypes.Add(newType);
            addBrickToPanel.AddBlockToPanel(newType.GetName(), newType.GetFilename());
        }
    }

    // Use this for initialization
    void Start () {
        connectionClassScript = GameObject.Find("ConnectionClass").GetComponent<ConnectionClass>();
        LoadAllBrickTypes();
	}
}
