using UnityEngine;

namespace MapEditor
{
    public class ImportExportManager : MonoBehaviour
    {
        //A rectangle marking the position where the text area should be
        [SerializeField]
        private GameObject textAreaPlaceholder;

        //The RectTransform script attatched to the text area placeholder
        private RectTransform rectTransform;
        //A vector to hold the world coordinates of each corner of the text area placeholder
        private Vector3[] corners = new Vector3[4];

        //The text field that the user will enter the map script in
        private string textArea = "";

        //Get the needed scripts
        void Awake()
        {
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

            //If the export popup is being shown, export the map script and set it as the text area content
            if (gameObject.name == "Export" && gameObject.activeSelf)
                textArea = MapManager.Instance.ToString();

            //If the popup was enabled, change the Editor to UI mode so that the map can not be edited
            if (gameObject.activeSelf)
                EditorManager.CurrentMode = EditorMode.UI;
            //If the popup was disabled, change the Editor back to edit mode
            else
                EditorManager.CurrentMode = EditorMode.Edit;
        }

        //Import the map text in the input field
        public void importTextField()
        {
            //Clear the existing map objects
            MapManager.clearMap();
            //Import the map script in the text field
            MapManager.loadMap(textArea);
            //Clear the textfield after the map is loaded
            textArea = "";
        }

        //Copy the content of the text field to the clipboard
        public void copyTextField()
        {
            TextEditor te = new TextEditor();
            te.text = textArea;
            te.SelectAll();
            te.Copy();
        }
    }
}