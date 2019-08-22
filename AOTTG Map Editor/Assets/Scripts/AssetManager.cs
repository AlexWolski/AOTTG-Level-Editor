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

    //Instantiate the GameObject wtih the given name
    public static GameObject instantiateRcObject(string objectName)
    {
        return (GameObject)MonoBehaviour.Instantiate((GameObject)RCAssets.Load(objectName));
    }

    //Load a material
    public static Material loadRcMaterial(string materialName)
    {
        return (Material)RCAssets.Load(materialName);
    }
}