using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class ConnectionType
{
    string string_identifier;
    int id;
    List<string> compatible_type_names;
    int[] compatible_types;
    bool multiple_connections = false;
    bool can_be_rotated = false;
    bool has_rotating_compatible = false;
    float rotation_max = 0.0f;
    float rotation_min = 0.0f;
    bool hasConnectorMesh = false;
    bool hasNormalMap = false;
    bool hasTexture = false;
    Mesh mesh = null;
    Texture normal_map = null;
    Texture texture = null;

    public bool HasConnectorMesh()
    {
        return hasConnectorMesh;
    }

    public bool HasNormalMap()
    {
        return hasNormalMap;
    }

    public bool HasTexture()
    {
        return hasTexture;
    }

    // Loads the normal map if there is one.
    void LoadNormalMap()
    {
        Object textureObject = Resources.Load("Connections/NormalMaps/" + string_identifier);
        if (textureObject != null)
        {
            normal_map = textureObject as Texture;
            hasNormalMap = true;
        }
    }

    // Loads the texture if there is one.
    void LoadTexture()
    {
        Object textureObject = Resources.Load("Connections/Textures/" + string_identifier);
        if (textureObject != null)
        {
            texture = textureObject as Texture;
            hasTexture = true;
        }
    }

    void LoadMesh()
    {
        Object meshObject = Resources.Load("Connections/Meshes/" + string_identifier, typeof(Mesh));
        if (meshObject != null)
        {
            mesh = meshObject as Mesh;
            hasConnectorMesh = true;
        }
    }

    public Texture GetNormalMap()
    {
        return normal_map;
    }

    public Texture GetTexture()
    {
        return texture;
    }

    public Mesh GetMesh()
    {
        return mesh;
    }

    public Vector2 GetRotationConstraints()
    {
        return new Vector2(rotation_min, rotation_max);
    }

    public bool HasRotationProperty()
    {
        return can_be_rotated;
    }

    public bool HasRotatingCompatible()
    {
        return has_rotating_compatible;
    }

    public bool HasMultipleConnections()
    {
        return multiple_connections;
    }

    public int[] GetCompatibleTypes()
    {
        return compatible_types;
    }

    public void SetCompatibleTypes(int[] compatible_types_p, ConnectionClass connScript)
    {
        compatible_types = compatible_types_p;
        // Check if any of the compatible types is a rotating one.
        for (int i = 0; i < compatible_types.Length; i++)
        {
            ConnectionType currType = connScript.ConnectionTypeFromId(compatible_types[i]);
            if (currType.HasRotationProperty())
            {
                has_rotating_compatible = true;
                break;
            }
        }
    }

    public List<string> GetCompatibleTypeNames()
    {
        return compatible_type_names;
    }

    public int GetID()
    {
        return id;
    }

    public string GetStringIdentifier()
    {
        return string_identifier;
    }

    public void setID(int newID)
    {
        id = newID;
    }

    public ConnectionType(TextAsset textData)
    {
        LoadConnectionType(textData);
    }

    public bool LoadConnectionType(TextAsset textData)
    {
        string text = textData.text;
        // The resource name (filename without extension) is the string identifier.
        string_identifier = textData.name;

        LoadNormalMap();
        LoadTexture();
        LoadMesh();

        compatible_type_names = new List<string>();

        using (StringReader reader = new StringReader(text))
        {
            string line = string.Empty;
            do
            {
                line = Tools.readNext(reader);
                if (line != null)
                {
                    switch (line)
                    {
                        case "compatible_type":
                            compatible_type_names.Add(Tools.readNext(reader));
                            break;
                        case "multiple_connections":
                            multiple_connections = bool.Parse(Tools.readNext(reader));
                            break;
                        case "can_be_rotated":
                            can_be_rotated = bool.Parse(Tools.readNext(reader));
                            break;
                        case "rotation_max":
                            rotation_max = float.Parse(Tools.readNext(reader));
                            break;
                        case "rotation_min":
                            rotation_min = float.Parse(Tools.readNext(reader));
                            break;
                    }
                }

            } while (line != null);
        }
        return true;
    }
}

// An available connection for a BrickType.
public class BrickTypeConnection
{
    int brickTypeConnArrayIndex;
    int connTypeIndex;
    Vector3 startPosition;
    Vector3 endPosition;
    List<Vector3> facing;
    bool visibleConnector = false;
    public BrickTypeConnection(int p_connTypeIndex, Vector3 p_startPosition, 
        Vector3 p_endPosition)
    {
        connTypeIndex = p_connTypeIndex;
        startPosition = p_startPosition;
        endPosition = p_endPosition;
    }
    public BrickTypeConnection(int p_connTypeIndex, Vector3 p_startPosition,
    Vector3 p_endPosition, bool p_visibleConnector, List<Vector3> p_facing)
    {
        connTypeIndex = p_connTypeIndex;
        startPosition = p_startPosition;
        endPosition = p_endPosition;
        visibleConnector = p_visibleConnector;
        facing = p_facing;
    }

    public int GetBrickTypeConnArrayIndex()
    {
        return brickTypeConnArrayIndex;
    }

    public void SetBrickTypeConnArrayIndex(int cai)
    {
        brickTypeConnArrayIndex = cai;
    }

    public void SetVisibleConnector(bool useVisibleConnector)
    {
        visibleConnector = useVisibleConnector;
    }

    public void AddFacing(Vector3 p_facing)
    {
        if (facing == null)
        {
            facing = new List<Vector3>();
        }
        facing.Add(p_facing);
    }

    public int GetConnTypeIndex()
    {
        return connTypeIndex;
    }

    public ConnectionType GetConnectionType(ConnectionClass connClassScript)
    {
        return connClassScript.ConnectionTypeFromId(connTypeIndex);
    }

    public Vector3 GetStartPosition()
    {
        return startPosition;
    }

    public Vector3 GetEndPosition()
    {
        return endPosition;
    }

    public List<Vector3> GetFacing()
    {
        return facing;
    }

    public bool hasVisibleConnector()
    {
        return visibleConnector;
    }
}

// Array of BrickTypeConnections.
public class BrickTypeConnectionList
{
    int connTypeIndex;
    List<BrickTypeConnection> brickTypeConns;

    public BrickTypeConnectionList(int p_connectionTypeIndex)
    {
        connTypeIndex = p_connectionTypeIndex;
    }

    public BrickTypeConnectionList(int p_connectionTypeIndex, List<BrickTypeConnection> p_brickTypeConns)
    {
        connTypeIndex = p_connectionTypeIndex;
        brickTypeConns = p_brickTypeConns;
    }

    public int GetConnTypeIndex()
    {
        return connTypeIndex;
    }

    public List<BrickTypeConnection> GetBrickTypeConnections()
    {
        return brickTypeConns;
    }

    public void AddBrickTypeConnection(BrickTypeConnection brickTypeConn)
    {
        if (brickTypeConns == null)
        {
            brickTypeConns = new List<BrickTypeConnection>();
        }

        brickTypeConn.SetBrickTypeConnArrayIndex(brickTypeConns.Count);
        brickTypeConns.Add(brickTypeConn);
    }
}

// Connection between two bricks.
public class BrickConnection
{
    Brick otherBrick;
    int connIndexThisBrick;
    int connIndexOtherBrick;
    int otherConnTypeIndex;
    int thisConnTypeIndex;
    BrickConnection otherConnection;
    // Used for bricks with turnable connections.
    float rotation = 0.0f;

    public void SetOtherBrick(Brick brick)
    {
        otherBrick = brick;
    }

    public float GetRotation()
    {
        return rotation;
    }

    public void SetRotation(float pRotation)
    {
        rotation = pRotation;
    }

    public BrickConnection(int p_connTypeIndex, Brick p_otherBrick, 
                      BrickTypeConnection brickTypeConn, BrickTypeConnection brickTypeConnOtherBrick,
                      int p_otherConnTypeIndex) {
        otherBrick = p_otherBrick;
        connIndexThisBrick = brickTypeConn.GetBrickTypeConnArrayIndex();
        connIndexOtherBrick = brickTypeConnOtherBrick.GetBrickTypeConnArrayIndex();
        otherConnTypeIndex = p_otherConnTypeIndex;
        thisConnTypeIndex = p_connTypeIndex;
    }

    public void SetOtherBrickConnection(BrickConnection pBrickConnection)
    {
        otherConnection = pBrickConnection;
    }

    public BrickConnection GetOtherBrickConnection()
    {
        return otherConnection;
    }

    public BrickTypeConnection GetThisBrickTypeConnection(BrickType thisBrickType,
                                                          ConnectionType thisConnType)
    {
        return thisBrickType.GetConnectionOfTypeAndIndex(thisConnType.GetID(),
            connIndexThisBrick);
    }

    public int GetThisBrickConnIndex()
    {
        return connIndexThisBrick;
    }

    public int GetOtherBrickConnIndex()
    {
        return connIndexOtherBrick;
    }

    public int GetOtherConnTypeIndex()
    {
        return otherConnTypeIndex;
    }

    public ConnectionType GetThisConnType(ConnectionClass connScript)
    {
        return connScript.ConnectionTypeFromId(thisConnTypeIndex);
    }

    public int GetThisConnTypeIndex()
    {
        return thisConnTypeIndex;
    }

    public Brick GetOtherBrick()
    {
        return otherBrick;
    }
}

public class BrickConnectionList
{
    int connTypeIndex;
    List<BrickConnection> brickConns;

    public BrickConnectionList(int p_connTypeIndex)
    {
        connTypeIndex = p_connTypeIndex;
    }

    public int GetConnectionTypeIndex()
    {
        return connTypeIndex;
    }

    public List<BrickConnection> GetBrickConnections()
    {
        return brickConns;
    }

    public void SetConnections(List<BrickConnection> pBrickConns)
    {
        brickConns = pBrickConns;
    }

    public void AddConnection(BrickConnection conn)
    {
        if (brickConns == null)
        {
            brickConns = new List<BrickConnection>();
        }

        brickConns.Add(conn);
    }
}

public class ConnectionClass : MonoBehaviour {

    public List<ConnectionType> connectionTypes;
    public ConnectionType[] connectionTypesArray;

    public int ConnectionIdFromName(string connection_name)
    {
        for (int i = 0; i < connectionTypes.Count; i++)
        {
            if (connectionTypes[i].GetStringIdentifier() == connection_name)
            {
                return connectionTypes[i].GetID();
            }
        }

        return -1;
    }

    public ConnectionType ConnectionTypeFromName(string name)
    {
        for (int i = 0; i < connectionTypes.Count; i++)
        {
            if (connectionTypes[i].GetStringIdentifier() == name)
            {
                return connectionTypes[i];
            }
        }

        return null;
    }

    public ConnectionType ConnectionTypeFromId(int id)
    {
        return connectionTypesArray[id];
    }

    public List<ConnectionType> GetAllConnectionTypes()
    {
        return connectionTypes;
    }

    void Awake () {
        // Load all the connector types and store them in a list.
        connectionTypes = new List<ConnectionType>();
	    Object [] connectorTextObject = Resources.LoadAll("Connections/ConnectionTypes/");
        for (int i = 0; i < connectorTextObject.Length; i++)
        {
            TextAsset connectorTextAsset = connectorTextObject[i] as TextAsset;
            ConnectionType newConnectionType = new ConnectionType(connectorTextAsset);

            // Add this connection to our list.
            connectionTypes.Add(newConnectionType);
        }

        // Create an integer index identifier for each ConnectorType (since string compairson is slow 
        // and we might compare connection types a lot).
        for (int i = 0; i < connectionTypes.Count; i++)
        {
            connectionTypes[i].setID(i);
        }
        connectionTypesArray = connectionTypes.ToArray();
        // Now we need to update the compatible arrays using the newly assigned IDs.
        for (int i = 0; i < connectionTypes.Count; i++)
        {
            List<string> compatibleNames = connectionTypes[i].GetCompatibleTypeNames();
            int[] compatibleTypes = new int[compatibleNames.Count];
            for (int ct = 0; ct < compatibleNames.Count; ct++)
            {
                compatibleTypes[ct] = ConnectionIdFromName(compatibleNames[ct]);
            }
            connectionTypes[i].SetCompatibleTypes(compatibleTypes, this);
        }
    }
}
