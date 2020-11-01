using UnityEngine;
using System.Collections.Generic;

public static class TransformExtension
{
    /// <summary>
    /// Does a recursive search for a child of a transform.
    /// Breadth-first search
    /// Source: https://answers.unity.com/questions/799429/transformfindstring-no-longer-finds-grandchild.html
    /// </summary>
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(aParent);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.name == aName)
                return c;
            foreach(Transform t in c)
                queue.Enqueue(t);
        }
        return null;
    }    

    public static void ScaleAround(this Transform target, Vector3 pivot, Vector3 newScale)
    {
        Vector3 A = target.localPosition;
        Vector3 B = pivot;
 
        Vector3 C = A - B; // diff from object pivot to desired pivot/origin
 
        float RS = newScale.x / target.localScale.x; // relative scale factor
 
        // calc final position post-scale
        Vector3 FP = B + C * RS;
 
        // finally, actually perform the scale/translation
        target.localScale = newScale;
        target.localPosition = FP;
    }

    public static void SetGlobalScale(this Transform target, Vector3 scale)
    {
        Transform oldParent = target.parent;
        target.parent = null;
        target.localScale = scale;
        target.SetParent(oldParent);
    }
}