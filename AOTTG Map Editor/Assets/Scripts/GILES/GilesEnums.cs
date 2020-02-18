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