using System.Numerics;
using PSMB.Physics.Structs;
using Raylib_cs;

namespace PSMB.Physics.Shapes
{
    public class BoxPhysShape : IShape
    {
        public IShape.Type ShapeType => IShape.Type.Box;

        public Vector2 Size
        {
            get => size;
            set
            {
                size = value;
                RecalculateVertices();
            }
        }
        internal Vector2 size = Vector2.One;
        
        public List<Vector2> LocalVertices { get; set; } = new();
        
        public BoxPhysShape(float width, float height)
        {
            size = new(width, height);
            RecalculateVertices();
        }

        private void RecalculateVertices()
        {
            // Build LocalVertices (centered at (0,0)).
            // We'll assume the box is centered in local space, so half extents:
            float hw = size.X / 2f;
            float hh = size.Y / 2f;

            // Clockwise corners around (0,0):
            LocalVertices.Add(new(hw, -hh));
            LocalVertices.Add(new(-hw, -hh));
            LocalVertices.Add(new(-hw, hh));
            LocalVertices.Add(new(hw, hh));
        }

        public Rectangle GetAABB(Vector2 center, float angle)
        {
            if(angle == 0)
            {
                return new(
                    new(center.X - size.X / 2, center.Y - size.Y / 2), 
                    new(size.X, size.Y)
                );
            }

            float cos       = Math.Abs((float)Math.Cos(angle));
            float sin       = Math.Abs((float)Math.Sin(angle));
            float newWidth  = size.X * cos + size.Y * sin;
            float newHeight = size.X * sin + size.Y * cos;

            return new Rectangle(
                new(center.X - newWidth / 2, center.Y - newHeight / 2),
                new(newWidth, newHeight)
            );
        }

        public float GetArea() => size.X * size.Y;

        public float GetMomentOfInertia(float mass)
        {
            return (mass / 12f) * (size.X * size.X + size.Y * size.Y);
        }

        public bool Contains(Vector2 point, Vector2 center, float angle)
        {
            // Translate point to local space relative to center.
            var localPoint = point - center;

            // Rotate the point by -angle to remove the rotation of the box.
            var cos = (float)Math.Cos(-angle);
            var sin = (float)Math.Sin(-angle);
            var localX = localPoint.X * cos - localPoint.Y * sin;
            var localY = localPoint.X * sin + localPoint.Y * cos;

            // Check against unrotated box extents.
            return Math.Abs(localX) <= size.X / 2 && Math.Abs(localY) <= size.Y / 2;
        }

        /// <summary>
        /// Gets the local point of a point in world space.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="center"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public Vector2 GetLocalPoint(Vector2 point, Vector2 center, float angle)
        {
            // Translate point to local space relative to center.
            var localPoint = point - center;
            // Rotate the point by -angle to remove the rotation of the box.
            var cos = (float)Math.Cos(-angle);
            var sin = (float)Math.Sin(-angle);
            var localX = localPoint.X * cos - localPoint.Y * sin;
            var localY = localPoint.X * sin + localPoint.Y * cos;
            return new(localX, localY);
        }
    }
}
