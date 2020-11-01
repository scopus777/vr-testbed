using UnityEngine;

public class CircleHelper : MonoBehaviour
{
    /// <summary>
    /// Source: http://csharphelper.com/blog/2016/09/draw-a-circle-through-three-points-in-c/
    /// </summary>
    public static void FindCircle(Vector2 a, Vector2 b, Vector2 c,
        out Vector2 center, out float radius)
    {
        // Get the perpendicular bisector of (x1, y1) and (x2, y2).
        float x1 = (b.x + a.x) / 2;
        float y1 = (b.y + a.y) / 2;
        float dy1 = b.x - a.x;
        float dx1 = -(b.y - a.y);

        // Get the perpendicular bisector of (x2, y2) and (x3, y3).
        float x2 = (c.x + b.x) / 2;
        float y2 = (c.y + b.y) / 2;
        float dy2 = c.x - b.x;
        float dx2 = -(c.y - b.y);

        // See where the lines intersect.
        bool lines_intersect, segments_intersect;
        Vector2 intersection, close1, close2;
        FindIntersection(
            new Vector2(x1, y1), new Vector2(x1 + dx1, y1 + dy1),
            new Vector2(x2, y2), new Vector2(x2 + dx2, y2 + dy2),
            out lines_intersect, out segments_intersect,
            out intersection, out close1, out close2);
        if (!lines_intersect)
        {
            Debug.Log("The points are colinear");
            center = new Vector2(0, 0);
            radius = 0;
        }
        else
        {
            center = intersection;
            float dx = center.x - a.x;
            float dy = center.y - a.y;
            radius = Mathf.Sqrt(dx * dx + dy * dy);
        }
    }

    /// <summary>
    /// Source: http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/
    /// </summary>
    public static void FindIntersection(
        Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
        out bool lines_intersect, out bool segments_intersect,
        out Vector2 intersection,
        out Vector2 close_p1, out Vector2 close_p2)
    {
        // Get the segments' parameters.
        float dx12 = p2.x - p1.x;
        float dy12 = p2.y - p1.y;
        float dx34 = p4.x - p3.x;
        float dy34 = p4.y - p3.y;

        // Solve for t1 and t2
        float denominator = (dy12 * dx34 - dx12 * dy34);

        float t1 =
            ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
            / denominator;
        if (float.IsInfinity(t1))
        {
            // The lines are parallel (or close enough to it).
            Debug.Log("The lines are parallel (or close enough to it)");
            lines_intersect = false;
            segments_intersect = false;
            intersection = new Vector2(float.NaN, float.NaN);
            close_p1 = new Vector2(float.NaN, float.NaN);
            close_p2 = new Vector2(float.NaN, float.NaN);
            return;
        }

        lines_intersect = true;

        float t2 =
            ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12)
            / -denominator;

        // Find the point of intersection.
        intersection = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);

        // The segments intersect if t1 and t2 are between 0 and 1.
        segments_intersect =
            ((t1 >= 0) && (t1 <= 1) &&
             (t2 >= 0) && (t2 <= 1));

        // Find the closest points on the segments.
        if (t1 < 0)
        {
            t1 = 0;
        }
        else if (t1 > 1)
        {
            t1 = 1;
        }

        if (t2 < 0)
        {
            t2 = 0;
        }
        else if (t2 > 1)
        {
            t2 = 1;
        }

        close_p1 = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);
        close_p2 = new Vector2(p3.x + dx34 * t2, p3.y + dy34 * t2);
    }
}
