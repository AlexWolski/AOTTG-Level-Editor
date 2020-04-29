using System;
using UnityEngine;

namespace MapEditor
{
    //A singleton class for managing the current mode
    public class EditorManager : MonoBehaviour
    {
        //A self-reference to the singleton instance of this script
        public static EditorManager Instance { get; private set; }
        //A hidden variable to hold the current mode
        private EditorMode currentModeValue;
        public bool shortcutsEnabled { get; set; }
        //Determines if the mouse is captured by a class or is available to use
        public bool cursorAvailable { get; private set; }

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
        //Event to notify listners when the cursor is captured or released
        public delegate void OnCursorCapturedEvent();
        public event OnCursorCapturedEvent OnCursorCaptured;
        public delegate void OnCursorReleasedEvent();
        public event OnCursorReleasedEvent OnCursorReleased;

        void Awake()
        {
            //Set this script as the only instance of the EditorManger script
            if (Instance == null)
                Instance = this;

            //Set the screen resolution
            Screen.fullScreen = false;
            Screen.SetResolution(800, 600, false);

            //The editor is in edit mode by default
            currentMode = EditorMode.Edit;
            shortcutsEnabled = true;
            cursorAvailable = true;
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

        //Locks the cursor until the caller is finished using it
        //Returns true if the cursor was succesfully captured, and false if it wasn't available
        public bool captureCursor()
        {
            if (cursorAvailable)
            {
                cursorAvailable = false;
                OnCursorCaptured?.Invoke();
                return true;
            }

            return false;
        }

        //Make the cursor available again to use
        public void releaseCursor()
        {
            cursorAvailable = true;
            OnCursorReleased?.Invoke();
        }
    }
}