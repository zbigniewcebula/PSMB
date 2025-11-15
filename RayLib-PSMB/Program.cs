using System.Numerics;
using Raylib_cs;

namespace PSMB;

internal class Program
{
	[System.STAThread]
	public static void Main(string[] args)
	{
		var windowSize = new Vector2(256 * 4, 240 * 4);
		Renderer render = new(
			"PSMB", windowSize
		) {
			BackgroundColor = Color.Black,
		};
		render.Debug = false;
		render.FPS = true;

		GameMap  mapHandler = null;
		{
			render.Camera.Parent.Position = new(0, windowSize.Y + 64);
			
			Object map = new("Map")
			{
				Scale  = Vector2.One * 64,
				Position = new(0, 0),
			};
			mapHandler = map.AddComponent<GameMap>();
			if(mapHandler.LoadFromFile("assets/map/map01.tmx") == false)
			{
				Console.WriteLine("Map loading failed! Path: 'assets/map/map01.tmx'");
				return;
			}
			mapHandler.HotReload = true;
			
			Object mario  = new("Mario")
			{
				Position = new(100, 1300),
				Scale    = Vector2.One * 8,
			};
			//var physics = mario.AddComponent<Physics>();
			var sprite = mario.AddComponent<Sprite>();
			sprite.Pivot = new(0, 0);
			var image   = Raylib.LoadImage("assets/mario-idle.png");
			var texture = Raylib.LoadTextureFromImage(image);
			sprite.Assign(texture);
			sprite.OnDestroyed += () =>
			{
				Raylib.UnloadTexture(texture);
				Raylib.UnloadImage(image);
			};
			sprite.Size  = Vector2.One * 10;
			
			var follower = render.Camera.Parent.AddComponent<Follower>();
			follower.FollowTarget =  mario;
		}

		long time = DateTime.Now.Ticks;
		while (!render.Closing)
		{
			long now   = DateTime.Now.Ticks;
			float delta = (float)((now - time) / (double)TimeSpan.TicksPerMillisecond);
			
			//
			
			if(Raylib.IsKeyReleased(KeyboardKey.F4))
			{
				render.Debug = !render.Debug;
				Console.WriteLine(
					$"Debug {(render.Debug? "enabled": "disabled")}!"
				);
			}
			if(Raylib.IsKeyReleased(KeyboardKey.F5))
			{
				mapHandler.HotReload = !mapHandler.HotReload;
				Console.WriteLine(
					$"Map hot reloading {(mapHandler.HotReload? "enabled": "disabled")}!"
				);
			}
			
			if(Raylib.IsKeyReleased(KeyboardKey.F9))
			{
				render.Camera.Zoom -= 1f;
				Console.WriteLine($"Zoom: {render.Camera.Zoom}");
			}
			if(Raylib.IsKeyReleased(KeyboardKey.F10))
			{
				render.Camera.Zoom += 1f;
				Console.WriteLine($"Zoom: {render.Camera.Zoom}");
			}
			
			if(Raylib.IsKeyReleased(KeyboardKey.F6))
			{
				render.IMGUIEnabled   = !render.IMGUIEnabled;
				render.Camera.FreeCam = !render.Camera.FreeCam;
				Console.WriteLine(
					$"IMGUI: {(render.IMGUIEnabled? "enabled": "disabled")}!"
				);
				Console.WriteLine($"Freecam {(render.Camera.FreeCam? "enabled": "disabled")}!");

			}

			ObjectUtils.UpdateAll(delta);
			render.Render(delta);
			
			time = DateTime.Now.Ticks;
		}
	}
}