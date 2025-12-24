using System.Numerics;
using PSMB.Physics;
using PSMB.Physics.Objects;
using PSMB.Physics.Shapes;
using Raylib_cs;

namespace PSMB;

internal class Program
{
	[System.STAThread]
	public static void Main(string[] args)
	{	
		var windowSize = new Vector2(1280, 720);
		Renderer render = new(
			"PSMB", windowSize, 60, true
		) {
			BackgroundColor = Color.Black,
		};
		render.ShowFPS     = true;
		
		Editor editor = new();
		Time.Scale = 1;

		PhysicsSystem physics = new()
		{
			GravityScale = 200f
		};

		Object camera = new("MainCamera");
		var    cam    = camera.AddComponent<Camera>();
		camera.AddComponent<FreeCamera>();
		render.Camera = cam;
		
		GameMap  mapHandler = null;
		{
			float mapScale = 64;
			Object map = new("Map")
			{
				Scale    = Vector2.One * mapScale,
				Position = new(0, 0),
			};
			mapHandler = map.AddComponent<GameMap>();
			if(mapHandler.LoadFromFile("assets/map/map01.tmx") == false)
			{
				Logger.Log("MapHandler", "Map loading failed! Path: 'assets/map/map01.tmx'");
				return;
			}
			mapHandler.HotReload = true;
			
			var spawnData = mapHandler.GetObject("main_spawn");
			var cameraBounds1 = mapHandler.GetObject("camera_bounds_1");
			var cameraBounds2 = mapHandler.GetObject("camera_bounds_2");

			PhysicsObject mario  = new("Mario", IShape.Type.Box)
			{
				Position = new(
					(spawnData.X + map.Position.X) * mapScale,
					(spawnData.Y + map.Position.Y) * mapScale
				),
				// Position = new(32 * mapScale, 176 * mapScale),
				// Position = new(20, 350),
			};

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
			sprite.Size  = Vector2.One * 80;

			/*
			var rigid = mario.AddComponent<Collider>();
			rigid.Box           = new(
				0, 0,
				sprite.Size.X * mario.Scale.Y,
				sprite.Size.Y * mario.Scale.Y
			);
			
			var platformPhysics = mario.AddComponent<PlatformerPhysics>();
			*/
			
			/*
			var follower = render.Camera.Parent.AddComponent<Follower>();
			follower.FollowTarget = mario;
			follower.Offset       = new(-256, -256);
			follower.ClampBounds  = new(
				cameraBounds1.X * map.Scale.X,
				(cameraBounds1.Y - cameraBounds1.Height) * map.Scale.Y,
				cameraBounds1.Width * map.Scale.X,
				cameraBounds1.Height * map.Scale.Y
			);*/

			// PhysicsObject falling  = new("Falling", IShape.Type.Box)
			// {
			// 	Position = new(0, 200),
			// 	Size     = new(50, 50),
			// 	CanRotate = false
			// };
			// falling.AddComponent<DebugPhysicsObject>();
			//
			// PhysicsObject platform = new("Platform", IShape.Type.Box)
			// {
			// 	Position = new(0, 0),
			// 	Size     = new(200, 100),
			// 	Locked   = true
			// };
			// platform.ChangeMass(1000);
			// platform.AddComponent<DebugPhysicsObject>();
			//
			// render.Camera.Parent.Position = new(-300, -300);
		}
		
		DebugShortcuts.RegisterCallback(KeyboardKey.F9, () =>
		{
			render.Camera.Zoom -= 1f;
			Logger.Log("Zoom", render.Camera.Zoom);
		});
		DebugShortcuts.RegisterCallback(KeyboardKey.F10, () =>
		{
			render.Camera.Zoom += 1f;
			Logger.Log("Zoom", render.Camera.Zoom);
		});
		
		long time = DateTime.Now.Ticks;
		while(!render.Closing)
		{
			long now   = DateTime.Now.Ticks;
			float delta = (float)((now - time) / (double)TimeSpan.TicksPerMillisecond);
			
			editor.Update(delta, render);

			DebugShortcuts.Update();
			
			if(Time.Scale > 0)
			{
				ObjectUtils.UpdateAll(delta * Time.Scale);
				physics.Tick(delta);
			}
			else
			{
				delta = 0;
			}

			render.Render(delta, editor);
			
			time = DateTime.Now.Ticks;
		}
	}
}