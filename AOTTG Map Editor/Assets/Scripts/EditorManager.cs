using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
    //Determines if the user is in fly more or edit mode. Default mode is edit
    public EditorMode currentMode;

    //If the x key is pressed, toggle between edit and fly mode
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if(currentMode == EditorMode.Fly)
            {
                currentMode = EditorMode.Edit;
                Screen.lockCursor = false;
            }
            else if(currentMode == EditorMode.Edit)
            {
                currentMode = EditorMode.Fly;
                Screen.lockCursor = true;
            }
        }
    }
}