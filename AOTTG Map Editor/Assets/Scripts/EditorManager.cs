using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
    //Determines if the user is in fly more or edit mode. Default mode is edit
    public string currentMode;

    //If the x key is pressed, toggle between edit and fly mode
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if(currentMode == "fly")
            {
                currentMode = "edit";
                Screen.lockCursor = false;
            }
            else if(currentMode == "edit")
            {
                currentMode = "fly";
                Screen.lockCursor = true;
            }
        }
    }
}