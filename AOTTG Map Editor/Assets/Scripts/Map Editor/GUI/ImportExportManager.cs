using UnityEngine;

namespace MapEditor
{
    public class ImportExportManager : MonoBehaviour
    {
        //The text area object
        [SerializeField] private GameObject textAreaObject;
        //A reference to the custom Text Area script on the text area object
        private TextArea textArea;

        //Determines if the Start function has been called or not
        private bool initialized = false;

        private void Start()
        {
            //Find and store the needed component references
            textArea = textAreaObject.GetComponent<TextArea>();
            initialized = true;
        }

        //Hide or show the import popup screen
        public void togglePopup()
        {
            //If this funciton was called before initialization, call the start function
            if (!initialized)
                Start();

            gameObject.SetActive(!gameObject.activeSelf);

            //If the export popup is being shown, export the map script and set it as the text area content
            if (gameObject.name == "Export" && gameObject.activeSelf)
                textArea.text = MapManager.Instance.ToString();

            //If the popup was enabled, change the Editor to UI mode so that the map can not be edited
            if (gameObject.activeSelf)
                EditorManager.Instance.currentMode = EditorMode.UI;
            //If the popup was disabled, change the Editor back to edit mode
            else
                EditorManager.Instance.currentMode = EditorMode.Edit;
        }

        //Import the map text in the input field
        public void importTextField()
        {
            //Clear the existing map objects
            MapManager.Instance.clearMap();
            //Import the map script in the text field
            MapManager.Instance.loadMap(textArea.text);
        }
    }
}