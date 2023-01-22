using SFML.System;

public class Math2D
{
    public static bool LineSegementsIntersect(Vector2f start, Vector2f end, Vector2f start2, Vector2f end2, 
                                                out Vector2f intersection, bool considerCollinearOverlapAsIntersect = false)
    {
        intersection = new Vector2f();

        var line1 = end - start;
        var line2 = end2 - start2;
        var line1Crossline2 = line1.Cross(line2);
        var startsCrossLine1 = (start2 - start).Cross(line1);

        // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
        if (line1Crossline2.IsZero() && startsCrossLine1.IsZero())
        {
            // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
            // then the two lines are overlapping,
            if (considerCollinearOverlapAsIntersect)
                if ((0 <= (start2 - start).Dot(line1) && (start2 - start).Dot(line1) <= line1.Dot(line1)) || (0 <= (start - start2).Dot(line2) && (start - start2).Dot(line2) <= line2.Dot(line2)))
                    return true;

            // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
        if (line1Crossline2.IsZero() && !startsCrossLine1.IsZero())
            return false;

        // t = (q - p) x s / (r x s)
        var time1 = (start2 - start).Cross(line2)/line1Crossline2;

        // u = (q - p) x r / (r x s)

        var time2 = (start2 - start).Cross(line1)/line1Crossline2;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point p + t r = q + u s.
        if (!line1Crossline2.IsZero() && (0 <= time1 && time1 <= 1) && (0 <= time2 && time2 <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = start + line1.Multiply(time1);

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }

    public static bool LineSegmentIntersectsRay(Vector2f start, Vector2f end, Vector2f rayStart, Vector2f rayDirection, out Vector2f intersection)
    {
        intersection = new Vector2f();

        var line1 = end - start;
        var line2 = rayDirection;
        var line1Crossline2 = line1.Cross(line2);
        var startsCrossLine1 = (rayStart - start).Cross(line1);

        // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
        if (line1Crossline2.IsZero() && startsCrossLine1.IsZero())
        {
            // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
            // then the two lines are overlapping,
            // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
            // then the two lines are collinear but disjoint.
            // No need to implement this expression, as it follows from the expression above.
            return false;
        }

        // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
        if (line1Crossline2.IsZero() && !startsCrossLine1.IsZero())
            return false;

        // t = (q - p) x s / (r x s)
        var time1 = (rayStart - start).Cross(line2)/line1Crossline2;

        // u = (q - p) x r / (r x s)

        var time2 = (rayStart - start).Cross(line1)/line1Crossline2;

        // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
        // the two line segments meet at the point p + t r = q + u s.
        if (!line1Crossline2.IsZero() && (0 <= time1 && time1 <= 1) && (0 <= time2 && time2 <= 1))
        {
            // We can calculate the intersection point using either t or u.
            intersection = start + line1.Multiply(time1);

            // An intersection was found.
            return true;
        }

        // 5. Otherwise, the two line segments are not parallel but do not intersect.
        return false;
    }

}