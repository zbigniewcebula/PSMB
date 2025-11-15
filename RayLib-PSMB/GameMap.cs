using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using Raylib_cs;
using TiledSharp;

namespace PSMB
{
	public class GameMap : Component, IRenderable, IUpdatable
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public Color Tint { get; private set; } = Color.White;
		public Color BackgroundColor { get; private set; } = Color.Black;
		
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
		private Dictionary<(int, int), ObjectData> objects = new();
		
		private List<Object> gameObjects = new();
		
		private string lastMapLoadedPath = string.Empty;
		private DateTime lastMapLoadedTime = DateTime.Now;
		
		public bool LoadFromFile(string tmxFilename) //TODO: TMX Project handling
		{
			tiles?.Clear();
			layers?.Clear();
			objects?.Clear();
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
						Console.WriteLine($"Map layer 'foreground', loaded!");
					}
				}
				if(tmxLayers.TryGetValue("background", out var background))
				{
					if(LoadLayer(background, tmxMap))
					{
						Console.WriteLine($"Map layer 'background', loaded!");
					}
				}

				if(tmxMap.ObjectGroups.TryGetValue("objects", out var objects))
				{
					LoadObjects(objects);
				}
				
				Console.WriteLine($"Map size: {Width}x{Height}");
				
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
				Console.WriteLine($"Error loading Tiled map from file '{tmxFilename}': {ex.Message}\n{ex.StackTrace}");
			}
			
			return true;
		}

		public bool ReloadMap()
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
				Console.WriteLine($"Preparing gfx asset '{asset.Key}'");
				asset.Value.LoadTexture();
			}
		}

		private void LoadObjects(TmxObjectGroup objectLayer)
		{
			var objs = objectLayer.Objects.Where(o => o != null);
			foreach(var obj in objs)
			{
				var data = new ObjectData()
				{
					ID = obj.Id,
					Name = obj.Name,
					Class = obj.Type,
					
					X = (int)obj.X,
					Y = (int)obj.Y,
					Width = (int)obj.Width,
					Height = (int)obj.Height,
					
					Properties = obj.Properties,
				};
				objects.Add((data.X, data.Y), data);
			}
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
						throw new FileNotFoundException("Tileset file not found", set.Image.Source); 
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

			var height = tmxMap.Height;
			var existingTiles = tileLayer.Tiles
									.Where(t => t != null && t.Gid != 0);
			foreach(var tile in existingTiles)
			{
				Object tileObject = new($"Tile_{tile.Gid}");
				tileObject.Parent     = Parent;
				tileObject.Position   = new(tile.X, height - tile.Y);
				
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

			return true;
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

		public class TileDataComponent: Component
		{
			public TileData Data { get; set; }
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
		}

		public void Render(Renderer renderer)
		{
			renderer.BackgroundColor = BackgroundColor;
			
			/*
			var viewport = renderer.GetViewport();
			foreach(var layer in layers)
			{
				foreach(var tile in layer.Value)
				{
					var (posX, posY) = tile.Key;
					if(posX < viewport.X
					&& posX > (viewport.X + viewport.Width)
					)
					{
						continue;
					} 
					
					var tileId = tile.Value;
					if(tiles.TryGetValue(tileId, out var tileData))
					{
						RenderTile(posX, posY, tileData);
					}
				}
			}*/
		}

		private void RenderTile(int x, int y, TileData tileData)
		{
			if(tileData.GfxAsset.Texture == null)
			{
				return;
			}

			var texture = tileData.GfxAsset.Texture.Value;
			Raylib.DrawTexturePro(
				texture,
				tileData.Rect,
				new(
					(Parent.Position.X + x * TileData.SIZE) * Parent.Scale.X,
					(Parent.Position.Y + y * TileData.SIZE) * Parent.Scale.Y, 
					TileData.SIZE * Parent.Scale.X, 
					TileData.SIZE  * Parent.Scale.Y
				),
				new(0.5f, 0.5f),
				Parent.Rotation,
				Tint
			);
		}
		
		public void RenderDebug(Renderer renderer)
		{
			//
		}

		public void Update(float delta)
		{
			if(HotReload)
			{
				if(File.GetLastWriteTime(lastMapLoadedPath) > lastMapLoadedTime)
				{
					if(ReloadMap())
					{
						Console.WriteLine($"Map hot reload performed!");
					}
					else
					{
						Console.WriteLine($"Map reloading failed! Path: {lastMapLoadedPath}");
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
	}
}