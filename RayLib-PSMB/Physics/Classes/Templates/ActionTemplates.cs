using System.Numerics;
using PSMB.Physics.Objects;

namespace PSMB.Physics.Classes.ObjectTemplates
{
    public static class ActionTemplates
    {
        public static void launch(PhysicsSystem physSystem, PhysicsObject physObj, Vector2 StartPointF, Vector2 EndPointF)
        {
            physSystem.ActivateAtPoint(StartPointF);
            var delta = (
                new Vector2 { X = EndPointF.X, Y = EndPointF.Y }
                - new Vector2 { X = StartPointF.X, Y = StartPointF.Y }
            );
            physSystem.AddVelocityToActive(delta * 2);
        }

        public static void PopAndMultiply(PhysicsSystem physSystem)
        {
            foreach(PhysicsObject obj in PhysicsSystem.ListStaticObjects)
            {
                physSystem.ActivateAtPoint(new(obj.Center.X, obj.Center.Y));
                var velocity = obj.Velocity;
                var origin = obj.Center;
                physSystem.RemoveActiveObject();
                physSystem.SetVelocity(ObjectTemplates.CreateSmallBall(origin.X, origin.Y), velocity);
                physSystem.SetVelocity(ObjectTemplates.CreateSmallBall(origin.X, origin.Y), velocity);
            }
        }
    }
}
