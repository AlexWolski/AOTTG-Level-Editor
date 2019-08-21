using UnityEngine;

public class MapObject : MonoBehaviour
{
    //The struct containing all of the data related to this object
    private ObjectData objectData;

    //Save the given data and modify the object accordingly
    public void setData(ObjectData newData)
    {
        objectData = newData;
        scale(objectData.scale);
        setTexture(objectData.texture);
        setTiling(objectData.tiling);
        setColor(objectData.color);
    }

    public void scale(Vector3 scale)
    {
        //Transform the object
        Vector3 currentScale = gameObject.transform.localScale;
        gameObject.transform.localScale = new Vector3(currentScale.x * scale.x, currentScale.y * scale.y, currentScale.z * scale.z);
    }

    //Apply the given texture as the new material of the object
    public void setTexture(string texture)
    {
        //Apply the material to all of the children of the object
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
        {
            Material newMaterial = AssetManager.loadRcMaterial(texture);
            renderer.material = newMaterial;
        }
    }

    //Resize the texture on the object
    public void setTiling(Vector2 tiling)
    {
        renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * tiling.x, renderer.material.mainTextureScale.y * tiling.y);
    }

    //Color the object
    public void setColor(Color color)
    {
        //Iterate through all of the filters in the object
        foreach (MeshFilter filter in gameObject.GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = filter.mesh;

            //Create an array filled with the new color to apply to the mesh
            Color[] colorArray = new Color[mesh.vertexCount];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = (Color)color;

            //Apply the colors
            mesh.colors = colorArray;
        }
    }

    //Convert the object into a script
    public override string ToString()
    {
        return "";
    }
}
