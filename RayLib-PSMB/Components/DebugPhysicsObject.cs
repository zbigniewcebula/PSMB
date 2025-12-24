using PSMB.Physics.Objects;
using PSMB.Physics.Shapes;
using Raylib_cs;

namespace PSMB
{
	public class DebugPhysicsObject : Component, IRenderable
	{
		public void Render(Renderer renderer) {}
		public void RenderDebug(Renderer renderer)
		{	
			if(Parent is not PhysicsObject)
			{
				Logger.Error("DebugPhysicsObject", "Parent object must be a PhysicsObject");
				return;
			}
			
			var obj = Parent as PhysicsObject;
			if(obj.Shape.ShapeType == IShape.Type.Circle)
			{
				renderer.DrawCircle(obj.Center, obj.Size.X, Color.Green);
			}
			else
			{
				renderer.DrawRectLines(obj.Rect, 1f, Color.Green);
			}

			var end = obj.Center + obj.Velocity;
			renderer.DrawLine(obj.Center, end, 1f, Color.Orange);
		}
	}
}