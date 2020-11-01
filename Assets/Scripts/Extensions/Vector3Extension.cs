using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extension
{
    /// <summary>
    /// Maps a Vector3 to a Vector2 by removing the value for a primary axis. 
    /// </summary>
    public static Vector2 ToVector2(this Vector3 vector3, Vector3 axis)
    {
        List<float> list = new List<float>();
        if (Mathf.Abs(axis.x) <= 0)
            list.Add(vector3.x);
        if (Mathf.Abs(axis.y) <= 0)
            list.Add(vector3.y);
        if (Mathf.Abs(axis.z) <= 0)
            list.Add(vector3.z);
        return new Vector2(list[0], list[1]);
    }

    public static string toDebugString(this Vector3 vector3)
    {
        return vector3.x + " " + vector3.y + " " + vector3.z;
    }
}
