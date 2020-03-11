using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GILES;

public class ToolButtonManager : MonoBehaviour
{
    //A list of the states the button can be in
    private enum buttonState
    {
        unselected,
        pressed,
        selected
    }

    //The images used for the button
    [SerializeField]
    private Sprite unselected;
    [SerializeField]
    private Sprite pressed;
    [SerializeField]
    private Sprite selected;

    //The current state of the button
    [SerializeField]
    private buttonState currentState;
    //The tool this button corresponds to
    [SerializeField]
    private Tool toolType;
    //The keyboard key that selects this button
    [SerializeField]
    private KeyCode shortCutKey;
    //Determine if the left mouse button is currently pressed or not
    private static bool mouseDown;
    //The button script that is currently pressed down
    private static ToolButtonManager pressedButton;
    //The button that is currently selected
    private static ToolButtonManager selectedButton;
    //The object that listens for event updates
    private static EventTrigger eventTrigger;

    //Initialize data members and set up the triggers
    void Awake()
    {
        //If this button is the one to be selected by default, select it
        if (currentState == buttonState.selected)
            select();
        //Otherwise the button should be unselected
        else
            unselect();

        mouseDown = false;
        eventTrigger = GetComponent<EventTrigger>();
        eventTrigger.AddEventTrigger(this.OnMouseEnter, EventTriggerType.PointerEnter);
        eventTrigger.AddEventTrigger(this.OnMouseExit, EventTriggerType.PointerExit);
        eventTrigger.AddEventTrigger(this.OnMouseDown, EventTriggerType.PointerDown);
        eventTrigger.AddEventTrigger(this.OnMouseUp, EventTriggerType.PointerUp);
    }

    //Check if the shortcut key was pressed
    private void Update()
    {
        if (CommonReferences.editorManager.currentMode == EditorMode.Edit && Input.GetKeyDown(shortCutKey))
        {
            selectedButton.unselect();
            select();
            action();
        }
    }

    //Change the image and state to selected
    private void select()
    {
        gameObject.GetComponent<Image>().sprite = selected;
        currentState = buttonState.selected;
        selectedButton = this;
    }

    //Change the image and state to unselected
    private void unselect()
    {
        gameObject.GetComponent<Image>().sprite = unselected;
        currentState = buttonState.unselected;
    }

    //If this button was last pressed and the mouse moves over it, change to the pressed image
    private void OnMouseEnter(BaseEventData data)
    {
        if (pressedButton == this && mouseDown && currentState == buttonState.unselected)
            OnMouseDown(data);
    }

    //If the button was pressed and the cursor moves off of the button, chagne to the unselected image
    private void OnMouseExit(BaseEventData data)
    {
        if (currentState == buttonState.pressed)
            unselect();
    }

    //If the mouse is pressed down on the button and its not selected, chagne to the pressed image
    private void OnMouseDown(BaseEventData data)
    {
        mouseDown = true;

        if (currentState == buttonState.unselected)
        {
            gameObject.GetComponent<Image>().sprite = pressed;
            currentState = buttonState.pressed;
            pressedButton = this;
        }
    }

    //If this button is clicked, select it and unselect all other buttons
    private void OnMouseUp(BaseEventData data)
    {
        mouseDown = false;

        if (currentState == buttonState.pressed)
        {
            selectedButton.unselect();
            select();
            action();
        }
    }

    //The action triggered by the button press
    private void action()
    {
        ObjectSelection.setTool(toolType);
    }
}
