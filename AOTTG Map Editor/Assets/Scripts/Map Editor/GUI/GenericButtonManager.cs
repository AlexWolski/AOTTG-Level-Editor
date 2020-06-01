using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MapEditor
{
    public class GenericButtonManager : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        //A list of the states the button can be in
        private enum ButtonState
        {
            Unpressed,
            Pressed
        }

        //Determine if the left mouse button is currently pressed or not
        private static bool MouseDown;
        //The button script that is currently pressed down
        private static GenericButtonManager PressedButton;

        //The images used for the button
        [SerializeField] private Sprite unpressed;
        [SerializeField] private Sprite pressed;
        //The function to call when the button is clicked
        [SerializeField] private UnityEvent onClick;

        //The current state of the button
        private ButtonState currentState;

        //Initialize data members and set up the triggers
        void Awake()
        {
            //The button is unselected by default
            currentState = ButtonState.Unpressed;
            MouseDown = false;
        }

        //If the game loses focus and a button is pressed down, unpress it
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && currentState == ButtonState.Pressed)
                unpress();
        }

        //Change the image and state to pressed
        private void press()
        {
            gameObject.GetComponent<Image>().sprite = pressed;
            currentState = ButtonState.Pressed;
        }

        //Change the image and state to unpressed
        private void unpress()
        {
            gameObject.GetComponent<Image>().sprite = unpressed;
            currentState = ButtonState.Unpressed;
        }

        //If this button was last pressed and the mouse moves over it, change to the pressed image
        public void OnPointerEnter(PointerEventData data)
        {
            if (PressedButton == this && MouseDown && currentState == ButtonState.Unpressed)
                OnPointerDown(data);
        }

        //If the button was pressed and the cursor moves off of the button, change to the unpressed image
        public void OnPointerExit(PointerEventData data)
        {
            if (currentState == ButtonState.Pressed)
                unpress();
        }

        //If the mouse is pressed down on the button and its not selected, change to the pressed image
        public void OnPointerDown(PointerEventData data)
        {
            MouseDown = true;

            if (currentState == ButtonState.Unpressed)
            {
                gameObject.GetComponent<Image>().sprite = pressed;
                currentState = ButtonState.Pressed;
                PressedButton = this;
            }
        }

        //If this button is clicked, unpress the button and invoke the 'on click' function
        public void OnPointerUp(PointerEventData data)
        {
            MouseDown = false;

            if (currentState == ButtonState.Pressed)
            {
                unpress();
                onClick.Invoke();
            }
        }
    }
}