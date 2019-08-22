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
        //Remove all of the new lines in the script
        mapScript = mapScript.Replace("\n", "");

        //Seperate the map by semicolon
        string[] parsedMap = mapScript.Split(';');

        //Create each object and add it to the map
        foreach (string objectScript in parsedMap)
        {
            //Parse the object script and create a new map object
            GameObject newMapObject = loadObject(objectScript);

            //If the object data is valid, add the object to the map hierarchy
            if (newMapObject)
                addObjectToMap(newMapObject);
        }
    }

    //Parse the given object script and instantiate a new GameObject with the data
    private GameObject loadObject(string objectScript)
    {
        //Seperate the object script by comma
        string[] parsedObject = objectScript.Split(',');

        //Get the type of the object
        objectType? type = stringToObjectType(parsedObject[0]);

        //If the type is invalid, skip the object
        if (!type.HasValue)
            return null;


        ////

        GameObject newObject = AssetManager.instantiateRcObject("cuboid");
        newObject.AddComponent<MapObject>();
        MapObject newMapObject = newObject.GetComponent<MapObject>();

        newMapObject.Position = new Vector3(-10, 0, 0);
        newMapObject.Scale = new Vector3(1, 1, 1);
        newMapObject.Rotation = Quaternion.identity;
        newMapObject.ObjectName = "cuboid";

        ////

        return newObject;
    }

    //Return the objectType assosiated with the given string
    private objectType? stringToObjectType(string typeString)
    {
        //Make a string array containing the names of each type of object
        string[] objectTypes = Enum.GetNames(typeof(objectType));

        //Check if the string matches any of the types
        foreach (string objectType in objectTypes)
        {
            //If the string matches a type, return that type
            if (typeString.StartsWith(objectType))
                return (objectType)Enum.Parse(typeof(objectType), objectType);
        }

        //If the object type is not valid, return null
        return null;
    }

    //Add the given object to the map hierarchy and make it selectable
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
