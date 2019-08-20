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

//Used in the ToolHandle class for which axis to move the object along
enum Axis
{
    None = 0x0,
    X = 0x1,
    Y = 0x2,
    Z = 0x4
}

//Used in the GILES classes to describe different culling options
public enum Culling
{
    Back = 0x1,
    Front = 0x2,
    FrontBack = 0x4
}