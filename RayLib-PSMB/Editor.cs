using System.Numerics;
using System.Reflection;
using Box2D.NetStandard.Collision.Shapes;
using ImGuiNET;
using PSMB.Physics.Shapes;
using Raylib_cs;
using rlImGui_cs;

namespace PSMB
{
	public class Editor
	{
		public bool Enabled { get; set; } = true;
		public bool GizmosEnabled { get; set; } = true;
		
		private const int MAIN_BAR_SIZE = 20;
		
		private bool hierarchyVisible = true;
		private bool consoleVisible = true;
		private bool rendererInfoVisible = false;
		
		private string inspectorCurrentTab = "Main Inspector";

		private Random random = new Random();

		private HashSet<Object> openedInspectors = new();
		
		private Dictionary<string, Action> shortcuts = new();
		
		public Editor()
		{
			rlImGui.Setup(true, true);
		}

		~Editor()
		{
			rlImGui.Shutdown();
		}

		public void Render(Renderer render, float deltaTime)
		{
			if(deltaTime <= 0)
			{
				deltaTime = -1F;
			}
			
			rlImGui.Begin(deltaTime);
			
			if(ImGui.BeginMainMenuBar()) 
			{
				if(ImGui.BeginMenu("Application"))
				{
					bool imguiEnabled = Enabled;
					if(ImGui.MenuItem("Debug UI", "F5", ref imguiEnabled))
					{
						Enabled = imguiEnabled;
					}
					ImGui.Separator();
					if(ImGui.MenuItem("Exit", "ALT+F4")) {
						Raylib.CloseWindow();
					}
					ImGui.EndMenu();
				}
				
				if(ImGui.BeginMenu("Time"))
				{
					if(Time.Scale > 0)
					{
						ImGui.BeginDisabled();
					}

					if(ImGui.MenuItem("Go step", "F3"))
					{
						//Time.StepExpected = true;
					}
					
					if(Time.Scale > 0)
					{
						ImGui.EndDisabled();
					}
					
					if(ImGui.MenuItem("Set Normal", "F2")) {
						Time.Scale = 1;
					}
					if(ImGui.MenuItem("Stop", "F1")) {
						Time.Scale = 0;
					}
					ImGui.Text("Scale");
					ImGui.SameLine();
					float scale = Time.Scale;
					if(ImGui.SliderFloat(
						"##timeScale",
						ref scale,
						0f,
						10f
					))
					{
						Time.Scale = scale;
					}
					ImGui.EndMenu();
				}
				
				ImGui.Separator();
				
                if(Utils.Approximately(Time.Scale, 1f) == false)
                {
					if(Time.Scale != 0)
					{
						ImGui.Text($"Scale: {Time.Scale}");
						ImGui.Separator();
					}
                }

				if(Utils.Approximately(Time.Scale, 0) && ImGui.Button("Play"))
				{
					Time.Scale = 1;
				}

				if(Utils.Approximately(Time.Scale, 1) && ImGui.Button("Stop"))
				{
					Time.Scale = 0;
				}
				
				ImGui.Separator();
				
				if(ImGui.BeginMenu("View"))
				{
					if(ImGui.MenuItem("Hierarchy", "F5", ref hierarchyVisible)) {}
					
					if(ImGui.MenuItem("Console", "`", ref consoleVisible)) {}
					
					if(ImGui.MenuItem("Renderer", "SHIFT+`", ref rendererInfoVisible)) {}

					bool gizmosEnabled = GizmosEnabled;
					if(ImGui.MenuItem("Gizmos", "F6", ref gizmosEnabled))
					{
						GizmosEnabled = gizmosEnabled;
					}
					ImGui.EndMenu();
				}
        
				ImGui.EndMainMenuBar();
			}
			
			if(hierarchyVisible)
			{
				RenderHierarchy(render);
			}

			if(consoleVisible)
			{
				consoleVisible = Logger.RenderConsole(render);
			}
			
			if(rendererInfoVisible)
			{
				rendererInfoVisible = RenderRendererInfo(render);
			}

			if(openedInspectors.Count > 0)
			{
				foreach(Object context in openedInspectors)
				{
					RenderInspector(render, context);
				}
			}
			
			rlImGui.End();	
		}

		private bool RenderRendererInfo(Renderer render)
		{
			string name = "Renderer Info";
			if(ImGui.Begin(name) == false)
			{
				ImGui.End();
				ImGui.SetWindowCollapsed(name, false);
				return false;
			}
			ImGui.SetWindowSize(new(500, 300));
			
			float labelWidth = 120f;
			ImGui.Columns(2, $"Renderer Info##table", true);
			ImGui.SetColumnWidth(0, labelWidth);
			
			ImGui.Text("Size"); ImGui.NextColumn();
			ImGui.Text($"{render.Size.X}x{render.Size.Y}"); ImGui.NextColumn();
			
			ImGui.Text("Background color"); ImGui.NextColumn();
			ImGui.Text($"{render.BackgroundColor.ToString()}"); ImGui.NextColumn();
			
			ImGui.Text("Show FPS"); ImGui.NextColumn();
			ImGui.Text(render.ShowFPS.ToString()); ImGui.NextColumn();
			
			ImGui.Text("Debug Gizmos"); ImGui.NextColumn();
			ImGui.Text(GizmosEnabled.ToString()); ImGui.NextColumn();
			
			ImGui.Columns(1); // end layout
			ImGui.End();

			return true;
		}

		public void Update(float delta, Renderer render)
		{
			if(IsPressed(KeyboardKey.F5))
			{
				Enabled = !Enabled;
			}
			
			if(IsPressed(KeyboardKey.F4, KeyboardKey.LeftAlt))
			{
				Raylib.CloseWindow();
			}
			
			if(IsPressed(KeyboardKey.F6))
			{
				GizmosEnabled = !GizmosEnabled;
			}
			
			if(IsPressed(KeyboardKey.F3))
			{
				//Time.StepExpected = true;
			}
			
			if(IsPressed(KeyboardKey.F2))
			{
				Time.Scale = 1;
			}
			
			if(IsPressed(KeyboardKey.F1))
			{
				Time.Scale = 0;
			}
			
			if(IsPressed(KeyboardKey.F7))
			{
				hierarchyVisible = !hierarchyVisible;
			}
			
			if(IsPressed(KeyboardKey.Grave, KeyboardKey.LeftShift))
			{
				rendererInfoVisible = !rendererInfoVisible;
			}
			
			
		}

		private bool IsPressed(KeyboardKey key, KeyboardKey mod = KeyboardKey.Null)
		{
			if(mod != KeyboardKey.Null)
			{
				if(Raylib.IsKeyDown(mod) == false)
				{
					return false;
				}
			}

			return Raylib.IsKeyPressed(key);
		}

		private void RenderInspector(Renderer render, Object context)
		{
			var inspectorName = $"Inspector##{context.GetHashCode()}";
			if(ImGui.Begin(inspectorName) == false)
			{
				ImGui.End();
				ImGui.SetWindowCollapsed(inspectorName, false);
				openedInspectors.Remove(context);
				return;
			}
			var size = new Vector2(400, render.Size.Y - MAIN_BAR_SIZE);
			// ImGui.SetWindowSize(inspectorName, size);
			// ImGui.SetWindowPos(inspectorName, new(render.Size.X - size.X, MAIN_BAR_SIZE));
			size.X = size.X * 0.9f;

			if(ImGui.BeginTabBar("MainTabs"))
			{
				RenderMainInspectorTab(render, context, size);
				
				RenderComponentsInspectorTab(render, context, size);

				ImGui.EndTabBar();
			}
			
			ImGui.End();
		}

		private void RenderComponentsInspectorTab(Renderer render, Object context, Vector2 size)
		{
			var id    = "Component Inspector";
			
			ImGuiTabItemFlags tabFlags = ImGuiTabItemFlags.NoCloseWithMiddleMouseButton | ImGuiTabItemFlags.NoPushId;
			if(inspectorCurrentTab == id)
			{
				tabFlags |= inspectorCurrentTab == id? ImGuiTabItemFlags.SetSelected: 0;
				inspectorCurrentTab = string.Empty;
			}
			
			var dummy = true;
			if(!ImGui.BeginTabItem(id, ref dummy, tabFlags))
			{
				return;
			}

			var allComponents = context.GetAllComponents<Component>();
			int i             = 0;
			int objID         = Object.All.IndexOf(context) + 1; 
			foreach(var component in allComponents)
			{
				var type = component.GetType();
				var subID = i + (1000 * objID);
				if(ImGui.CollapsingHeader($"{type.Name}##{subID}", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent(20);
					RenderReflectionFields(component);
					RenderInspectorButtons(component);
					RenderCustomEditor(component);
					ImGui.Unindent(20);
				}
				
				++i;
			}
				
			ImGui.EndTabItem();
		}

		private void RenderCustomEditor(Component component)
		{
			if(component is not ICustomEditor customEditor)
			{
				return;
			}
			
			ImGui.Separator();
			customEditor.RenderEditor();
		}

		private void RenderInspectorButtons(Component component)
		{
			var type   = component.GetType();
			var flags  = BindingFlags.Instance | BindingFlags.InvokeMethod
				|  BindingFlags.Public | BindingFlags.NonPublic;
			var methods = type.GetMethods(flags).Where(
				m => m.GetCustomAttribute<InspectorAttribute>() != null
			);
			foreach(var method in methods)
			{
				var name     = method.Name;
				if(ImGui.Button($"{name}##{component.ID}"))
				{
					method.Invoke(component, null);
				}
			}
		}

		private void RenderMainInspectorTab(Renderer render, Object context, Vector2 size)
		{
			var id    = "Main Inspector";
			
			ImGuiTabItemFlags tabFlags = ImGuiTabItemFlags.NoCloseWithMiddleMouseButton | ImGuiTabItemFlags.NoPushId;
			if(inspectorCurrentTab == id)
			{
				tabFlags |= inspectorCurrentTab == id? ImGuiTabItemFlags.SetSelected: 0;
				inspectorCurrentTab = string.Empty;
			}

			var dummy = true;
			if(!ImGui.BeginTabItem(id, ref dummy, tabFlags))
			{
				return;
			}

			if(ImGui.Button("Focus", new Vector2(size.X, 20)))
			{
				render.Camera.Parent.Position = new(
					context.Position.X + render.Size.X / 2,	
					context.Position.Y - render.Size.Y / 2
				);
				Console.WriteLine(
					$"[ImGui] Focused camera on [{context.Name}]: {render.Camera.Parent.Position}"
				);
			}

			RenderReflectionFields(context);
					
			ImGui.EndTabItem();
		}

		private void RenderHierarchy(Renderer render)
		{
			Vector2          minSize = new(300, 500);
			ImGuiWindowFlags flags   = ImGuiWindowFlags.AlwaysVerticalScrollbar;
			if(ImGui.Begin("Hierarchy", flags) == false)
			{
				ImGui.End();
				hierarchyVisible = false;
				ImGui.SetWindowCollapsed("Hierarchy", false);
				return;
			}
			Vector2 currentSize = ImGui.GetWindowSize();
			if(currentSize.X < minSize.X)
			{
				currentSize.X = minSize.X;
				ImGui.SetWindowSize("Hierarchy", currentSize);
			}
			if(currentSize.Y < minSize.Y)
			{
				currentSize.Y = minSize.Y;
				ImGui.SetWindowSize("Hierarchy", currentSize);
			}

			ImGui.BeginChild(
				"Scrolling",
				new(),
				ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY
				| ImGuiChildFlags.AlwaysAutoResize
			);

			for(var index = 0; index < Object.All.Count; index++)
			{
				var obj = Object.All[index];
				if(obj.Parent == null)
				{
					RenderNode(obj);
				}
			}

			ImGui.EndChild();
			ImGui.End();
		}

		private void RenderNode(Object obj)
		{
			bool isSelected = openedInspectors.Contains(obj);

			// Flags control tree behavior
			var flags = ImGuiTreeNodeFlags.OpenOnArrow 
				| ImGuiTreeNodeFlags.OpenOnDoubleClick
				| ImGuiTreeNodeFlags.SpanFullWidth 
				| (isSelected ? ImGuiTreeNodeFlags.Selected : 0);

			// If no children -> make it a leaf node
			if(obj.ChildCount == 0)
			{
				flags |= ImGuiTreeNodeFlags.Leaf;
			}

			bool open = ImGui.TreeNodeEx($"{obj.Name}##{obj.GetHashCode()}", flags);

			// Selection detection
			if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
			{
				openedInspectors.Add(obj);
			}

			if(open)
			{
				foreach(var child in obj)
				{
					int childID = Object.All.IndexOf(child);
					RenderNode(child);
				}

				ImGui.TreePop();
			}
		}

		public struct FieldChange
		{
			public readonly object NewValue = null;
			public readonly bool ContainsChange = false;
			
			public static readonly FieldChange None = new FieldChange();

			public FieldChange(object newValue)
			{
				NewValue = newValue;
				ContainsChange = true;
			}
			public FieldChange()
			{
				ContainsChange = false;
			}
		}

		
		private void RenderReflectionFields(object context)
		{
			Dictionary<Type, Func<object, string, FieldChange>> mapped = new();
			mapped.Add(typeof(Vector2), RenderField_Vector2);
			mapped.Add(typeof(Vector3), RenderField_Vector3);
			mapped.Add(typeof(string), RenderField_String);
			mapped.Add(typeof(bool), RenderField_Bool);
			mapped.Add(typeof(float), RenderField_Float);
			mapped.Add(typeof(int), RenderField_Int);
			
			mapped.Add(typeof(Color), RenderField_Color);
			mapped.Add(typeof(Rectangle), RenderField_Rectangle);
			mapped.Add(typeof(Texture2D), RenderField_Texture);
			
			mapped.Add(typeof(PSMB.Object), RenderField_Object);
			//mapped.Add(typeof(PSMB.CollisionType), RenderField_Enum<CollisionType>);
			
			var type   = context.GetType();
			var flags  = BindingFlags.Instance | BindingFlags.Public;
			
			/*
			var fields = type.GetFields(flags);
			foreach(var field in fields)
			{
				if(field.GetCustomAttribute<HideInEditorAttribute>() != null)
				{
					continue;
				}

				if(mapped.TryGetValue(
						field.FieldType,
						out Func<object, string, FieldChange> f
					) == false)
				{
					ImGui.Text($"{field.Name} -> Type unsupported");
					continue;
				}

				string uniqueName = string.Empty;
				if(context is Object obj)
				{
					uniqueName = obj.Name;
				} else if(context is Component cmp)
				{
					uniqueName = $"{cmp.ID}_??";
				}

				var newVal = f.Invoke(field.GetValue(context), $"{field.Name}##{uniqueName}");
				field.SetValue(context, newVal);
			}
			*/
			
			var props  = type.GetProperties(flags);
			foreach(var prop in props)
			{
				if(prop.GetCustomAttribute<HideInEditorAttribute>() != null)
				{
					continue;
				}
				
				if(prop.CanRead == false)
				{
					continue;
				}
				
				if(mapped.TryGetValue(
						prop.PropertyType,
						out Func<object, string, FieldChange> f
					)
					== false)
				{
					ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1));
					ImGui.Text($"{prop.Name} -> Type unsupported");
					ImGui.PopStyleColor();
					continue;
				}
				
				string uniqueName = string.Empty;
				if(context is Object obj)
				{
					uniqueName = obj.Name;
				}
				else if(context is Component cmp)
				{
					uniqueName = $"{cmp.ID}_??";
				}
				
				if(prop.CanWrite == false)
				{
					ImGui.BeginDisabled();
				}
				var result = f.Invoke(prop.GetValue(context), $"{prop.Name}##{uniqueName}");
				if(prop.CanWrite)
				{
					if(result.ContainsChange)
					{
						prop.SetValue(context, result.NewValue);
					}
				}
				else
				{
					ImGui.EndDisabled();
				}
			}
		}

		private FieldChange RenderField_Vector2(object position, string name)
		{
			var pos = (Vector2)position;
			if(ImGui.DragFloat2(name, ref pos))
			{
				return new(pos);
			}
			return FieldChange.None;
		}

		private FieldChange RenderField_Vector3(object position, string name)
		{
			var pos = (Vector3)position;
			if(ImGui.DragFloat3(name, ref pos))
			{
				return new(pos);
			}
			return FieldChange.None;
		}

		private FieldChange RenderField_String(object text, string name)
		{
			var str = (string)text;
			if(text == null)
			{
				str = string.Empty; 
			}
			if(ImGui.InputText(name, ref str, 64))
			{
				return new(str);
			}
			return FieldChange.None;
		}

		private FieldChange RenderField_Bool(object boolean, string name)
		{
			var b = (bool)boolean;
			if(ImGui.Checkbox(name, ref b))
			{
				return new(b);
			}
			return FieldChange.None;
		}

		private FieldChange RenderField_Int(object integer, string name)
		{
			var i = (int)integer;
			if(ImGui.InputInt(name, ref i))
			{
				return new(i);
			}
			return FieldChange.None;
		}
		
		private FieldChange RenderField_Float(object floating, string name)
		{
			var f = (float)floating;
			if(ImGui.InputFloat(name, ref f))
			{
				return new(f);
			}
			return FieldChange.None;
		}
		
		private FieldChange RenderField_Color(object color, string name)
		{
			var c = (Color)color;
			var newC = new Vector4(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f, c.A / 255.0f);
			if(ImGui.ColorEdit4(name, ref newC))
			{
				return new(new Color(newC.X, newC.Y, newC.Z, newC.W));
			}
			return FieldChange.None;
		}
		
		private FieldChange RenderField_Texture(object texture, string name)
		{
			var t  = (Texture2D)texture;
			
			RenderFieldPrefix(name);
			
			rlImGui.ImageSize(t, 256, 256);
			return FieldChange.None;
		}
		
		private FieldChange RenderField_Rectangle(object rect, string name)
		{
			var r    = (Rectangle)rect;
			var newR = new Vector4(r.X, r.Y, r.Width, r.Height);
			if(ImGui.InputFloat4(name, ref newR))
			{
				return new(new Rectangle(newR.X, newR.Y, newR.Z, newR.W));
			}
			return FieldChange.None;
		}
		
		private FieldChange RenderField_RectangleNullable(object rect, string name)
		{
			if(rect == null)
			{
				rect = new();
			}
			
			var r    = (Rectangle?)rect;
			var newR = new Vector4(
				r.Value.X, r.Value.Y, r.Value.Width, r.Value.Height
			);
			if(ImGui.InputFloat4(name, ref newR))
			{
				return new(newR);
			}
			return FieldChange.None;
		}

		// private FieldChange RenderField_CollisionType(object type, string name)
		// {
		// 	RenderFieldPrefix(name);
		// 	return new(EnumComboBox(name, (CollisionType)type));
		// }

		private FieldChange RenderField_Enum<T>(object type, string name)
		{
			RenderFieldPrefix(name);
			return new(EnumComboBox(name, (T)type));
		}

		private T EnumComboBox<T>(string name, T currentValue)
		{
			var names       = Enum.GetNames(typeof(T));
			var currentName = currentValue.ToString();
			if(ImGui.BeginCombo($"##{name}", currentName))
			{
				foreach(string n in names)
				{
					bool isSelected = n == currentName;

					if(ImGui.Selectable(n, isSelected))
					{
						var result = (T)Enum.Parse(typeof(T), n);
						ImGui.EndCombo();

						return result;
						break;
					}

					if(isSelected)
					{
						ImGui.SetItemDefaultFocus();
					}
				}

				ImGui.EndCombo();
			}
			return currentValue;
		}
		
		private FieldChange RenderField_Object(object obj, string name)
		{
			var o          = (PSMB.Object)obj;
			RenderFieldPrefix(name);
			
			if(o != null)
			{
				var rnd = (random.Next().ToString());
				if(ImGui.Button($"{o.Name}##{name}"))
				{
					openedInspectors.Add(o);
					inspectorCurrentTab = "Main Inspector";
				}
				ImGui.SameLine();
				ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1f, 0f, 0f, 1f));
				if(ImGui.Button($"Clear##{name}"))
				{
					FieldChange result = new(null);
					ImGui.PopStyleColor(1);
					return result;
				}
				ImGui.PopStyleColor(1);
			}
			else
			{
				ImGui.Text("No parent!");
			}
			return FieldChange.None;
		}

		private void RenderFieldPrefix(string name)
		{
			var nameParsed = name;
			if(name.Contains("##"))
			{
				var idx = name.IndexOf("##", StringComparison.Ordinal);
				if(idx > 0)
				{
					nameParsed = name[..idx];
				}
			}
			
			ImGui.Text($"{nameParsed}: ");
			ImGui.SameLine();
		}
	}
}