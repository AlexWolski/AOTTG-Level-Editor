using UnityEngine;

namespace MapEditor
{
    //A singleton class for managing the current mode
    public class EditorManager : MonoBehaviour
    {
        #region Data Members
        //A hidden variable to hold the current mode
        private EditorMode currentModeValue;

        //A self-reference to the singleton instance of this script
        public static EditorManager Instance { get; private set; }
        public bool ShortcutsEnabled { get; set; }
        //Determines if the mouse is captured by a class or is available to use
        public bool CursorAvailable { get; private set; }

        //A property for accessing the current mode variable
        public EditorMode CurrentMode
        {
            get { return currentModeValue; }

            set
            {
                OnChangeMode?.Invoke(currentModeValue, value);
                currentModeValue = value;
            }
        }
        #endregion

        #region Delegates
        //Event to notify listeners when the mode changes
        public delegate void OnChangeModeEvent(EditorMode prevMode, EditorMode newMode);
        public event OnChangeModeEvent OnChangeMode;
        //Event to notify listeners when the cursor is captured or released
        public delegate void OnCursorCapturedEvent();
        public event OnCursorCapturedEvent OnCursorCaptured;
        public delegate void OnCursorReleasedEvent();
        public event OnCursorReleasedEvent OnCursorReleased;
        #endregion

        #region Initialization
        void Awake()
        {
            //Set this script as the only instance of the EditorManger script
            if (Instance == null)
                Instance = this;

            //Set the screen resolution
            Screen.fullScreen = false;
            Screen.SetResolution(800, 600, false);

            //The editor is in edit mode by default
            CurrentMode = EditorMode.Edit;
            ShortcutsEnabled = true;
            CursorAvailable = true;
        }
        #endregion

        #region Update
        private void Update()
        {
            //If the x key is pressed and nothing is being dragged, toggle between edit and fly mode
            if (Input.GetKeyDown(KeyCode.X) && !SelectionHandle.Instance.GetDragging() && !DragSelect.Instance.getDragging())
                toggleFlyEditMode();
        }

        private void toggleFlyEditMode()
        {
            if (CurrentMode == EditorMode.Fly)
            {
                CurrentMode = EditorMode.Edit;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (CurrentMode == EditorMode.Edit)
            {
                CurrentMode = EditorMode.Fly;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        #endregion

        #region Cursor Methods
        //Locks the cursor until the caller is finished using it
        //Returns true if the cursor was successfully captured, and false if it wasn't available
        public bool CaptureCursor()
        {
            if (CursorAvailable)
            {
                CursorAvailable = false;
                OnCursorCaptured?.Invoke();
                return true;
            }

            return false;
        }

        //Make the cursor available again to use
        public void releaseCursor()
        {
            CursorAvailable = true;
            OnCursorReleased?.Invoke();
        }
        #endregion
    }
}