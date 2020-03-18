using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityFBXExporter;

public static class Exporter
{
    private static string texturePath = Application.dataPath + "/Exports/Textures/";
    private static string materialPath = "Assets/Exports/Materials/";
    private static string fbxPath = "Assets/Exports/FBX Models/";

    public static void exportAllTextures()
    {
        Texture[] textures = AssetManager.loadAllTextures();

        foreach (Texture currTexture in textures)
            exportTexture(currTexture, texturePath);
    }

    public static void exportAllMaterials()
    {
        Material[] materials = AssetManager.loadAllMaterials();

        foreach (Material currMaterial in materials)
        {
            //The new directory to store the material and texture
            string newFolder = materialPath + currMaterial.name;

            //Create a directory for the texture
            Directory.CreateDirectory(newFolder);
            //Export the material
            exportMaterial(currMaterial, newFolder + "/");

            //Export the texture of that material
            if (currMaterial.mainTexture != null)
                exportTexture(currMaterial.mainTexture, newFolder + "/");
        }
    }

    public static void instantiateAllMapObjects()
    {
        GameObject[] gameObjects = AssetManager.loadAllGameObjects();

        foreach (GameObject currObject in gameObjects)
        {
            //The new directory to store the material and texture
            string newFolder = fbxPath;

            //Create a directory for the texture
            Directory.CreateDirectory(newFolder);
            //Export the game object
            exportMapObject(currObject, newFolder);
            //Export the mesh of the gameobject
        }
    }

    public static void exportAllMapObjects()
    {
        foreach(GameObject mapObject in AssetManager.loadAllGameObjects())
            exportMapObject(mapObject, fbxPath);
    }

    public static void exportTexture(Texture texture, string path)
    {
        RenderTexture rendTex = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, rendTex);
        RenderTexture currentRend = RenderTexture.active;
        RenderTexture.active = rendTex;
        Texture2D tex2D = new Texture2D(texture.width, texture.height);
        tex2D.ReadPixels(new Rect(0, 0, rendTex.width, rendTex.height), 0, 0);
        tex2D.Apply();
        RenderTexture.active = currentRend;
        RenderTexture.ReleaseTemporary(rendTex);

        byte[] imageData = tex2D.EncodeToPNG();
        File.WriteAllBytes(path + texture.name + ".png", imageData);
    }

    public static void exportMaterial(Material material, string path)
    {
        Material tempMat = Object.Instantiate(material);

        string fileName = tempMat.name.Substring(0, tempMat.name.Length - 7) + ".mat";
        AssetDatabase.CreateAsset(tempMat, path + fileName);
    }

    public static void exportMapObject(GameObject gameObject, string path)
    {
        //If the gameobject has a mesh, add the outline script
        if (gameObject.GetComponent<Renderer>() != null)
            gameObject.AddComponent<Outline>();

        //Go through the children of the object and add the outline script if it has a mesh
        foreach (Transform child in gameObject.transform)
        {
            if (child.GetComponent<Renderer>() != null)
            {
                //Add the outline script to the object and give it a selectable tag
                child.gameObject.AddComponent<Outline>();
                child.gameObject.tag = "Selectable Object";
            }
        }

        //Find all mesh colliders in the object's children and alter the settings for the newer version of Unity
        foreach (MeshCollider meshCollider in gameObject.GetComponentsInChildren<MeshCollider>())
        {
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
        }

        //Add a Map Object tag to the object and set the layer to Default
        gameObject.tag = "Map Object";
        gameObject.layer = 0;

        //Instantiate the object
        GameObject tempObj = Object.Instantiate(gameObject);

        //Export the object
        string fileName;

        if (gameObject.name.EndsWith("(Cuboid)"))
            fileName = gameObject.name.Substring(0, gameObject.name.Length - 7) + ".fbx";
        else
            fileName = gameObject.name + ".fbx";

        //FBXExporter.ExportGameObjToFBX(gameObject, path + fileName, true, true);
    }
}