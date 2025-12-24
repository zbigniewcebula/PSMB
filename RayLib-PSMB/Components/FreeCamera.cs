using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class FreeCamera : Component, IUpdatable 
	{
		private Camera camera;

		private float freecamSpeed = 5000f;

		public override void OnCreate()
		{
			camera = Parent.GetComponent<Camera>();
		}

		public void Update(float delta)
		{
			if(camera == null)
			{
				return;
			}
			
			if(Raylib.IsKeyDown(KeyboardKey.Left))
			{
				Parent.Position -= new Vector2(delta * freecamSpeed, 0);
				Logger.Log("Camera", $"New pos: {Parent.Position.X};{Parent.Position.Y}");
			}
			if(Raylib.IsKeyDown(KeyboardKey.Right))
			{
				Parent.Position += new Vector2(delta * freecamSpeed, 0);
				Logger.Log("Camera", $"New pos: {Parent.Position.X};{Parent.Position.Y}");
			}
			if(Raylib.IsKeyDown(KeyboardKey.Down))
			{
				Parent.Position -= new Vector2(0, delta * freecamSpeed);
				Logger.Log("Camera", $"New pos: {Parent.Position.X};{Parent.Position.Y}");
			}
			if(Raylib.IsKeyDown(KeyboardKey.Up))
			{
				Parent.Position += new Vector2(0, delta * freecamSpeed);
				Logger.Log("Camera", $"New pos: {Parent.Position.X};{Parent.Position.Y}");
			}
			if(Raylib.IsKeyDown(KeyboardKey.PageUp))
			{
				freecamSpeed += delta * 1000;
				Logger.Log("Camera", $"+FreecamSpeed: {freecamSpeed}");
			}
			if(Raylib.IsKeyDown(KeyboardKey.PageDown))
			{
				freecamSpeed -= delta * 1000;
				Logger.Log("Camera", $"-FreecamSpeed: {freecamSpeed}");
			}

			float scroll = Raylib.GetMouseWheelMove() * 0.01f; 
			if(scroll > 0)
			{
				camera.Zoom += scroll;
				Logger.Log("FreeCamera", $"Zoom: {camera.Zoom}");
			} 
			else if(scroll < 0)
			{
				camera.Zoom += scroll;
				Logger.Log("FreeCamera", $"Zoom: {camera.Zoom}");
			}
		}
	}
}