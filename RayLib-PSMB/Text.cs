using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class Text : Object, IRenderable
	{
		public float Spacing { get; set; } = 8;
		public int FontSize { get; set; }
		public Color Color { get; set; } = Raylib_cs.Color.White;
		public Font Font { get; set; } = Raylib.GetFontDefault();
		public string Content { get; set; }
		
		public Text(string name, string content = null, int fontSize = 16) : base(name)
		{
			Content = string.IsNullOrWhiteSpace(content)? string.Empty : content;
			FontSize = fontSize;
			Spacing = fontSize >> 2;
		}
		
		public void Render(Renderer renderer)
		{
			if(string.IsNullOrWhiteSpace(Content)) return;
			if(FontSize <= 0) return;
			float scaleLength = Scale.Length();
			if(scaleLength <= 0) return;
			if(Color.A <= 0) return;
			
			Raylib.DrawTextPro(
				Font,
				Content,
				Position,
				Vector2.Zero,
				Rotation,
				FontSize * scaleLength,
				Spacing,
				Color
			);
		}

		public void RenderDebug(Renderer renderer)
		{
			int     posX     = (int)Position.X;
			int     posY     = (int)Position.Y;
			Vector2 measured = Raylib.MeasureTextEx(Font, Content, FontSize, Spacing);
			int     W        = (int)measured.X;
			int     H        = (int)measured.Y;
			Raylib.DrawCircle(posX, posY, 1f, Color.Red);
			Raylib.DrawRectangleLines(posX, posY, W, H, Color.Red);
		}
	}
	
}