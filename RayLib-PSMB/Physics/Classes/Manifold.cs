using System.Numerics;
using PSMB.Physics.Objects;

namespace PSMB.Physics.Classes
{
    public class Manifold
    {
        public PhysicsObject A;
        public PhysicsObject B;
        public float Penetration;
        public Vector2 Normal;
        public Vector2 ContactPoint;

        public void Reset()
        {
            A            = null;
            B            = null;
            Penetration  = 0;
            Normal       = Vector2.Zero;
            ContactPoint = Vector2.Zero;
        }
    }
}
