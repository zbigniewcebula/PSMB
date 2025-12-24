using System.Collections.Generic;
using System.Numerics;
using PSMB.Physics.Structs;
using Raylib_cs;

namespace PSMB.Physics.Shapes
{
    public interface IShape
    {
        /// <summary>
        /// Computes the axis-aligned bounding box for this shape, given a center position and rotation angle (in radians).
        /// </summary>
        Rectangle GetAABB(Vector2 center, float angle);

        // Shape Enum so we don't have to run IsInstanceOfClass 100 million times every minute
        public Type ShapeType { get; }
        
        public Vector2 Size { get; set; }

        public List<Vector2> LocalVertices { get; set; }

        /// <summary>
        /// Returns the area of the shape.
        /// </summary>
        float GetArea();

        /// <summary>
        /// Returns the moment of inertia for the shape given a mass.
        /// </summary>
        float GetMomentOfInertia(float mass);

        /// <summary>
        /// Determines whether a given point (in world coordinates) lies within the shape, given the shape’s center and rotation.
        /// </summary>
        bool Contains(Vector2 point, Vector2 center, float angle);

        /// <summary>
        /// Gets the local point of a point in world space.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="center"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public Vector2 GetLocalPoint(Vector2 point, Vector2 center, float angle)
        {
            return point - center;
        }

        /// <summary>
        /// Returns a list of the shape's vertices transformed into world space,
        /// using the provided center and angle.
        /// </summary>
        public virtual Vector2[] GetTransformedVertices(Vector2 center, float angle)
        {
            var transformed = new Vector2[LocalVertices.Count];

            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            for(int i = 0; i < LocalVertices.Count; i++)
            {
                var local = LocalVertices[i];
                // Rotate the local vertex
                float rx = local.X * cos - local.Y * sin;
                float ry = local.X * sin + local.Y * cos;

                // Then translate by the object's center
                float worldX = center.X + rx;
                float worldY = center.Y + ry;

                transformed[i] = new(worldX, worldY);
            }

            return transformed;
        }
        
        public enum Type
        {
            Circle,
            Box,
            Polygon
        }
    }
}
