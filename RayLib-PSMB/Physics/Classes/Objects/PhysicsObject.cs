using System.Numerics;
using PSMB.Physics.Shapes;
using PSMB.Physics.Structs;
using Raylib_cs;

namespace PSMB.Physics.Objects
{
    public class PhysicsObject : Object
    {
        private static float SupportObjectNormalThreshold = 0.2f;
        
        public IShape Shape { get; protected set; }
        public Rectangle Rect => Shape.GetAABB(Center, Angle);
        public Vector2 Center => Position + Size / 2;
        public Vector2 Size
        {
            get => Shape.Size;
            set => Shape.Size = value;
        }
        public Vector2 Velocity { get; set; }
        public float Restitution { get; set; }
        public float Mass { get; protected set; }
        public float IMass { get; protected set; }
        public bool Locked { get; set; }
        public bool CanRotate { get; internal set; }
        public readonly List<PhysicsObject> ConnectedObjects = new();
        public float AngularVelocity { get; set; }
        public float Inertia { get; private set; }
        public float IInertia { get; private set; }

        // Friction, in newtons
        public float Friction { get; set; } = 0.5f;

        /// <summary>
        /// Orientation in radians.
        /// </summary>
        //public float Angle { get; set; }
        public float Angle
        {
            get => Rotation;
            set => Rotation = value;
        }

        // --- New caching and events ---
        // Event fired when a new contact point is added.
        public event Action<PhysicsObject, (Vector2, Vector2)> ContactPointAdded;
        // Event fired when a contact point is removed.
        public event Action<PhysicsObject, (Vector2, Vector2)> ContactPointRemoved;
        
        // Current contact points. The key is the object, and the value is a tuple of (point, normal).
        private readonly Dictionary<PhysicsObject, (Vector2, Vector2)> _contactPoints = new();
        // Cached contacts from the previous update.
        private readonly Dictionary<PhysicsObject, (Vector2, Vector2)> _previousContactPoints = new();

        // --- Sleep/Wake state management ---
        public bool Sleeping { get; private set; } = false;
        private float sleepTimer = 0f;

        // Store the previous center to compute displacement.
        private Vector2 _prevPosition;

        // Support network tracking - objects that this object is resting on
        private readonly HashSet<PhysicsObject> _supportingObjects = new();
        // Objects that are being supported by this object
        private readonly HashSet<PhysicsObject> _supportedObjects = new();
        // Persistent contact points for sleeping objects
        private readonly Dictionary<PhysicsObject, (Vector2, Vector2)> _sleepingContactPoints = new();
        // Position when going to sleep, to detect if supporting objects have moved
        private Vector2 _sleepPosition;
        // Whether to validate supports next frame
        private bool _validateSupportsNextFrame = false;

        public PhysicsObject(string name, IShape.Type type) : base(name)
        {
            if(type == IShape.Type.Box)
            {
                Shape       = new BoxPhysShape(1, 1);
            }
            else if(type == IShape.Type.Circle)
            {
                Shape       = new CirclePhysShape(1);
            }
            else if(type == IShape.Type.Polygon)
            {
                Shape       = new PolygonPhysShape(new List<Vector2>()
                {
                    new(0, 0), new(1, 0), new(0, 1), 
                });
            }
            
            _prevPosition   = Vector2.Zero; // initialize previous center
            
            Angle           = 0;
            Velocity        = Vector2.Zero;
            Restitution     = 0f;
            Locked          = false;
            AngularVelocity = 0;
            CanRotate       = true;
            ChangeMass(1f);
            
            PhysicsSystem.ListStaticObjects.Add(this);
        }

        ~PhysicsObject()
        {
            PhysicsSystem.ListStaticObjects.Remove(this);
            all.Remove(this);
        }

        /// <summary>
        /// Runs the object's update logic.
        /// </summary>
        public void Update(float dt)
        {
            if(!Sleeping)
            {
                Move(dt);
                UpdateRotation(dt);
                
                // Notify any objects we're supporting if we moved significantly
                if(_supportedObjects.Count > 0 && (Center - _prevPosition).Length() > 0.01f)
                {
                    foreach(var supported in _supportedObjects)
                    {
                        supported._validateSupportsNextFrame = true;
                    }
                }
            }
            else if(_validateSupportsNextFrame)
            {
                // Check if supports are still valid
                ValidateSupports();
                _validateSupportsNextFrame = false;
            }
            
            // Update sleep state based on actual displacement (movement) rather than instantaneous velocity.
            UpdateSleepState(dt);
            UpdateContactPoints();
            // Update the previous center for next frame comparison.
            _prevPosition = Center;
        }

        /// <summary>
        /// Checks whether the object has moved less than the threshold between updates.
        /// If so, accumulates a timer. Once the timer exceeds a set threshold, the object is put to sleep.
        /// If movement exceeds the threshold, the timer is reset.
        /// </summary>
        public void UpdateSleepState(float dt)
        {
            if(Locked)
            {
                sleepTimer = 0f;
                return;
            }

            // Compute displacement as the distance between the current center and previous center.
            float displacement = (Center - _prevPosition).Length();

            // Compare displacement against the threshold and also check angular movement.
            if(displacement < PhysicsSystem.LinearSleepThreshold 
            && Math.Abs(AngularVelocity) < PhysicsSystem.AngularSleepThreshold
            )
            {
                sleepTimer += dt;
                if(sleepTimer >= PhysicsSystem.SleepTimeThreshold)
                {
                    Sleep();
                }
            }
            else
            {
                sleepTimer = 0f;
                if(Sleeping)
                {
                    Wake();
                }
            }
        }

        /// <summary>
        /// Validates if supports are still in place. If not, wakes the object.
        /// </summary>
        private void ValidateSupports()
        {
            if(!Sleeping)
            {
                return;
            }

            var shouldWake = false;
            
            // If we have no supports, we should wake up
            if(_supportingObjects.Count == 0)
            {
                shouldWake = true;
            }
            else
            {
                // Check if any supporting object has moved away
                foreach(var supportingObj in _supportingObjects)
                {
                    if(_sleepingContactPoints.TryGetValue(supportingObj, out var contactData))
                    {
                        var contactPoint = contactData.Item1;
                        var normal = contactData.Item2;
                        
                        // Simple check: is the point still contained within the supporting object?
                        // A more robust check would recompute the actual contact point
                        if(!supportingObj.Contains(contactPoint))
                        {
                            shouldWake = true;
                            break;
                        }
                    }
                    else
                    {
                        // No contact data for a supporting object is suspicious, wake up
                        shouldWake = true;
                        break;
                    }
                }
            }

            if(shouldWake)
            {
                Wake();
            }
        }

        /// <summary>
        /// Puts the object to sleep: sets Sleeping to true, and zeroes out velocity and angular velocity.
        /// </summary>
        public void Sleep()
        {
            if(Sleeping)
            {
                return;
            }
            Sleeping        = true;
            Velocity        = Vector2.Zero;
            AngularVelocity = 0;
            
            // Store the sleep position
            _sleepPosition = Center;
            
            // Preserve current contacts for sleeping state
            _sleepingContactPoints.Clear();
            foreach(var contact in _previousContactPoints)
            {
                _sleepingContactPoints[contact.Key] = contact.Value;
            }
        }

        /// <summary>
        /// Wakes the object from sleep.
        /// </summary>
        public void Wake()
        {
            if(!Sleeping)
            {
                return;
            }
            Sleeping = false;
            sleepTimer = 0f;
            
            // Clear the sleeping contacts
            _sleepingContactPoints.Clear();
            
            // Recursively wake objects resting on us
            foreach(var supported in _supportedObjects)
            {
                if(supported.Sleeping)
                {
                    supported.Wake();
                }
            }
        }

        /// <summary>
        /// Safe method to add a contact point to the dictionary.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="point"></param>
        /// <param name="normal"></param>
        public void AddContact(PhysicsObject obj, Vector2 point, Vector2 normal)
        {
            if(_contactPoints.ContainsKey(obj))
            {
                return;
            }

            _contactPoints[obj] = (point, normal);
                
            // If normal points upward, this object might be supporting us
            if(normal.Y > SupportObjectNormalThreshold) // Y is negative when pointing up in many engines
            {
                _supportingObjects.Add(obj);
                obj._supportedObjects.Add(this);
            }
        }

        /// <summary>
        /// Retrieve the most recent contact points.
        /// </summary>
        /// <returns></returns>
        public Dictionary<PhysicsObject, (Vector2, Vector2)> GetContacts() =>
            _previousContactPoints;

        /// <summary>
        /// Compares the current contact points to the previous frame’s contact points,
        /// fires events for added and removed contacts, updates the cache, and then clears the current list.
        /// This method is written to be allocation-efficient.
        /// </summary>
        public void UpdateContactPoints()
        {
            // If both the current and previous contacts are empty, there's nothing to do.
            if(_contactPoints.Count == 0 && _previousContactPoints.Count == 0)
            {
                return;
            }

            // Cache the event delegates locally to avoid repeated null checks.
            var addedHandler = ContactPointAdded;
            var removedHandler = ContactPointRemoved;

            // Fire events for newly added contacts.
            foreach(var kv in _contactPoints)
            {
                if(!_previousContactPoints.ContainsKey(kv.Key))
                {
                    addedHandler?.Invoke(kv.Key, kv.Value);
                }
            }

            // Fire events for contacts that were removed.
            foreach(var kv in _previousContactPoints)
            {
                if(!_contactPoints.ContainsKey(kv.Key))
                {
                    removedHandler?.Invoke(kv.Key, kv.Value);
                    
                    // Update support networks when contacts are lost
                    if(_supportingObjects.Contains(kv.Key))
                    {
                        _supportingObjects.Remove(kv.Key);
                        kv.Key._supportedObjects.Remove(this);
                    }
                }
            }

            // Update the cached contacts
            _previousContactPoints.Clear();
            foreach(var kv in _contactPoints)
            {
                _previousContactPoints.Add(kv.Key, kv.Value);
            }

            // Clear the current contact points for the next update cycle.
            _contactPoints.Clear();
        }

        /// <summary>
        /// Moves the object based on its velocity and updates its AABB.
        /// </summary>
        public virtual void Move(float dt)
        {
            if(Locked)
            {
                return;
            }

            RoundSpeedToZero();
            Position += Velocity * dt;
        }

        protected void RoundSpeedToZero()
        {
            if(Math.Abs(Velocity.X) + Math.Abs(Velocity.Y) < 0.01f)
            {
                Velocity = Vector2.Zero;
            }
        }

        /// <summary>
        /// Directly translates the object by a given vector.
        /// </summary>
        public virtual void Move(Vector2 dVector)
        {
            if(Locked)
            {
                return;
            }
            
            Position += dVector;
        }

        /// <summary>
        /// Updates rotation. By default, does nothing.
        /// </summary>
        public virtual void UpdateRotation(float dt)
        {
            if(!CanRotate || Locked)
            {
                return;
            }

            Angle += AngularVelocity * dt;
            AngularVelocity *= 0.999f; // Apply angular damping.
            if(Math.Abs(AngularVelocity) < 0.001f)
            {
                AngularVelocity = 0;
            }
        }

        /// <summary>
        /// Determines whether a given point (in world coordinates) lies within the object.
        /// This method delegates to the shape's own containment logic using the object's center and rotation.
        /// </summary>
        public bool Contains(Vector2 point)
        {
            return Shape.Contains(point, Center, Angle);
        }
        
        public void ChangeMass(float mass)
        {
            if(mass <= 0)
            {
                Mass  = Shape.GetArea();
            }
            
            Mass     = mass;
            IMass    = 1 / Mass;
            Inertia  = Shape.GetMomentOfInertia(Mass);
            IInertia = (Inertia != 0) ? 1 / Inertia : 0;
        }
    }
}