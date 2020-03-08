using UnityEngine;
using System;

public class MapObject : MonoBehaviour
{
    #region Data Members
    //The type of the object
    private objectType type;
    //The actual type name specified in the map script
    private string fullType;
    //The specific object
    private string objectName;
    //The name of the region if the object is a region
    private string regionName;
    //The amount of time until the titan spawns
    private float spawnTimer;
    //Determines if the the spawner will continue to spawn titans
    private bool endlessSpawn;
    //The name of the texture applied to the object
    private string texture;
    //The scale factor of the object from its default size
    private Vector3 scale;
    //How many times the texture will repeat in the x and y directions
    private Vector2 tiling;
    //Determines if colored materials are enabled
    private bool colorEnabled;
    //The color of the object, including opacity
    private Color color;
    #endregion

    #region Properties
    //Properties for getting and setting the data members
    public objectType Type { get; set; }
    public string FullTypeName { get; set; }
    public string ObjectName { get; set; }
    public string RegionName { get; set; }
    public float SpawnTimer { get; set; }
    public bool EndlessSpawn { get; set; }

    public string Texture
    {
        get { return texture; }
        set { texture = value; setTexture(value); }
    }

    public Vector3 Scale
    {
        get { return scale; }
        set { scale = value; setScale(value); }
    }

    public Vector2 Tiling
    {
        get { return tiling; }
        set { tiling = value; setTiling(value); }
    }

    public bool ColorEnabled { get; set; }

    public Color Color
    {
        get { return color; }
        set { color = value; setColor(color); }
    }

    public Vector3 Position
    {
        get { return gameObject.transform.position; }
        set { setPosition(value); }
    }

    public Quaternion Rotation
    {
        get { return gameObject.transform.rotation; }
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
        //Transform the object. Divide by 10 to adjust the scale for the editor
        Vector3 currentScale = gameObject.transform.localScale;
        gameObject.transform.localScale = new Vector3(currentScale.x * newScale.x, currentScale.y * newScale.y, currentScale.z * newScale.z)/10;
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
        //Divide the position by 10 to adjust the scale for the editor
        newPosition /= 10;
        gameObject.transform.position = newPosition;
    }

    //Set the rotation of the object
    private void setRotation(Quaternion newAngle)
    {
        gameObject.transform.rotation = newAngle;
    }
    #endregion

    #region Utility Methods
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

    #region Methods
    //Takes an array containing a parsed object script and set all of the variables except for the type
    public void loadProperties(string[] properties)
    {
        //The position in the properties array where the position data starts.
        //Defaults to 3 for objects that only have a type, name, posiiton, and angle
        int indexOfPosition = 2;

        //Store the full type
        this.FullTypeName = properties[0];
        //Store the object name
        this.ObjectName = properties[1];

        //If the object is a titan spawner, store the spawn timer and whether or not it spawns endlessly
        if (this.Type == objectType.photon && this.ObjectName.StartsWith("spawn"))
        {
            this.SpawnTimer = Convert.ToSingle(properties[2]);
            this.EndlessSpawn = (Convert.ToInt32(properties[3]) != 0);
            indexOfPosition = 4;
        }
        //If the object is a region, store the region name and scale
        else if (this.ObjectName.StartsWith("region"))
        {
            this.RegionName = properties[2];
            this.Scale = parseVector3(properties[3], properties[4], properties[5]);
            indexOfPosition = 6;
        }
        //If the object has a texture, store the texture, scale, color, and tiling information
        else if (this.Type == objectType.custom || properties.Length >= 15 && (this.Type == objectType.@base || this.Type == objectType.photon))
        {
            this.Texture = properties[2];
            this.Scale = parseVector3(properties[3], properties[4], properties[5]);
            this.ColorEnabled = (Convert.ToInt32(properties[6]) != 0);

            if (this.ColorEnabled)
            {
                //If the transparent texture is applied, parse the opacity and use it. Otherwise default to fully opaque
                if (this.Texture.StartsWith("transparent"))
                    this.Color = parseColor(properties[7], properties[8], properties[9], this.Texture.Substring(11));
                else
                    this.Color = parseColor(properties[7], properties[8], properties[9], "1");
            }

            this.Tiling = parseVector2(properties[10], properties[11]);
            indexOfPosition = 12;
        }
        //If the object has scale information just before the position and rotation, store the scale
        else if (this.Type == objectType.racing || this.Type == objectType.misc)
        {
            this.Scale = parseVector3(properties[2], properties[3], properties[4]);
            indexOfPosition = 5;
        }

        //If the object is a spawnpoint, set its default size
        if (this.Type == objectType.spawnpoint || this.Type == objectType.photon)
            this.Scale = new Vector3(1f, 1f, 1f);

        //Set the position and rotation for all objects
        this.Position = parseVector3(properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++]);
        this.Rotation = parseQuaternion(properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++], properties[indexOfPosition++]);
    }
    #endregion
}