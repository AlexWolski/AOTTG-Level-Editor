using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonImageManager : MonoBehaviour
{
    //A list of the states the button can be in
    enum buttonState
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
    private buttonState currentState;
    //Determine if the left mouse button is currently pressed or not
    private static bool mouseDown;
    //The button script that is currently pressed down
    private static ButtonImageManager pressedButton;
    //The object that listens for event updates
    private EventTrigger eventTrigger;

    //Initialize data members and set up triggers
    void Start()
    {
        mouseDown = false;
        currentState = buttonState.unselected;
        eventTrigger = GetComponent<EventTrigger>();
        eventTrigger.AddEventTrigger(this.OnMouseEnter, EventTriggerType.PointerEnter);
        eventTrigger.AddEventTrigger(this.OnMouseExit, EventTriggerType.PointerExit);
        eventTrigger.AddEventTrigger(this.OnMouseDown, EventTriggerType.PointerDown);
        eventTrigger.AddEventTrigger(this.OnMouseUp, EventTriggerType.PointerUp);
    }

    //If this button was last pressed and the mouse moves over it, change to the pressed image
    void OnMouseEnter(BaseEventData data)
    {
        if (pressedButton == this && mouseDown && currentState == buttonState.unselected)
            OnMouseDown(data);
    }

    //If the button was pressed and the cursor moves off of the button, chagne to the unselected image
    void OnMouseExit(BaseEventData data)
    {
        if (currentState == buttonState.pressed)
        {
            gameObject.GetComponent<Image>().sprite = unselected;
            currentState = buttonState.unselected;
        }
    }

    //If the mouse is pressed down on the button and its not selected, chagne to the pressed image
    void OnMouseDown(BaseEventData data)
    {
        mouseDown = true;

        if (currentState == buttonState.unselected)
        {
            gameObject.GetComponent<Image>().sprite = pressed;
            currentState = buttonState.pressed;
            pressedButton = this;
        }
    }

    //If the mouse is released on the button, toggle its state
    void OnMouseUp(BaseEventData data)
    {
        mouseDown = false;

        if (currentState == buttonState.pressed)
        {
            gameObject.GetComponent<Image>().sprite = selected;
            currentState = buttonState.selected;
        }
        else
        {
            gameObject.GetComponent<Image>().sprite = unselected;
            currentState = buttonState.unselected;
        }
    }
}
