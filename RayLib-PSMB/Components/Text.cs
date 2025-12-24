using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class Text : Component, IRenderable
	{
		public float Spacing { get; set; } = 8;
		public int FontSize { get; set; }
		public Color Color { get; set; } = Color.White;
		public Font Font { get; set; } = Raylib.GetFontDefault();
		public string Content { get; set; }
		
		public void Render(Renderer renderer)
		{
			if(string.IsNullOrWhiteSpace(Content)) return;
			if(FontSize <= 0) return;
			float scaleLength = Parent.Scale.Length();
			if(scaleLength <= 0) return;
			if(Color.A <= 0) return;

			var pos = renderer.PointToScreenView(Parent.Position);
			
			Raylib.DrawTextPro(
				Font,
				Content,
				pos,
				Vector2.Zero,
				Parent.Rotation,
				FontSize * scaleLength,
				Spacing,
				Color
			);
		}

		public void RenderDebug(Renderer renderer)
		{
			var     pos= renderer.PointToScreenView(Parent.Position);
			Vector2 measured = Raylib.MeasureTextEx(Font, Content, FontSize, Spacing);
			int     W        = (int)measured.X;
			int     H        = (int)measured.Y;
			renderer.DrawCircle(pos, 1f, Color.Red);
			renderer.DrawRectLines(new(pos, new(W, H)), 1f, Color.Red);
		}
	}
}