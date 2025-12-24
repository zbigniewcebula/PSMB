using System.Numerics;
using System;
using System.Collections.Generic;

namespace PSMB.Physics.Helpers
{
    public static class PolygonShapeHelper
    {
        /// <summary>
        /// Creates vertices for a circle (convex polygon approximation) of a given radius and resolution in CCW order.
        /// Then reverses the list before returning.
        /// </summary>
        public static List<Vector2> CreateCircleVertices(int resolution, float radius)
        {
            var vertices = new List<Vector2>();
            var twoPi = (float)(Math.PI * 2);
            // CCW order: iterate from 0 to 2π.
            for(var i = 0; i < resolution; i++)
            {
                var angle = twoPi * i / resolution;
                var x = radius * (float)Math.Cos(angle);
                var y = radius * (float)Math.Sin(angle);
                vertices.Add(new(x, y));
            }
            vertices.Reverse();
            return vertices;
        }

        /// <summary>
        /// Creates vertices for a box of a given width and height in CCW order.
        /// For a box centered at (0,0), the vertices are:
        /// bottom-left, top-left, top-right, bottom-right.
        /// Then reverses the list before returning.
        /// </summary>
        public static List<Vector2> CreateBoxVertices(float width, float height)
        {
            var vertices = new List<Vector2>
            {
                new(-width / 2f, -height / 2f), // bottom-left
                new(-width / 2f,  height / 2f), // top-left
                new( width / 2f,  height / 2f), // top-right
                new( width / 2f, -height / 2f)  // bottom-right
            };
            vertices.Reverse();
            return vertices;
        }

        /// <summary>
        /// Creates vertices for a vertical capsule shape in CCW order.
        /// The capsule is built from two semicircular caps (top and bottom) and two vertical edges.
        /// Then reverses the list before returning.
        /// Parameters:
        ///   resolution: number of segments for each semicircular cap (minimum 2 recommended).
        ///   radius: radius of the semicircular caps.
        ///   bodyLength: vertical distance between the flat edges of the caps.
        /// The overall capsule height will be bodyLength + 2 * radius.
        /// 
        /// Vertex ordering (CCW):
        ///   1. Start at the bottom-right corner.
        ///   2. Right vertical edge (from bottom to top).
        ///   3. Top cap (arc from right to left).
        ///   4. Left vertical edge (from top to bottom).
        ///   5. Bottom cap (arc from left to right).
        /// Then the list is reversed.
        /// </summary>
        public static List<Vector2> CreateCapsuleVertices(int resolution, float radius, float bodyLength)
        {
            if(resolution < 2)
            {
                throw new ArgumentException(
                    "Resolution must be at least 2.",
                    nameof(resolution)
                );
            }

            var vertices = new List<Vector2>();
            var halfBody = bodyLength / 2f;
            
            // Define centers for the caps.
            var topCenter = new Vector2(0, halfBody);
            var bottomCenter = new Vector2(0, -halfBody);

            // --- Right Vertical Edge ---
            // Start at the bottom-right vertex.
            var start = new Vector2(radius, -halfBody);
            vertices.Add(start);
            // Next vertex: top-right.
            var rightEdge = new Vector2(radius, halfBody);
            vertices.Add(rightEdge);

            // --- Top Cap ---
            // Generate top cap vertices (arc from right to left) about topCenter.
            // Let angle vary from 0 to π.
            for(var i = 1; i <= resolution; i++)
            {
                var angle = (float)Math.PI * i / resolution; // from 0 to π.
                var x = topCenter.X + radius * (float)Math.Cos(angle);
                var y = topCenter.Y + radius * (float)Math.Sin(angle);
                vertices.Add(new(x, y));
            }

            // --- Left Vertical Edge ---
            // Add the left vertical edge from top to bottom.
            var leftEdge = new Vector2(-radius, -halfBody);
            vertices.Add(leftEdge);

            // --- Bottom Cap ---
            // Generate bottom cap vertices (arc from left to right) about bottomCenter.
            // Let angle vary from π to 2π, skipping the first (duplicate leftEdge) and last (duplicate start).
            for(var i = 1; i < resolution; i++)
            {
                var angle = (float)Math.PI + (float)Math.PI * i / resolution; // from π to 2π.
                var x = bottomCenter.X + radius * (float)Math.Cos(angle);
                var y = bottomCenter.Y + radius * (float)Math.Sin(angle);
                vertices.Add(new(x, y));
            }

            vertices.Reverse();
            return vertices;
        }
    }
}