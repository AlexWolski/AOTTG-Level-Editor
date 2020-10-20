using UnityEngine;
using TMPro;

namespace MapEditor
{
    public class BottomBarInfoManager : MonoBehaviour
    {
        //Groups that show info for the current state
        [SerializeField] private GameObject flyEditMode;
        [SerializeField] private GameObject movementKeys;
        [SerializeField] private GameObject editorKeys;

        //The game object and text component displaying the current mode
        [SerializeField] private GameObject currentMode;
        private TextMeshProUGUI currentModeText;
        //The game object and text component displaying the object count
        [SerializeField] private GameObject objectCount;
        private TextMeshProUGUI objectCountText;

        private void Start()
        {
            //Find and store the needed component references
            currentModeText = currentMode.GetComponent<TextMeshProUGUI>();
            objectCountText = objectCount.GetComponent<TextMeshProUGUI>();

            //Listen for when the editor mode is changed
            EditorManager.Instance.OnChangeMode += OnModeChange;
        }

        private void Update()
        {
            //Update the object count
            objectCountText.text = "Objects: " + ObjectSelection.Instance.GetSelectionCount() + "/" + ObjectSelection.Instance.GetSelectableCount();
        }

        private void OnModeChange(EditorMode prevMode, EditorMode newMode)
        {
            //Hide the fly/edit mode info when in UI mode
            flyEditMode.SetActive(newMode != EditorMode.UI);
            //Show the movement keys when in fly mode
            movementKeys.SetActive(newMode == EditorMode.Fly);

            if (newMode == EditorMode.Edit)
            {
                //If a drag selection is in progress, show the drag select modifiers
                if (DragSelect.Instance.GetDragging())
                {

                }
                //If control is held and nothing is being dragged, show the commands list
                else if(Input.GetKey(KeyCode.LeftControl))
                {

                }
                //Otherwise show the general editor keys
                else
                {
                    editorKeys.SetActive(true);
                }
            }
            //If the editor is not in edit mode, hide the editor info
            else
            {
                editorKeys.SetActive(false);
            }

            //Set the mode text to display the current mode
            currentModeText.text = newMode.ToString();
        }
    }
}