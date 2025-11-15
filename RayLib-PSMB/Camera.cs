using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class Camera : Component, IUpdatable, IRenderable
	{
		public Camera2D Camera2D => camera2D;

		public float Zoom
		{
			get => camera2D.Zoom;
			set => camera2D.Zoom = value;
		}

		public bool FreeCam { get; set; } = false;
		
		private Camera2D camera2D;

		private float freecamSpeed = 5000f;

		public override void OnCreate()
		{
			Zoom = 1.0f;
		}

		public void Update(float delta)
		{
			if(FreeCam)
			{
				if(Raylib.IsKeyDown(KeyboardKey.Left))
				{
					Parent.Position -= new Vector2(delta * freecamSpeed, 0);
					Console.WriteLine($"Cam pos: {Parent.Position.X};{Parent.Position.Y}");
				}
				if(Raylib.IsKeyDown(KeyboardKey.Right))
				{
					Parent.Position += new Vector2(delta * freecamSpeed, 0);
					Console.WriteLine($"Cam pos: {Parent.Position.X};{Parent.Position.Y}");
				}
				if(Raylib.IsKeyDown(KeyboardKey.Down))
				{
					Parent.Position -= new Vector2(0, delta * freecamSpeed);
					Console.WriteLine($"Cam pos: {Parent.Position.X};{Parent.Position.Y}");
				}
				if(Raylib.IsKeyDown(KeyboardKey.Up))
				{
					Parent.Position += new Vector2(0, delta * freecamSpeed);
					Console.WriteLine($"Cam pos: {Parent.Position.X};{Parent.Position.Y}");
				}
				if(Raylib.IsKeyDown(KeyboardKey.PageUp))
				{
					freecamSpeed += delta * 1000;
					Console.WriteLine($"+FreecamSpeed {freecamSpeed}");
				}
				if(Raylib.IsKeyDown(KeyboardKey.PageDown))
				{
					freecamSpeed -= delta * 1000;
					Console.WriteLine($"-FreecamSpeed {freecamSpeed}");
				}
				camera2D.Offset   = new(-Parent.Position.X, Parent.Position.Y);
				camera2D.Rotation = Parent.Rotation;
			}
		}

		public void Render(Renderer renderer)
		{
			//
		}
		public void RenderDebug(Renderer renderer)
		{
			Raylib.DrawText(
				$"({-camera2D.Offset.X:F2}; {camera2D.Offset.Y:F2}) x{Zoom:F2}", 
				((int)-camera2D.Offset.X) + 100, ((int)-camera2D.Offset.Y) + 10,
				24, Color.Black
			);
		}
	}
}