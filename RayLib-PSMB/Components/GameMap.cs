using System.Numerics;
using ImGuiNET;
using PSMB.Physics.Objects;
using PSMB.Physics.Shapes;
using Raylib_cs;
using TiledSharp;

namespace PSMB
{
	public class GameMap : Component, 
		IRenderable, IRenderableOutsideViewport, IUpdatable, ICustomEditor
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public Color Tint { get; private set; } = Color.White;
		public Color BackgroundColor { get; private set; } = Color.Black;
		
		[HideInEditor]
		public TileData this[string layer, int x, int y]
		{
			get
			{
				if(layers.TryGetValue(layer, out var idMap) == false
				|| idMap.TryGetValue((x, y), out int gid) == false
				)
				{
					return null!;
				}

				return tiles.GetValueOrDefault(gid)!;
			}
		}
		
		public bool HotReload { get; set; } = false;
		
		private TmxMap tmxMap;

		internal Dictionary<string, Asset> gfxAssets = new();
		private Dictionary<int, TileData> tiles = new();
		
		private Dictionary<string, Dictionary<(int, int), int>> layers = new();
		private Dictionary<string, ObjectData> objects = new();
		private Dictionary<string, Text> texts = new();
		
		private List<Object> gameObjects = new();
		
		private string lastMapLoadedPath = string.Empty;
		private DateTime lastMapLoadedTime = DateTime.Now;
		
		public bool LoadFromFile(string tmxFilename) //TODO: TMX Project handling
		{
			tiles?.Clear();
			layers?.Clear();
			objects?.Clear();
			texts?.Clear();
			gameObjects?.Clear();
			if(gfxAssets != null)
			{
				foreach(var keyValuePair in gfxAssets)
				{
					keyValuePair.Value.Dispose();
				}

				gfxAssets.Clear();
			}
			
			try
			{
				tmxMap = new(tmxFilename);
				
				LoadTilesets(tmxMap.Tilesets);
				
				var tmxLayers = tmxMap.Layers;
				if(tmxLayers.TryGetValue("foreground", out var foreground))
				{
					if(LoadLayer(foreground, tmxMap))
					{
						Logger.Log("GameMap", $"Map layer 'foreground', loaded!");
					}
				}
				if(tmxLayers.TryGetValue("background", out var background))
				{
					if(LoadLayer(background, tmxMap))
					{
						Logger.Log("GameMap", $"Map layer 'background', loaded!");
					}
				}

				if(tmxMap.ObjectGroups.TryGetValue("objects", out var objects))
				{
					LoadObjects(objects);
				}
				
				Logger.Log("GameMap", $"Map size: {Width}x{Height}");
				
				//BackgroundColor = map.BackgroundColor.R
				
				BackgroundColor = new(
					tmxMap.BackgroundColor.R,
					tmxMap.BackgroundColor.G,
					tmxMap.BackgroundColor.B
				);
				
				lastMapLoadedPath = tmxFilename;
				lastMapLoadedTime = DateTime.Now;
			}
			catch(Exception ex)
			{
				Logger.Error("GameMap", $"Error loading Tiled map from file '{tmxFilename}': {ex.Message}\n{ex.StackTrace}");
			}
			
			return true;
		}

		public ObjectData GetObject(string name)
		{
			return objects.GetValueOrDefault(name);
		}

		private bool ReloadMap()
		{
			if(string.IsNullOrEmpty(lastMapLoadedPath))
			{
				return false;
			}
			
			if(LoadFromFile(lastMapLoadedPath) == false)
			{
				return false;
			}
			PrepareGFXAssets();

			return true;
		}

		public void PrepareGFXAssets()
		{
			var notPreparedTextures =
				gfxAssets.Where(a => a.Value.Texture == null);
			foreach(var asset in notPreparedTextures)
			{
				Logger.Log("GameMap", $"Preparing gfx asset '{asset.Key}'");
				asset.Value.LoadTexture();
			}
		}

		private void LoadObjects(TmxObjectGroup objectLayer)
		{
			var height = tmxMap.Height;
			var objs   = objectLayer.Objects.Where(o => o != null);
			foreach(var obj in objs)
			{
				var type = GetObjectType(obj);
				
				var props = obj.Properties.ToDictionary(
					p => p.Key,
					p => p.Value
				);
				var data = new ObjectData()
				{
					ID    = obj.Id,
					Name  = obj.Name,
					Class = obj.Type,
					Type  = type,
					
					X      = (int)obj.X / TileData.SIZE,
					Y      = height - ((int)obj.Y / TileData.SIZE) + 1,
					Width  = (int)obj.Width / TileData.SIZE,
					Height = (int)obj.Height / TileData.SIZE,
					
					Properties = props,
				};
				
				if(type == ObjectData.ObjectType.Text)
				{
					props.Add("text", new("string", obj.Text.Value));
					
					Object gameObject = new(obj.Name);
					gameObject.Parent   = Parent;
					gameObject.Position = new(data.X, data.Y);
					
					var    text       = gameObject.AddComponent<Text>();
					text.Content = obj.Text.Value;
					text.Color   = new(
						obj.Text.Color.R, obj.Text.Color.G, obj.Text.Color.B
					);
					//text.Font = TODO
					text.FontSize = obj.Text.PixelSize / TileData.SIZE;
					
					var    tileData   = gameObject.AddComponent<ObjectDataComponent>();
					tileData.Data     = data;
					
					texts.Add(obj.Name, text);
				}
				else if(type == ObjectData.ObjectType.Polygon)
				{
					props.Add("points", new("points", obj.Points));
				}
				else if(type == ObjectData.ObjectType.Polyline)
				{
					props.Add("points", new("points", obj.Points));
				}
				
				objects.Add(obj.Name, data);
			}
		}

		private ObjectData.ObjectType GetObjectType(TmxObject tmxObject)
		{
			if(tmxObject.Text != null)
			{
				return ObjectData.ObjectType.Text;
			}

			return tmxObject.ObjectType switch
			{
				TmxObjectType.Point   => ObjectData.ObjectType.Point,
				TmxObjectType.Ellipse => ObjectData.ObjectType.Ellipse,
				TmxObjectType.Polygon => ObjectData.ObjectType.Polygon,
				TmxObjectType.Polyline => ObjectData.ObjectType.Polyline,
				_                     => ObjectData.ObjectType.Rectangle,
			};
		}

		private void LoadTilesets(TmxList<TmxTileset> mapTilesets)
		{
			foreach(var set in mapTilesets)
			{
				if(gfxAssets.TryGetValue(set.Image.Source, out Asset img) == false)
				{
					if(File.Exists(set.Image.Source))
					{
						var image = Raylib.LoadImage(set.Image.Source);
						gfxAssets.Add(set.Image.Source, img = new(image));
						img.LoadTexture();
					}
					else
					{
						throw new FileNotFoundException(); 
					}
				}
				
				foreach(var tile in set.Tiles)
				{
					var gid = tile.Key + set.FirstGid;
					int cols = set.Columns;
					
					var data = new TileData()
					{
						GID = gid,
						Class = tile.Value.Type,
						GfxAsset = img,
						Rect = new(
							(tile.Key % cols) * TileData.SIZE, 
							(tile.Key / cols) * TileData.SIZE,
							TileData.SIZE, TileData.SIZE - 0.01f
						),
						Properties = tile.Value.Properties
					};
					tiles.Add(gid, data);
				}
			}
		}

		private bool LoadLayer(ITmxLayer layer, TmxMap tmxMap)
		{
			if(layer is not TmxLayer tileLayer)
			{
				return false;
			}

			if(layers.TryGetValue(layer.Name, out var loadedData))
			{
				Console.WriteLine($"Layer '{layer.Name}' already loaded!");
				return false;
			}

			if(loadedData != null)
			{
				Console.WriteLine($"Layer '{layer.Name}' loaded incorrectly!");
				return false;
			}

			loadedData = new();
			layers.Add(layer.Name, loadedData);

			var height       = tmxMap.Height;
			var currentScale = Parent.Scale;
			var existingTiles = tileLayer.Tiles
									.Where(t => t != null && t.Gid != 0);
			foreach(var tile in existingTiles)
			{
				Object tileObject = null!;
				if(tiles[tile.Gid].Properties.TryGetValue(
						"collision",
						out var prop
					)	&& prop.Type == "bool"
					&& prop.ReadBool()
				)
				{
					var physicalObject = new PhysicsObject(
						$"Tile_{tile.Gid}",
						IShape.Type.Box
					)
					{
						Locked    = true,
						CanRotate = false
					};
					physicalObject.ChangeMass(100);
					physicalObject.AddComponent<DebugPhysicsObject>();
					physicalObject.Size = Parent.Scale;
					tileObject          = physicalObject;
				}
				else
				{
					tileObject = new Object($"Tile_{tile.Gid}");
				}
				
				tileObject.Parent     = Parent;
				tileObject.Position   = new(
					tile.X, (height - tile.Y)
				); 
				
				var    tileData   = tileObject.AddComponent<TileDataComponent>();
				tileData.Data = tiles[tile.Gid];
				
				var    tileSprite = tileObject.AddComponent<Sprite>();
				tileSprite.Assign(tileData.Data.GfxAsset.Texture.Value);
				tileSprite.Size       = Vector2.One;
				tileSprite.Pivot      = new(0, 0);
				tileSprite.SourceRect = tileData.Data.Rect;
				
				gameObjects.Add(tileObject);
				loadedData.Add((tile.X, tile.Y), tile.Gid);
				
				if(tile.X > Width)
				{
					Width = tile.X;
				}
				if(tile.Y > Height)
				{
					Height = tile.Y;
				}
			}

			Parent.OnScaleChanged -= OnScaleChanged;
			Parent.OnScaleChanged += OnScaleChanged;
			
			return true;
		}

		private void OnScaleChanged(Vector2 oldScale, Vector2 newScale)
		{
			Parallel.ForEach(gameObjects, obj =>
			{
				// var physics = obj.GetComponent<Collider>();
				// physics.Box = new(0, newScale.Y, newScale.X, newScale.Y);
				if(obj is PhysicsObject physicsObj)
				{
					physicsObj.Position = new(
						physicsObj.Position.X * newScale.X,
						physicsObj.Position.Y * newScale.Y
					);
					physicsObj.Size = new(newScale.X, newScale.Y);
				}
			});
		}

		public class TileData
		{
			public const int SIZE = 16;
			
			public int GID { get; init; }
			public string Class { get; init; }
			
			public Asset GfxAsset { get; init; }
			public Rectangle Rect { get; init; }
			
			public IReadOnlyDictionary<string, PropertyDict.TypeValueEntry> Properties { get; init; }
		}

		public class TileDataComponent : Component
		{
			public TileData Data { get; set; }
		}
		public class ObjectDataComponent : Component
		{
			public ObjectData Data { get; set; }
		}

		public class ObjectData
		{
			public int ID { get; init; }
			public string Name { get; init; }
			public string Class { get; init; }
			public int X { get; init; }
			public int Y { get; init; }
			public int Width { get; init; }
			public int Height { get; init; }
			public IReadOnlyDictionary<string, PropertyDict.TypeValueEntry> Properties { get; init; }
			public ObjectType Type { get; init; }

			public enum ObjectType
			{
				Text,
				Rectangle,
				Ellipse,
				Polygon,
				Polyline,
				Point
			}
		}

		public void Render(Renderer renderer)
		{
			renderer.BackgroundColor = BackgroundColor;
		}
		
		public void RenderDebug(Renderer renderer)
		{
			return;
			foreach(var entry in objects)
			{
				var data = entry.Value;

				var pos = new Vector2(
					Parent.Position.X + data.X * Parent.Scale.X,
					Parent.Position.Y + data.Y * Parent.Scale.Y
				);
				var size = new Vector2(
					data.Width * Parent.Scale.X,
					data.Height * Parent.Scale.Y
				);
				Raylib.DrawText(
					entry.Key, 
					(int)pos.X, (int)pos.Y, 
					14, 
					Color.Black
				);
				renderer.DrawText(entry.Key, pos, color: Color.Black);
				//Console.WriteLine($"{entry.Key}: {data.X}, {data.Y}");

				switch(data.Type)
				{
					case ObjectData.ObjectType.Text:
						break;
					case ObjectData.ObjectType.Rectangle:
						var rectSize = new Vector2(
							data.Width * Parent.Scale.X, data.Height * Parent.Scale.Y
						);
						Raylib.DrawRectangleLinesEx(
							new Rectangle(pos.X, pos.Y, rectSize.X, rectSize.Y),
							1f,
							Color.Yellow
						);
						break;
					case ObjectData.ObjectType.Ellipse:
						Vector2 radius = size / 2f;
						Vector2 center = new(
							pos.X + radius.X,
							pos.Y + radius.Y
						); 
						renderer.DrawEllipseLines(center, radius, Color.Yellow);
						break;
					case ObjectData.ObjectType.Polygon or ObjectData.ObjectType.Polyline:
						if(data.Properties.TryGetValue(
							"points",
							out var points
						))
						{
							var pts = points
								.ReadPoints()
								.Select(p => new Vector2(p.x, -p.y) / TileData.SIZE)
								.ToList();

							if(pts.Count <= 1)
							{
								break;
							}
							var prev = pts[0];
							foreach(var point in pts.Skip(1))
							{
								renderer.DrawLine(new(
									Parent.Position.X + (data.X + prev.X) * Parent.Scale.X,
									Parent.Position.Y + (data.Y + prev.Y) * Parent.Scale.Y
								), new(
									Parent.Position.X + (data.X + point.X) * Parent.Scale.X,
									Parent.Position.Y + (data.Y + point.Y) * Parent.Scale.Y
								), 1f, Color.Yellow);
								
								prev = point;
							}
						}
						break;
					case ObjectData.ObjectType.Point:
						break;
				}
			}
		}

		public void Update(float delta)
		{
			if(HotReload)
			{
				if(File.GetLastWriteTime(lastMapLoadedPath) > lastMapLoadedTime)
				{
					if(ReloadMap())
					{
						Logger.Log("GameMap", "Map hot reload performed!");
					}
					else
					{
						Logger.Log("GameMap", $"Map reloading failed! Path: {lastMapLoadedPath}");
					}
				}
			}
		}

		public class Asset(Image image) : IDisposable
		{
			public Texture2D? Texture { get; private set; }

			public void LoadTexture()
			{
				Texture = Raylib.LoadTextureFromImage(image);
			}

			~Asset()
			{
				Dispose();
			}

			public void Dispose()
			{
				if(Texture != null)
				{
					Raylib.UnloadTexture(Texture.Value);
					Texture = null;
				}
			}
		}

		public bool OverrideEditor => false;

		public void RenderEditor()
		{
			var flags = ImGuiTreeNodeFlags.OpenOnArrow;
			string objectsTitle = $"Map Objects##{ID}";
			if(ImGui.CollapsingHeader(objectsTitle, ImGuiTreeNodeFlags.DefaultOpen))
			{
				ImGui.Indent(20);
				foreach(var (name, obj) in objects)
				{
					string objHeader = $"{name}##{obj.GetHashCode()}";
					bool   open      = ImGui.TreeNodeEx(objHeader, flags);
					if(open)
					{
						RenderObjNode(obj);

						ImGui.TreePop();
					}
				}
				ImGui.Unindent(20);
			}

			return;

			void RenderObjNode(ObjectData data)
			{
				float labelWidth = 120f;

				ImGui.Columns(3, $"props##{data.ID}", true);

				// Label column
				ImGui.SetColumnWidth(0, labelWidth);
				ImGui.SetColumnWidth(1, labelWidth);
				
				ImGui.Text("Position"); ImGui.NextColumn();
				ImGui.Text($"{data.X};{data.Y}"); ImGui.NextColumn();
				if(ImGui.Button($"Camera focus##{ID}")) 
				{
						
				} ImGui.NextColumn();
				
				ImGui.Text("Size"); ImGui.NextColumn();
				ImGui.Text($"{data.Width};{data.Height}"); ImGui.NextColumn();
				ImGui.NextColumn();
				
				ImGui.Text("Class"); ImGui.NextColumn();
				ImGui.Text(data.Class); ImGui.NextColumn();
				ImGui.NextColumn();
				
				ImGui.Text("Type"); ImGui.NextColumn();
				ImGui.Text(data.Type.ToString()); ImGui.NextColumn();
				ImGui.NextColumn();
				
				ImGui.Columns(1); // end layout
				
				if(ImGui.CollapsingHeader($"Properties##{data.ID}", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent(20);
					ImGui.Columns(3, $"props_inner##{data.ID}", true);
					foreach(var (name, entry) in data.Properties)
					{
						ImGui.Text(name);
						ImGui.NextColumn();
						ImGui.Text(entry.Type);
						ImGui.NextColumn();
						ImGui.Text(entry.Value);
						ImGui.NextColumn();
					}
					ImGui.Columns(1); // end layout
					ImGui.Unindent(20);
				}
			}
		}
	}
}