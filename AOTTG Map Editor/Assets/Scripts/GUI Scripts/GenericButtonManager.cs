using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class GenericButtonManager : MonoBehaviour
{
    //A list of the states the button can be in
    private enum buttonState
    {
        unpressed,
        pressed
    }

    //The images used for the button
    [SerializeField]
    private Sprite unpressed;
    [SerializeField]
    private Sprite pressed;
    //The function to call when the button is clicked
    [SerializeField]
    private UnityEvent onClick;

    //The current state of the button
    private buttonState currentState;
    //Determine if the left mouse button is currently pressed or not
    private static bool mouseDown;
    //The button script that is currently pressed down
    private static GenericButtonManager pressedButton;
    //The object that listens for event updates
    private static EventTrigger eventTrigger;

    //Initialize data members and set up the triggers
    void Awake()
    {
        //The button is unselected by default
        currentState = buttonState.unpressed;

        mouseDown = false;
        eventTrigger = GetComponent<EventTrigger>();
        eventTrigger.AddEventTrigger(this.OnMouseEnter, EventTriggerType.PointerEnter);
        eventTrigger.AddEventTrigger(this.OnMouseExit, EventTriggerType.PointerExit);
        eventTrigger.AddEventTrigger(this.OnMouseDown, EventTriggerType.PointerDown);
        eventTrigger.AddEventTrigger(this.OnMouseUp, EventTriggerType.PointerUp);
    }

    //Change the image and state to pressed
    private void press()
    {
        gameObject.GetComponent<Image>().sprite = pressed;
        currentState = buttonState.pressed;
    }

    //Change the image and state to unpressed
    private void unpress()
    {
        gameObject.GetComponent<Image>().sprite = unpressed;
        currentState = buttonState.unpressed;
    }

    //If this button was last pressed and the mouse moves over it, change to the pressed image
    private void OnMouseEnter(BaseEventData data)
    {
        if (pressedButton == this && mouseDown && currentState == buttonState.unpressed)
            OnMouseDown(data);
    }

    //If the button was pressed and the cursor moves off of the button, chagne to the unpressed image
    private void OnMouseExit(BaseEventData data)
    {
        if (currentState == buttonState.pressed)
            unpress();
    }

    //If the mouse is pressed down on the button and its not selected, chagne to the pressed image
    private void OnMouseDown(BaseEventData data)
    {
        mouseDown = true;

        if (currentState == buttonState.unpressed)
        {
            gameObject.GetComponent<Image>().sprite = pressed;
            currentState = buttonState.pressed;
            pressedButton = this;
        }
    }

    //If this button is clicked, unpress the button and invoke the 'on click' function
    private void OnMouseUp(BaseEventData data)
    {
        mouseDown = false;

        if (currentState == buttonState.pressed)
        {
            unpress();
            onClick.Invoke();
        }
    }
}
