using System;
using System.Collections.Generic;
using PSMB.Physics.Classes;
using PSMB.Physics.Objects;
using PSMB.Physics.Shapes;
using System.Linq;
using System.Numerics;

public static class CollisionHelpers
{
    // Computes the four corners of a rectangle (OBB) in world space.
    public static List<Vector2> GetRectangleCorners(PhysicsObject obj)
    {
        var box = (BoxPhysShape)obj.Shape;

        var corners = new List<Vector2>(4);
        var halfW = box.Size.X / 2f;
        var halfH = box.Size.Y / 2f;

        // Define corners in local space in clockwise order.
        // The Sutherland-Hodgman algorithm expects the clip polygon to be in counterclockwise order, however, we're in
        // a coordinate system where the y-axis points down, so we define the corners in clockwise order.
        // For example, starting at the bottom-right:
        // bottom-right, bottom-left, top-left, top-right.
        var localCorners = new Vector2[]
        {
            new( halfW, -halfH),   // bottom-right
            new(-halfW, -halfH),   // bottom-left
            new(-halfW,  halfH),   // top-left
            new( halfW,  halfH)    // top-right
        };

        var cos = (float)Math.Cos(obj.Angle);
        var sin = (float)Math.Sin(obj.Angle);
        foreach(var lc in localCorners)
        {
            // Rotate the local corner and translate to world space.
            var worldX = obj.Center.X + lc.X * cos - lc.Y * sin;
            var worldY = obj.Center.Y + lc.X * sin + lc.Y * cos;
            corners.Add(new(worldX, worldY));
        }
        return corners;
    }

    public static List<Vector2> SutherlandHodgmanClip(Vector2[] subjectPolygon, Vector2[] clipPolygon)
    {
        // Start with the subject polygon.
        var poly = new List<Vector2>(subjectPolygon);
        // For each edge of the clip polygon:
        var clipCount = clipPolygon.Length;
        for(var i = 0; i < clipCount; i++)
        {
            var next = (i + 1) % clipCount;
            var clipEdgeStart = clipPolygon[i];
            var clipEdgeEnd = clipPolygon[next];
            poly = ClipEdge(poly, clipEdgeStart, clipEdgeEnd);
            if(poly.Count == 0)
            {
                return subjectPolygon.ToList();
            }
        }

        return poly;
    }

    /// <summary>
    /// Clips a polygon (poly) against a single clip edge defined by clipEdgeStart and clipEdgeEnd.
    /// This function implements the logic from the provided C++ code, using floats.
    /// </summary>
    private static List<Vector2> ClipEdge(List<Vector2> poly, Vector2 clipEdgeStart, Vector2 clipEdgeEnd)
    {
        var newPoly = new List<Vector2>();
        int polySize = poly.Count;
        if(polySize == 0)
        {
            return newPoly;
        }

        // Iterate over each edge of the polygon.
        for(var i = 0; i < polySize; i++)
        {
            var k = (i + 1) % polySize;
            var current = poly[i];
            var next = poly[k];

            // Compute the "position" of the points relative to the clip edge.
            // A point is considered "inside" if this value is < 0.
            var currentPos = (clipEdgeEnd.X - clipEdgeStart.X) * (current.Y - clipEdgeStart.Y)
                               - (clipEdgeEnd.Y - clipEdgeStart.Y) * (current.X - clipEdgeStart.X);
            var nextPos = (clipEdgeEnd.X - clipEdgeStart.X) * (next.Y - clipEdgeStart.Y)
                            - (clipEdgeEnd.Y - clipEdgeStart.Y) * (next.X - clipEdgeStart.X);

            // Case 1: Both points are inside.
            if(currentPos < 0 && nextPos < 0)
            {
                // Add the second point.
                newPoly.Add(next);
            }
            // Case 2: Current is outside, next is inside.
            else if(currentPos >= 0 && nextPos < 0)
            {
                // Add the intersection point and the next point.
                var intersect = ComputeIntersection(current, next, clipEdgeStart, clipEdgeEnd);
                newPoly.Add(intersect);
                newPoly.Add(next);
            }
            // Case 3: Current is inside, next is outside.
            else if(currentPos < 0 && nextPos >= 0)
            {
                // Add the intersection point only.
                var intersect = ComputeIntersection(current, next, clipEdgeStart, clipEdgeEnd);
                newPoly.Add(intersect);
            }
            // Case 4: Both are outside – add nothing.
        }
        return newPoly;
    }

    /// <summary>
    /// Computes the intersection point of the infinite lines through points s->e and cp1->cp2.
    /// This is the same as our existing ComputeIntersection, but included here for clarity.
    /// </summary>
    public static Vector2 ComputeIntersection(Vector2 s, Vector2 e, Vector2 cp1, Vector2 cp2)
    {
        var dc = cp1 - cp2;
        var dp = s - e;
        var n1 = cp1.X * cp2.Y - cp1.Y * cp2.X;
        var n2 = s.X * e.Y - s.Y * e.X;
        var denom = dc.X * dp.Y - dc.Y * dp.X;
        if(Math.Abs(denom) < 1e-6f)
        {
            return s; // Lines are parallel; return s as fallback.
        }
        var x = (n1 * dp.X - n2 * dc.X) / denom;
        var y = (n1 * dp.Y - n2 * dc.Y) / denom;
        return new(x, y);
    }


    // Helper: Computes the signed area of a polygon.
    // Positive area means vertices are in counter-clockwise order.
    public static float ComputeSignedArea(List<Vector2> poly)
    {
        var area = 0f;
        for(var i = 0; i < poly.Count; i++)
        {
            var j = (i + 1) % poly.Count;
            area += (poly[i].X * poly[j].Y) - (poly[j].X * poly[i].Y);
        }
        return area / 2f;
    }


    // Returns true if point p is inside the half-space defined by edge from a to b.
    // Assumes clip polygon is defined in counterclockwise order.
    public static bool IsInside(Vector2 a, Vector2 b, Vector2 p)
    {
        // Compute the cross product: if p is to the left of ab, it is inside.
        return Cross(b - a, p - a) >= 0;
    }

        // Computes the centroid (center of mass) of a polygon.
    public static Vector2 ComputeCentroid(List<Vector2> polygon)
    {
        var accumulatedArea = 0f;
        var centerX = 0f;
        var centerY = 0f;
        for(int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i, i++)
        {
            var temp = polygon[j].X * polygon[i].Y - polygon[i].X * polygon[j].Y;
            accumulatedArea += temp;
            centerX += (polygon[j].X + polygon[i].X) * temp;
            centerY += (polygon[j].Y + polygon[i].Y) * temp;
        }

        // Much greater than the recommended epsilon of 1e-6, but the system becomes unstable below this
        if(Math.Abs(accumulatedArea) < 0.5f)
        {
            return polygon[0];
        }

        // Multiply accumulatedArea by 3 to get the proper divisor (6 * area).
        accumulatedArea *= 3f;
        return new(centerX / accumulatedArea, centerY / accumulatedArea);
    }

    // This function computes the actual contact point between two rotated rectangles.
    // It clips rectangle A's corners against rectangle B and computes the centroid of the intersection polygon.
    public static void UpdateContactPoint(ref Manifold m)
    {
        var polyA = m.A.Shape.GetTransformedVertices(m.A.Center, m.A.Angle);
        var polyB = m.B.Shape.GetTransformedVertices(m.B.Center, m.B.Angle);

        var intersection = SutherlandHodgmanClip(polyA, polyB);
        if(intersection.Count == 0)
        {
            // Fallback: use midpoint between centers.
            m.ContactPoint = (m.A.Center + m.B.Center) * 0.5f;
        }
        else
        {
            m.ContactPoint = ComputeCentroid(intersection);
        }
    }

    // Helper: 2D cross product returning a scalar.
    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }
}
