using UnityEngine;
using System.Collections;

public class MapObject : MonoBehaviour
{
    //A list of types an object can be
    private enum objectType
    {
        vanilla,
        spawnpoint,
        photon,
        custom,
        racing,
        misc
    }

    //The type of the object
    private objectType type;
    //The actual type name specified in the map script (can be longer than the type)
    private string fullTypeName;
    //The specific object
    private string objectName;
    //The name of the region if the object is a region
    private string regionName;
    //The name of the material applied to the object
    private string texture;
    //Determines if colored materials are enabled
    private bool color;
    //Determines if the transparant material is enabled
    private bool transparant;
    //The opacity of the transparant material
    private float opacity;

    public MapObject(string objectScript)
    {
        parseScript(objectScript);
    }

    private void parseScript(string objectScript)
    {

    }
}
