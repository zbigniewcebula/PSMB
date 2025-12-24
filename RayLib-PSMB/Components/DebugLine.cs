using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class DebugLine : Component, IRenderable
	{
		public Vector2 From { get; set; }
		public Vector2 To { get; set; }
		public Color Color { get; set; } = Color.Red;
		public float Thickness { get; set; } = 1;
		
		public void Render(Renderer renderer) {}
		public void RenderDebug(Renderer renderer)
		{
			renderer.DrawLine(From, To, Thickness, Color);
		}
	}
}