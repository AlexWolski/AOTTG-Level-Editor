using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class ImportMap : MonoBehaviour
{
    //The object that listens for clicks
    private static EventTrigger eventTrigger;

    //Set up the triggers
    void Start()
    {
        eventTrigger = GetComponent<EventTrigger>();
        eventTrigger.AddEventTrigger(this.importMap, EventTriggerType.PointerClick);
    }

    //Prompt the user to choose a file
    private void importMap(BaseEventData data)
    {
        Debug.Log("Test");
    }
}
