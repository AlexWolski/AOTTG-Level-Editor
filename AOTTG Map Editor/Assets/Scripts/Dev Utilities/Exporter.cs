using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Exporter
{
    private static string materialPath = "Assets/Exports/Materials/";

    public static void exportAll()
    {
        Material[] materials = AssetManager.loadAllMaterials();

        foreach (Material currMaterial in materials)
            exportMaterial(currMaterial);
    }

    public static void exportMaterial(Material material)
    {
        Material tempMat = Object.Instantiate(material);

        string fileName = tempMat.name + " export.mat";
        AssetDatabase.CreateAsset(tempMat, materialPath + fileName);
    }
}