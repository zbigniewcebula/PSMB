using System.Numerics;

namespace PSMB.Physics.Structs
{
    /// <summary>
    /// Axis Aligned bounding box struct that represents the position of an object within a coordinate system.
    /// </summary>
    public struct Rect
    {
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        
        public float X
        {
            get => Position.X; 
            set => Position = new(value, Position.Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new(Position.X, value);
        }
        
        public float Width
        {
            get => Size.X;
            set => Size = new(value, Size.Y);
        }

        public float Height
        {
            get => Size.Y;
            set => Size = new(Size.X, value);
        }
        
        public Vector2 Min => Position + Size;
        public Vector2 Max => Position + Size;
        
        public float Area => (Max.X - Min.X) * (Max.Y - Min.Y);
        
        public Rect() {}

        public Rect(Vector2 position, Vector2 size)
        {
            Position = position;
            Size = size;
        }

        public Rect(Vector2 position, float size)
        {
            Position = position;
            Size = new Vector2(size, size);
        }
    }
}
