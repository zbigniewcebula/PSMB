using System.Numerics;
using PSMB.Physics.Classes;
using PSMB.Physics.Helpers;
using PSMB.Physics.Objects;
using PSMB.Physics.Shapes;
using PSMB.Physics.Structs;

namespace PSMB.Physics
{
    public static class Collision
    {
        private const float EPLISON = 0.0001f;

        public static bool AABBvsAABB(Rect a, Rect b)
        {
            // Exit with no intersection if found separated along an axis
            if(a.Max.X < b.Min.X || a.Min.X > b.Max.X)
            {
                return false;
            }
            if(a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y)
            {
                return false;
            }

            // No separating axis found, therefor there is at least one overlapping axis
            return true;
        }

        public static bool PolygonVsPolygon(ref Manifold m)
        {
            /*
            * 1) Get the shapes (both assumed to be “polygons” here).
            *    - If a shape is BoxPhysShape, you can generate the 4 corners via CollisionHelpers.GetRectangleCorners.
            *    - If a shape is PolygonPhysShape, create a method GetTransformedVertices(PhysicsObject) that returns
            *      all vertices in world space.
            */
            var A = m.A;
            var B = m.B;

            // Safety check: if both objects are locked, no need to resolve collision
            if(A.Locked && B.Locked)
            {
                return false;
            }

            // Collect polygon vertices in *world space*.
            // For example, if your shapes are BoxPhysShape, you can do:
            //   List<Vector2> polyA = CollisionHelpers.GetRectangleCorners(A);
            //   List<Vector2> polyB = CollisionHelpers.GetRectangleCorners(B);
            // For a general PolygonPhysShape, you might do polygonShape.GetTransformedVertices(A.Center, A.Angle), etc.
            var polyA = GetWorldVertices(A);
            var polyB = GetWorldVertices(B);

            // The overall penetration and normal (to fill into the manifold).
            var minPenetration = float.MaxValue;
            var bestAxis = Vector2.Zero;

            /*
            * 2) For SAT, we must:
            *    - Take every edge of polygon A, compute its normal,
            *      project both polygons onto that normal, and check for overlap.
            *    - Repeat for every edge of polygon B.
            *    - If any projection does not overlap, return false (no collision).
            *    - Otherwise, find the minimum overlap of all tested axes. That overlap is our final penetration,
            *      and the corresponding axis is our collision normal.
            */

            // Check edges from A
            for(var i = 0; i < polyA.Length; i++)
            {
                var next = (i + 1) % polyA.Length;
                // Edge = current -> next
                var edge = polyA[next] - polyA[i];
                // Normal = perpendicular; you can do (-edge.Y, edge.X)
                var axis = Vector2.Normalize(new Vector2(-edge.Y, edge.X));

                // Project both polygons onto 'axis'
                if(!ProjectAndCheckOverlap(polyA, polyB, axis, ref minPenetration, ref bestAxis))
                    return false;
            }

            // Check edges from B
            for(var i = 0; i < polyB.Length; i++)
            {
                var next = (i + 1) % polyB.Length;
                // Edge = current -> next
                var edge = polyB[next] - polyB[i];
                // Normal = perpendicular
                var axis = Vector2.Normalize(new Vector2(-edge.Y, edge.X));

                // Project both polygons onto 'axis'
                if(!ProjectAndCheckOverlap(polyA, polyB, axis, ref minPenetration, ref bestAxis))
                    return false;
            }

            // After you finalize bestAxis and minPenetration, ensure the normal points from A to B.
            var centerDiff = B.Center - A.Center;
            if(Vector2.Dot(centerDiff, bestAxis) < 0)
            {
                bestAxis = -bestAxis;
            }

            // If we reach this point, there is a collision.
            m.Normal = bestAxis;
            m.Penetration = minPenetration;

            // Approximate contact point: You can do a midpoint between centers as a fallback:
            m.ContactPoint = (A.Center + B.Center) * 0.5f;

            // Update to accurate contact point after confirmed collision
            CollisionHelpers.UpdateContactPoint(ref m);

            return true;
        }

        /*
        * Example helper to project two polygons onto the given axis and check for overlap.
        * If there is an overlap, we return true; otherwise, false. This also updates the minimum
        * penetration depth and best-axis if the new overlap is smaller.
        */
        private static bool ProjectAndCheckOverlap(
            Vector2[] polyA,
            Vector2[] polyB,
            Vector2 axis,
            ref float minPenetration,
            ref Vector2 bestAxis)
        {
            // 1) Project polygon A
            (float minA, float maxA) = ProjectPolygon(polyA, axis);
            // 2) Project polygon B
            (float minB, float maxB) = ProjectPolygon(polyB, axis);

            // 3) Check for gap
            if(maxA < minB || maxB < minA)
            {
                return false; // No overlap => no collision
            }

            // 4) Overlap distance = min(maxA, maxB) - max(minA, minB)
            var overlap = Math.Min(maxA, maxB) - Math.Max(minA, minB);

            // Track the smallest overlap (for the final collision normal)
            if(overlap >= minPenetration)
            {
                return true;
            }

            minPenetration = overlap;
            // Ensure the normal points from A to B (optional consistency)
            // You can check the direction by comparing centers or by sign of Dot
            bestAxis = axis;

            return true;
        }

        /*
        * Projects all vertices of a polygon onto 'axis' and returns (min, max) scalar values.
        */
        private static (float min, float max) ProjectPolygon(Vector2[] poly, Vector2 axis)
        {
            var min = float.MaxValue;
            var max = float.MinValue;

            foreach(var p in poly)
            {
                float dot = p.X * axis.X + p.Y * axis.Y; // Dot product
                if(dot < min)
                {
                    min = dot;
                } else if(dot > max)
                {
                    max = dot;
                }
            }
            return (min, max);
        }

        /*
        * Example helper to retrieve a shape's vertices in world space. 
        * For a BoxPhysShape, you can reuse CollisionHelpers.GetRectangleCorners.
        * For a PolygonPhysShape, you might store a local List<Vector2> and transform each by center + rotation.
        */
        private static Vector2[] GetWorldVertices(PhysicsObject obj)
        {
            return obj.Shape.GetTransformedVertices(obj.Center, obj.Angle);
        }

        public static bool CirclevsCircle(ref Manifold m)
        {
            var A = m.A;
            var B = m.B;

            // Ensure both objects are circles.
            var circleA = (CirclePhysShape)A.Shape;
            var circleB = (CirclePhysShape)B.Shape;

            if(circleA == null || circleB == null)
            {
                throw new ArgumentException("CirclevsCircle requires both objects to have a CircleShape.");
            }

            // Vector from A to B.
            var n = B.Center - A.Center;

            // Radii of the circles.
            var rA = circleA.Radius;
            var rB = circleB.Radius;
            var radiusSum = rA + rB;

            // Early out if circles are not colliding.
            if(n.LengthSquared() > radiusSum * radiusSum)
            {
                return false;
            }

            // Compute the distance between circle centers.
            var d = n.Length();

            if(d != 0)
            {
                // Penetration is the difference between the sum of the radii and the distance.
                m.Penetration = radiusSum - d;
                // The collision normal is the normalized vector from A to B.
                m.Normal = n / d;

                // Compute contact points on each circle's perimeter along the collision normal.
                var contactA = A.Center + m.Normal * rA;
                var contactB = B.Center - m.Normal * rB;
                m.ContactPoint = (contactA + contactB) * 0.5f;

                return true;
            }
            
            // If the circles are at the same position, choose an arbitrary normal and contact point.
            m.Penetration  = rA;
            m.Normal       = new(1, 0);
            m.ContactPoint = A.Center;
            return true;
        }

        public static bool PolygonVsCircle(ref Manifold m)
        {
            // m.A is assumed to be the polygon; m.B must be the circle.
            var polyObj = m.A;
            var circleObj = m.B;

            // Cast to the correct shape types.
            var circleShape = (CirclePhysShape)circleObj.Shape;

            // Get polygon vertices in world space.
            var poly = GetWorldVertices(polyObj);
            var circleCenter = circleObj.Center;
            var radius = circleShape.Radius;

            // Find the closest point on the polygon's perimeter to the circle's center.
            var minDistSq = float.MaxValue;
            var closestPoint = new Vector2();

            for(var i = 0; i < poly.Length; i++)
            {
                var j = (i + 1) % poly.Length;
                var a = poly[i];
                var b = poly[j];
                var pt = ClosestPointOnSegment(a, b, circleCenter);
                var distSq = (circleCenter - pt).LengthSquared();
                if(distSq < minDistSq)
                {
                    minDistSq = distSq;
                    closestPoint = pt;
                }
            }

            // If the closest distance is greater than the circle's radius, there is no collision.
            if(minDistSq > radius * radius)
            {
                return false;
            }

            // Compute collision details.
            var d = (float)Math.Sqrt(minDistSq);
            var normal = (d > 0) ? (circleCenter - closestPoint) / d : new(1, 0); // Arbitrary if centers coincide.
            m.Normal = normal;
            m.Penetration = radius - d;
            // Approximate contact point: on the circle's perimeter along the collision normal.
            m.ContactPoint = circleCenter - normal * radius;

            return true;
        }

        /// <summary>
        /// Helper: Returns the point on the segment [a, b] that is closest to point p.
        /// </summary>
        private static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var t = Vector2.Dot(p - a, ab) / ab.LengthSquared();
            t = Math.Max(0, Math.Min(1, t));
            return a + ab * t;
        }

        public static void ResolveCollisionRotational(ref Manifold m)
        {
            // Retrieve the two physics objects.
            var A = m.A;
            var B = m.B;

            // For each object, if it's rotational, get its angular velocity and inverse inertia; otherwise, treat as zero.
            var angularVelA = A.CanRotate ? A.AngularVelocity : 0f;
            var iInertiaA =   A.CanRotate ? A.IInertia        : 0f;
            var angularVelB = B.CanRotate ? B.AngularVelocity : 0f;
            var iInertiaB =   B.CanRotate ? B.IInertia        : 0f;

            // Compute vectors from centers to contact point.
            var rA = m.ContactPoint - A.Center;
            var rB = m.ContactPoint - B.Center;

            // Compute the relative velocity at the contact point (including any rotational contribution).
            var vA_contact = A.Velocity + PhysMath.Perpendicular(rA) * angularVelA;
            var vB_contact = B.Velocity + PhysMath.Perpendicular(rB) * angularVelB;
            var relativeVelocity = vB_contact - vA_contact;

            float velAlongNormal = Vector2.Dot(relativeVelocity, m.Normal);
            if(velAlongNormal > 0)
            {
                return;
            }

            var e = Math.Min(A.Restitution, B.Restitution);

            // Compute cross products for the normal.
            var rA_cross_N = PhysMath.Cross(rA, m.Normal);
            var rB_cross_N = PhysMath.Cross(rB, m.Normal);

            // Denominator includes linear inertia plus rotational contributions.
            var invMassSum = A.IMass + B.IMass
                + (rA_cross_N * rA_cross_N) * iInertiaA
                + (rB_cross_N * rB_cross_N) * iInertiaB;

            float j = -(1 + e) * velAlongNormal;
            j /= invMassSum;

            var impulse = m.Normal * j;

            if(!A.Locked && !A.Sleeping)
            {
                A.Velocity -= impulse * A.IMass;
                if(A.CanRotate)
                {
                    A.AngularVelocity -= PhysMath.Cross(rA, impulse) * iInertiaA;
                }
            }
            if(!B.Locked && !B.Sleeping)
            {
                B.Velocity += impulse * B.IMass;
                if(B.CanRotate)
                {
                    B.AngularVelocity += PhysMath.Cross(rB, impulse) * iInertiaB;
                }
            }

            // --- Friction impulse ---
            var tangent = relativeVelocity - m.Normal * Vector2.Dot(relativeVelocity, m.Normal);
            if(tangent.LengthSquared() > EPLISON)
            {
                tangent = Vector2.Normalize(tangent);
            }
            else
            {
                tangent = Vector2.Zero;
            }

            var jt = -Vector2.Dot(relativeVelocity, tangent);

            var rA_cross_t = PhysMath.Cross(rA, tangent);
            var rB_cross_t = PhysMath.Cross(rB, tangent);
            var invMassSumFriction = A.IMass + B.IMass
                + (rA_cross_t * rA_cross_t) * iInertiaA
                + (rB_cross_t * rB_cross_t) * iInertiaB;
            jt /= invMassSumFriction;

            // Clamp friction impulse (Coulomb friction).
            var mu = Math.Max(A.Friction, B.Friction);
            jt =  Math.Min(Math.Abs(jt), mu * Math.Abs(j));
            jt *= (jt < 0 ? -1 : 1); // restore sign

            var frictionImpulse = tangent * jt;

            if(!A.Locked && !A.Sleeping)
            {
                A.Velocity += frictionImpulse * A.IMass;
                if(A.CanRotate)
                {
                    A.AngularVelocity += PhysMath.Cross(rA, frictionImpulse) * iInertiaA;
                }
            }
            if(!B.Locked && !B.Sleeping)
            {
                B.Velocity -= frictionImpulse * B.IMass;
                if(B.CanRotate)
                {
                    B.AngularVelocity -= PhysMath.Cross(rB, frictionImpulse) * iInertiaB;
                }
            }
        }


        public static void PositionalCorrection(ref Manifold m)
        {
            var percent = 0.6f; // usually 20% to 80%
            var slop = 0.05f;    // usually 0.01 to 0.1

            // Only correct penetration beyond the slop.
            var penetration = Math.Max(m.Penetration - slop, 0.0f);
            var correctionMagnitude = penetration / (m.A.IMass + m.B.IMass) * percent;
            var correction = m.Normal * correctionMagnitude;

            if(!m.A.Locked && !m.A.Sleeping)
            {
                m.A.Move(-correction * m.A.IMass);
            }

            if(!m.B.Locked && !m.B.Sleeping)
            {
                m.B.Move(correction * m.B.IMass);
            }
        }

        public static void AngularPositionalCorrection(ref Manifold m)
        {
            // Tuning factor for angular correction; adjust as needed.
            const float angularCorrectionPercent = 0.01f;

            // Compute lever arms (r vectors) from each object's center to the contact point.
            var rA = m.ContactPoint - m.A.Center;
            var rB = m.ContactPoint - m.B.Center;

            // For object A:
            if(!m.A.Locked && !m.A.Sleeping && m.A.CanRotate && rA.LengthSquared() > EPLISON)
            {
                // The farther the contact point is from the center, the smaller the required angular adjustment.
                var angularErrorA = m.Penetration / rA.Length();
                // The sign of the correction is given by the cross product of rA and the collision normal.
                var signA = Math.Sign(PhysMath.Cross(rA, m.Normal));
                // Adjust the angle by a fraction of the error.
                m.A.Angle -= angularCorrectionPercent * angularErrorA * signA;
            }

            // For object B:
            if(!m.B.Locked && !m.B.Sleeping && m.B.CanRotate && rB.LengthSquared() > EPLISON)
            {
                var angularErrorB = m.Penetration / rB.Length();
                var signB = Math.Sign(PhysMath.Cross(rB, m.Normal));
                m.B.Angle += angularCorrectionPercent * angularErrorB * signB;
            }
        }
    }
}