using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class Follower : Component, IUpdatable
	{
		public Object FollowTarget { get; set; }
		
		public Vector2 Offset { get; set; }
		public bool OffsetXEnabled { get; set; } = true;
		public bool OffsetYEnabled { get; set; } = true;
		
		public Rectangle ClampBounds { get; set; } = new();
		
		public void Update(float delta)
		{
			if(FollowTarget == null && Parent != null)
			{
				return;
			}

			var newPos = FollowTarget.Position + Offset;
			if(OffsetXEnabled == false)
			{
				newPos.X = Parent.Position.X;
			}
			if(OffsetYEnabled == false)
			{
				newPos.Y = Parent.Position.Y;
			}
			
			if(ClampBounds.IsZero() == false)
			{
				newPos = ClampBounds.RestrictPoint(newPos);
			}

			Parent.Position = newPos;
		}
	}
}