using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public static class TransformTools
{
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
            //Get the current scale and position of the object
            Vector3 newScale = CommonReferences.mapManager.objectScriptTable[mapObject].Scale;
            Vector3 newPosition = mapObject.transform.position;

            //Scale the position and scale of the object
            for (int axis = 0; axis < 3; axis++)
            {
                //Don't scale the axis if the scale factor is 1
                if (scaleFactor[axis] != 1f)
                {
                    newScale[axis] *= scaleFactor[axis];

                    //If the position isn't locked, scale the position
                    if (!lockPos)
                    {
                        //Subtract the pivot before scaling, then add it back
                        newPosition[axis] = (newPosition[axis] - pivot[axis]) * scaleFactor[axis];
                        newPosition[axis] += pivot[axis];
                    }
                }
            }

            //Apply the new scale and position
            CommonReferences.mapManager.objectScriptTable[mapObject].Scale = newScale;
            mapObject.transform.position = newPosition;
        }
    }
}
