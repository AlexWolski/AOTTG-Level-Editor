using GILES;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommonReferences
{
    //References to commonly used game objects
    public static GameObject mainObject { get; private set; }
    public static GameObject toolHandle { get; private set; }

    //The scripts attatched to those game objects
    public static EditorManager editorManager { get; private set; }
    public static MapManager mapManager { get; private set; }
    public static SelectionHandle selectionHandle { get; private set; }

    //Get references to the gameobjects and their script components
    static CommonReferences()
    {
        mainObject = GameObject.Find("Main Object");
        toolHandle = GameObject.Find("Tool Handle");

        editorManager = mainObject.GetComponent<EditorManager>();
        mapManager = mainObject.GetComponent<MapManager>();
        selectionHandle = toolHandle.GetComponent<SelectionHandle>();
    }
}
