//In fly mode, the camera can move but none of the tools can be used
//In edit mode, the camera is stationary but objects can be interacted with
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
enum objectType
{
    //The '@' indicates that 'base' is a literal, not a keyword
    @base,
    spawnpoint,
    photon,
    custom,
    racing,
    misc
}