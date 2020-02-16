using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ImportExportManager : MonoBehaviour
{
    //A reference to the main object of the scene
    [SerializeField]
    private GameObject mainObject;
    //A rectangle marking the position where the text area should be
    [SerializeField]
    private GameObject textAreaPlaceholder;

    //The MapManager script attached to the main object
    private MapManager mapManager;
    //The RectTransform script attatched to the text area placeholder
    private RectTransform rectTransform;
    //A vector to hold the world coordinates of each corner of the text area placeholder
    private Vector3[] corners = new Vector3[4];

    //The text field that the user will enter the map script in
    private string textArea = "";

    //Get the needed scripts
    void Start()
    {
        mapManager = mainObject.GetComponent<MapManager>();
        rectTransform = textAreaPlaceholder.GetComponent<RectTransform>();
    }

    //Update the text area with the user's input
    void OnGUI()
    {
        //Resize the text area and refresh the contents
        textArea = GUI.TextArea(getTextAreaRect(), textArea);
    }

    //Create a rect based on the world corners of the text area placeholder
    private Rect getTextAreaRect()
    {
        //Get the world position of each corner of the text area placeholder
        rectTransform.GetWorldCorners(corners);

        //The order of the corners starts at the top left and moves counter clockwise
        float width = corners[3].x - corners[0].x;
        float height = corners[1].y - corners[0].y;

        //Create a new rectangle object with those positions and return it
        return new Rect(corners[0].x, corners[0].y, width, height);
    }

    //Hide or show the import popup screen
    public void togglePopup()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void importTextField()
    {
        //Clear the existing map objects
        mapManager.clearMap();
        //Import the map script in the text field
        mapManager.loadMap(textArea);
        //Clear the textfield after the map is loaded
        textArea = "";
    }
}
