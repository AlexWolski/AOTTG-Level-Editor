using UnityEngine;

public class LevelObject : MonoBehaviour
{
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

    //
    public void setData()
    {

    }
}
