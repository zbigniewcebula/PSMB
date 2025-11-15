using System.Numerics;
using Raylib_cs;
using Camera2D = Raylib_cs.Camera2D;
using Color = Raylib_cs.Color;
using Raylib = Raylib_cs.Raylib;

namespace PSMB
{
	public class Renderer
	{
		private const int INITIAL_RENDER_QUEUE_SIZE = 128;

		public bool Closing => closing || Raylib.WindowShouldClose();

		public Camera Camera { get; private set; }
		private Object camera = null;
		
		public bool FPS { get; set; } = false;
		public bool Debug { get; set; } = false;
		public bool IMGUIEnabled { get; set; }
		public Vector2 Size { get; private set; }
		
		public Color BackgroundColor { get; set; } = Color.White;
		
		private RenderTexture2D renderTexture;
		private IMGUI imgui;

		private bool closing = false;

		public Renderer(string windowTitle, Vector2 windowSize, int targetFPS = 60)
		{
			Size = windowSize;
			Raylib.InitWindow(
				(int)windowSize.X, (int)windowSize.Y, windowTitle
			);
			Raylib.SetTargetFPS(targetFPS);
			
			renderTexture = Raylib.LoadRenderTexture(
				(int)windowSize.X, (int)windowSize.Y
			);

			camera = new("Camera");
			Camera = camera.AddComponent<Camera>();
			
			imgui = new();

			IMGUIEnabled   = true;
			Camera.FreeCam = true;
		}

		~Renderer()
		{
			imgui = null;
			Raylib.UnloadRenderTexture(renderTexture);
			Raylib.CloseWindow();
		}

		public void Render(float delta)
		{
			Raylib.ClearBackground(BackgroundColor);
			Raylib.BeginDrawing();
			Raylib.BeginMode2D(Camera.Camera2D);
			{
				ObjectUtils.RenderAll(this);

				if(Debug)
				{
					ObjectUtils.RenderDebugAll(this);
				}
			}
			Raylib.EndMode2D();	
			
			if(FPS)
			{
				Raylib.DrawFPS(0, 0);
			}
			if(IMGUIEnabled)
			{
				imgui.Render(this, delta);
			}
			Raylib.EndDrawing();
		}
		
		public Rectangle GetViewport()
		{
			var cam = Camera.Camera2D;
			
			// 1. Screen center before applying camera offset
			float viewWidth  = renderTexture.Texture.Width  / cam.Zoom;
			float viewHeight = renderTexture.Texture.Height / cam.Zoom;

			// 2. Because target is the world-space point at screen offset,
			//    the visible range is centered around (target - offset/zoom)
			float worldX = Camera.Parent.Position.X / cam.Zoom;
			float worldY = Camera.Parent.Position.Y / cam.Zoom;

			return new(worldX, worldY, viewWidth, viewHeight);
		}

		public Vector2 PointToScreenView(Vector2 point)
		{
			return point with {Y = renderTexture.Texture.Height - point.Y};
		}
	}
}