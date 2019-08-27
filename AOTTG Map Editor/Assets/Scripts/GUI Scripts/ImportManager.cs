using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ImportManager : MonoBehaviour
{
    //A reference to the main object of the scene
    [SerializeField]
    private GameObject mainObject;
    //The MapManager script attached to the main object
    private MapManager mapManager;
    //A reference to the text box containing the map script
    [SerializeField]
    private InputField inputField;

    //Get the MapManager script from the gameobject
    void Start()
    {
        mapManager = mainObject.GetComponent<MapManager>();
    }

    public void togglePopup()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    //Import the map script in the text field
    public void importTextField()
    {
        mapManager.loadMap(inputField.text);
    }
}
