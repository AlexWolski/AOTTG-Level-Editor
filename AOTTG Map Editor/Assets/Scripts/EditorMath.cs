using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EditorMath
{
    //Returns a vector describing the octant the point is in relative to the origin
    public static Vector3 getOctant(Vector3 origin, Vector3 point)
    {
        //Get the point's position relative to the origin
        Vector3 octant = point - origin;

        //Store the sign of each component
        for (int i = 0; i < 3; i++)
            octant[i] = Mathf.Sign(octant[i]);

        //Return a representation of the octant
        return octant;
    }
}
