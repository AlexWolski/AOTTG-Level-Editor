using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public static class TransformTools
{
    //A table that stores the original position and scale of objects being scaled
    private static Dictionary<GameObject, (Vector3, Vector3)> scaleTransformTable = new Dictionary<GameObject, (Vector3, Vector3)>();

    //When the scaling using the tool handle first begins, store the original scale and position of the selected objects
    public static void saveScaleTransforms()
    {
        foreach (GameObject mapObject in ObjectSelection.getSelection())
            scaleTransformTable.Add(mapObject, (mapObject.transform.localScale, mapObject.transform.position));
    }

    //Release the saved transforms when the tool handle scaling is finished
    public static void releaseScaleTransforms()
    {
        scaleTransformTable = new Dictionary<GameObject, (Vector3, Vector3)>();
    }

    //Translate each object in the selection by the displacement
    public static void TranslateSelection(ref List<GameObject> objectsToTranslate, Vector3 displacement)
    {
        foreach (GameObject mapObject in objectsToTranslate)
            mapObject.transform.position += displacement;
    }

    //Rotate the object in place around the given axis
    public static void RotateSelection(ref List<GameObject> objectsToRotate, Vector3 rotationAxis, float angle)
    {
        foreach (GameObject mapObject in objectsToRotate)
            mapObject.transform.RotateAround(mapObject.transform.position, rotationAxis, angle);
    }

    //Rotate the object around the given axis and pivot
    public static void RotateSelection(ref List<GameObject> objectsToRotate, Vector3 pivot, Vector3 rotationAxis, float angle)
    {
        foreach (GameObject mapObject in objectsToRotate)
            mapObject.transform.RotateAround(pivot, rotationAxis, angle);
    }

    //Scale a group of objects. The scale factor is applied to the object transform from when the scale first started
    public static void ScaleSelection(ref List<GameObject> objectsToScale, Vector3 pivot, Vector3 scaleFactor, bool lockPos)
    {
        foreach (GameObject mapObject in objectsToScale)
        {
            //Retreive the transform tuple from the transform table
            (Vector3, Vector3) originalTransform = scaleTransformTable[mapObject];
            
            Vector3 newScale = originalTransform.Item1;
            Vector3 newPosition = originalTransform.Item2;

            //Scale the position and scale of the object
            for (int axis = 0; axis < 3; axis++)
            {
                newScale[axis] *= scaleFactor[axis];
                
                //If the position isn't locked, scale the position
                if(!lockPos)
                {
                    //Subtract the pivot before scaling, then add it back
                    newPosition[axis] = (newPosition[axis] - pivot[axis]) * scaleFactor[axis];
                    newPosition[axis] += pivot[axis];
                }
            }

            //Apply the new scale and position
            mapObject.transform.localScale = newScale;
            mapObject.transform.position = newPosition;
        }
    }
}
