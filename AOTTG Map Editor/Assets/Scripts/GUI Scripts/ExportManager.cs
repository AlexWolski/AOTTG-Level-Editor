using UnityEngine;
using System.Collections;

public class ExportManager : MonoBehaviour
{
    public void togglePopup()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}