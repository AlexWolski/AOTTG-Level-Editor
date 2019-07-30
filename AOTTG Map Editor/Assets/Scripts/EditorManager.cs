using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
    //Determines if the user is in fly more or edit mode. Default mode is fly
    public string editMode;

    void Update()
    {
        //If the x key is pressed, toggle between edit and fly mode
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
}