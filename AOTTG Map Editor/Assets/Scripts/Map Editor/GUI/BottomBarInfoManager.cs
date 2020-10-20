using UnityEngine;
using UnityEngine.UI;

namespace MapEditor
{
    public class BottomBarInfoManager : MonoBehaviour
    {
        //The info displayed when in fly or edit mode
        [SerializeField] private GameObject flyEditMode;

        //Groups that show info for the current state
        [SerializeField] private GameObject movementKeys;

        //The game object and text component displaying the current mode
        [SerializeField] private GameObject currentMode;
        private Text currentModeText;
        //The game object and text component displaying the object count
        [SerializeField] private GameObject objectCount;
        private Text objectCountText;

        private void Start()
        {
            //Find and store the needed component references
            currentModeText = currentMode.GetComponent<Text>();
            objectCountText = objectCount.GetComponent<Text>();

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
            //If the new mode is UI mode, hide the current mode info.
            if (newMode == EditorMode.UI)
                flyEditMode.SetActive(false);
            //If new mode is Fly or Edit, show the current mode info again.
            else if (prevMode == EditorMode.UI)
                flyEditMode.SetActive(true);

            //Toggle the movement keys based on if the current mode is fly mode
            movementKeys.SetActive(newMode == EditorMode.Fly);

            //Set the mode text to display the current mode
            currentModeText.text = newMode.ToString();
        }
    }
}