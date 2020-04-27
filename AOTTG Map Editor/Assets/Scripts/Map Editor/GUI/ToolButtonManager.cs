using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MapEditor
{
    public class ToolButtonManager : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region Data Members
        //A list of the states the button can be in
        private enum buttonState
        {
            unselected,
            pressed,
            selected
        }

        //The images used for the button
        [SerializeField] private Sprite unselected;
        [SerializeField] private Sprite pressed;
        [SerializeField] private Sprite selected;

        //The image component attached to this game object
        private Image imageScript;

        //The current state of the button
        [SerializeField] private buttonState currentState;
        //The tool this button corresponds to
        [SerializeField] private Tool toolType;
        //The keyboard key that selects this button
        [SerializeField] private KeyCode shortCutKey;

        //Determines if the left mouse button is currently pressed or not
        private static bool mouseDown;
        //The button script that is currently pressed down
        private static ToolButtonManager pressedButton;
        //The button that is currently selected
        private static ToolButtonManager selectedButton;

        //A dictionary mapping the different tools to their respective button scripts
        private static Dictionary<Tool, ToolButtonManager> toolTable = new Dictionary<Tool, ToolButtonManager>();
        #endregion

        #region Initialization
        //Initialize data members and set up the triggers
        void Start()
        {
            //Save the image component
            imageScript = gameObject.GetComponent<Image>();

            //Add this button management script to the static table
            toolTable.Add(toolType, this);

            //If this button is the one to be selected by default, select it
            if (currentState == buttonState.selected)
                select();
            //Otherwise the button should be unselected
            else
                unselect();

            mouseDown = false;
        }
        #endregion

        #region Update
        //Check if the shortcut key was pressed
        private void Update()
        {
            if (EditorManager.Instance.currentMode == EditorMode.Edit &&
                EditorManager.Instance.shortcutsEnabled  &&
                Input.GetKeyDown(shortCutKey))
            {
                selectedButton.unselect();
                select();
            }
        }
        #endregion

        #region Events
        //If the game loses focus and a button is pressed down, unpress it
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && currentState == buttonState.pressed)
                unselect();
        }
        #endregion

        #region Button Methods
        //Change the image and state to selected
        private void select()
        {
            //Set the button images
            imageScript.sprite = selected;
            currentState = buttonState.selected;
            selectedButton = this;
            //Execute the action of the button
            action();
        }

        //Change the image and state to unselected
        private void unselect()
        {
            imageScript.sprite = unselected;
            currentState = buttonState.unselected;
        }

        //The action triggered by the button press
        private void action()
        {
            ObjectSelection.Instance.setTool(toolType);
        }

        //Set the current tool and select the appropriate button from an external script
        public static void setTool(Tool newTool)
        {
            //Unselect the currently active button and select the button for the new tool
            selectedButton.unselect();
            toolTable[newTool].select();
        }
        #endregion

        #region Pointer Methods
        //If this button was last pressed and the mouse moves over it, change to the pressed image
        public void OnPointerEnter(PointerEventData data)
        {
            if (pressedButton == this && mouseDown && currentState == buttonState.unselected)
                OnPointerDown(data);
        }

        //If the button was pressed and the cursor moves off of the button, chagne to the unselected image
        public void OnPointerExit(PointerEventData data)
        {
            if (currentState == buttonState.pressed)
                unselect();
        }

        //If the mouse is pressed down on the button and its not selected, chagne to the pressed image
        public void OnPointerDown(PointerEventData data)
        {
            mouseDown = true;

            if (currentState == buttonState.unselected)
            {
                imageScript.sprite = pressed;
                currentState = buttonState.pressed;
                pressedButton = this;
            }
        }

        //If this button is clicked, select it and unselect all other buttons
        public void OnPointerUp(PointerEventData data)
        {
            mouseDown = false;

            if (currentState == buttonState.pressed)
            {
                selectedButton.unselect();
                select();
            }
        }
        #endregion
    }
}