using UnityEngine;
using System.Collections;

public static class AssetManager
{
    //The unity3d file that contains the assets from RC mod
    private static AssetBundle RCAssets;

    //Load the RC mod assets from RCAssets.unity3d
    public static IEnumerator LoadRCAssets()
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
    public static GameObject instantiateRcObject(string objectName, Vector3 position, Quaternion angle)
    {
        //Instantiate the object with the given position and angle
        GameObject newObject = (GameObject)MonoBehaviour.Instantiate((GameObject)RCAssets.Load(objectName), position, angle);

        return newObject;
    }

    //Load a material
    public static Material loadRcMaterial(string materialName)
    {
        return (Material)RCAssets.Load(materialName);
    }
}