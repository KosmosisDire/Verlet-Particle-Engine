using SFML.System;

public class Math2D
{
    public static bool LineSegmentsIntersect(Vector2f start, Vector2f end, Vector2f start2, Vector2f end2, 
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

    public static bool LineSegmentIntersectsCircle(Vector2f start, Vector2f end, Vector2f circleCenter, float circleRadius)
    {
        var lineSegment = end - start;
        var startToCircle = circleCenter - start;
        var projection = startToCircle.Dot(lineSegment) / lineSegment.Dot(lineSegment);

        if (projection > 1 + circleRadius / lineSegment.Magnitude() || projection < 0)
            return false;

        var closestPoint = start + projection * lineSegment;
        var delta = circleCenter - closestPoint;
        var squaredDistanceToClosestPoint = delta.Dot(delta);

        return squaredDistanceToClosestPoint <= circleRadius * circleRadius;
    }

    public static bool LineSegmentIntersectsBox(Vector2f start, Vector2f end, Vector2f boxCenter, float boxExtent)
    {
        var boxMin = boxCenter - new Vector2f(boxExtent, boxExtent);
        var boxMax = boxCenter + new Vector2f(boxExtent, boxExtent);

        var topLeft = new Vector2f(boxMin.X, boxMax.Y);
        var bottomRight = new Vector2f(boxMax.X, boxMin.Y);

        // Check if the line segment endpoints are inside the box
        if (start.X >= boxMin.X && start.X <= boxMax.X && start.Y >= boxMin.Y && start.Y <= boxMax.Y)
            return true;

        if (end.X >= boxMin.X && end.X <= boxMax.X && end.Y >= boxMin.Y && end.Y <= boxMax.Y)
            return true;

        // Perform the line segment to line segment intersection tests
        if (LineSegmentsIntersect(start, end, topLeft, bottomRight, out _))
            return true;

        return false;
    }



}