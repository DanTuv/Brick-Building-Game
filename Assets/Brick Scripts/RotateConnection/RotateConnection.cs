using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RotateConnection : MonoBehaviour {

    //SelectScript selectScript;
    Bricks bricksScript;
    BrickMaterials brickMaterialsScript;
    RotateInput rotateInputScript;
    // The female rotator is the one that actually has the rotate values.
    //Brick femaleRotator;
    // 
    //Brick maleRotator;
    BrickConnection rotatingRootConnection;
    BrickConnection otherRootConnection;
    ConnectionType rotatingRootConnType;
    ConnectionType otherRootConnType;
    ConnectionClass connectionScript;
    //bool rotatingBricksSet = false;
    List<Brick> bricksToRotate;
    List<Brick> rotatesWithRoot;
    List<BrickConnection> rotatesWithRootConnection;
    List<bool> rotatesOpposite;

    List<BrickRestoreMap> BrickRestoreList;
    List<ConnectionRestoreMap> connRestoreList;
    BrickTypeConnection rotatingRootBrickTypeConn;
    Brick rotatingRootBrick;
    Brick compRootBrick;
    Brick unsplitRoot;
    Brick unsplitComp;
    int rotatingRootConnIndex = 0;
    List<BrickConnection> rotatingRootConnections;
    Color originalCompColor;
    Color compColor = new Color(0.8f, 0.6f, 0.8f, 1.0f);
    bool colorSaved = false;
    List<GameObject> gameObjectsToRotate;
    List<GameObject> tempRotatingParents;
    bool waitingForCollisionCheck = false;
    float lastRotation = 0.0f;
    int reverseSteps = 0;
    float reverseDirection = 1.0f;
    float desiredRotation = 0.0f;
    float totalDesiredRotation = 0.0f;
    float originalRotation;
    float totalAppliedRotation = 0.0f;
    RotationData rotationData;
    long fixedUpdateCount = 0;
    const int maximumReverseSteps = 10;

    public float GetPossibleRotation(float difference)
    {
        difference = -difference;
        float minimumLimit = difference;
        float rotateLimit = difference;
        for (int i = 0; i < rotatesWithRoot.Count; i++)
        {
            bool opposite = rotatesOpposite[i];
            Brick brick = rotatesWithRoot[i];
            BrickConnection brickConn = rotatesWithRootConnection[i];
            float thisRotation = GetRotationFromConns(brickConn, brickConn.GetOtherBrickConnection());
            float newRotation;
            if (opposite)
            {
                newRotation = thisRotation - difference;
            } else
            {
                newRotation = thisRotation + difference;
            }
            Vector2 limits = rotatingRootConnType.GetRotationConstraints();
            if (newRotation < limits[0] && limits[0] != -360)
            {
                rotateLimit = limits[0] - thisRotation;

                if (opposite)
                {
                    rotateLimit = -rotateLimit;
                }

                if (Mathf.Abs(rotateLimit) < Mathf.Abs(minimumLimit))
                {
                    minimumLimit = rotateLimit;
                }
            } 

            if (newRotation > limits[1] && limits[1] != 360)
            {
                rotateLimit = limits[1] - thisRotation;

                if (opposite)
                {
                    rotateLimit = -rotateLimit;
                }

                if (Mathf.Abs(rotateLimit) < Mathf.Abs(minimumLimit))
                {
                    minimumLimit = rotateLimit;
                }
            }
        }

        return -rotateLimit;
    }

    void PrepareDepthFirstSearch(ref List<Brick> brickList)
    {
        for (int i = 0; i < brickList.Count; i++)
        {
            brickList[i].visited = false;
        }
    }

    List<bool> SaveAndResetDfsState(ref List<Brick> brickList)
    {
        List<bool> saveList = new List<bool>();
        for (int i = 0; i < brickList.Count; i++)
        {
            saveList.Add(brickList[i].visited);
            brickList[i].visited = false;
        }
        return saveList;
    }

    void LoadDfsState(ref List<Brick> brickList, ref List<bool> savedList)
    {
        for (int i = 0; i < brickList.Count; i++)
        {
            brickList[i].visited = savedList[i];
        }
    }

    bool isParallelToRoot(RotatorBrick brick, out bool rotatesOpposite)
    {
        BrickConnection conn = CheckAndGetRotatingConn(brick.rotating);
        ConnectionType connType = conn.GetThisConnType(connectionScript);
        BrickTypeConnection foundRotBrickTypeConn = conn.GetThisBrickTypeConnection(
                                                    brick.rotating.brickType, connType);
        Brick foundRotBrick = brick.rotating;

        Vector3 rotatingConnStart = rotatingRootBrickTypeConn.GetStartPosition();
        Vector3 rotatingConnEnd = rotatingRootBrickTypeConn.GetEndPosition();

        Vector3 otherConnStart = foundRotBrickTypeConn.GetStartPosition();
        Vector3 otherConnEnd = foundRotBrickTypeConn.GetEndPosition();
        // Get the normalized world directions of the connections.
        Vector3 rotatingConnDirection = Vector3.Normalize(
            Tools.GetConnectionWorldDirection(rotatingRootBrick, rotatingConnStart, rotatingConnEnd));
        Vector3 otherConnDirection = Vector3.Normalize(
            Tools.GetConnectionWorldDirection(foundRotBrick, otherConnStart, otherConnEnd));
        // Get the normalized world directions between the bricks.
        Vector3 connStartWorldPos = Tools.GetPointWorldPosition(rotatingRootBrick, rotatingConnStart);
        Vector3 connEndWorldPos = Tools.GetPointWorldPosition(foundRotBrick, otherConnStart);
        Vector3 betweenDirection = connEndWorldPos - connStartWorldPos;
        betweenDirection.Normalize();

        if (betweenDirection == rotatingConnDirection || betweenDirection == -rotatingConnDirection || 
            betweenDirection.magnitude == 0.0f)
        {
            // Check if they are on the same line.
            if (rotatingConnDirection == otherConnDirection)
            {
                rotatesOpposite = false;
                return true;
            }
            else if (rotatingConnDirection == -otherConnDirection)
            {
                rotatesOpposite = true;
                return true;
            }
        }
        rotatesOpposite = false;
        return false;
    }

    BrickConnection CheckAndGetRotatingConn(Brick brick)
    {
        // Check for any connection with the rotating property.
        List<BrickConnection> rotatingConns = FindRotatingConnections(brick);
        if (rotatingConns.Count > 0)
        {
            return rotatingConns[0];
        } else
        {
            return null;
        }
    }

    void AddAllNonRotatingAdjacent(RotatorBrick parallelRot, bool addFromComp, ref Stack<Brick> stack)
    {
        Brick addFromBrick;
        Brick dontAddBrick;

        if (addFromComp)
        {
            addFromBrick = parallelRot.comp;
            dontAddBrick = parallelRot.rotating;
        }
        else
        {
            addFromBrick = parallelRot.rotating;
            dontAddBrick = parallelRot.comp;
        }

        List<Brick> adjacentBricks = addFromBrick.GetConnectedBricks();
        for (int abi = 0; abi < adjacentBricks.Count; abi++)
        {
            if (!adjacentBricks[abi].SameBrick(dontAddBrick))
            {
                stack.Push(adjacentBricks[abi]);
            }
        }
    }

    void AddAllAdjacent(Brick brick, ref Stack<Brick> stack)
    {
        List<Brick> adjacentBricks = brick.GetConnectedBricks();
        for (int abi = 0; abi < adjacentBricks.Count; abi++)
        {
            stack.Push(adjacentBricks[abi]);
        }
    }

    int GetIndexInRotatorList(RotatorBrick rotator, ref List<RotatorBrick> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].isSame(rotator))
            {
                return i;
            }
        }

        return -1;
    }

    bool isCompInRotatorList(Brick brick, ref List<RotatorBrick> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].isComp(brick))
            {
                return true;
            }
        }

        return false;
    }

    class RotatorBrick
    {
        public Brick rotating;
        public Brick comp;
        public RotatorBrick(Brick rotatingBrick, BrickConnection rotatingConn)
        {
            rotating = rotatingBrick;
            comp = rotatingConn.GetOtherBrick();
        }
        public bool isSame(RotatorBrick otherRotBrick)
        {
            return otherRotBrick.comp.SameBrick(rotating) || otherRotBrick.rotating.SameBrick(rotating);
        }
        public bool isComp(Brick brick)
        {
            return brick.SameBrick(comp);
        }
    }

    // Returns the list of bricks that are to be rotated around the rotating connection.
    // Returns an empty list if no bricks are to be rotated.
    List<Brick> CreateRotationList(List<Brick> allBricks)
    {
        List<Brick> bricksToRotate = new List<Brick>();
        //List<Brick> allBricks = bricksScript.GetBricks();
        rotatesWithRoot = new List<Brick>();
        rotatesWithRootConnection = new List<BrickConnection>();
        List<RotatorBrick> rotatorsRotateWithRoot = new List<RotatorBrick>();
        rotatesOpposite = new List<bool>();
        PrepareDepthFirstSearch(ref allBricks);
        Stack<Brick> stack = new Stack<Brick>();
        RotatorBrick root = new RotatorBrick(rotatingRootBrick, rotatingRootConnection);
        // Potential rotators.
        List<RotatorBrick> rotators = new List<RotatorBrick>();
        List<BrickConnection> rotatorConnections = new List<BrickConnection>();
        List<bool> opposites = new List<bool>();
        // Rotating bricks that should be considered as normal bricks are stored
        // in thist list.
        List<RotatorBrick> staticRotators = new List<RotatorBrick>();

        AddAllNonRotatingAdjacent(root, false, ref stack);
        root.rotating.visited = true;
        Brick brick;
        // Perform depth-first search.
        // While the Stack is not empty. 
        // This search finds which rotators
        // are static rotators.
        while (stack.Count > 0)
        {
            brick = stack.Pop();
            if (!brick.visited)
            {
                BrickConnection rotatingConn = CheckAndGetRotatingConn(brick);
                if (rotatingConn != null)
                {
                    RotatorBrick rotator = new RotatorBrick(brick, rotatingConn);
                    // We found a potential rotator brick. Check if it's the root.
                    if (rotator.isSame(root))
                    {
                        // There is a loop-back to the root, a lock, return null.
                        return null;
                    }

                    // Make sure it's not a static rotator.
                    if (GetIndexInRotatorList(rotator, ref staticRotators) == -1)
                    {
                        bool opposite;
                        //BrickConnection brickConn;

                        // Check if it's parallel to the root.
                        if (isParallelToRoot(rotator, out opposite))
                        {
                            // If the rotator is a comp then it will be regarded as a normal
                            // brick. Add it to the static rotators and remove it from the 
                            // potential rotators list.
                            if (isCompInRotatorList(rotator.rotating, ref rotators))
                            {
                                staticRotators.Add(rotator);
                                int index = GetIndexInRotatorList(rotator, ref rotators);
                                if (index != -1)
                                {
                                    rotators.RemoveAt(index);
                                    rotatorConnections.RemoveAt(index);
                                    opposites.RemoveAt(index);
                                }

                            } else {
                                // It's parallel to root. Check if we already have it in our list 
                                // of potential rotators. If not, add it.
                                int index = GetIndexInRotatorList(rotator, ref rotators);
                                if (index == -1)
                                {
                                    rotators.Add(rotator);
                                    rotatorConnections.Add(rotatingConn);
                                    opposites.Add(opposite);
                                }
                            
                                // Add all non-rotator adjacent bricks and mark it as visited.
                                brick.visited = true;
                                AddAllNonRotatingAdjacent(rotator, false, ref stack);
                            }
                        }
                    }
                }
                if (!brick.visited)
                {
                    brick.visited = true;
                    // Check all adjacent bricks.
                    AddAllAdjacent(brick, ref stack);
                }
            }
        }

        // The first depth-first search is finished. We now know which rotators are 
        // static. Continue by searching the bottom connections from the root, 
        // adding all potential rotators found as rotators that will rotate with the root.
        PrepareDepthFirstSearch(ref allBricks);
        stack = new Stack<Brick>();
        // This time we add bricks adjacent to the complementing rotator brick,
        // therefore we set the second argument to true.
        AddAllNonRotatingAdjacent(root, true, ref stack);
        root.comp.visited = true;

        while (stack.Count > 0)
        {
            brick = stack.Pop();
            if (!brick.visited)
            {
                BrickConnection rotatingConn = CheckAndGetRotatingConn(brick);
                if (rotatingConn != null)
                {
                    RotatorBrick rotator = new RotatorBrick(brick, rotatingConn);
                    // Make sure the rotator is in the potential rotators list.
                    int index = GetIndexInRotatorList(rotator, ref rotators);
                    if (index != -1)
                    {
                        // Get the other rotator (which will have the correct "comp" brick).
                        rotator = rotators[index];
                        // This brick must now have fullfilled all requirements to rotate with 
                        // the root.
                        // 1. It can not have a loop-back to itself (checked in the first DFS).
                        // 2. It must be parallel to the root (since it's in the rotator list).
                        // 3. It has a connection to the root from the bottom (since it's found
                        // in this DFS starting from the root bottom).
                        rotatesWithRoot.Add(rotator.rotating);
                        rotatesOpposite.Add(opposites[index]);
                        rotatorsRotateWithRoot.Add(rotator);
                        rotatesWithRootConnection.Add(rotatorConnections[index]);
                        // Add the bricks connected to the comp of the rotator to continue our DFS.
                        AddAllNonRotatingAdjacent(rotator, true, ref stack);
                        rotator.comp.visited = true;
                    }
                }
                if (!brick.visited)
                {
                    brick.visited = true;
                    // Check all adjacent bricks.
                    AddAllAdjacent(brick, ref stack);
                }
            }
        }
        // We now know all bricks that are going to rotate with the root. We make a final
        // DFS where we look for which bricks to rotate. When we encounter a rotator we only
        // add the non-rotating bricks if it's a part of the rotate with root bricks.
        PrepareDepthFirstSearch(ref allBricks);
        stack = new Stack<Brick>();
        AddAllNonRotatingAdjacent(root, false, ref stack);
        root.rotating.visited = true;

        while (stack.Count > 0)
        {
            brick = stack.Pop();
            if (!brick.visited)
            {
                if (brick.IsBaseplate())
                {
                    // If we get to the basePlate don't allow rotation as we don't want it to move away.
                    return null;
                }
                
                BrickConnection rotatingConn = CheckAndGetRotatingConn(brick);
                if (rotatingConn != null)
                {
                    RotatorBrick rotator = new RotatorBrick(brick, rotatingConn);
                    // This is also a rotator much like the root. Add all but the comp brick.
                    if (GetIndexInRotatorList(rotator, ref rotatorsRotateWithRoot) != -1)
                    {
                        AddAllNonRotatingAdjacent(rotator, false, ref stack);
                        rotator.rotating.visited = true;
                    }
                }
                if (!brick.visited)
                {
                    brick.visited = true;
                    // Check all adjacent bricks.
                    AddAllAdjacent(brick, ref stack);
                }
                // This brick should be rotated.
                bricksToRotate.Add(brick);
            }
        }

        // Add the root to the rotating bricks.
        bricksToRotate.Add(root.rotating);
        // Add the root to the rotates with root bricks.
        rotatesWithRoot.Add(root.rotating);
        rotatesWithRootConnection.Add(rotatingRootConnection);
        rotatesOpposite.Add(false);

        return bricksToRotate;
    }

    List<GameObject> GetGameObjectsToRotate()
    {
        List<GameObject> goToRotate = new List<GameObject>();
        for (int i = 0; i < bricksToRotate.Count; i++)
        {
            Tools.AddToListIfUnique(ref goToRotate, bricksToRotate[i].brickGO);
    
        }
        return goToRotate;
    }

    void RotateGameObjects(RotationData rotationData, List<GameObject> gameObjectsToRotate)
    {
        for (int i = 0; i < gameObjectsToRotate.Count; i++)
        {
            RotateGameObject(gameObjectsToRotate[i], rotationData); 
        }
    }

    public static BrickConnection GetRotatingBrickConnection(Brick brick, int connTypeIndex)
    {
        List<BrickConnection> conns = brick.GetUsedConnectionsOfType(connTypeIndex);
        if (conns == null)
        {
            return null;
        }
        if (conns.Count == 0)
        {
            return null;
        }
        return conns[0];
    }

    public static Brick GetCompatibleRotatingBrick(Brick brick, ConnectionClass connectionScript, out int rotConnType)
    {
        BrickType brickType = brick.brickType;
        List<int> connTypesIndices = brickType.GetConnectionTypesIndices();
        for (int i = 0; i < connTypesIndices.Count; i++)
        {
            ConnectionType connType = connectionScript.ConnectionTypeFromId(connTypesIndices[i]);
            if (connType.HasRotatingCompatible())
            {
                List<BrickConnection> conns = brick.GetUsedConnectionsOfType(connType.GetID());
                if (conns != null)
                {
                    for (int c = 0; c < conns.Count; c++)
                    {
                        Brick otherBrick = conns[c].GetOtherBrick();
                        ConnectionType otherBrickConnType = connectionScript.ConnectionTypeFromId(conns[c].GetOtherConnTypeIndex());
                        if (otherBrickConnType.HasRotationProperty())
                        {
                            rotConnType = otherBrickConnType.GetID();
                            return otherBrick;
                        }
                    }
                }
            }
        }

        rotConnType = -1;
        return null;
    }

    // Returns all rotating connections. If there are no rotating connections, then 
    // a list with 0 elements is returned.
    public List<BrickConnection> FindRotatingConnections(Brick brick)
    {
        List<BrickConnection> rotatingConnections = new List<BrickConnection>();
        BrickType brickType = brick.brickType;
        List<int> connTypesIndices = brickType.GetConnectionTypesIndices();
        for (int i = 0; i < connTypesIndices.Count; i++)
        {
            ConnectionType connType = connectionScript.ConnectionTypeFromId(connTypesIndices[i]);
            if (connType.HasRotationProperty())
            {
                // Add rotating connections.
                List<BrickConnection> brickConns = brick.GetUsedConnectionsOfType(connType.GetID());
                if (brickConns != null)
                {
                    rotatingConnections.AddRange(brickConns);
                }
            }
        }

        return rotatingConnections;
    }

    public void FindAndSetRotatingConnections(Brick brick)
    {
        rotatingRootConnections = new List<BrickConnection>();
        BrickType brickType = brick.brickType;
        List<int> connTypesIndices = brickType.GetConnectionTypesIndices();
        for (int i = 0; i < connTypesIndices.Count; i++)
        {
            ConnectionType connType = connectionScript.ConnectionTypeFromId(connTypesIndices[i]);
            if (connType.HasRotationProperty())
            {
                // Add rotating connections.
                List<BrickConnection> usedConns = brick.GetUsedConnectionsOfType(connType.GetID());
                if (usedConns != null)
                {
                    rotatingRootConnections.AddRange(usedConns);
                }
            }
        }
    }

    public int GetNumberOfRotatingConnections()
    {
        return rotatingRootConnections.Count;
    }

    public static float GetRotationFromConns(BrickConnection brick1Conn, BrickConnection brick2Conn)
    {
        float rotation = brick1Conn.GetRotation() + brick2Conn.GetRotation();
        if (rotation < -360.0f)
        {
            rotation += 360.0f;
        } else if (rotation > 360.0f)
        {
            rotation -= 360.0f;
        }
        return rotation;
    }

    public float GetCurrentRootRotation()
    {
        return rotatingRootConnection.GetRotation() + otherRootConnection.GetRotation();
    }

    public Vector2 GetLimits()
    {
        return new Vector2(Mathf.Min(rotatingRootConnType.GetRotationConstraints()[0],
                                    otherRootConnType.GetRotationConstraints()[0]),
                           Mathf.Max(rotatingRootConnType.GetRotationConstraints()[1],
                                    otherRootConnType.GetRotationConstraints()[1]));
    }

    struct RotationData
    {
        public float angle;
        public Vector3 pivot;
        public Vector3 axis;
        public RotationData(float pAngle, Vector3 pPivot, Vector3 pAxis)
        {
            angle = pAngle;
            pivot = pPivot;
            axis = pAxis;
        }
    }

    /*
    // If we encounter connection mapFrom we need to change its
    // 
    class ConnectionMapping
    {
        BrickConnection mapFrom;
        BrickConnection mapTo;
        public bool isMapFrom(BrickConnection brickConn)
        {
            // Check if the same brick connection is referenced.
            return mapFrom.Equals(brickConn);
        }
        public ConnectionMapping(BrickConnection pMapTo)
        {
            mapTo = pMapTo;
            mapFrom = mapTo.GetOtherBrickConnection();
        }
    }
    */

    class ConnectionRestoreMap {
        BrickConnection brickConn;
        BrickConnection otherBrickConn;
        Brick otherBrick;

        public ConnectionRestoreMap(BrickConnection pBrickConn)
        {
            brickConn = pBrickConn;
            otherBrickConn = brickConn.GetOtherBrickConnection();
            otherBrick = brickConn.GetOtherBrick();
        }

        public void RestoreConnection()
        {
            brickConn.SetOtherBrickConnection(otherBrickConn);
            brickConn.SetOtherBrick(otherBrick);
        }
    }

    void UpdateConnection(Brick newBrickTarget, BrickConnection newBrickConnection, BrickConnection brickConnToUpdate, 
        Brick brickToUpdate, Brick oldBrickTarget)
    {
        brickConnToUpdate.SetOtherBrickConnection(newBrickConnection);
        brickConnToUpdate.SetOtherBrick(newBrickTarget);
        List<Brick> connectedBricks = brickToUpdate.GetConnectedBricks();
        // Remove old brick target from our brick connection list (if it's still in the list).
        for (int i = 0; i < connectedBricks.Count; i++)
        {
            if (connectedBricks[i].SameBrick(oldBrickTarget))
            {
                brickToUpdate.RemoveBrickFromConnectedListAtIndex(i);
                break;
            }
        }
        // Insert the new brick into the connected brick's list.
        brickToUpdate.AddBrickToBrickListIfNew(newBrickTarget);
    }

    // Saves and restores data in a brick that's affected by a split brick or is a split brick.
    class BrickRestoreMap
    {
        Brick brick;
        List<Brick> connectedBricks;
        List<BrickConnectionList> brickConnectionLists;

        public Brick GetBrick()
        {
            return brick;
        }

        public BrickRestoreMap(Brick pBrick)
        {
            brick = pBrick;
            connectedBricks = brick.CopyConnectedBricksList();
            brickConnectionLists = brick.CopyAllBrickConnectionLists();
        }

        public void RestoreBrick()
        {
            brick.SetConnectedBricks(connectedBricks);
            brick.SetAllBrickConnectionLists(brickConnectionLists);
        }

        public override bool Equals(System.Object obj)
        {
            Brick otherBrick = (obj as BrickRestoreMap).GetBrick();
            return otherBrick.Equals(brick);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    
    void RestoreAllBricks(List<BrickRestoreMap> restoreList)
    {
        for (int i = 0; i < restoreList.Count; i++)
        {
            restoreList[i].RestoreBrick();
        }
    }

    void RestoreAllConnections(List<ConnectionRestoreMap> restoreList)
    {
        for (int i = 0; i < restoreList.Count; i++)
        {
            restoreList[i].RestoreConnection();
        }
    }

    // Splits a brick on its rotating connections and returns the new bricks.
    // The base brick will be the first brick in the list.
    List<Brick> SplitBrick(Brick brick, ref List<BrickRestoreMap> restoreList, ref List<ConnectionRestoreMap> connRestoreList)
    {
        List<Brick> newBricks = new List<Brick>();
        List<BrickConnection> brickConns = FindRotatingConnections(brick);
        Brick baseBrick = new Brick();
        Tools.AddToListIfUnique(ref restoreList, new BrickRestoreMap(brick));
        baseBrick.visited = true;
        baseBrick.brickGO = brick.brickGO;
        baseBrick.brickType = brick.brickType;
        baseBrick.SetConnectedBricks(brick.CopyConnectedBricksList());
        baseBrick.SetAllBrickConnectionLists(brick.CopyAllBrickConnectionLists());
        newBricks.Add(baseBrick);

        for (int c = 0; c < brickConns.Count; c++)
        {
            BrickConnection brickConn = brickConns[c];
            ConnectionType connType = brickConn.GetThisConnType(connectionScript);
            Brick newBrick = new Brick();
            newBrick.visited = true;
            newBrick.brickGO = brick.brickGO;
            newBrick.brickType = brick.brickType;
            // Add the connection in question.
            newBrick.AddConnection(brickConn, connType);
            // Add a connection to our new base brick.
            newBrick.AddBrickToBrickListIfNew(baseBrick);
            // Add a connection from the base brick to this brick.
            baseBrick.AddBrickToBrickListIfNew(newBrick);

            //newBrick.AddConnection(brickConn, connectionScript.ConnectionTypeFromName("stud_female"));

            // Add the other bricks to our restore list.
            Brick otherBrick = brickConn.GetOtherBrick();
            BrickConnection otherBrickConn = brickConn.GetOtherBrickConnection();
            Tools.AddToListIfUnique(ref restoreList, new BrickRestoreMap(otherBrick));

            // Store the brick connection in the restore list.
            Tools.AddToListIfUnique(ref connRestoreList, new ConnectionRestoreMap(otherBrickConn));
            // Fix the other brick's connection to this brick. 
            UpdateConnection(newBrick, brickConn, otherBrickConn, otherBrick, 
                             otherBrickConn.GetOtherBrick());
            // Remove this conneciton from the basebrick.
            baseBrick.RemoveConnectionWithIndex(brickConn.GetThisBrickConnIndex(), 
                                                brickConn.GetThisConnTypeIndex());

            newBricks.Add(newBrick);
        }

        return newBricks;
    }
    /*
    void fixConnectionMapping(BrickConnection brickConn, ref List<ConnectionMapping> connMaps)
    {
        for (int i = 0; i < connMaps.Count; i++)
        {
            if (connMaps[i].isMapFrom(brickConn))
            {

            }
        }
    }
    */
    // Splits brick that needs to be splitted and saves their information so they can be restored.
    List<Brick> CreateDfsBrickList(Brick root, List<Brick> brickList, out List<BrickRestoreMap> restoreList, 
                                   out List<ConnectionRestoreMap> connRestoreMap)
    {
        restoreList = new List<BrickRestoreMap>();
        connRestoreMap = new List<ConnectionRestoreMap>();
        Stack<Brick> stack = new Stack<Brick>();
        List<Brick> dfsBrickList = new List<Brick>();
        PrepareDepthFirstSearch(ref brickList);
        AddAllAdjacent(root, ref stack);
        root.visited = true;
        Brick brick;

        while (stack.Count > 0)
        {
            brick = stack.Pop();
            if (!brick.visited)
            {
                // Check all adjacent bricks next.
                AddAllAdjacent(brick, ref stack);
                // Get all used rotating connections, if any.
                List<BrickConnection> rotatingBrickConns = FindRotatingConnections(brick);
                if (rotatingBrickConns.Count > 1)
                {
                    // We have some rotating connections (more than one), so start splitting.
                    dfsBrickList.AddRange(SplitBrick(brick, ref restoreList, ref connRestoreMap));
                }
                else {
                    dfsBrickList.Add(brick);
                }
                brick.visited = true;
            }
        }

        //  Now lastly do the root.
        List<BrickConnection> rootBrickConns = FindRotatingConnections(root);
        if (rootBrickConns.Count > 1)
        {
            // We have some rotating connections (more than one), so start splitting.
            List<Brick> splitRoot = SplitBrick(root, ref restoreList, ref connRestoreMap);
            dfsBrickList.AddRange(splitRoot);
            // Assign the root brick.
            rotatingRootBrick = splitRoot[rotatingRootConnIndex + 1];
        } else
        {
            rotatingRootBrick = unsplitRoot;
        }

        // Now assign some root comp values. We want to do this after all the splits, 
        // in case the comp brick was split.
        GetOtherRotBrickData(rotatingRootConnType, rotatingRootConnection,
            out compRootBrick, out otherRootConnection, out otherRootConnType);

        return dfsBrickList;
    }

    void RotateGameObject(GameObject gameObject, RotationData rotationData)
    {
        // Rotate the brick.
        gameObject.transform.RotateAround(rotationData.pivot, rotationData.axis, rotationData.angle);
    }

    RotationData GetRotationData(float newRotation)
    {
        float angleDifference = GetRotationFromConns(rotatingRootConnection, otherRootConnection) - newRotation;
        Vector3 rootConnLocalStartPos = rotatingRootBrickTypeConn.GetStartPosition();
        Vector3 rootConnLocalEndPos = rotatingRootBrickTypeConn.GetEndPosition();
        Vector3 rootConnWorldStartPos = Tools.GetPointWorldPosition(rotatingRootBrick, rootConnLocalStartPos);
        Vector3 rootCorrectedDirection = Tools.GetConnectionWorldDirection(rotatingRootBrick, rootConnLocalStartPos,
            rootConnLocalEndPos);
        Vector3 rotationAxis = rootCorrectedDirection;
        return new RotationData(angleDifference, rootConnWorldStartPos, rotationAxis);
    }

    // Returns true and sets the data, if possible, otherwise returns false.
    public bool GetOtherRotBrickData(
        ConnectionType knownConnType, 
        BrickConnection knownBrickConnection,
        out Brick otherBrick,
        out BrickConnection otherBrickConn,
        out ConnectionType otherConnType)
    {
        otherBrick = null;
        otherBrickConn = null;
        otherConnType = null;
        // int[] compatibleTypes = knownConnType.GetCompatibleTypes();
        otherBrick = knownBrickConnection.GetOtherBrick();
        BrickType otherBrickType = otherBrick.brickType;
        if (otherBrickType != null) {
            otherBrickConn = knownBrickConnection.GetOtherBrickConnection();
            if (otherBrickConn != null)
            {
                otherConnType = otherBrickConn.GetThisConnType(connectionScript);
            }
        }
        if (otherBrick != null && otherBrickConn != null && otherConnType != null)
        {
            return true;
        }

        return false;
    }

    // Returns the total rotation of a brick by checking both
    // itself and it's connected brick for how much rotation 
    // that is applied on a particular connection index. Also 
    // gives the combined limits of the brick and its connected
    // brick. If the brick accepts multiple connections then this
    // function will just return the limits and the rotate
    // value from the brick itself as other information is irrelevant.
    //float GetRotationBrickTotalRotation(out Vector2 combinedLimits)
    //{
        
    //}

    // Set this brick as the rotating brick.
    public void SetRotatingBrick(Brick pRotatingbrick)
    {
        //RestoreOriginalColor();

        if (rotatingRootConnIndex >= rotatingRootConnections.Count)
        {
            rotatingRootConnIndex = 0;
        }
        unsplitRoot = pRotatingbrick;
        //rotatingRootBrick = pRotatingbrick;
        rotatingRootConnection = rotatingRootConnections[rotatingRootConnIndex];
        unsplitComp = rotatingRootConnection.GetOtherBrick();
        rotatingRootConnType = rotatingRootConnection.GetThisConnType(connectionScript);
        //GetOtherRotBrickData(rotatingRootConnType, rotatingRootConnection,
        //    out compRootBrick, out otherRootConnection, out otherRootConnType);
        //rotatingBricksSet = false;
        rotatingRootBrickTypeConn = rotatingRootConnection.GetThisBrickTypeConnection(
                                    unsplitRoot.brickType, rotatingRootConnType);
        otherRootConnection = rotatingRootConnection.GetOtherBrickConnection();
        otherRootConnType = otherRootConnection.GetThisConnType(connectionScript);

        if (!colorSaved)
        {
            SaveColor();
            ChangeToCompMaterial();
        }
    }

    void SaveColor()
    {
        Brick otherBrick = rotatingRootConnection.GetOtherBrick();
        if (otherBrick != null)
        {
            if (otherBrick.brickGO != null)
            {
                originalCompColor = rotatingRootConnection.GetOtherBrick().brickGO.GetComponent<Renderer>().sharedMaterial.color;
                colorSaved = true;
            }
        }
    }

    // Restores the original color for the comp brick. 
    public void RestoreOriginalColor()
    {
        if (unsplitComp != null && colorSaved)
        {
            if (unsplitComp.brickGO != null)
            {
                if (unsplitComp.brickGO.name != "basePlane")
                {
                    brickMaterialsScript.SetBrickAndStudColor(unsplitComp, originalCompColor);
                }
            }
        }
        colorSaved = false;
    }

    void ChangeToCompMaterial()
    {
        if (unsplitComp.brickGO != null)
        {
            if (unsplitComp.brickGO.name != "basePlane")
            {
                brickMaterialsScript.SetBrickAndStudColor(unsplitComp, compColor);
            }
        }
    }

    public void NextConnIndex()
    {
        RestoreOriginalColor();
        rotatingRootConnIndex++;
        if (rotatingRootConnIndex >= rotatingRootConnections.Count)
        {
            rotatingRootConnIndex = 0;
        }
        SetRotatingBrick(unsplitRoot);
    }

    void rotateRotator(BrickConnection brickConn, float rotationAdd)
    {
        brickConn.SetRotation(brickConn.GetRotation() + rotationAdd);
        if (brickConn.GetRotation() > 360)
        {
            brickConn.SetRotation(brickConn.GetRotation() - 360);
        } 

        if (brickConn.GetRotation() < -360)
        {
            brickConn.SetRotation(brickConn.GetRotation() + 360);
        }
    }

    bool TestRotation(float rotation)
    {
        List<Brick> allBricks = CreateDfsBrickList(unsplitRoot, bricksScript.GetBricks(),
                                           out BrickRestoreList, out connRestoreList);
        bricksToRotate = CreateRotationList(allBricks);

        RotationData rotationData = GetRotationData(rotation);
        rotationData.angle = GetPossibleRotation(rotationData.angle);

        RestoreAllConnections(connRestoreList);
        RestoreAllBricks(BrickRestoreList);

        if (bricksToRotate != null)
        {
            if (bricksToRotate.Count == 0)
            {
                return false;
            }
            return true;
        }

        return false;
    }

    public bool TestRotationPositive()
    {
        Vector2 limits = GetLimits();
        float currentRotation = GetCurrentRootRotation();

        if (currentRotation + 1.0f < limits[1] || currentRotation + 1.0f > 360.0f)
        {
            return TestRotation(1.0f);
        } else
        {
            return false;
        }
    }

    public bool TestRotationNegative()
    {
        Vector2 limits = GetLimits();
        float currentRotation = GetCurrentRootRotation();

        if (currentRotation - 1.0f > limits[0] || currentRotation - 1.0f < -360.0f)
        {
            return TestRotation(-1.0f);
        }
        else
        {
            return false;
        }
    }

    public bool SetRotation(float rotation)
    {
        if (unsplitRoot != null)
        {
            originalRotation = GetCurrentRootRotation();
            //if (!rotatingBricksSet)
            //{
            //    rotatingBricksSet = true;
            List<Brick> allBricks = CreateDfsBrickList(unsplitRoot, bricksScript.GetBricks(), 
                                                       out BrickRestoreList, out connRestoreList);
            bricksToRotate = CreateRotationList(allBricks);
            //}
            if (bricksToRotate != null)
            {
                rotationData = GetRotationData(rotation);
                rotationData.angle = GetPossibleRotation(rotationData.angle);
                desiredRotation = rotationData.angle;
                if (rotationData.angle > 0)
                {
                    reverseDirection = -1.0f;
                } else
                {
                    reverseDirection = 1.0f;
                }
                totalDesiredRotation = rotation;

                // Set the rotating bricks rotation.
                // Fix so that we update the rotation on the correct bricks (not temp bricks).
                RestoreAllConnections(connRestoreList);
                // GameObjects to rotate.
                gameObjectsToRotate = GetGameObjectsToRotate();

                RestoreAllBricks(BrickRestoreList);

                PrepareCollisionDetection(rotationData.angle);

                return true;
            }
            RestoreAllConnections(connRestoreList);
            RestoreAllBricks(BrickRestoreList);
        }
        return false;
    }

    public void PrepareCollisionDetection(float rotation)
    {
        waitingForCollisionCheck = true;
        lastRotation = rotation;
        disabledColliders = false;
        ticksWaitingForCollision = 0;
        reverseSteps = 0;
        tempRotatingParents = new List<GameObject>();

        for (int i = 0; i < gameObjectsToRotate.Count; i++)
        {
            GameObject brickObject = gameObjectsToRotate[i];
            GameObject tempObject = new GameObject();
            Rigidbody rigid = tempObject.AddComponent<Rigidbody>();
            rigid.detectCollisions = true;
            rigid.useGravity = false;
            rigid.isKinematic = true;
            tempObject.AddComponent<RotationTrigger>();

            // Copy rotation, position and scale to the temp GameObject.
            tempObject.transform.position = brickObject.transform.position;
            tempObject.transform.rotation = brickObject.transform.rotation;
            tempObject.transform.localScale = brickObject.transform.localScale;
            
            // Set the  colliders' parent to the temp object.
            Collider [] colliders = brickObject.GetComponentsInChildren<Collider>();
            for (int c = 0; c < colliders.Length; c++)
            {
                colliders[c].transform.SetParent(tempObject.transform);
                colliders[c].isTrigger = true;
            }

            // Add the temp object to our temp list.
            tempRotatingParents.Add(tempObject);
        }

        totalAppliedRotation = rotationData.angle;
        RotateGameObjects(rotationData, tempRotatingParents);

        /*
        waitingForCollisionCheck = true;
        lastRotation = rotation;
        disabledColliders = false;
        ticksWaitingForCollision = 0;
        for (int i = 0; i < rotatedBricks.Count; i++)
        {
            GameObject brickObject = rotatedBricks[i];
            Rigidbody rigid = brickObject.AddComponent<Rigidbody>();
            rigid.detectCollisions = true;
            rigid.useGravity = false;
            rigid.isKinematic = true;
            brickObject.AddComponent<RotationTrigger>();
        }
        */
    }

    public void DisableCollisionDetection()
    {
        waitingForCollisionCheck = false;
        disabledColliders = true;
        ticksWaitingForCollision = 0;
        for (int i = 0; i < tempRotatingParents.Count; i++)
        {
            GameObject brickObject = tempRotatingParents[i];
            Collider[] colliders = brickObject.GetComponentsInChildren<Collider>();
            for (int c = 0; c < colliders.Length; c++)
            {
                // Set the collider to have its real parent back.
                Transform trans = colliders[c].transform;
                trans.SetParent(gameObjectsToRotate[i].transform);
                trans.localPosition = new Vector3();
                trans.localRotation = Quaternion.identity;
                colliders[c].isTrigger = false;
            }
            Rigidbody rigid = brickObject.GetComponent<Rigidbody>();
            Destroy(rigid);
            // Destroy this temp brick.
            Destroy(tempRotatingParents[i]);
        }
    }

    // Applies rotation to the real game objects.
    public void ApplyRotation()
    {
        ticksWaitingForCollision = 0;
        for (int i = 0; i < rotatesWithRoot.Count; i++)
        {
            rotateRotator(rotatesWithRootConnection[i], -rotationData.angle);
        }

        RotateGameObjects(rotationData, gameObjectsToRotate);
    }

    public bool isWaitingForCollisionCheck()
    {
        return waitingForCollisionCheck || !disabledColliders;
    }

    // Returns true if we need to keep rotationg back after a collision.
    bool needStepBack()
    {
        return (reverseSteps < maximumReverseSteps);
    }

    bool StepBackRotation()
    {
        reverseSteps++;
        lastRotation += reverseDirection;
        if ((lastRotation > originalRotation && reverseDirection > 0.0f) ||
           (lastRotation < originalRotation && reverseDirection < 0.0f))
        {
            return false;
        }
        ticksWaitingForCollision = 0;
        rotationData.angle = reverseDirection;
        totalAppliedRotation += reverseDirection;
        RotateGameObjects(rotationData, tempRotatingParents);
        return true;
    }

    void RestoreOriginalRotation()
    {
        rotationData.angle = -totalAppliedRotation;
        RotateGameObjects(rotationData, tempRotatingParents);
        DisableCollisionDetection();
        rotateInputScript.RevertRotation(GetCurrentRootRotation());
    }

    long checkedCollisionFrame = 0;

    public void Colliding()
    {
        if (waitingForCollisionCheck && checkedCollisionFrame != fixedUpdateCount)
        {
            checkedCollisionFrame = fixedUpdateCount;
            if (needStepBack())
            {
                if (!StepBackRotation())
                {
                    RestoreOriginalRotation();
                }
            }
            // We don't need a stepback but we are colliding, this means we have
            // to reset to original rotation.
            else {
                RestoreOriginalRotation();
            }
        }
    }

    int ticksWaitingForCollision = 0;
    bool disabledColliders = true;

    void FixedUpdate()
    {
        if (ticksWaitingForCollision > 0)
        {
            DisableCollisionDetection();
            rotationData.angle = totalAppliedRotation;
            ApplyRotation();
            if (reverseSteps > 0)
            {
                rotateInputScript.RevertRotation(GetCurrentRootRotation());
            }
            ticksWaitingForCollision = 0;
        }
        if (!disabledColliders)
        {
            ticksWaitingForCollision++;
        }

        fixedUpdateCount++;
    }

    // Use this for initialization
    void Start () {
        bricksScript = GameObject.Find("BricksScript").GetComponent<Bricks>();
        connectionScript = GameObject.Find("ConnectionClass").GetComponent<ConnectionClass>();
        brickMaterialsScript = GameObject.Find("BrickMaterialsScript").GetComponent<BrickMaterials>();
        rotateInputScript = GameObject.Find("RotateInputScript").GetComponent<RotateInput>();
    }
}
