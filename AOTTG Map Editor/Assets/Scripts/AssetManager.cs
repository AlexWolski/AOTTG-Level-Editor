using UnityEngine;
using System.Collections;

public class AssetManager : MonoBehaviour
{
    //A reference to the main object
    [SerializeField]
    private  GameObject mainObject;
    //A reference to the empty level object to add objects to
    [SerializeField]
    private GameObject levelRoot;
    //A reference to object selection script
    private ObjectSelection objectSelection;
    //The unity3d file that contains the assets from RC mod
    private AssetBundle RCAssets;

    //Load the assets and set references from other scripts
    void Start()
    {
        base.StartCoroutine(LoadRCAssets());
        objectSelection = mainObject.GetComponent<ObjectSelection>();
    }

    //Load the RC mod assets from RCAssets.unity3d
    private IEnumerator LoadRCAssets()
    {
        //Reference to the file. Compatible with windows and Mac.
        string url = "File:///" + Application.dataPath + "/RCAssets.unity3d";

        //Wait until caching is available
        while (!Caching.ready)
            yield return null;

        //Load the file
        using (WWW iteratorVariable2 = WWW.LoadFromCacheOrDownload(url, 1))
        {
            RCAssets = iteratorVariable2.assetBundle;
        }
    }

    //Instantiate a GameObject wtih only a position and angle
    public GameObject instantiateRCAsset(string objectName, Vector3 position, Quaternion angle)
    {
        //Instantiate the object with the given position and angle
        GameObject newObject = (GameObject)Instantiate((GameObject)RCAssets.Load(objectName), position, angle);

        //Make the new object a child of the level root.
        newObject.transform.parent = levelRoot.transform;
        //Make the new object selectable
        objectSelection.addSelectable(newObject);

        return newObject;
    }

    //Instantiate a GameObject wtih a texture
    public GameObject instantiateRCAsset(string objectName, string material, Vector3? scale, Color? color, Vector2? tile, Vector3 position, Quaternion angle)
    {
        //Instantiate the object with only a position and angle
        GameObject newObject = instantiateRCAsset(objectName, position, angle);

        //If the scale is not null, scale the object
        if (scale != null)
        {
            //Transform the object
            Vector3 currentScale = newObject.transform.localScale;
            newObject.transform.localScale = new Vector3(currentScale.x * scale.Value.x, currentScale.y * scale.Value.y, currentScale.z * scale.Value.z);
        }

        //If the material is not null, apply it to the object
        if (material != null)
        {
            //If no tiling was given, default to 1 by 1
            if (!tile.HasValue)
                tile = new Vector2(1, 1);

            //Apply the material to all of the children of the object
            foreach (Renderer renderer in newObject.GetComponentsInChildren<Renderer>())
            {
                Material newMaterial = (Material)RCAssets.Load(material);
                renderer.material = newMaterial;

                renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * tile.Value.x, renderer.material.mainTextureScale.y * tile.Value.y);
            }
        }

        //If the color is not null, color the material
        if (color != null)
        {
            //Iterate through all of the filters in the object
            foreach (MeshFilter filter in newObject.GetComponentsInChildren<MeshFilter>())
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

        return newObject;
    }
}