using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
    //Determines if the user is in fly more or edit mode. Default mode is edit
    public string editMode;
    //A list containing the objects currently selected
    private List<GameObject> selectedObjects = new List<GameObject>();

    //If the x key is pressed, toggle between edit and fly mode
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if(editMode == "fly")
            {
                editMode = "edit";
                Screen.lockCursor = false;
            }
            else if(editMode == "edit")
            {
                editMode = "fly";
                Screen.lockCursor = true;
            }
        }
    }

    //Add an object to the list of selected objects
    public void selectObject(GameObject objectToAdd)
    {
        Debug.Log(objectToAdd.name + " Selected!");
        selectedObjects.Add(objectToAdd);
    }

    //Remove an object to the list of selected objects
    public void deselectObject(GameObject objectToRemove)
    {
        Debug.Log(objectToRemove.name + " Deselected!");
        selectedObjects.Remove(objectToRemove);
    }

    //Returns true if the object is already in the list of selected objects
    public bool isSelected(GameObject objectToCheck)
    {
        return selectedObjects.Contains(objectToCheck);
    }
}