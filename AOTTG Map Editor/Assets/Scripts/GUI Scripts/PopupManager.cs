using UnityEngine;
using System.Collections;

public class PopupManager : MonoBehaviour
{
    //The GUI objects for the popups
    [SerializeField]
    private GameObject inputPopup;
    [SerializeField]
    private GameObject exportPopup;

    public void toggleImportPopup()
    {
        inputPopup.SetActive(!inputPopup.activeSelf);
    }

    public void toggleExportPopup()
    {
        exportPopup.SetActive(!exportPopup.activeSelf);
    }
}
