using UnityEngine;
using System.Collections;
using System;

public class LevelManager : MonoBehaviour
{
    //A reference to the empty level to add objects to
    [SerializeField]
    private GameObject levelRoot;
    //A reference to object selection script
    private ObjectSelection objectSelection;

    //Get a reference to the ObjectSelection script
    void Start()
    {
        objectSelection = gameObject.GetComponent<ObjectSelection>();
    }

    //Parse the given level script and load the level
    public void loadLevel(string levelScript)
    {
        //Seperate the level by new lines into object scripts
        string[] parsedLevel = levelScript.Split('\n');

        //Add each object to the level
        foreach (string objectScript in parsedLevel)
            addObjectToLevel(parseObject(objectScript));
    }

    //Parse the given object script and instantiate the object
    private GameObject parseObject(string objectScript)
    {
        //
        objectType type;

        //Seperate the object script by comma
        string[] parsedObject = objectScript.Split(',');

        //Make a string array containing the names of each type of object
        string[] objectTypes = Enum.GetNames(typeof(objectType));

        //Check if the first element of the object script matches any of the types
        foreach (string objectType in objectTypes)
        {
            //If the first element matches a type, set it as the type of the object
            if (parsedObject[0].StartsWith(objectType))
                type = (objectType)Enum.Parse(typeof(objectType), objectType);
        }

        //
        return AssetManager.instantiateRCAsset("cuboid", "earth1", new Vector3(1f, 1f, 1f), new Color(0, 1, 0), new Vector2(0.5f, 0.5f), new Vector3(-10, 0, 0), Quaternion.Euler(0, 0, 0));
    }

    private void addObjectToLevel(GameObject objectToAdd)
    {
        //Make the new object a child of the level root.
        objectToAdd.transform.parent = levelRoot.transform;
        //Make the new object selectable
        objectSelection.addSelectable(objectToAdd);
    }
}
