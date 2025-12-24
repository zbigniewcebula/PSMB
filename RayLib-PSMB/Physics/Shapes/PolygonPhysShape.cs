using System.Numerics;
using PSMB.Physics.Structs;
using Raylib_cs;

namespace PSMB.Physics.Shapes
{
    public class PolygonPhysShape : IShape
    {
        public IShape.Type ShapeType => IShape.Type.Polygon;

        public Vector2 Size
        {
            get => new(_localMaxX - _localMinX, _localMaxY - _localMinY);
            set {}
        }
        
        /// <summary>
        /// Local-space vertices of the polygon, in clockwise or counterclockwise order.
        /// </summary>
        public List<Vector2> LocalVertices { get; set; }

        // Precomputed bounding box in local space, just for convenience (or you can compute on the fly).
        private float _localMinX;
        private float _localMaxX;
        private float _localMinY;
        private float _localMaxY;

        /// <summary>
        /// Constructs a new polygon shape from a list of local vertices.
        /// The vertices should define a closed, convex polygon in either clockwise or CCW order.
        /// </summary>
        public PolygonPhysShape(IEnumerable<Vector2> vertices)
        {
            LocalVertices = new(vertices);

            var centroid = CollisionHelpers.ComputeCentroid(LocalVertices);

            // Then shift each vertex so the centroid is at (0,0)
            for(var i = 0; i < LocalVertices.Count; i++)
            {
                var v = LocalVertices[i] - centroid;
                LocalVertices[i] = v;
            }

            // 4) Recalculate _localMinX, etc., now that we've shifted everything
            _localMinX = float.MaxValue;
            _localMaxX = float.MinValue;
            _localMinY = float.MaxValue;
            _localMaxY = float.MinValue;

            foreach(var v in LocalVertices)
            {
                if(v.X < _localMinX)
                {
                    _localMinX = v.X;
                };
                if(v.X > _localMaxX)
                {
                    _localMaxX = v.X;
                }
                if(v.Y < _localMinY)
                {
                    _localMinY = v.Y;
                }
                if(v.Y > _localMaxY)
                {
                    _localMaxY = v.Y;
                }
            }
        }


        /// <summary>
        /// The Axis-Aligned Bounding Box for this polygon when placed at 'center' and rotated by 'angle'.
        /// </summary>
        public Rectangle GetAABB(Vector2 center, float angle)
        {
            // We'll rotate each local vertex by 'angle', offset by center, and track min & max.
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            foreach(var v in LocalVertices)
            {
                // Rotate point v by angle around origin, then translate by center
                float rx = v.X * cos - v.Y * sin;
                float ry = v.X * sin + v.Y * cos;
                float worldX = center.X + rx;
                float worldY = center.Y + ry;

                if(worldX < minX)
                {
                    minX = worldX;
                } else if(worldX > maxX)
                {
                    maxX = worldX;
                }
                if(worldY < minY)
                {
                    minY = worldY;
                } else if(worldY > maxY)
                {
                    maxY = worldY;
                }
            }

            return new(new(minX, minY), new(maxX - minX, maxY - minY));
        }

        /// <summary>
        /// Returns the polygon's area (using the shoelace formula), 
        /// always returning a positive value for both CW and CCW vertices.
        /// </summary>
        public float GetArea()
        {
            var total = 0f;
            int count = LocalVertices.Count;

            // Sum over edges, possibly negative for CW ordering
            for(var i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                total += (LocalVertices[i].X * LocalVertices[j].Y) 
                    - (LocalVertices[j].X * LocalVertices[i].Y);
            }

            // Multiply by 0.5 and take absolute value so the area is positive
            return Math.Abs(total * 0.5f);
        }

        public float GetMomentOfInertia(float mass)
        {
            // 1) Compute the polygon’s area using the shoelace formula.
            float area = GetArea(); 
            if(area < 1e-6f)
            {
                return 0f; // Degenerate polygon
            }

            var crossSum = 0f;
            var numer = 0f;

            // 2) Sum over each edge in the polygon.
            for(var i = 0; i < LocalVertices.Count; i++)
            {
                var j = (i + 1) % LocalVertices.Count;
                var v0 = LocalVertices[i];
                var v1 = LocalVertices[j];

                var cross = (v0.X * v1.Y - v1.X * v0.Y);
                var termX = (v0.X * v0.X) + (v0.X * v1.X) + (v1.X * v1.X);
                var termY = (v0.Y * v0.Y) + (v0.Y * v1.Y) + (v1.Y * v1.Y);

                numer += cross * (termX + termY);
                crossSum += cross;
            }

            crossSum = Math.Abs(crossSum);
            if(crossSum < 1e-8f)
            {
                return 0f; // Nearly degenerate polygon
            } 

            // 3) Use the standard formula:
            // I = (mass/(6 * sum(cross))) * numer
            var iPoly = (mass * numer) / (6f * crossSum);
            return Math.Abs(iPoly);
        }


        /// <summary>
        /// Returns true if the given world-space point is inside this polygon, assuming the polygon is 
        /// positioned at 'center' with rotation 'angle'.
        /// </summary>
        public bool Contains(Vector2 point, Vector2 center, float angle)
        {
            // Transform 'point' into local space of the polygon
            var local = WorldToLocalPoint(point, center, angle);

            // Ray-casting or winding number approach. Here's a simple ray-cast:
            // (For brevity, a naive winding or crossing approach is shown.)
            var count = LocalVertices.Count;
            var inside = false;
            for(var i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                var v0 = LocalVertices[i];
                var v1 = LocalVertices[j];

                bool intersect = ((v0.Y > local.Y) != (v1.Y > local.Y))
                                 && (local.X < (v1.X - v0.X) * (local.Y - v0.Y) / (v1.Y - v0.Y) + v0.X);
                if(intersect)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public Vector2 WorldToLocalPoint(Vector2 worldPoint, Vector2 center, float angle)
        {
            // Translate
            var translated = worldPoint - center;

            // Rotate by -angle
            var cos = (float)Math.Cos(-angle);
            var sin = (float)Math.Sin(-angle);
            var rx = translated.X * cos - translated.Y * sin;
            var ry = translated.X * sin + translated.Y * cos;

            return new(rx, ry);
        }
    }
}

