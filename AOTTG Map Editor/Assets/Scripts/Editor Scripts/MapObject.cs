using UnityEngine;
using System;

public class MapObject : MonoBehaviour
{
    #region Data Members
    //The underlying values for properties with complex setters
    //These extra fields are required due to a Unity bug
    private string textureValue;
    private Vector3 scaleValue;
    private Vector2 tilingValue;
    private Color colorValue;

    //The number of properties the object has
    int propertyNumber = 0;
    #endregion

    #region Properties
    //The type of the object
    public objectType Type { get; set; }
    //The actual type name specified in the map script
    public string FullTypeName { get; set; }
    //The specific object
    public string ObjectName { get; set; }
    //The name of the region if the object is a region
    public string RegionName { get; set; }
    //The amount of time until the titan spawns
    public float SpawnTimer { get; set; }
    //Determines if the the spawner will continue to spawn titans
    public bool EndlessSpawn { get; set; }
    //Determines if colored materials are enabled
    public bool ColorEnabled { get; set; }

    //The name of the texture applied to the object
    public string Texture
    {
        get { return textureValue; }
        set { textureValue = value; setTexture(value); }
    }

    //How many times the texture will repeat in the x and y directions
    public Vector2 Tiling
    {
        get { return tilingValue; }
        set { tilingValue = value; setTiling(value); }
    }

    //The color of the object, including opacity
    public Color Color
    {
        get { return colorValue; }
        set { colorValue = value; setColor(colorValue); }
    }

    //Shorthand ways of accessing variables in the transform component
    public Vector3 Scale
    {
        get { return scaleValue; }
        set { scaleValue = value; setScale(value); }
    }

    public Vector3 Position
    {
        get { return transform.position; }
        set { setPosition(value); }
    }

    public Quaternion Rotation
    {
        get { return transform.rotation; }
        set { setRotation(value); }
    }
    #endregion

    #region Setters
    //Setters for implementing more complicated varaibles

    //Apply the given texture as the new material of the object
    private void setTexture(string newTexture)
    {
        //Apply the material to all of the children of the object
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
        {
            //Don't apply the default texture and don't apply the material to the particle system of supply stations
            if (!(newTexture == "default" || renderer.name.Contains("Particle System") && ObjectName.StartsWith("aot_supply")))
                renderer.material = AssetManager.loadRcMaterial(newTexture);
        }
    }

    //Change the scale factor of the length, width, or height of the object
    private void setScale(Vector3 newScale)
    {
        Vector3 currentScale = gameObject.transform.localScale;
        gameObject.transform.localScale = new Vector3(currentScale.x * newScale.x, currentScale.y * newScale.y, currentScale.z * newScale.z);
    }

    //Resize the texture on the object
    private void setTiling(Vector2 newTiling)
    {
        //Apply the texture resizing to all of the children of the object
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
            renderer.material.mainTextureScale = new Vector2(renderer.material.mainTextureScale.x * newTiling.x, renderer.material.mainTextureScale.y * newTiling.y);
    }

    //Change the color of the texture on the object
    private void setColor(Color newColor)
    {
        //Iterate through all of the filters in the object
        foreach (MeshFilter filter in gameObject.GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = filter.mesh;

            //Create an array filled with the new color to apply to the mesh
            Color[] colorArray = new Color[mesh.vertexCount];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = (Color)newColor;

            //Apply the colors
            mesh.colors = colorArray;
        }
    }

    //Position the object
    private void setPosition(Vector3 newPosition)
    {
        gameObject.transform.position = newPosition;
    }

    //Set the rotation of the object
    private void setRotation(Quaternion newAngle)
    {
        gameObject.transform.rotation = newAngle;
    }
    #endregion

    #region Parsing Utility Methods
    //A set of methods for parsing parts of an object script

    //Return the objectType assosiated with the given string
    public static objectType parseType(string typeString)
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
    public static Color parseColor(string r, string g, string b, string a)
    {
        return new Color(Convert.ToSingle(r), Convert.ToSingle(g), Convert.ToSingle(b), Convert.ToSingle(a));
    }

    //Create a vector with the two given strings
    public static Vector2 parseVector2(string x, string y)
    {
        return new Vector2(Convert.ToSingle(x), Convert.ToSingle(y));
    }

    //Create a vector with the three given strings
    public static Vector3 parseVector3(string x, string y, string z)
    {
        return new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
    }

    //Create a quaternion with the three given strings
    public static Quaternion parseQuaternion(string x, string y, string z, string w)
    {
        return new Quaternion(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToSingle(w));
    }
    #endregion

    #region Exporting Utility Methods
    //Convert a boolean to the string 1 or 0
    private string boolToString(bool boolToStringify)
    {
        if (boolToStringify)
            return "1";

        return "0";
    }

    //Convert a color to a script friendly string
    private string colorToString(Color colorToStrinfigy)
    {
        return colorToStrinfigy.r.ToString() + "," +
               colorToStrinfigy.g.ToString() + "," +
               colorToStrinfigy.b.ToString();
    }

    //Convert a vector2 to a script friendly string
    private string vector2ToString(Vector2 vectorToStringify)
    {
        return vectorToStringify.x.ToString() + ","+
               vectorToStringify.y.ToString();
    }
    //Convert a vector2 to a script friendly string
    private string vector3ToString(Vector3 vectorToStringify)
    {
        return vectorToStringify.x.ToString() + "," +
               vectorToStringify.y.ToString() + "," +
               vectorToStringify.z.ToString();
    }

    //Convert a vector2 to a script friendly string
    private string quaternionToString(Quaternion quatToStringify)
    {
        return quatToStringify.x.ToString() + "," +
               quatToStringify.y.ToString() + "," +
               quatToStringify.z.ToString() + "," +
               quatToStringify.w.ToString();
    }
    #endregion

    #region Methods
    //Takes an array containing a parsed object script and set all of the variables except for the type
    public void loadProperties(string[] properties)
    {
        //Save the number of properties the object hsa
        propertyNumber = properties.Length;

        //The position in the properties array where the position data starts.
        //Defaults to 3 for objects that only have a type, name, posiiton, and angle
        int indexOfPosition = 2;

        //Store the full type
        FullTypeName = properties[0];
        //Store the object name
        ObjectName = properties[1];

        //If the object is a titan spawner, store the spawn timer and whether or not it spawns endlessly
        if (Type == objectType.photon && ObjectName.StartsWith("spawn"))
        {
            SpawnTimer = Convert.ToSingle(properties[2]);
            EndlessSpawn = (Convert.ToInt32(properties[3]) != 0);
            indexOfPosition = 4;
        }
        //If the object is a region, store the region name and scale
        else if (ObjectName.StartsWith("region"))
        {
            RegionName = properties[2];
            Scale = parseVector3(properties[3], properties[4], properties[5]);
            indexOfPosition = 6;
        }
        //If the object has a texture, store the texture, scale, color, and tiling information
        else if (Type == objectType.custom || propertyNumber >= 15 && (Type == objectType.@base || Type == objectType.photon))
        {
            Texture = properties[2];
            Scale = parseVector3(properties[3], properties[4], properties[5]);
            ColorEnabled = (Convert.ToInt32(properties[6]) != 0);

            //If the color is enabled, parse the color and set it
            if (ColorEnabled)
            {
                //If the transparent texture is applied, parse the opacity and use it. Otherwise default to fully opaque
                if (Texture.StartsWith("transparent"))
                    Color = parseColor(properties[7], properties[8], properties[9], Texture.Substring(11));
                else
                    Color = parseColor(properties[7], properties[8], properties[9], "1");
            }
            //Otherwise, use white as a default color
            else
                Color = Color.white;

            Tiling = parseVector2(properties[10], properties[11]);
            indexOfPosition = 12;
        }
        //If the object has scale information just before the position and rotation, store the scale
        else if (Type == objectType.racing || Type == objectType.misc)
        {
            Scale = parseVector3(properties[2], properties[3], properties[4]);
            indexOfPosition = 5;
        }

        //If the object is a spawnpoint, set its default size
        if (Type == objectType.spawnpoint || Type == objectType.photon)
            Scale = new Vector3(1f, 1f, 1f);

        //Set the position and rotation for all objects
        Position = parseVector3(properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++]);
        Rotation = parseQuaternion(properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++]);
    }

    //Convert the map object into a script
    public override string ToString()
    {
        //The exported object script. Every script starts with the type and name
        string objectScript = FullTypeName + "," + ObjectName;

        //Add properties to the script based on what type of object it is
        if (Type == objectType.photon && ObjectName.StartsWith("spawn"))
            objectScript += "," + SpawnTimer + "," + boolToString(EndlessSpawn);
        else if (ObjectName.StartsWith("region"))
            objectScript += "," + RegionName + "," + vector3ToString(Scale);
        else if (Type == objectType.custom || propertyNumber >= 15 && (Type == objectType.@base || Type == objectType.photon))
            objectScript += "," + Texture + "," + vector3ToString(Scale) + "," + boolToString(ColorEnabled) + "," + colorToString(Color) + "," + vector2ToString(Tiling);
        else if (Type == objectType.racing || Type == objectType.misc)
            objectScript += "," + vector3ToString(Scale);

        //Add the position and rotation to all objects. Scale the position up by a factor of 10
        objectScript += "," + vector3ToString(Position) + "," + quaternionToString(Rotation) + ";";

        return objectScript;
    }
    #endregion
}