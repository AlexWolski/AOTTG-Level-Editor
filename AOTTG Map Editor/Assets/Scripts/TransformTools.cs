using UnityEngine;
using System.Collections.Generic;

public static class TransformTools
{
    //Translate each object in the selection by the displacement
    public static void TranslateSelection(ref List<GameObject> objectsToTranslate, Vector3 displacement)
    {
        foreach (GameObject gameObject in objectsToTranslate)
            gameObject.transform.position += displacement;
    }

    //Rotate the object in place around the given axis
    public static void RotateSelection(ref List<GameObject> objectsToRotate, Vector3 rotationAxis, float angle)
    {
        foreach (GameObject gameObject in objectsToRotate)
            gameObject.transform.RotateAround(gameObject.transform.position, rotationAxis, angle);
    }

    //Rotate the object around the given axis and pivot
    public static void RotateSelection(ref List<GameObject> objectsToRotate, Vector3 pivot, Vector3 rotationAxis, float angle)
    {
        foreach (GameObject gameObject in objectsToRotate)
            gameObject.transform.RotateAround(pivot, rotationAxis, angle);
    }

    //Scale a group of objects. The scale factor is applied to the object transform from when the scale first started
    public static void ScaleSelection(ref List<GameObject> objectsToScale, Vector3 scaleFactor, bool lockPos)
    {
        
    }
}
