using UnityEngine;

namespace MapEditor
{
    //A singleton class for managing the current mode
    public class EditorManager : MonoBehaviour
    {
        //A self-reference to the singleton instance of this script
        public static EditorManager Instance { get; private set; }
        //Determines if the user is in fly more or edit mode
        private EditorMode currentModeValue;

        //A property for accessing the current mode variable
        public EditorMode currentMode
        {
            get
            {
                return currentModeValue;
            }

            set
            {
                OnChangeMode?.Invoke(currentModeValue, value);
                currentModeValue = value;
            }
        }

        //Event to notify listners when the mode changes
        public delegate void OnChangeModeEvent(EditorMode prevMode, EditorMode newMode);
        public event OnChangeModeEvent OnChangeMode;

        void Awake()
        {
            //Set this script as the only instance of the EditorManger script
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            //Set the screen resolution
            Screen.fullScreen = false;
            Screen.SetResolution(800, 600, false);

            //The editor is in edit mode by default
            currentMode = EditorMode.Edit;
        }

        private void Update()
        {
            //If the x key is pressed and nothing is being dragged, toggle between edit and fly mode
            if (Input.GetKeyDown(KeyCode.X) && !SelectionHandle.Instance.getDragging() && !DragSelect.Instance.getDragging())
                toggleFlyEditMode();
        }

        private void toggleFlyEditMode()
        {
            if (currentMode == EditorMode.Fly)
            {
                currentMode = EditorMode.Edit;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (currentMode == EditorMode.Edit)
            {
                currentMode = EditorMode.Fly;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}