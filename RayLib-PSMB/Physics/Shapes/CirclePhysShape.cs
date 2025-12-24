using System.Numerics;
using System;
using System.Collections.Generic;
using PSMB.Physics.Structs;
using Raylib_cs;

namespace PSMB.Physics.Shapes
{
    public class CirclePhysShape : IShape
    {
        const int SEGMENTS = 8;

        public IShape.Type ShapeType => IShape.Type.Circle;

        public float Radius { get; private set; }

        public Vector2 Size
        {
            get => new(Radius, Radius);
            set
            {
                Radius = Math.Max(value.X / 2f, value.Y / 2f);
                RecalculateVertices();
            }
        }
        
        public List<Vector2> LocalVertices { get; set; } = new();
        
        public CirclePhysShape(float radius)
        {
            Radius = radius;

            RecalculateVertices();
        }

        private void RecalculateVertices()
        {
            // Build local vertices approximating a circle with 'resolution' points
            // around local (0,0).
            for(var i = 0; i < SEGMENTS; i++)
            {
                float theta = (2f * (float)Math.PI * i) / SEGMENTS;
                float x     = Radius * (float)Math.Cos(theta);
                float y     = Radius * (float)Math.Sin(theta);
                LocalVertices.Add(new Vector2(x, y));
            }
        }

        public Rectangle GetAABB(Vector2 center, float angle)
        {
            // A circle's AABB is independent of rotation.
            return new(
                new(center.X - Radius, center.Y - Radius), 
                new(Radius, Radius)
            );
        }

        public float GetArea() => (float)(Math.PI * Radius * Radius);

        public float GetMomentOfInertia(float mass)
        {
            return 0.5f * mass * Radius * Radius;
        }

        public bool Contains(Vector2 point, Vector2 center, float angle)
        {
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            return (dx * dx + dy * dy) <= (Radius * Radius);
        }
    }
}
