using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BrickMaterials : MonoBehaviour {

    List<Material> commonMaterials;
    List<SpecialBrickMaterialList> specialBrickMaterialLists;
    List<SpecialConnectionMaterialList> specialConnMaterialLists;
    Material defaultMaterial;
    ConnectionClass connectionClassScript;

    public static bool SameColor(Color col1, Color col2)
    {
        return (col1.r == col2.r && col1.g == col2.g && col1.b == col2.b);
    }

    public static Material createNewMaterial(ref Material defaultMaterial, Color color, 
        Texture normalMap = null, Texture texture = null)
    {

        Material newMaterial = Instantiate(defaultMaterial);
        newMaterial.color = color;

        if (texture != null)
        {
            newMaterial.SetTexture("_MainTex", texture);
        } else
        {
            newMaterial.SetTexture("_MainTex", null);
        }
        if (normalMap != null)
        {
            newMaterial.SetTexture("_BumpMap", normalMap);
        }
        else
        {
            newMaterial.SetTexture("_BumpMap", null);
        }

        return newMaterial;
    }

    public static void ChangeColorInMaterialList(Color oldColor, Color newColor, ref List<Material> materials)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (SameColor(materials[i].color, oldColor))
            {
                materials[i].color = newColor;
            }
        }
    }

    public abstract class SpecialMaterial
    {
        protected List<Material> materials;
        protected Texture texture = null;
        protected Texture normalMap = null;

        public SpecialMaterial ()
        {
            materials = new List<Material>();
        }

        public void ChangeColor(Color oldColor, Color newColor)
        {
            ChangeColorInMaterialList(oldColor, newColor, ref materials);
        }

        public Material GetOrCreateMaterialOfColor(ref Material defaultMaterial, Color color)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                if (SameColor(materials[i].color, color))
                {
                    return materials[i];
                }
            }
            Material newMaterial = createNewMaterial(ref defaultMaterial, color, normalMap, texture);
            materials.Add(newMaterial);
            return newMaterial;
        }
    }

    public class SpecialConnectionMaterialList : SpecialMaterial
    {
        ConnectionType connType;

        public SpecialConnectionMaterialList(ConnectionType p_connType) : base()
        {
            connType = p_connType;
            texture = connType.GetTexture();
            normalMap = connType.GetNormalMap();
        }

        public ConnectionType GetConnectionType()
        {
            return connType;
        }
    }

    public class SpecialBrickMaterialList : SpecialMaterial
    {
        BrickType brickType;

        public SpecialBrickMaterialList(BrickType p_brickType) : base()
        {
            brickType = p_brickType;
            texture = brickType.GetTexture();
            normalMap = brickType.GetNormalMap();
        }

        public BrickType GetBrickType()
        {
            return brickType;
        }
    }

    /*
    public void RemoveUnusedMaterials(Bricks bricksScript)
    {
        List<Brick> bricks = bricksScript.GetBricks();
        for (int m = 0; m < brickMaterials.Count; m++) {
            bool materialInUse = false;
            for (int i = 0; i < bricks.Count; i++)
            {
                if (sameColor(bricks[i].brickGO.GetComponent<Renderer>().material.color, 
                    brickMaterials[m].material.color)) {
                    materialInUse = true;
                }
            }
            if (!materialInUse)
            {
                for (int i = 0; i < studMaterials.Count; i++)
                {
                    if (sameColor(studMaterials[i].material.color, brickMaterials[m].material.color)) {
                        Destroy(studMaterials[i].material);
                        studMaterials.RemoveAt(i);
                    }
                }
                Destroy(brickMaterials[m].material);
                brickMaterials.RemoveAt(m);
            }
        }
    }
    */

    public Material GetCommonMaterial(Color color)
    {
        for (int i = 0; i < commonMaterials.Count; i++)
        {
            if (SameColor(commonMaterials[i].color, color))
            {
                return commonMaterials[i];
            }
        }
        Material newMaterial = createNewMaterial(ref defaultMaterial, color);
        commonMaterials.Add(newMaterial);
        return newMaterial;
    }

    public Material GetSpecialMaterialForBrick(Color color, BrickType brickType)
    {
        for (int i = 0; i < specialBrickMaterialLists.Count; i++)
        {
            if (specialBrickMaterialLists[i].GetBrickType().GetName() == brickType.GetName())
            {
                return specialBrickMaterialLists[i].GetOrCreateMaterialOfColor(ref defaultMaterial, color);
            }
        }
        SpecialBrickMaterialList newMaterialList = new SpecialBrickMaterialList(brickType);
        Material newMaterial = newMaterialList.GetOrCreateMaterialOfColor(ref defaultMaterial, color);
        specialBrickMaterialLists.Add(newMaterialList);
        return newMaterial;
    }

    public Material GetSpecialMaterialForConnector(Color color, ConnectionType connType)
    {
        for (int i = 0; i < specialConnMaterialLists.Count; i++)
        {
            if (specialConnMaterialLists[i].GetConnectionType().GetID() == connType.GetID())
            {
                return specialConnMaterialLists[i].GetOrCreateMaterialOfColor(ref defaultMaterial, color);
            }
        }
        SpecialConnectionMaterialList newMaterialList = new SpecialConnectionMaterialList(connType);
        Material newMaterial = newMaterialList.GetOrCreateMaterialOfColor(ref defaultMaterial, color);
        specialConnMaterialLists.Add(newMaterialList);
        return newMaterial;
    }

    public void ChangeMaterial(Color oldColor, Color newColor)
    {
        for (int i = 0; i < specialBrickMaterialLists.Count; i++)
        {
            specialBrickMaterialLists[i].ChangeColor(oldColor, newColor);
        }

        for (int i = 0; i < specialConnMaterialLists.Count; i++)
        {
            specialConnMaterialLists[i].ChangeColor(oldColor, newColor);
        }

        ChangeColorInMaterialList(oldColor, newColor, ref commonMaterials);
    }

    public void SetBrickAndStudColor(Brick brick, Color color)
    {
        SetBrickMaterials(brick, color);
        SetStudMaterials(brick, color);
    }

    public void SetBrickMaterials(Brick brick, Color color)
    {
        BrickType brickType = brick.brickType;
        Renderer renderer = brick.brickGO.GetComponent<Renderer>();
        // This brick has many materials, some might have fixed colors.
        List<Material> materials = brickType.GetMaterials();
        if (renderer.sharedMaterials.Length < materials.Count)
        {
            renderer.sharedMaterials = new Material[materials.Count];
        }

        Material[] newMaterials = renderer.sharedMaterials;

        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] == null)
            {
                if (brickType.GetNormalMap() != null ||
                    brickType.GetTexture() != null)
                {
                    newMaterials[i] = GetSpecialMaterialForBrick(color, brickType);
                }
                else
                {
                    // Use the simple one-coloured material.
                    newMaterials[i] = GetCommonMaterial(color);
                }
            }
            else
            {
                newMaterials[i] = materials[i];
            }
        }

        renderer.sharedMaterials = newMaterials;
    }

    public void SetStudMaterials(Brick brick, Color color)
    {
        foreach (Transform child in brick.brickGO.transform)
        {
            Renderer childRenderer = child.gameObject.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                ConnectionTypeIdentifier connTypeIdentifier =
                    childRenderer.gameObject.GetComponent<ConnectionTypeIdentifier>();
                if (connTypeIdentifier != null)
                {
                    int connTypeIndex = connTypeIdentifier.GetConnTypeIndex();
                    ConnectionType connType = connectionClassScript.ConnectionTypeFromId(connTypeIndex);
                    // Check if this connType is special.
                    if (connType.HasNormalMap() || connType.HasTexture())
                    {
                        childRenderer.sharedMaterial =
                            GetSpecialMaterialForConnector(color, connType);
                    }
                    else
                    {
                        childRenderer.sharedMaterial =
                            GetCommonMaterial(color);
                    }
                }
            }
        }
    }
    
    // Checks if the color is in use by brick.
    public static bool IsColorInUse(Brick brick, Color color)
    {
        return SameColor(brick.brickGO.GetComponent<Renderer>().sharedMaterial.color, color);
    }

    // Use this for initialization
    void Awake () {
        specialBrickMaterialLists = new List<SpecialBrickMaterialList>();
        specialConnMaterialLists = new List<SpecialConnectionMaterialList>();
        commonMaterials = new List<Material>();
        defaultMaterial = Resources.Load("Materials/DefaultMaterial") as Material;
        connectionClassScript = GameObject.Find("ConnectionClass").GetComponent<ConnectionClass>();
	}
}
