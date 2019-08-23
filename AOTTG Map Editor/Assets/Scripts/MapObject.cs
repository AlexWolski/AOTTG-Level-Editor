using UnityEngine;

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

    #region Getters And Setters
    //Getter and setter definitions for the private variables
    public objectType Type
    {
        get { return type; }
        set { type = value; }
    }

    public string FullTypeName
    {
        get { return fullType; }
        set { fullType = value; }
    }

    public string ObjectName
    {
        get { return objectName; }
        set { objectName = value; }
    }

    public string RegionName
    {
        get { return regionName; }
        set { regionName = value; }
    }

    public float SpawnTimer
    {
        get { return spawnTimer; }
        set { spawnTimer = value; }
    }

    public bool EndlessSpawn
    {
        get { return endlessSpawn; }
        set { endlessSpawn = value; }
    }

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

    public bool ColorEnabled
    {
        get { return colorEnabled; }
        set { colorEnabled = value; }
    }

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

    #region Implementations
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
        //Transform the object
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

    //Convert the object into a script
    public override string ToString()
    {
        return "";
    }
    #endregion
}