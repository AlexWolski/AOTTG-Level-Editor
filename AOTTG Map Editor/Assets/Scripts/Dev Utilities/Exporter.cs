using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static class Exporter
{
    private static string texturePath = Application.dataPath + "/Exports/Textures/";
    private static string materialPath = "Assets/Exports/Materials/";

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

    public static void exportTexture(Texture texture, string path)
    {
        RenderTexture rendTex = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, rendTex);
        RenderTexture currentRend = RenderTexture.active;
        RenderTexture.active = rendTex;
        Texture2D tex2D = new Texture2D(texture.width, texture.height);
        tex2D.ReadPixels(new Rect(0, 0,  rendTex.width, rendTex.height), 0, 0);
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
}