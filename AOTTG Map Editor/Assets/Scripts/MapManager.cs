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
    //Determines if the small map bounds have been disabled or not
    private bool boundsDisabled;

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
        string[] parsedMap = mapScript.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        //Create each object and add it to the map
        for (int i = 0; i < parsedMap.Length; i++)
        {
            try
            {
                //Parse the object script and create a new map object
                GameObject newMapObject = loadObject(parsedMap[i]);

                //If the object is defined, add it to the map hierarchy and make it selectable
                if(newMapObject)
                    addObjectToMap(newMapObject);
            }
            catch(Exception e)
            {
                //If there was an issue parsing the object, log the error and skip it
                Debug.Log("Skipping object on line " + i);
                Debug.Log(e.Message);
            }
        }
    }

    //Add the given object to the map hierarchy and make it selectable
    private void addObjectToMap(GameObject objectToAdd)
    {
        //Make the new object a child of the map root.
        objectToAdd.transform.parent = mapRoot.transform;
        //Make the new object selectable
        objectSelection.addSelectable(objectToAdd);
    }

    //Parse the given object script and instantiate a new GameObject with the data
    private GameObject loadObject(string objectScript)
    {
        //Seperate the object script by comma
        string[] parsedObject = objectScript.Split(',');
        //The GameObject loaded from RCAssets corresponding to the object name
        GameObject newObject = null;
        //The MapObject script that will be attatched to the new GameObject
        MapObject mapObject;
        //The type of the object
        objectType type;
        //The position in the parsedObject string array where the position data starts. Defaults to 3 for objects that only have a type, name, posiiton, and angle
        int indexOfPosition = 2;

        try
        {
            //If the script is "map,disableBounds" then set a flag to disable the map boundries and skip the object
            if (parsedObject[0].StartsWith("map") && parsedObject[1].StartsWith("disablebounds"))
            {
                boundsDisabled = true;
                return null;
            }

            //If the length of the string is too short, raise an error
            if (parsedObject.Length < 9)
                throw new Exception("Too few elements in object script");

            //Parse the object type
            type = parseType(parsedObject[0]);
            //Use the object name to load the asset
            newObject = createMapObject(type, parsedObject[1]);
            //Get the MapObject script attached to the new GameObject
            mapObject = newObject.GetComponent<MapObject>();
            //Store the type
            mapObject.Type = type;
            //Store the full type
            mapObject.FullTypeName = parsedObject[0];
            //Store the object name
            mapObject.ObjectName = parsedObject[1];

            //If the object is a titan spawner, store the spawn timer and whether or not it spawns endlessly
            if (mapObject.Type == objectType.photon && mapObject.ObjectName.StartsWith("spawn"))
            {
                mapObject.SpawnTimer = Convert.ToSingle(parsedObject[2]);
                mapObject.EndlessSpawn = (Convert.ToInt32(parsedObject[3]) != 0);
                indexOfPosition = 4;
            }
            //If the object is a region, store the region name and scale
            else if (mapObject.ObjectName.StartsWith("region"))
            {
                mapObject.RegionName = parsedObject[2];
                mapObject.Scale = parseVector3(parsedObject[3], parsedObject[4], parsedObject[5]);
                indexOfPosition = 3;
            }
            //If the object has a texture, store the texture, scale, color, and tiling information
            else if(mapObject.Type == objectType.custom || parsedObject.Length >= 15 && (mapObject.Type == objectType.@base || mapObject.Type == objectType.photon))
            {
                mapObject.Texture = parsedObject[2];
                mapObject.Scale = parseVector3(parsedObject[3], parsedObject[4], parsedObject[5]);
                mapObject.ColorEnabled = (Convert.ToInt32(parsedObject[6]) != 0);

                if(mapObject.ColorEnabled)
                {
                    //If the transparent texture is applied, parse the opacity and use it. Otherwise default to fully opaque
                    if (mapObject.Texture.StartsWith("transparent"))
                        mapObject.Color = parseColor(parsedObject[7], parsedObject[8], parsedObject[9], mapObject.Texture.Substring(11));
                    else
                        mapObject.Color = parseColor(parsedObject[7], parsedObject[8], parsedObject[9], "1");
                }

                mapObject.Tiling = parseVector2(parsedObject[10], parsedObject[11]);
                indexOfPosition = 12;
            }
            //If the object just scale information before the position and rotation, store the scale
            else if(mapObject.Type == objectType.racing || mapObject.Type == objectType.misc)
            {
                mapObject.Scale = parseVector3(parsedObject[2], parsedObject[3], parsedObject[4]);
                indexOfPosition = 5;
            }

            //Set the position and rotation for all objects
            mapObject.Position = parseVector3(parsedObject[indexOfPosition++], parsedObject[indexOfPosition++], parsedObject[indexOfPosition++]);
            mapObject.Rotation = parseQuaternion(parsedObject[indexOfPosition++], parsedObject[indexOfPosition++], parsedObject[indexOfPosition++], parsedObject[indexOfPosition++]);

            //If there is a flag to disable the boundries, disable them
            if (boundsDisabled)
                disableMapBounds();

            return newObject;
        }
        //If there was an error converting an element to a float, destroy the object and pass a new exception to the caller
        catch(FormatException)
        {
            destroyObject(newObject);
            throw new Exception("Error conveting data");
        }
        //If there are any other errors, destroy the object and pass them back up to the caller
        catch (Exception e)
        {
            destroyObject(newObject);
            throw e;
        }
    }

    #region Parser Helpers
    //Check if the object exists. Then disable and destroy it
    private void destroyObject(GameObject objectToDestroy)
    {
        if (objectToDestroy)
        {
            objectToDestroy.SetActive(false);
            Destroy(objectToDestroy);
        }
    }

    //Destroy the smaller bounds around the map and isntantiate the larger bounds
    private void disableMapBounds()
    {
        //
    }

    //Load the GameObject from RCAssets with the corresponding object name and attach a MapObject script to it
    private GameObject createMapObject(objectType type, string objectName)
    {
        //The GameObject loaded from RCAssets corresponding to the object name
        GameObject newObject;

        //Instantiate the object using the object name. If the 
        if(type == objectType.@base)
        {
            //
            newObject = null;
        }
        else
            newObject = AssetManager.instantiateRcObject(objectName);

        //If the object name wasn't valid, raise an error
        if (!newObject)
            throw new Exception("The object '" + objectName + "' does not exist");

        //Attatch the MapObject script to the new object
        newObject.AddComponent<MapObject>();

        //Return the new object 
        return newObject;
    }

    //Return the objectType assosiated with the given string
    private objectType parseType(string typeString)
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

        //If the object type is not valid, raise an error
        throw new Exception("The type '" + typeString + "' does not exist");
    }

    //Create a Color object with the three given color values and opacity
    private Color parseColor(string r, string g, string b, string a)
    {
        return new Color(Convert.ToSingle(r), Convert.ToSingle(g), Convert.ToSingle(b), Convert.ToSingle(a));
    }

    //Create a vector with the two given strings
    private Vector2 parseVector2(string x, string y)
    {
        return new Vector2(Convert.ToSingle(x), Convert.ToSingle(y));
    }

    //Create a vector with the three given strings
    private Vector3 parseVector3(string x, string y, string z)
    {
        return new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
    }

    //Create a quaternion with the three given strings
    private Quaternion parseQuaternion(string x, string y, string z, string w)
    {
        return new Quaternion(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToSingle(w));
    }
    #endregion

    //Convert the map into a script
    public override string ToString()
    {
        return "";
    }
}
