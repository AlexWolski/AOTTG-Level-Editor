using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class TogglePopup : MonoBehaviour
{
    [SerializeField]
    private GameObject popup;
    //The object that listens for event updates
    private static EventTrigger eventTrigger;

    //Set up the triggers
    void Start()
    {
        eventTrigger = GetComponent<EventTrigger>();
        eventTrigger.AddEventTrigger(this.togglePopup, EventTriggerType.PointerUp);
    }

    //When the button is clicked, toggle the given gameobject
    public void togglePopup(BaseEventData data)
    {
        popup.SetActive(!popup.activeSelf);
    }
}
