using System.Numerics;
using PSMB.Physics.Classes;
using PSMB.Physics.Constraints;
using PSMB.Physics.Helpers;
using PSMB.Physics.Objects;
using PSMB.Physics.Shapes;

namespace PSMB.Physics
{
    public class PhysicsSystem
    {
        #region Public Properties

        public float GravityScale = 1F;
        public Vector2 Gravity { get; set; } = new(0, -9.71f);
        public float Friction { get; set; }

        // Set this to roughly your average AABB size – adjust as needed.
        public float SpatialHashCellSize { get; set; } = 500F;

        // Sleep / Wake thresholds
        public static float WakeImpulseThreshold { get; private set; } = 4.0f;
        public static float LinearSleepThreshold { get; set; } = 0.05f;
        public static float AngularSleepThreshold { get; set; } = 0.1f;
        public static float SleepTimeThreshold { get; set; } = 0.9f;

        #endregion

        #region Local Declarations
        public const float FPS = 144;
        private const float _dt = 1 / FPS;
        private const int PHYSICS_ITERATIONS = 8;
        private double accumulator = 0;

        public static PhysicsObject ActiveObject;
        private static readonly List<CollisionPair> ListCollisionPairs = new List<CollisionPair>();
        public static readonly List<PhysicsObject> ListGravityObjects = new List<PhysicsObject>();
        public static readonly List<PhysicsObject> ListStaticObjects = new List<PhysicsObject>();
        private readonly ManifoldPool _manifoldPool = new ManifoldPool();
        public List<Constraint> Constraints = new List<Constraint>();

        internal IEnumerable<PhysicsObject> GetMoveableObjects()
        {
            for(var i = ListStaticObjects.Count - 1; i >= 0; i--)
            {
                var obj = ListStaticObjects[i];
                if(!obj.Locked && obj.Mass < 1000000)
                {
                    yield return obj;
                }
            }
        }

        internal void SetVelocity(PhysicsObject physicsObject, Vector2 velocity)
        {
            physicsObject.Velocity = velocity;
        }

        public static readonly Queue<PhysicsObject> RemovalQueue = new();

        #endregion

        #region Reusable Broadphase Fields

        // These fields are now allocated once and reused every tick.
        private readonly Dictionary<(int, int), List<PhysicsObject>> _spatialHash = new();
        private readonly HashSet<(PhysicsObject, PhysicsObject)> _pairSet = new(new PhysicsObjectPairComparer());

        #endregion

        #region Constructors

        public PhysicsSystem()
        {
            Friction = 1f;
        }

        #endregion

        #region Public Methods

        public static PhysicsObject CreateStaticCircle(Vector2 loc, int radius, float restitution, bool locked)
        {
            /*
            // Create the circle shape using the given radius.
            IShape shape = new CirclePhysShape(radius);
            // For a circle, the center is the provided location.
            var obj = new PhysicsObject(shape, loc, restitution, locked, canRotate: true);
            ListStaticObjects.Add(obj);
            return obj;
            */
            return null;
        }

        public static PhysicsObject CreateStaticBox(Vector2 start, Vector2 end, bool locked, float mass)
        {
            /*
            // Ensure start and end define the correct bounds.
            var min = new Vector2(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
            var max = new Vector2(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));

            // Compute width and height.
            var width = max.X - min.X;
            var height = max.Y - min.Y;
            // Calculate the center from the corrected bounds.
            var center = new Vector2((min.X + max.X) / 2f, (min.Y + max.Y) / 2f);

            // Create a box shape with the computed dimensions.
            IShape shape = new BoxPhysShape(width, height);
            var obj = new PhysicsObject(shape, center, 0.2f, locked, mass);
            ListStaticObjects.Add(obj);
            return obj;
            */
            return null;
        }

        public static PhysicsObject CreateStaticBox2(Vector2 start, Vector2 end, bool locked, float mass)
        {
            /*
            // Compute the corrected bounding box.
            var min = new Vector2(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
            var max = new Vector2(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));

            // Compute width and height.
            var width = max.X - min.X;
            var height = max.Y - min.Y;
            // Compute the center.
            var center = new Vector2((min.X + max.X) / 2f, (min.Y + max.Y) / 2f);

            // Create the box shape.
            IShape shape = new BoxPhysShape(width, height);
            var obj = new PhysicsObject(shape, center, 0.2f, locked, mass, canRotate: true);
            obj.Friction = 0.8f;
            ListStaticObjects.Add(obj);
            return obj;
            */
            return null;
        }

        public static PhysicsObject CreatePolygon(Vector2 origin, Vector2[] points, bool locked = false, bool canRotate = true)
        {
            /*
            // Create the polygon shape.
            IShape shape = new PolygonPhysShape(points);
            var obj = new PhysicsObject(shape, origin, 0.2f, locked, canRotate: canRotate);
            obj.Friction = 0.8f;
            ListStaticObjects.Add(obj);
            return obj;
            */
            return null;
        }


        public bool ActivateAtPoint(Vector2 p)
        {
            ActiveObject = CheckObjectAtPoint(p);

            if(ActiveObject == null!)
            {
                ActiveObject = null;
                return false;
            }

            if(ActiveObject.Sleeping)
            {
                ActiveObject.Wake();
            }

            return true;
        }

        public void AddVelocityToActive(Vector2 velocityDelta)
        {
            if(ActiveObject == null! || ActiveObject.Mass >= 1000000)
            {
                return;
            }

            ActiveObject.Velocity += velocityDelta;
        }

        public void SetVelocityOfActive(Vector2 velocityDelta)
        {
            if(ActiveObject == null! || ActiveObject.Mass >= 1000000)
            {
                return;
            }

            ActiveObject.Velocity = velocityDelta;
        }

        public void FreezeStaticObjects()
        {
            foreach(var physicsObject in ListStaticObjects)
            {
                physicsObject.Velocity = Vector2.Zero;
            }
        }

        public Vector2 GetActiveObjectCenter()
        {
            if(ActiveObject == null!)
            {
                return Vector2.Zero;
            }

            return ActiveObject.Center;
        }

        public void MoveActiveTowardsPoint(Vector2 point)
        {
            if(ActiveObject == null!)
            {
                return;
            }

            var delta = ActiveObject.Center - point;
            AddVelocityToActive(-delta / 10000);
        }

        public void HoldActiveAtPoint(Vector2 point)
        {
            if(ActiveObject == null!)
            {
                return;
            }
            var delta = ActiveObject.Center - point;
            SetVelocityOfActive(-delta * 10);
        }

        public void ReleaseActiveObject()
        {
            ActiveObject = null!;
        }

        public void RemoveActiveObject()
        {
            if(ListGravityObjects.Contains(ActiveObject))
            {
                ListGravityObjects.Remove(ActiveObject);
            }

            // Add to removal queue for proper removal
            RemovalQueue.Enqueue(ActiveObject);
            ActiveObject = null!;
        }

        public void RemoveAllMoveableObjects()
        {
            foreach(var obj in GetMoveableObjects())
            {
                RemovalQueue.Enqueue(obj);
            }
        }

        public void Tick(double elapsedTime)
        {
            accumulator += elapsedTime;

            // Avoid accumulator spiral of death by clamping
            if(accumulator > 0.1f)
            {
                accumulator = 0.1f;
            }

            while(accumulator > _dt)
            {
                BroadPhase_GeneratePairs();
                UpdatePhysics(_dt);
                ProcessRemovalQueue();
                accumulator -= _dt;
            }
        }

        #endregion

        #region Private Methods

        private void AddGravity(PhysicsObject obj, float dt)
        {
            obj.Velocity += GetGravityVector(obj) * dt;
        }

        private void ApplyConstants(PhysicsObject obj, float dt)
        {
            if(obj.Locked || obj.Sleeping)
            {
                return;
            }

            AddGravity(obj, dt);

            var friction = Friction * dt;

            // Store velocity in a temporary variable.
            var velocity = obj.Velocity;
            velocity.X += velocity.X == 0 ? 0 : velocity.X > 0 ? -friction : friction;
            velocity.Y += velocity.Y == 0 ? 0 : velocity.Y > 0 ? -friction : friction;
            // Assign the modified velocity back.
            obj.Velocity = velocity;

            if(obj.Center.Y > 2000 || obj.Center.Y < -2000 || obj.Center.X > 2000 || obj.Center.X < -2000)
            {
                RemovalQueue.Enqueue(obj);
            }
        }


        private Vector2 CalculatePointGravity(PhysicsObject obj)
        {
            var forces = Vector2.Zero;

            if(obj.Locked)
            {
                return forces;
            }

            foreach(var gpt in ListGravityObjects)
            {
                var diff = gpt.Center - obj.Center;
                PhysMath.RoundToZero(ref diff, 5F);

                // Apply inverse square law
                var falloffMultiplier = gpt.Mass / diff.LengthSquared();

                diff.X = (int)diff.X == 0 ? 0 : diff.X * falloffMultiplier;
                diff.Y = (int)diff.Y == 0 ? 0 : diff.Y * falloffMultiplier;

                if(diff.Length() > .005F)
                {
                    forces += diff;
                }
            }

            return forces;
        }

        private PhysicsObject CheckObjectAtPoint(Vector2 p)
        {
            foreach(var physicsObject in ListStaticObjects)
            {
                if(physicsObject.Contains(p))
                {
                    return physicsObject;
                }
            }

            return null!;
        }

        private Vector2 GetGravityVector(PhysicsObject obj)
        {
            return CalculatePointGravity(obj) + Gravity * GravityScale;
        }

        private void ProcessRemovalQueue()
        {
            while(RemovalQueue.Count > 0)
            {
                var obj = RemovalQueue.Dequeue();
                ListStaticObjects.Remove(obj);
                ListGravityObjects.Remove(obj);
            }
        }

        public void HandleConstraints(float dt)
        {
            foreach(var constraint in Constraints)
            {
                constraint.ApplyConstraint(dt);
            }
        }

        private void UpdatePhysics(float dt)
        {
            // Loop over physics iterations.
            for(var iter = 0; iter < PHYSICS_ITERATIONS; iter++)
            {
                //TODO: Constraints
                //HandleConstraints(dt);
                
                // Use a for-loop to iterate over collision pairs.
                for(var i = 0; i < ListCollisionPairs.Count; i++)
                {
                    var pair = ListCollisionPairs[i];
                    var objA = pair.A;
                    var objB = pair.B;

                    // Skip narrow phase if both objects are sleeping.
                    if(objA.Sleeping && objB.Sleeping)
                    {
                        continue;
                    }

                    // Cache shape references.
                    var shapeA = objA.Shape;
                    var shapeB = objB.Shape;

                    // Retrieve a manifold from the pool.
                    var m = _manifoldPool.Get();
                    var collision = false;

                    // Set ordering: if objA is a circle and objB is a box, swap them.
                    if(shapeA.ShapeType == IShape.Type.Circle && (
                        shapeB.ShapeType == IShape.Type.Box || shapeB.ShapeType == IShape.Type.Polygon
                    ))
                    {
                        m.A = objB;
                        m.B = objA;
                    }
                    else
                    {
                        m.A = objA;
                        m.B = objB;
                    }

                    // Cache again for m.A and m.B.
                    var shapeA2 = m.A.Shape;
                    var shapeB2 = m.B.Shape;

                    // Determine collision detection method based on shape types.
                    if(shapeA2.ShapeType == IShape.Type.Box || shapeA2.ShapeType == IShape.Type.Polygon)
                    {
                        if(shapeB2.ShapeType == IShape.Type.Box || shapeB2.ShapeType == IShape.Type.Polygon)
                        {
                            collision = Collision.PolygonVsPolygon(ref m);
                        }
                        else if(shapeB2.ShapeType == IShape.Type.Circle)
                        {
                            collision = Collision.PolygonVsCircle(ref m);
                        }
                    }
                    else if(shapeA2.ShapeType == IShape.Type.Circle && shapeB2.ShapeType == IShape.Type.Circle)
                    {
                        collision = Collision.CirclevsCircle(ref m);
                    }

                    // Resolve collision if detected.
                    if(collision)
                    {

                        // Here, instead of immediately waking sleeping objects, we compute a rough impulse magnitude.
                        // (For example, you might approximate impulse as the penetration depth times relative velocity along the normal.)
                        float relativeVel = Math.Abs(Vector2.Dot(m.B.Velocity - m.A.Velocity, m.Normal));
                        float impulseApprox = m.Penetration * relativeVel;

                        // Only wake if a significant impulse is delivered.
                        if(impulseApprox > WakeImpulseThreshold)
                        {
                            if(objA.Sleeping && !objA.Locked)
                            {
                                objA.Wake();
                            }
                            if(objB.Sleeping && !objB.Locked)
                            {
                                objB.Wake();
                            }
                        }

                        // Add to object contact points once per physics tick
                        if(iter == PHYSICS_ITERATIONS - 1)
                        {
                            m.A.AddContact(m.B, m.ContactPoint, m.Normal);
                            m.B.AddContact(m.A, m.ContactPoint, -m.Normal);
                        }

                        // Resolve Collision
                        Collision.ResolveCollisionRotational(ref m);
                        Collision.PositionalCorrection(ref m);
                        Collision.AngularPositionalCorrection(ref m);
                    }
                    else
                    {
                        // Return manifold to pool if no collision.
                        _manifoldPool.Return(m);
                    }
                }
            }

            // Process static objects.
            for(var i = 0; i < ListStaticObjects.Count; i++)
            {
                var staticObj = ListStaticObjects[i];
                ApplyConstants(staticObj, dt);
                staticObj.Update(dt);
            }
        }



        #endregion

        #region Broad Phase Collision Detection

        private void BroadPhase_GeneratePairs()
        {
            // Reuse the ListCollisionPairs (clear it first)
            ListCollisionPairs.Clear();

            // Clear reusable structures to avoid allocations
            _spatialHash.Clear();
            _pairSet.Clear();

            float cellSize = SpatialHashCellSize;

            // Populate the spatial hash.
            foreach(var obj in ListStaticObjects)
            {
                // Get min / max extents, divide by cellSize for grid coordinates.
                var minX = (int)Math.Floor(
                    obj.Rect.Position.X / cellSize
                );
                var minY = (int)Math.Floor(
                    obj.Rect.Position.Y / cellSize
                );
                var maxX = (int)Math.Floor(
                    (obj.Rect.Position.X + obj.Rect.Width) / cellSize
                );
                var maxY = (int)Math.Floor(
                    (obj.Rect.Position.Y + obj.Rect.Height) / cellSize
                );

                for(var x = minX; x <= maxX; x++)
                {
                    for(var y = minY; y <= maxY; y++)
                    {
                        var key = (x, y);
                        if(!_spatialHash.TryGetValue(key, out var cellList))
                        {
                            cellList = new();
                            _spatialHash[key] = cellList;
                        }
                        cellList.Add(obj);
                    }
                }
            }

            // Use the reusable hash set to avoid duplicate pairs.
            foreach(var cell in _spatialHash.Values)
            {
                var count = cell.Count;
                if(count <= 1)
                {
                    continue;
                }

                for(var i = 0; i < count - 1; i++)
                {
                    for(var j = i + 1; j < count; j++)
                    {
                        var objA = cell[i];
                        var objB = cell[j];

                        // Skip pairs where both objects are sleeping.
                        if(objA.Sleeping && objB.Sleeping)
                            continue;

                        // Add the pair if it has not already been processed.
                        if(_pairSet.Add((objA, objB)))
                        {
                            ListCollisionPairs.Add(new CollisionPair(objA, objB));
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Helper Classes

        /// <summary>
        /// Custom comparer for collision pair tuples so that the order of objects does not matter.
        /// </summary>
        private class PhysicsObjectPairComparer : IEqualityComparer<(PhysicsObject, PhysicsObject)>
        {
            public bool Equals((PhysicsObject, PhysicsObject) x, (PhysicsObject, PhysicsObject) y)
            {
                return (ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2)) ||
                       (ReferenceEquals(x.Item1, y.Item2) && ReferenceEquals(x.Item2, y.Item1));
            }

            public int GetHashCode((PhysicsObject, PhysicsObject) pair)
            {
                // XOR is order-independent.
                return pair.Item1.GetHashCode() ^ pair.Item2.GetHashCode();
            }
        }

        #endregion
    }
}
