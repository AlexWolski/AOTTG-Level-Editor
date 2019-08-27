using UnityEngine;
using System.Collections;

public class ExportManager : MonoBehaviour
{
    //Hide or show the export popup screen
    public void togglePopup()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}