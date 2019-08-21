//In fly mode, the camera can move but none of the tools can be used
//In edit mode, the camera is stationary but objects can be interacted with
using UnityEngine;

public enum EditorMode
{
    Fly,
    Edit
}

//While in edit mode, there are three basic tools that can be used
public enum Tool
{
    None,
    Position,
    Rotate,
    Scale
}

//A list of types an object can be
public enum objectType
{
    //The '@' indicates that 'base' is a literal, not a keyword
    @base,
    spawnpoint,
    photon,
    custom,
    racing,
    misc
}

//A struct that holds all of the information for a map object
public struct ObjectData
{
    //The type of the object
    public objectType type;
    //The actual type name specified in the map script (can be longer than the type)
    public string fullTypeName;
    //The specific object
    public string objectName;
    //The name of the region if the object is a region
    public string regionName;
    //The scale factor of the object from its default size
    public Vector3 scale;
    //The name of the texture applied to the object
    public string texture;
    //How many times the texture will repeat in the x and y directions
    public Vector2 tiling;
    //Determines if colored materials are enabled
    public bool colorEnabled;
    //Determines if the transparant material is enabled
    public bool transparantEnabled;
    //The color of the object, including opacity
    public Color color;
}