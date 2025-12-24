using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class Sprite : Component, IRenderable
	{
		public Texture2D Texture { get; private set; }
		public Color Color { get; set; } = Color.White;
		public Vector2 Pivot { get; set; } = Vector2.One / 2;
		public Rectangle SourceRect { get; set; }
		public Vector2 Size { get; set; } = Vector2.One;
		
		public void Assign(Texture2D texture)
		{
			Texture    = texture;
			SourceRect = new(0, 0, Texture.Width, Texture.Height);
			Size = new(Texture.Width, Texture.Height);
		}
		
		public void Render(Renderer renderer)
		{
			var W = (int)(Size.X * Parent.Scale.X);
			var H = (int)(Size.Y * Parent.Scale.Y);
			var pos = renderer.PointToScreenView(new(Parent.Position.X, Parent.Position.Y));
			Raylib.DrawTexturePro(
				Texture,
				SourceRect,
				new(pos.X, pos.Y, W, H),
				new Vector2(Pivot.X, 1 - Pivot.Y) * new Vector2(W, H),
				Parent.Rotation,
				Color
			);
		}

		public void RenderDebug(Renderer renderer)
		{
			return;
			var W = (int)(Size.X * Parent.Scale.X);
			var H = (int)(Size.Y * Parent.Scale.Y);
			var pos = renderer.PointToScreenView(new(Parent.Position.X, Parent.Position.Y));
			float zoom = renderer.Camera.Zoom;
			renderer.DrawRectLines(
				new(Parent.Position, new Vector2(W, H)),
				1f / zoom,
				Color.Red
			);
			renderer.DrawCircle(pos, 2f / zoom, Color.Yellow);

			var pivot = new Vector2(Pivot.X, 1 - Pivot.Y) * new Vector2(W, H);
			pivot.X += pos.X;
			pivot.Y += pos.Y;
			renderer.DrawCircle(pos, 1f / zoom, Color.Red);
		}
	}
}