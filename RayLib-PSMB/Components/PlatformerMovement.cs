using System.Numerics;
using PSMB.Physics.Objects;
using Raylib_cs;

namespace PSMB
{
	public class PlatformerMovement : Component, IUpdatable
	{
		public float HorizontalDamping { get; set; } = 2f;
		
		public float HorizontalAcceleration { get; set; } = 10f;

		private float horizontalVel = 0;
		
		public void Update(float delta)
		{
			if(Parent is not PhysicsObject)
			{
				Logger.Error("PlatformMovement", "Not an PhysicsObject");
				return;
			}

			if(Raylib.IsKeyDown(KeyboardKey.A))
			{
				horizontalVel -= HorizontalAcceleration;
			}
			
			if(Raylib.IsKeyDown(KeyboardKey.D))
			{
				horizontalVel += HorizontalAcceleration;
			}
			
			if(Utils.Approximately(horizontalVel, 0) == false)
			{
				horizontalVel -= Math.Sign(horizontalVel) * HorizontalDamping;
			}
			
			//var moveVec = new Vector2(horizontalVel, 0);
		}
	}
}