using UnityEngine;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

public class Tools
{
    public static void SetBrickLayer(Brick brick, int layer)
    {
        Collider[] colliders = GetColliders(brick);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].gameObject.layer = layer;
        }
    }

    public static void SetBricksLayer(List<Brick> bricks, int layer)
    {
        for (int i = 0; i < bricks.Count; i++)
        {
            SetBrickLayer(bricks[i], layer);
        }
    }

    public static Collider [] GetColliders(Brick brick)
    {
        Collider[] colliders = brick.brickGO.GetComponentsInChildren<Collider>();
        return colliders;
    }

    public static bool VectorsSameDirection(Vector3 vec1, Vector3 vec2, bool bidirectional)
    {
        vec1 = vec1.normalized;
        vec2 = vec2.normalized;
        if (vec1 == vec2)
        {
            return true;
        } 

        if (bidirectional && vec1 == -vec2)
        {
            return true;
        }

        return false;
    }

    public static Vector3 ClosestPointOnLineSegmentFromPoint(Vector3 start, Vector3 end, Vector3 pnt)
    {
        var line = (end - start);
        var len = line.magnitude;
        line.Normalize();

        var v = pnt - start;
        var d = Vector3.Dot(v, line);
        d = Mathf.Clamp(d, 0f, len);
        return start + line * d;
    }
    
    public static Vector3 GetPointWorldPosition(Brick brick, Vector3 connStartPos)
    {
        return brick.brickGO.transform.TransformPoint(connStartPos / 100.0f);
    }

    public static Vector3 GetConnectionWorldDirection(Brick brick, Vector3 connLocalStart, Vector3 connLocalEnd)
    {
        Vector3 connStartWorldPos = GetPointWorldPosition(brick, connLocalStart);
        Vector3 connEndWorldPos = GetPointWorldPosition(brick, connLocalEnd);
        Vector3 connDirection = connEndWorldPos - connStartWorldPos;
        return connDirection;
    }

    public static Vector3 GetConnectionLocalDirection(Vector3 connLocalStart, Vector3 connLocalEnd)
    {
        Vector3 connDirection = connLocalEnd - connLocalStart;
        return connDirection;
    }

    /*
    public static void SetBrickRotation(Brick brick, Brick otherBrick, Vector3 eulerRotation)
    {
        Transform transform = brick.brickGO.transform;
        // z, x, y
        Vector3 currentRot = transform.eulerAngles;
        transform.rotation = Quaternion.identity;
        transform.Rotate(Vector3.up, eulerRotation.y + currentRot.y, Space.World);
        transform.Rotate(Vector3.right, eulerRotation.x + currentRot.x, Space.World);
        transform.Rotate(Vector3.forward, eulerRotation.z + currentRot.z, Space.World);

        //Quaternion.Euler(new Vector3(eulerRotation.x, eulerRotation.y, eulerRotation.z)) * brick.brickGO.transform.rotation;
    }
    */
    public static bool VectorsAlmostEqual(Vector3 vector1, Vector3 vector2, float maxError)
    {
        return (Mathf.Abs(Mathf.Abs(vector1.x) - Mathf.Abs(vector2.x)) < maxError &&
            Mathf.Abs(Mathf.Abs(vector1.y) - Mathf.Abs(vector2.y)) < maxError &&
            Mathf.Abs(Mathf.Abs(vector1.z) - Mathf.Abs(vector2.z)) < maxError);
    }

    public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 axis)
    {
        return Mathf.Atan2(
            Vector3.Dot(axis, Vector3.Cross(v1, v2)),
            Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
    }

    public static bool AddToListIfUnique<T>(ref List<T> list, T newItem)
    {
        if (!list.Contains(newItem))
        {
            list.Add(newItem);
            return true;
        }
        return false;
    }

    public static bool AddBrickToListIfUnique(ref List<Brick> list, Brick newItem)
    {
        for (int i = 0; i < list.Count; i++) 
        {
            if (list[i].SameBrick(newItem))
            {
                return false;
            }
        }
        list.Add(newItem);
        return true;
    }

    public static Vector3 StringToVec3(string line)
    {
        string[] str_split = line.Split(null);
        Vector3 returnVector = new Vector3(
        float.Parse(str_split[0], CultureInfo.InvariantCulture.NumberFormat),
        float.Parse(str_split[1], CultureInfo.InvariantCulture.NumberFormat),
        float.Parse(str_split[2], CultureInfo.InvariantCulture.NumberFormat)
        );
        return returnVector;
    }

    public static Vector4 StringToVec4(string line)
    {
        string[] str_split = line.Split(null);
        Vector4 returnVector = new Vector4(
        float.Parse(str_split[0], CultureInfo.InvariantCulture.NumberFormat),
        float.Parse(str_split[1], CultureInfo.InvariantCulture.NumberFormat),
        float.Parse(str_split[2], CultureInfo.InvariantCulture.NumberFormat),
        float.Parse(str_split[3], CultureInfo.InvariantCulture.NumberFormat)
        );
        return returnVector;
    }

    public static Vector3 ReadVector3(StringReader reader)
    {
        string line = readNext(reader);
        return StringToVec3(line);
    }

    public static string readNext(StringReader reader)
    {
        string line = reader.ReadLine();
        if (line != null)
        {
            return line.Trim();
        }
        return null;
    }
}

public class ToolClass : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}