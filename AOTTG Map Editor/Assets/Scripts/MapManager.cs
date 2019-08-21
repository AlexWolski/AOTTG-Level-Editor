using UnityEngine;
using System.Collections;
using System;

public class MapManager : MonoBehaviour
{
    //A reference to the empty map to add objects to
    [SerializeField]
    private GameObject mapRoot;
    //A reference to object selection script
    private ObjectSelection objectSelection;

    //Get a reference to the ObjectSelection script
    void Start()
    {
        objectSelection = gameObject.GetComponent<ObjectSelection>();
    }

    //Parse the given map script and load the map
    public void loadMap(string mapScript)
    {
        //Seperate the map by new lines into object scripts
        string[] parsedMap = mapScript.Split('\n');

        //Create each object and add it to the map
        foreach (string objectScript in parsedMap)
            addObjectToMap(parseObject(objectScript));
    }

    //Parse the given object script and instantiate the object
    private GameObject parseObject(string objectScript)
    {
        ObjectData objectData = new ObjectData();

        //Seperate the object script by comma
        string[] parsedObject = objectScript.Split(',');

        //Make a string array containing the names of each type of object
        string[] objectTypes = Enum.GetNames(typeof(objectType));

        //Check if the first element of the object script matches any of the types
        foreach (string objectType in objectTypes)
        {
            //If the first element matches a type, set it as the type of the object
            if (parsedObject[0].StartsWith(objectType))
                objectData.type = (objectType)Enum.Parse(typeof(objectType), objectType);
        }

        //
        return AssetManager.instantiateRcObject("cuboid", new Vector3(-10, 0, 0), Quaternion.Euler(0, 0, 0));
    }

    private void addObjectToMap(GameObject objectToAdd)
    {
        //Make the new object a child of the map root.
        objectToAdd.transform.parent = mapRoot.transform;
        //Make the new object selectable
        objectSelection.addSelectable(objectToAdd);
    }

    //Convert the map into a script
    public override string ToString()
    {
        return "";
    }
}
