using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Bricks : MonoBehaviour {

    const float isConnectingRange = 0.02f;
    const float mmPerRaycast = 2;
    public const int multipleRaycastConnLimit = 100;
    public float connectionRange;
    List<Brick> bricks;
    List<Brick> hiddenBricks;
    ConnectionClass connectionClassScript;

    public bool ExistsHiddenBricks()
    {
        if (hiddenBricks != null)
        {
            return hiddenBricks.Count > 0;
        }

        return false;
    }

    public void ShowHiddenBricks()
    {
        for (int i = 0; i < hiddenBricks.Count; i++)
        {
            Brick brick = hiddenBricks[i];
            Renderer[] renderer = brick.brickGO.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < renderer.Length; r++)
            {
                renderer[r].enabled = true;
            }
        }
        Tools.SetBricksLayer(hiddenBricks, 1);
        bricks.AddRange(hiddenBricks);
        hiddenBricks.Clear();
    }

    public void HideBricks(List<Brick> bricksToHide)
    {
        for (int i = 0; i < bricksToHide.Count; i++)
        {
            Brick brick = bricksToHide[i];
            bricks.Remove(brick);
            Renderer [] renderer = brick.brickGO.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < renderer.Length; r++)
            {
                renderer[r].enabled = false;
            }
        }
        Tools.SetBricksLayer(bricksToHide, Physics.IgnoreRaycastLayer);
        hiddenBricks.AddRange(bricksToHide);
    }

    // Takes two floating point values and returns the 
    // value with the lowest absolute value.
    float MinAbs(float x1, float x2)
    {
        if (Math.Abs(x1) < Math.Abs(x2))
        {
            return x1;
        } else if (Math.Abs(x1) == Math.Abs(x2))
        {
            return Math.Min(x1, x2);
        } else
        {
            return x2;
        }
    }

    // Modulo function.
    float Mod(float a, float b)
    {
        return a - b * Mathf.Floor(a / b);
    }

    // Returns a quaternion set as the minimum degrees of rotation brickToRotate 
    // needs to be rotated in order to get "rotation of otherBrick" * 90 * n
    // rotation.
    Quaternion GetMatchRotation(Brick brickToRotate, Brick otherBrick)
    {
        Transform brickTransform = brickToRotate.brickGO.transform;
        Transform otherTransform = otherBrick.brickGO.transform;
        Quaternion originalRot = brickTransform.rotation;
        Quaternion otherOriginalRot = otherTransform.rotation;
        Quaternion matchRot = originalRot;
        Vector3 difference = otherTransform.rotation.eulerAngles - brickTransform.rotation.eulerAngles;

        float x, y, z;
        // We can safely apply Y rotation.
        float y1 = Mod(Math.Abs(difference.y), 90) * Math.Sign(difference.y);
        float y2 = (y1 - 90 * Math.Sign(difference.y));  
        //float y2 = mod(Math.Abs(Math.Abs(difference.y) - 90), 90) * -Math.Sign(difference.y);
        y = MinAbs(y1, y2);

        float x1 = Mod(Math.Abs(difference.x), 90) * Math.Sign(difference.x);
        //float x2 = mod(-Math.Abs(difference.x), 90) * -Math.Sign(difference.x);
        float x2 = (x1 - 90 * Math.Sign(difference.x));
        x = MinAbs(x1, x2);

        float z1 = Mod(Math.Abs(difference.z), 90) * Math.Sign(difference.z);
        //float z2 = mod(-Math.Abs(difference.z), 90) * -Math.Sign(difference.z);
        float z2 = (z1 - 90 * Math.Sign(difference.z));
        z = MinAbs(z1, z2);

        // 1. Remove the x, y and z rotation from other brick.
        float otherX = otherTransform.eulerAngles.x;
        float otherY = otherTransform.eulerAngles.y;
        //float otherZ = otherTransform.eulerAngles.z;
        otherTransform.rotation = Quaternion.identity;
        // We should now have a stright Y-axis. Let's rotate around it.
        brickTransform.RotateAround(brickTransform.position, otherTransform.up, y);
        // Now we reapply the rotation around the Y-axis for the other brick.
        otherTransform.rotation = otherTransform.rotation * Quaternion.AngleAxis(otherY, Vector3.up);
        // Rotate our brick around the X-axis.
        brickTransform.RotateAround(brickTransform.position, otherTransform.right, x);
        // Now we reapply the X-rotation.
        otherTransform.rotation = otherTransform.rotation * Quaternion.AngleAxis(otherX, Vector3.right);
        // Rotate our brick around the Z-axis.
        brickTransform.RotateAround(brickTransform.position, otherTransform.forward, z);
        // Restore other-brick.
        otherTransform.rotation = otherOriginalRot;
        // Save the matching rotation.
        matchRot = brickTransform.rotation;
        // Restore brick.
        brickTransform.rotation = originalRot;
        /*
        if (Input.GetKey(KeyCode.T) && otherBrick.brickType.GetFilename() != "rocker-bearing" &&
                otherBrick.brickType.GetFilename() != "BasePlate")
        {
            GameObject go = GameObject.Instantiate(brickToRotate.brickGO);
            go.transform.position = otherBrick.brickGO.transform.position;
            go.transform.rotation = matchRot;
            go.transform.Translate(go.transform.up);
        }
        */
        return matchRot;
    }
    
    // 1. Assign a connection as default, look first for a stud male, then go down
    // the lists to find another if there is none.
    // 2. Cast a ray to see what object the mouse is at
    bool RaycastFromConnection(Brick brick, Vector3 connection, 
        ref RaycastHit hitInfo, int mask = ~(1 << Physics.IgnoreRaycastLayer))
    {
        // Get the start position and the direction of the ray to be cast for this connection.
        brick.brickGO.transform.position = Camera.main.transform.position;
        Vector3 cameraPoint = brick.brickGO.transform.TransformPoint(connection / 100.0f);
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0.1f;
        Vector3 screenToWorld = Camera.main.ScreenToWorldPoint(mousePos);
        brick.brickGO.transform.position = screenToWorld;
        Vector3 mousePoint = brick.brickGO.transform.TransformPoint(connection / 100.0f);
        Vector3 rayStart = cameraPoint;
        Vector3 rayDirection = (mousePoint - rayStart) * 1000.0f;
        Debug.DrawRay(rayStart, rayDirection, new Color(1.0f, 1.0f, 1.0f, 0.3f), 1f);
        // Do the raycast and return what we hit.
        bool hit = Physics.Raycast(new Ray(rayStart, rayDirection), out hitInfo, float.PositiveInfinity, mask);
        return hit;
    }

    struct ConnectionHit
    {
        public float distanceToCamera;
        public Vector3 connWorldPos;
        public BrickTypeConnection thisConnection;
        public Vector3 thisConnLocalStartPos;
        public Brick otherBrick;
        public Quaternion placerRotation;
    }

    public static bool IsFacing(Brick brick1, Brick brick2, 
        BrickTypeConnection brickTypeConn1, BrickTypeConnection brickTypeConn2, 
        ConnectionType connType1, ConnectionType connType2, out Vector3 angles)
    {
        bool isFacing = false;
        Vector3 direction1 = Tools.GetConnectionWorldDirection(brick1, brickTypeConn1.GetStartPosition(), 
            brickTypeConn1.GetEndPosition());
        Vector3 direction2 = Tools.GetConnectionWorldDirection(brick2, brickTypeConn2.GetStartPosition(),
            brickTypeConn2.GetEndPosition());
        angles = new Vector3();

        // Check how many degrees there are between the direction vectors.
        //float angleX = Vector3.Angle(direction1, -direction2);
        
        isFacing = Vector3.Normalize(direction1) == Vector3.Normalize(-direction2);
        if (!isFacing && (connType1.HasMultipleConnections() || connType2.HasMultipleConnections()))
        {
            isFacing = Vector3.Normalize(direction1) == Vector3.Normalize(direction2);
        }

        if (isFacing)
        {
            List<Vector3> facings1 = brickTypeConn1.GetFacing();
            List<Vector3> facings2 = brickTypeConn2.GetFacing();
            if (facings1 != null && facings2 != null)
            {
                for (int i = 0; i < facings1.Count; i++)
                {
                    Vector3 rotation1 = brick1.brickGO.transform.TransformDirection(facings1[i]);
                    for (int f = 0; f < facings2.Count; f++)
                    {
                        Vector3 rotation2 = brick2.brickGO.transform.TransformDirection(facings2[f]);
                        float angle = Vector3.Angle(rotation1, rotation2);
                        if (angle < 1.0f)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        return isFacing;
    }

    class Box
    {
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;

        public void AddIfGreater(Vector3 point)
        {
            if (point.x < minX) { minX = point.x; }
            if (point.y < minY) { minY = point.y; }
            if (point.z < minZ) { minZ = point.z; }
            if (point.x > maxX) { maxX = point.x; }
            if (point.y > maxY) { maxY = point.y; }
            if (point.z > maxZ) { maxZ = point.z; }
        }

        public bool IsPointWithinRange(Vector3 point, float connectionRange)
        {
            /*
            Debug.DrawLine(new Vector3(minX, minY, minZ), 
                new Vector3(maxX, minY, minZ), 
                new Color(1.0f, 1.0f, 1.0f), 1.0f);
            Debug.DrawLine(new Vector3(minX, minY, minZ),
                new Vector3(minX, maxY, minZ),
                new Color(1.0f, 1.0f, 1.0f), 1.0f);
            Debug.DrawLine(new Vector3(minX, minY, minZ),
                new Vector3(minX, minY, maxZ),
                new Color(1.0f, 1.0f, 1.0f), 10.0f);

            Debug.DrawLine(point - new Vector3(connectionRange, 0, 0),
                point + new Vector3(connectionRange, 0, 0),
                new Color(1.0f, 0.0f, 1.0f), 1.0f);
            Debug.DrawLine(point - new Vector3(0, connectionRange, 0),
                point + new Vector3(0, connectionRange, 0),
                new Color(1.0f, 0.0f, 1.0f), 1.0f);
            Debug.DrawLine(point - new Vector3(0, 0, connectionRange),
                point + new Vector3(0, 0, connectionRange),
                new Color(1.0f, 0.0f, 1.0f), 1.0f);
                */
            return (point.z + connectionRange > minZ && point.z - connectionRange < maxZ) &&
                    (point.y + connectionRange > minY && point.y - connectionRange < maxY) &&
                    (point.x + connectionRange > minX && point.x - connectionRange < maxX);

        }
    }

    public static bool IsWithinConnectionSpan(Brick hitBrick, BrickTypeConnection hitBrickTypeConn, Vector3 hitPoint, 
        out Vector3 correctedPos, float connectionRange)
    {
        Vector3 lineWorldStart = Tools.GetPointWorldPosition(hitBrick, hitBrickTypeConn.GetStartPosition());
        Vector3 lineWorldEnd = Tools.GetPointWorldPosition(hitBrick, hitBrickTypeConn.GetEndPosition());
        Vector3 closestPointHit = Tools.ClosestPointOnLineSegmentFromPoint(lineWorldStart, lineWorldEnd, hitPoint);

        Debug.DrawRay(closestPointHit, new Vector3(1, 0, 0), new Color(1.0f, 1.0f, 0.0f), 3.0f);
        Debug.DrawRay(closestPointHit, new Vector3(-1, 0, 0), new Color(1.0f, 0.0f, 0.0f), 3.0f);
        float distance = Vector3.Distance(closestPointHit, hitPoint);
        if (distance < connectionRange)
        {
            correctedPos = closestPointHit;
            return true;
        }
     
        correctedPos = new Vector3();
        return false;   
    }

    // Brick within the AABB of the placer brick.
    class HitObjectPoints
    {
        ConnectionType connType;
        public Brick hitBrick;
        public List<Vector3> hitPoints = new List<Vector3>();
        public List<Vector3> placerConnStartList = new List<Vector3>();
        public Box box = new Box();
        List<BrickTypeConnection> placerConnList = new List<BrickTypeConnection>();
        Quaternion placerRotation;

        public bool SameBrick(Brick otherBrick)
        {
            return hitBrick.SameBrick(otherBrick);
        }

        public ConnectionType GetConnectionType()
        {
            return connType;
        }

        public HitObjectPoints(Brick pHitBrick, ConnectionType pConnType, Quaternion pPlacerRotation)
        {
            hitBrick = pHitBrick;
            connType = pConnType;
            placerRotation = pPlacerRotation;
        }

        public void AddHitPoint(Vector3 point, BrickTypeConnection placerConn, Vector3 placerConnStart)
        {
            placerConnStartList.Add(placerConnStart);
            hitPoints.Add(point);
            box.AddIfGreater(point);
            placerConnList.Add(placerConn);
        }

        // Needs to return all of the following:
        // 1. Distance to camera from the best connection.
        // 2. v2x3 of the best connection.
        // 3. The position of the best connection.
        public ConnectionHit GetBestConnection(ref Brick placeBrick, Camera gameCamera, float connectionRange,
            ConnectionClass connClassScript)
        {

            // Match the rotation of the hit brick.
            Quaternion originalRot = placeBrick.brickGO.transform.rotation;
            placeBrick.brickGO.transform.rotation = placerRotation;
            /* if (Input.GetKey(KeyCode.T) && hitBrick.brickType.GetFilename() != "rocker-bearing" &&
                 hitBrick.brickType.GetFilename() != "BasePlate")
             GameObject.Instantiate(placeBrick.brickGO).transform.position = hitPoints[0];*/


            ConnectionHit connectionHit = new ConnectionHit()
            {
                distanceToCamera = float.PositiveInfinity
            };
            int [] hitConnectionTypeIndices = connType.GetCompatibleTypes();

            for (int compTypes = 0; compTypes < hitConnectionTypeIndices.Length; compTypes++)
            {
                int compTypeIndex = hitConnectionTypeIndices[compTypes];
                ConnectionType hitConnectionType = connClassScript.ConnectionTypeFromId(compTypeIndex);
                List<BrickTypeConnection> hitConnections = hitBrick.GetFreeConnectionsOfType(hitConnectionType);
                if (hitConnections != null)
                {
                    for (int conn = 0; conn < hitConnections.Count; conn++)
                    {
                        Vector3 hitConnPos;
                        BrickTypeConnection hitBrickTypeConn = hitConnections[conn];
                        hitConnPos = hitBrickTypeConn.GetStartPosition();

                        Vector3 hitConnWorldPos = Tools.GetPointWorldPosition(hitBrick, hitConnPos);
                        if (box.IsPointWithinRange(hitConnWorldPos, connectionRange) || 
                            hitConnectionType.HasMultipleConnections())
                        {
                            for (int hp = 0; hp < hitPoints.Count; hp++)
                            {
                                BrickTypeConnection currentPlacerConnection = placerConnList[hp];

                                Vector3 hitPoint = hitPoints[hp];

                                bool withinConnDistance;

                                // Check if this connection on the hit brick is of a connection type 
                                // that accepts multiple bricks.
                                if (hitBrickTypeConn.GetConnectionType(connClassScript).HasMultipleConnections())
                                {
                                    Vector3 correctedHitPos;
                                    withinConnDistance = IsWithinConnectionSpan(hitBrick, hitBrickTypeConn, 
                                        hitPoint, out correctedHitPos, connectionRange);
                                    hitConnWorldPos = correctedHitPos;
                                }
                                else
                                {
                                    float connDistance = Vector3.Distance(hitConnWorldPos, hitPoint);
                                    withinConnDistance = connDistance < connectionRange;
                                }
                            
                                if (withinConnDistance)
                                {
                                    Vector3 connAngleDiff;

                                    if (IsFacing(placeBrick, hitBrick, currentPlacerConnection, hitBrickTypeConn, 
                                        currentPlacerConnection.GetConnectionType(connClassScript),
                                        hitConnectionType, out connAngleDiff))
                                    {
                                        float distanceToCamera = Vector3.Distance(hitConnWorldPos, gameCamera.transform.position);
                                        if (distanceToCamera < connectionHit.distanceToCamera)
                                        {
                                            connectionHit.distanceToCamera = distanceToCamera;
                                            connectionHit.connWorldPos = hitConnWorldPos;
                                            connectionHit.thisConnection = currentPlacerConnection;
                                            connectionHit.otherBrick = hitBrick;
                                            connectionHit.placerRotation = placerRotation;
                                            connectionHit.thisConnLocalStartPos = placerConnStartList[hp];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            placeBrick.brickGO.transform.rotation = originalRot;
            return connectionHit;
        }
    } // Class HitObjectPoints end.

    Brick GetBrickFromHitCollider(GameObject hitCollider)
    {
        BrickTypeIdentifier hitIdentifier = hitCollider.GetComponentInParent<BrickTypeIdentifier>();
        if (hitIdentifier != null)
        {
             return hitIdentifier.thisBrick;
        }

        return null;
    } 

    void AddHit(RaycastHit hitInfo, ConnectionType connType, 
        ref List<HitObjectPoints> hitObjectPointsList, 
        BrickTypeConnection placerConn, Quaternion matchRotation,
        Vector3 placerConnStart)
    {
        GameObject hitObject = hitInfo.transform.gameObject;
        Brick hitBrick = GetBrickFromHitCollider(hitObject);

        bool isInList = false;
        // Check if this gameobject is already in list.
        for (int i = 0; i < hitObjectPointsList.Count; i++) {
            HitObjectPoints hitObjectPoints = hitObjectPointsList[i];
            if (hitObjectPoints.SameBrick(hitBrick) && hitObjectPoints.GetConnectionType() == connType)
            {
                hitObjectPoints.AddHitPoint(hitInfo.point, placerConn, placerConnStart);
                isInList = true;
                break;
            }
        }

        if (!isInList)
        {
            HitObjectPoints newHOP = new HitObjectPoints(hitBrick, connType, matchRotation);
            newHOP.AddHitPoint(hitInfo.point, placerConn, placerConnStart);
            hitObjectPointsList.Add(newHOP);
        }
    }

    Vector3 BrickPositionFromConnectionOffset(Brick brickToPlace, ConnectionHit connectionHit)
    {
        Vector3 connPosition;
        connPosition = connectionHit.thisConnLocalStartPos;
        Vector3 connectionOffset = Tools.GetPointWorldPosition(brickToPlace, new Vector3(0.0f, 0.0f)) -
            Tools.GetPointWorldPosition(brickToPlace, connPosition);
        return connectionHit.connWorldPos + connectionOffset;
    }

    class RotationMatches
    {
        Quaternion rotationMatch;
        List<Brick> bricks = new List<Brick>();
        public Quaternion GetRotation()
        {
            return rotationMatch;
        }
        public List<Brick> GetBricks()
        {
            return bricks;
        }
        public RotationMatches(Quaternion pRotationMatch)
        {
            rotationMatch = pRotationMatch;
        }
        public void AddBrick(Brick brick)
        {
            bricks.Add(brick);
        }
    }

    void AddToMatchListFromCurrentConnection(ref RaycastHit hitInfo, Quaternion originalRot, 
        ref List<RotationMatches> rotationMatches, Vector3 currentRot, 
        ref List<Brick> hitBricks, Brick brickToPlace)
    {
        GameObject hitObject = hitInfo.transform.gameObject;
        Brick hitBrick = GetBrickFromHitCollider(hitObject);
        if (hitBrick != null)
        {
            if (Tools.AddBrickToListIfUnique(ref hitBricks, hitBrick))
            {
                // Remove our custom rotation before getting the match rotation.
                brickToPlace.brickGO.transform.rotation = originalRot;
                // Get the matching rotation.
                Quaternion matchRotation = GetMatchRotation(brickToPlace, hitBrick);
                // Set our custom rotation back.
                brickToPlace.brickGO.transform.Rotate(currentRot);
                //SetBrickRotation(brickToPlace, hitBrick, currentRot);
                // Get the euler angles of the matching rotation and add it if it's not there.
                bool added = false;
                for (int rmListIndex = 0; rmListIndex < rotationMatches.Count; rmListIndex++)
                {
                    if (Tools.VectorsAlmostEqual(rotationMatches[rmListIndex].GetRotation().eulerAngles,
                        matchRotation.eulerAngles, 0.01f))
                    {
                        rotationMatches[rmListIndex].AddBrick(hitBrick);
                        // Break if it's already added
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    // If we didn't already have it added.
                    RotationMatches newRotationMatch = new RotationMatches(matchRotation);
                    newRotationMatch.AddBrick(hitBrick);
                    rotationMatches.Add(newRotationMatch);
                }
            }
        }
    }

    void AddToMatchListFromCurrentRotation(Brick brickToPlace,
        ref List<RotationMatches> rotationMatches, ref List<Brick> hitBricks, 
        ref List<BrickTypeConnection> connections, Quaternion originalRot,
        Vector3 currentRot)
    {
        for (int i = 0; i < connections.Count; i++)
        {
            BrickTypeConnection currentConnection = connections[i];
            Vector3 startPos = currentConnection.GetStartPosition();
            RaycastHit hitInfo = new RaycastHit();

            if (currentConnection.GetConnectionType(connectionClassScript).HasMultipleConnections())
            {
                Vector3 endPos = currentConnection.GetEndPosition();
                float length = Vector3.Distance(startPos, endPos);
                int numberOfSteps = (int)(length / mmPerRaycast) + 2;
                Vector3 direction = Vector3.Normalize(endPos - startPos);
                for (int step = 0; step < numberOfSteps; step++)
                {
                    Vector3 thisPos;
                    if (numberOfSteps >= 3 && (step == numberOfSteps / 2 || 
                        ((numberOfSteps %2) == 0 &&  step == numberOfSteps / 2 - 1)))
                    {
                        thisPos = startPos + direction * (length / 2.0f);
                    }

                    else if (step != numberOfSteps - 1) {
                        thisPos = startPos + direction * (mmPerRaycast * (float)step);
                    } else {
                        thisPos = startPos + direction * length;
                    }
                    bool hit = RaycastFromConnection(brickToPlace, thisPos, ref hitInfo);
                    if (hit)
                    {
                        AddToMatchListFromCurrentConnection(ref hitInfo, originalRot, ref rotationMatches,
                            currentRot, ref hitBricks, brickToPlace);
                    }
                }
            }
            else {
                bool hit = RaycastFromConnection(brickToPlace, startPos, ref hitInfo);
                if (hit)
                {
                    AddToMatchListFromCurrentConnection(ref hitInfo, originalRot, ref rotationMatches,
                        currentRot, ref hitBricks, brickToPlace);
                }
            }
        }
    }

    List<RotationMatches> GetRotationMatchList(Brick brickToPlace, List<ConnectionType> connTypes)
    {   
        // First check what objects are nearby. We do 9 raycasts, 3 
        // different orientations for each axis.
        List<RotationMatches> rotationMatches = new List<RotationMatches>();
        List<Brick> hitBricks = new List<Brick>();
        Quaternion originalRot = brickToPlace.brickGO.transform.rotation;
        for (int connIndex = 0; connIndex < connTypes.Count; connIndex++)
        {
            ConnectionType currentConnType = connTypes[connIndex];
            List<BrickTypeConnection> connections = brickToPlace.GetFreeConnectionsOfType(currentConnType);
            if (connections != null)
            {
                if (connections.Count > multipleRaycastConnLimit)
                {
                    // We do a simple raycast check where we don't care about the rotation.
                    Vector3 currentRot = new Vector3(0, 0, 0);
                    AddToMatchListFromCurrentRotation(brickToPlace, ref rotationMatches, ref hitBricks,
                        ref connections, originalRot, currentRot);
                }
                else {
                    for (int axis = 0; axis < 3; axis++)
                    {
                        Vector3 rotation;
                        if (axis == 0)
                        {
                            rotation = new Vector3(45, 0, 0);
                        }
                        else if (axis == 1)
                        {
                            rotation = new Vector3(0, 45, 0);
                        }
                        else
                        {
                            rotation = new Vector3(0, 0, 45);
                        }
                        for (int rot = -1; rot <= 1; rot++)
                        {
                            Vector3 currentRot = rotation * rot;
                            // Rotate the brick -45, 0 or 45 degrees around the current axis.
                            brickToPlace.brickGO.transform.rotation = originalRot;
                            brickToPlace.brickGO.transform.Rotate(currentRot);
                            AddToMatchListFromCurrentRotation(brickToPlace, ref rotationMatches, ref hitBricks,
                                ref connections, originalRot, currentRot);
                            // Restore the rotation of the brick.
                            brickToPlace.brickGO.transform.rotation = originalRot;
                        }
                    }
                }
            }
        }
        return rotationMatches;
    }

    public bool SetToSnapPoint(Brick brickToPlace)
    {
        // Shoot rays from each connection in the brickToPlace.
        RaycastHit hitInfo = new RaycastHit();
        BrickTypeConnection currentConnection;
        List<ConnectionType> connTypes = new List<ConnectionType>();
        connTypes = connectionClassScript.GetAllConnectionTypes();

        List<HitObjectPoints> hitObjectPointsList = new List<HitObjectPoints>();

        ConnectionHit nearestConnectionHit;
        nearestConnectionHit.distanceToCamera = float.PositiveInfinity;
        nearestConnectionHit.connWorldPos = new Vector3();
        nearestConnectionHit.thisConnection = new BrickTypeConnection(0, new Vector3(), new Vector3());
        nearestConnectionHit.otherBrick = null;
        nearestConnectionHit.placerRotation = Quaternion.identity;
        nearestConnectionHit.thisConnLocalStartPos = new Vector3();

        List<RotationMatches> rotationMatches = GetRotationMatchList(brickToPlace, connTypes);

        // Do all raycasting.
        for (int rotMatchIndex = 0; rotMatchIndex < rotationMatches.Count; rotMatchIndex++)
        {
            Quaternion matchRotation = rotationMatches[rotMatchIndex].GetRotation();
            List<Brick> bricks = rotationMatches[rotMatchIndex].GetBricks();
            // Save the original rotation and rotate this brick to its match rotation.
            Quaternion originalRot = brickToPlace.brickGO.transform.rotation;
            brickToPlace.brickGO.transform.rotation = matchRotation;
            // We will raycast in a custom layer (10) since we only are interested in those bricks.
            Tools.SetBricksLayer(bricks, 10);

            for (int connIndex = 0; connIndex < connTypes.Count; connIndex++)
            {
                ConnectionType currentConnType = connTypes[connIndex];
                List<BrickTypeConnection> connections = brickToPlace.GetFreeConnectionsOfType(currentConnType);

                if (connections != null)
                {
                    for (int i = 0; i < connections.Count; i++)
                    {
                        currentConnection = connections[i];
                        Vector3 startPos = currentConnection.GetStartPosition();
                        if (currentConnType.HasMultipleConnections())
                        {
                            Vector3 endPos = currentConnection.GetEndPosition();
                            float length = Vector3.Distance(startPos, endPos);
                            int numberOfSteps = (int)(length / mmPerRaycast) + 2;
                            Vector3 direction = Vector3.Normalize(endPos - startPos);
                            int mask = 1 << 10;
                            for (int step = 0; step < numberOfSteps; step++)
                            {
                                Vector3 thisPos;
                                if (numberOfSteps >= 3 && (step == numberOfSteps / 2 || 
                                    ((numberOfSteps % 2) == 0 && step == numberOfSteps / 2 - 1)))
                                {
                                    thisPos = startPos + direction * (length / 2.0f);
                                }

                                else if (step != numberOfSteps - 1)
                                {
                                    thisPos = startPos + direction * (mmPerRaycast * (float)step);
                                }
                                else {
                                    thisPos = startPos + direction * length;
                                }
                                bool hit = RaycastFromConnection(brickToPlace, thisPos, ref hitInfo, mask);
                                if (hit)
                                {
                                    AddHit(hitInfo, currentConnType, ref hitObjectPointsList, currentConnection, matchRotation,
                                        thisPos);
                                }
                            }
                        }
                        else {
                            // We want to mask so we only are raycasting against layer 10, which is the current
                            // bricks with the current rotation match.
                            int mask = 1 << 10;
                            bool hit = RaycastFromConnection(brickToPlace, startPos, ref hitInfo, mask);
                            if (hit)
                            {
                                AddHit(hitInfo, currentConnType, ref hitObjectPointsList, currentConnection, 
                                    matchRotation, startPos);
                            }
                        }
                    }
                }
            }
            // Restore the rotation.
            brickToPlace.brickGO.transform.rotation = originalRot;
            // Restore the layer of the bricks.
            Tools.SetBricksLayer(bricks, 1);
        }
        
        for (int i = 0; i < hitObjectPointsList.Count; i++) {
            ConnectionHit thisConnectionHit = 
                hitObjectPointsList[i].GetBestConnection(ref brickToPlace, Camera.main, connectionRange,
                connectionClassScript);
            if (thisConnectionHit.distanceToCamera != float.PositiveInfinity)
            {
                Vector3 resultingPosition = BrickPositionFromConnectionOffset(brickToPlace, thisConnectionHit);
                float resultingDistanceToCamera = Vector3.Distance(resultingPosition, Camera.main.transform.position);

                if (resultingDistanceToCamera < nearestConnectionHit.distanceToCamera)
                {
                    nearestConnectionHit.distanceToCamera = resultingDistanceToCamera;
                    nearestConnectionHit.connWorldPos = thisConnectionHit.connWorldPos;
                    nearestConnectionHit.thisConnection = thisConnectionHit.thisConnection;
                    nearestConnectionHit.otherBrick = thisConnectionHit.otherBrick;
                    nearestConnectionHit.placerRotation = thisConnectionHit.placerRotation;
                    nearestConnectionHit.thisConnLocalStartPos = thisConnectionHit.thisConnLocalStartPos;
                }
            }
        }

        if (nearestConnectionHit.distanceToCamera != float.PositiveInfinity)
        {
            brickToPlace.brickGO.transform.rotation =  nearestConnectionHit.placerRotation;
            brickToPlace.brickGO.transform.position = BrickPositionFromConnectionOffset(brickToPlace, 
                                                                                nearestConnectionHit);
            return true;
        }

        return false;
    
        // 1. See what object a ray hits.
        // 2. Check how close the connection is to respective connections in the hit object.
        // 3. Save the position and distance to all compatible connections that are within the connection
        // range.
        // 4. Do the same for the rest of the rays.
        // 5. Check which "hit" (within range) connection that are closest to the camera. Let that connection
        // be the one used to position the brick.
    }

    // Checks wether two connections are close enough to connect, and have matching orientations.
    bool IsConnecting(BrickTypeConnection conn1, Brick brick1, BrickTypeConnection conn2, 
        Brick brick2, ConnectionType connType1, ConnectionType connType2)
    {
        Vector3 pos1;
        Vector3 pos2;
        // 1. Check if the positions match.

        pos1 = Tools.GetPointWorldPosition(brick1, conn1.GetStartPosition());
        pos2 = Tools.GetPointWorldPosition(brick2, conn2.GetStartPosition());

        if (connType1.HasMultipleConnections())
        {
            Vector3 correctedPos = new Vector3();
            if (IsWithinConnectionSpan(brick1, conn1, pos2, out correctedPos, isConnectingRange))
            {
                Vector3 connAngleDiff;
                return IsFacing(brick1, brick2, conn1, conn2, connType1, connType2, out connAngleDiff);
            }
        }

        if (connType2.HasMultipleConnections())
        {
            Vector3 correctedPos = new Vector3();
            if (IsWithinConnectionSpan(brick2, conn2, pos1, out correctedPos, isConnectingRange))
            {
                Vector3 connAngleDiff;
                return IsFacing(brick1, brick2, conn1, conn2, connType1, connType2, out connAngleDiff);
            }
        }

        if (Vector3.Distance(pos1, pos2) < isConnectingRange)
        {
            //Debug.DrawLine(pos1, new Vector3(0.0f, 100.0f, , new Color(1f, 1f, 0f, 0.2f), 100);
            //Debug.DrawRay(pos1, Vector3.Normalize(worldDirection2) * 100.0f, new Color(1f, 1f, 0f, 0.2f), 100);
            // 2. Check if the directions face each others.
            Vector3 connAngleDiff;
            return IsFacing(brick1, brick2, conn1, conn2, connType1, connType2, out connAngleDiff);
        }

        return false;
    }

    void RemoveConnectors(Brick brick, Vector3 studPos) 
    {
        foreach (Transform child in brick.brickGO.transform)
        {
            if (child.gameObject.GetComponent<ConnectionTypeIdentifier>() != null)
            {
                if (child.transform.position == studPos)
                {
                    Destroy(child.gameObject);
                    return;
                }
            }
        }
    }

    void AddConnection(ConnectionType connectionType1, ConnectionType connectionType2,
        Brick brick1, Brick brick2, BrickTypeConnection conn1, BrickTypeConnection conn2)
    {
        BrickConnection brickConn1 =
            AddSingleConnection(connectionType1, connectionType2, brick1, conn1, brick2, conn2);
        BrickConnection brickConn2 =
            AddSingleConnection(connectionType2, connectionType1, brick2, conn2, brick1, conn1);
        brickConn1.SetOtherBrickConnection(brickConn2);
        brickConn2.SetOtherBrickConnection(brickConn1);
    }

    BrickConnection AddSingleConnection(ConnectionType connectionType, ConnectionType otherConnectionType, 
        Brick thisBrick, BrickTypeConnection thisConnection, 
        Brick connectedBrick, BrickTypeConnection connectedConnection)
    {
        if (connectionType.HasConnectorMesh() && !connectedConnection.hasVisibleConnector())
        {
            RemoveConnectors(thisBrick, Tools.GetPointWorldPosition(thisBrick, thisConnection.GetStartPosition()));
        }
        BrickConnection brickConn = new BrickConnection(connectionType.GetID(), connectedBrick,
            thisConnection, connectedConnection,
            otherConnectionType.GetID());

        thisBrick.AddConnection(brickConn, connectionType);

        return brickConn;
    }

    void DisableColliders(Brick brick)
    {
        Collider[] colliders = brick.brickGO.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    void EnableColliders(Brick brick)
    {
        Collider[] colliders = brick.brickGO.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }
    }

    public Collider[] GetNearbyColliders(Brick brick)
    {
        DisableColliders(brick);
        Vector3 axisAlignedBoxExtents = brick.brickGO.GetComponent<Renderer>().bounds.extents;
        Vector3 axisAlignedBoxCenter = brick.brickGO.GetComponent<Renderer>().bounds.center;
        Collider[] nearbyColliders = Physics.OverlapBox(axisAlignedBoxCenter,
            axisAlignedBoxExtents + new Vector3(0.3f, 0.3f, 0.3f),
            Quaternion.identity);
        EnableColliders(brick);
        return nearbyColliders;
    }
    
    public void PlaceBrick(Brick brick)
    {
        // 1. Get a list of all nearby bricks (overlap sphere, largest dimension of
        // brick as radius).
        // 2. Check all the free connections on nearby bricks if they are equal to (or very  close to)
        // the compatible free connections on the brick to be placed.

        Collider[] nearbyColliders = GetNearbyColliders(brick);

        List<int> connectionTypesIndices = brick.brickType.GetConnectionTypesIndices();
 
        for (int connTypes = 0; connTypes < connectionTypesIndices.Count; connTypes++)
        {
            int connTypeIndex = connectionTypesIndices[connTypes];
            ConnectionType currentConnType = connectionClassScript.ConnectionTypeFromId(connTypeIndex);
            List<BrickTypeConnection> brickConnections = brick.GetFreeConnectionsOfType(currentConnType);

            // Create a list from the nearby colliders containing the actual bricks (some bricks
            // have multiple colliders).
            List<Brick> nearbyBricks = new List<Brick>();
            for (int collIndex = 0; collIndex < nearbyColliders.Length; collIndex++)
            {
                BrickTypeIdentifier brickTypeIdentifier =
                    nearbyColliders[collIndex].GetComponentInParent<BrickTypeIdentifier>();
                if (brickTypeIdentifier != null)
                {
                    Brick foundBrick = brickTypeIdentifier.thisBrick;
                    // Check if it's already in the list.
                    bool brickAlreadyAdded = false;
                    for (int i = 0; i < nearbyBricks.Count; i++)
                    {
                        if (foundBrick.SameBrick(nearbyBricks[i]))
                        {
                            brickAlreadyAdded = true;
                        }
                    }
                    // Add the brick as it wasn't in the ist.
                    if (!brickAlreadyAdded)
                    {
                        nearbyBricks.Add(foundBrick);
                    }
                }
            }

            for (int objIndex = 0; objIndex < nearbyBricks.Count; objIndex++)
            {
                Brick foundBrick = nearbyBricks[objIndex];    

                int [] compatibleTypesIndices =
                    currentConnType.GetCompatibleTypes();

                for (int compType = 0; compType < compatibleTypesIndices.Length; compType++)
                {
                    int compTypeIndex = compatibleTypesIndices[compType];
                    ConnectionType compatibleType = connectionClassScript.ConnectionTypeFromId(compTypeIndex);

                    List<BrickTypeConnection> foundFreeConnections =
                        foundBrick.GetFreeConnectionsOfType(compatibleType);

                    if (foundFreeConnections != null)
                    {
                        for (int connIndex = 0; connIndex < brickConnections.Count; connIndex++)
                        {
                            BrickTypeConnection conn1 = brickConnections[connIndex];
                            for (int foundConnIndex = 0; foundConnIndex < foundFreeConnections.Count; foundConnIndex++)
                            {
                                BrickTypeConnection conn2 = foundFreeConnections[foundConnIndex];
                                if (IsConnecting(conn1, brick, conn2, foundBrick, currentConnType, compatibleType))
                                {
                                    // The bricks are connecting. We create connections.
                                    AddConnection(currentConnType, compatibleType, brick, foundBrick, conn1, conn2);
                                }
                            }
                        }
                    }
                }
            }
        }

        Destroy(brick.brickGO.GetComponent<Rigidbody>());
        Destroy(brick.brickGO.GetComponent<PlacerTrigger>());
        Collider [] colldiers = brick.brickGO.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colldiers.Length; i++)
        {
            colldiers[i].isTrigger = false;
        }
        Tools.SetBrickLayer(brick, 1);
        brick.brickGO.isStatic = true;
        bricks.Add(brick);
    }
        
    public List<Brick> GetBricks()
    {
        return bricks;
    }
    
    public int NumberOfBricks()
    {
        return bricks.Count;
    }

    public void AddBrick(Brick brick)
    {
        bricks.Add(brick);
    }

    public void RemoveBrick(Brick brick)
    {
        for (int i = 0; i < bricks.Count; i++)
        {
            if (bricks[i].SameBrick(brick))
            {
                bricks.RemoveAt(i);
                return;
            }
        }
    }

	// Use this for initialization
	void Awake () {
        bricks = new List<Brick>();
        hiddenBricks = new List<Brick>();
        connectionClassScript = GameObject.Find("ConnectionClass").GetComponent<ConnectionClass>();
	}
}
