using UnityEngine;
using System;
using System.Collections;

public class AssetManager : MonoBehaviour
{
    private AssetBundle RCAssets;

    void Start()
    {
        base.StartCoroutine(LoadRCAssets());
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

    //Instantiate a GameObject wtih the given name from the RCAssets AssetBundle
    public GameObject instantiateRCAsset(string objectName, string material, float tileX, float tileY, Vector3 scale, Vector3 position, Quaternion angle)
    {
        //Instantiate the object with the given position and angle
        GameObject newObject = (GameObject) Instantiate((GameObject) RCAssets.Load(objectName), position, angle);

        //Transform the object
        Vector3 currentScale = newObject.transform.localScale;
        newObject.transform.localScale = new Vector3(currentScale.x * scale.x, currentScale.y * scale.y, currentScale.z * scale.z);

        //Apply a material to the object
        foreach(Renderer renderer in newObject.GetComponentsInChildren<Renderer>())
        {
            Material newMaterial = (Material)RCAssets.Load(material);
            renderer.material = newMaterial;
            renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * tileX, renderer.material.mainTextureScale.y * tileY);
        }

        return newObject;
    }
}