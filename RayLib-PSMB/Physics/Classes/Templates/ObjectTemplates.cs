using System.Numerics;
using PSMB.Physics.Helpers;
using PSMB.Physics.Objects;

namespace PSMB.Physics.Classes.ObjectTemplates
{
    public static class ObjectTemplates
    {
        public static PhysicsObject CreateSmallBall(float originX, float originY)
        {
            // Using a diameter of 5.
            int diameter = 5;
            return PhysicsSystem.CreateStaticCircle(new Vector2(originX, originY), diameter, 0.6F, false);
        }

        public static PhysicsObject CreateMedBall(float originX, float originY)
        {
            int diameter = 10;
            return PhysicsSystem.CreateStaticCircle(new Vector2(originX, originY), diameter, 0.8F, false);
        }

        public static PhysicsObject CreateAttractor(float originX, float originY)
        {
            var diameter = 50;
            // Use a different shader type for attractors.
            var oPhysicsObject = PhysicsSystem.CreateStaticCircle(new Vector2(originX, originY), diameter, 0.95F, true);
            PhysicsSystem.ListGravityObjects.Add(oPhysicsObject);
            return oPhysicsObject;
        }

        public static PhysicsObject CreateWall(Vector2 origin, int width, int height)
        {
            var max = origin + new Vector2(width, height);
            return PhysicsSystem.CreateStaticBox(origin, max, true, 1000000);
        }

        public static PhysicsObject CreateBox(Vector2 origin, int width, int height)
        {
            // Compute mass from dimensions.
            var mass = width * height;
            var max = origin + new Vector2(width, height);
            return PhysicsSystem.CreateStaticBox2(origin, max, false, mass);
        }

        public static PhysicsObject CreatePolygonTriangle(Vector2 origin)
        {
            var points = new Vector2[]
            {
                new(25, -25),
                new(-25, -25),
                new(0, 12.5f)
            };
            return PhysicsSystem.CreatePolygon(origin, points);
        }

        public static PhysicsObject CreatePolygonCapsule(Vector2 origin)
        {
            return PhysicsSystem.CreatePolygon(
                origin,
                PolygonShapeHelper.CreateCapsuleVertices(32, 20, 50).ToArray(),
                canRotate: false
            );
        }

    }
}
