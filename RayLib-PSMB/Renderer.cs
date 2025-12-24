using System.Numerics;
using System.Runtime.Intrinsics.X86;
using Raylib_cs;
using Camera2D = Raylib_cs.Camera2D;
using Color = Raylib_cs.Color;
using Raylib = Raylib_cs.Raylib;

namespace PSMB
{
	public class Renderer
	{
		public bool Closing => closing || Raylib.WindowShouldClose();

		public Camera Camera { get; set; }
		
		public bool ShowFPS { get; set; } = false;
		
		public Vector2 Size { get; private set; }
		
		public Color BackgroundColor { get; set; } = Color.White;

		private bool closing = false;

		public Renderer(
			string windowTitle, Vector2 windowSize,
			int targetFPS = 60, bool fullscreen = false
		) {
			Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
			Raylib.InitWindow(
				(int)windowSize.X, (int)windowSize.Y, windowTitle
			);
			Raylib.SetTargetFPS(targetFPS);
			
			if(fullscreen)
			{
				Raylib.MaximizeWindow();
				
				windowSize = new(
					Raylib.GetScreenWidth(), Raylib.GetScreenHeight()
				);
			}

			Size = windowSize;
		}

		~Renderer()
		{
			Raylib.CloseWindow();
		}

		public void Render(float delta, Editor editor = null!)
		{
			Raylib.ClearBackground(BackgroundColor);
			
			Raylib.BeginDrawing();
			if(Camera != null)
			{
				Raylib.BeginMode2D(Camera.Camera2D);
			}
			{
				ObjectUtils.RenderAll(this);

				if(editor != null && editor.GizmosEnabled)
				{
					ObjectUtils.RenderDebugAll(this);
				}
			}
			if(Camera != null)
			{
				Camera.Render(this);
				Raylib.EndMode2D();
			}
			
			if(ShowFPS)
			{
				Raylib.DrawFPS(0, 0);
			}

			if(editor != null)
			{
				editor?.Render(this, delta);
			}
			
			Raylib.EndDrawing();
		}
		
		public Rectangle GetViewport()
		{
			var cam = Camera.Camera2D;
			
			// 1. Screen center before applying camera offset
			//float viewWidth  = renderTexture.Texture.Width  / cam.Zoom;
			//float viewHeight = renderTexture.Texture.Height / cam.Zoom;
			float viewWidth  = Size.X  / cam.Zoom;
			float viewHeight = Size.Y / cam.Zoom;

			// 2. Because target is the world-space point at screen offset,
			//    the visible range is centered around (target - offset/zoom)
			float worldX = Camera.Parent.Position.X / cam.Zoom;
			float worldY = Camera.Parent.Position.Y / cam.Zoom;

			return new(worldX, worldY, viewWidth, viewHeight);
		}

		public Vector2 PointToScreenView(Vector2 point)
		{
			return new(
				point.X,
				Size.Y - point.Y
			);
		}

		public void DrawLine(Vector2 start, Vector2 end, float thickness = 1f, Color? color = null)
		{
			if(color == null)
			{
				color = Color.White;
			}
			
			start = PointToScreenView(start);
			end   = PointToScreenView(end);
			Raylib.DrawLineEx(
				start, end,
				thickness,
				color.Value
			);
		}
		
		public void DrawRectLines(Rectangle rect, float thickness = 1f, Color? color = null)
		{
			if(color == null)
			{
				color = Color.White;
			}
			
			var pos = PointToScreenView(rect.Position);
			pos.Y -= rect.Size.Y;
			Raylib.DrawRectangleLinesEx(
				new(pos, rect.Size),
				thickness,
				color.Value
			);
		}

		public void DrawEllipseLines(Vector2 center, Vector2 radius, Color? color = null)
		{
			if(color == null)
			{
				color = Color.White;
			}
			
			var pos = PointToScreenView(center);
			Raylib.DrawEllipseLines(
				(int)pos.X, (int)pos.Y, 
				radius.X,  radius.Y, 
				color.Value
			);
		}
		
		public void DrawCircle(Vector2 center, float radius, Color? color = null)
		{
			DrawEllipseLines(center, new Vector2(radius, radius), color);
		}

		public void DrawText(string text, Vector2 position, int fontSize = 14, Color? color = null)
		{
			if(color == null)
			{
				color = Color.White;
			}
			
			var pos = PointToScreenView(position);
			Raylib.DrawText(
				text, 
				(int)pos.X, (int)pos.Y, 
				fontSize, 
				color.Value
			);
		}
	}
}