using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class Physics : Component, IUpdatable
	{
		public const float BASE_GRAVITY = 9.81f;
		
		public Vector2 Gravity { get; set; } = new(0, -BASE_GRAVITY);
		
		public Rectangle CollisionBox { get; set; } = new(0, 0, 1, 1);
		public CollisionType CollisionType { get; set; }
		
		public void Update(float delta)
		{
			var newPos = Parent.Position + Gravity * delta;
			Parent.Position = newPos;
		}
	}

	public enum CollisionType
	{
		None,
		Box,
		Circle
	}
}