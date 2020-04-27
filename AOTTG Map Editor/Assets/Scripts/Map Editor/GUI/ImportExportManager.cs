using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MapEditor
{
    public class ImportExportManager : MonoBehaviour
    {
        //A refrence to the canvas object
        [SerializeField] private GameObject canvas;
        //The canvas group component attached to the canvas
        private CanvasGroup canvasGroup;

        //The Import popup
        [SerializeField] private GameObject importPopup;
        //Displays the imported text
        [SerializeField] private GameObject importTextArea;
        //The text area component attached to the import text area object
        private TextArea importTextAreaComponent;

        //Displays the importing message
        [SerializeField] private GameObject importingMessage;
        //Displays how many map objects have been imported
        [SerializeField] private GameObject progressText;
        //The text component attached to the progress text object
        private Text progressTextComponent;
        //Displays how many map objects are in the script
        [SerializeField] private GameObject totalText;
        //The text component attached to the total text object
        private Text totalTextComponent;

        //The export popup
        [SerializeField] private GameObject exportPopup;
        //Displays the exported text
        [SerializeField] private GameObject exportTextArea;
        //The text area component attached to the export text area object
        private TextArea exportTextAreaComponent;

        private void Start()
        {
            //Find and store the needed component references
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            importTextAreaComponent = importTextArea.GetComponent<TextArea>();
            exportTextAreaComponent = exportTextArea.GetComponent<TextArea>();
            progressTextComponent = progressText.GetComponent<Text>();
            totalTextComponent = totalText.GetComponent<Text>();
        }

        //Hide or show the import popup screen
        public void toggleImportPopup()
        {
            importPopup.SetActive(!importPopup.activeSelf);

            //When the popup is enabled, make sure the text area is not in focus
            if (importPopup.activeSelf)
                importTextAreaComponent.setFocused(false);

                toggleMode();
        }

        //Hide or show the export popup screen
        public void toggleExportPopup()
        {
            exportPopup.SetActive(!exportPopup.activeSelf);

            //If the export popup was enabled, export the map script and set it as the text area content
            if (exportPopup.activeSelf)
            {
                exportTextAreaComponent.setFocused(false);
                exportTextAreaComponent.text = MapManager.Instance.ToString();
            }

            toggleMode();
        }

        //Set the editor mode based on if the popups are enabled or not
        private void toggleMode()
        {
            if(importPopup.activeSelf || exportPopup.activeSelf)
                EditorManager.Instance.currentMode = EditorMode.UI;
            else
                EditorManager.Instance.currentMode = EditorMode.Edit;
        }

        //Import the map text in the input field
        public void importFromTextField()
        {
            //Disable the UI, shortcuts, and drag select
            canvasGroup.blocksRaycasts = false;
            EditorManager.Instance.shortcutsEnabled = false;
            DragSelect.Instance.enabled = false;
            //Show the importing text
            importingMessage.SetActive(true);

            //Clear the old map before loading the new one
            MapManager.Instance.clearMap();
            //Import the map script in the text field
            StartCoroutine(MapManager.Instance.loadMap(importTextAreaComponent.text, progressTextComponent, totalTextComponent, endImport));
            
            //Clear the import text area
            importTextAreaComponent.clearText();
            //Hide the import popup
            toggleImportPopup();
        }

        //Prepares the editor for use after importing
        private void endImport()
        {
            //Enable the UI, shortcuts, and drag select
            canvasGroup.blocksRaycasts = true;
            EditorManager.Instance.shortcutsEnabled = true;
            DragSelect.Instance.enabled = true;
            //Hide the importing text
            importingMessage.SetActive(false);
        }
    }
}