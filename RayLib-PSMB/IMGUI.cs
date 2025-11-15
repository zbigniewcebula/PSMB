using System.Numerics;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace PSMB
{
	internal class IMGUI
	{
		private int hierarchySelectedObject = -1;
		private string inspectorCurrentTab = "Main Inspector";
		
		private Random random = new Random();
		
		public IMGUI()
		{
			rlImGui.Setup(true);
		}

		~IMGUI()
		{
			rlImGui.Shutdown();
		}

		public void Render(Renderer render, float deltaTime)
		{
			rlImGui.Begin(deltaTime);
			{
				RenderHierarchy(render);
				RenderInspector(render);
			}
			rlImGui.End();	
		}

		private void RenderInspector(Renderer render)
		{
			if(hierarchySelectedObject == -1)
			{
				return;
			}

			var context = Object.All[hierarchySelectedObject];
			
			if(ImGui.Begin(
					"Inspector", 
					ImGuiWindowFlags.NoCollapse
					| ImGuiWindowFlags.NoMove
					| ImGuiWindowFlags.NoResize
				) == false)
			{
				return;
			}
			var size = new Vector2(400, render.Size.Y);
			ImGui.SetWindowSize("Inspector", size);
			ImGui.SetWindowPos("Inspector", new(render.Size.X - size.X, 0));
			size.X = size.X * 0.9f;

			if (ImGui.BeginTabBar("MainTabs"))
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
				if (ImGui.CollapsingHeader($"{type.Name}##{subID}", ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent(20);
					RenderReflectionFields(component);
					ImGui.Unindent(20);
				}
				
				++i;
			}
				
			ImGui.EndTabItem();
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
			if (!ImGui.Begin(
					"Hierarchy",
					ImGuiWindowFlags.NoCollapse |
					ImGuiWindowFlags.NoMove |
					ImGuiWindowFlags.NoResize))
				return;

			ImGui.SetWindowSize(new Vector2(200, render.Size.Y));
			ImGui.SetWindowPos(new(0, 0));

			ImGui.BeginChild("Scrolling");

			for(var index = 0; index < Object.All.Count; index++)
			{
				var obj = Object.All[index];
				if(obj.Parent == null)
				{
					RenderNode(obj, index);
				}
			}

			ImGui.EndChild();
			ImGui.End();
		}

		private void RenderNode(Object obj, int id)
		{
			bool isSelected = (hierarchySelectedObject == id);

			// Flags control tree behavior
			ImGuiTreeNodeFlags flags =
				ImGuiTreeNodeFlags.OpenOnArrow |
				ImGuiTreeNodeFlags.OpenOnDoubleClick |
				ImGuiTreeNodeFlags.SpanFullWidth |
				(isSelected ? ImGuiTreeNodeFlags.Selected : 0);

			// If no children -> make it a leaf node
			if (obj.ChildCount == 0)
				flags |= ImGuiTreeNodeFlags.Leaf;

			bool open = ImGui.TreeNodeEx($"{obj.Name}##{id}", flags);

			// Selection detection
			if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
			{
				hierarchySelectedObject = id;
			}

			if (open)
			{
				foreach(var child in obj)
				{
					int childID = Object.All.IndexOf(child);
					RenderNode(child, childID);
				}

				ImGui.TreePop();
			}
		}

		private void RenderReflectionFields(object context)
		{
			Dictionary<Type, Func<object, string, object>> mapped = new();
			mapped.Add(typeof(PSMB.Object), RenderField_Object);
			mapped.Add(typeof(Vector2), RenderField_Vector2);
			mapped.Add(typeof(Color), RenderField_Color);
			mapped.Add(typeof(string), RenderField_String);
			mapped.Add(typeof(bool), RenderField_Bool);
			mapped.Add(typeof(float), RenderField_Float);
			mapped.Add(typeof(int), RenderField_Int);
			
			var type   = context.GetType();
			var flags  = BindingFlags.Instance | BindingFlags.Public;
			var fields = type.GetFields(flags);
			foreach(var field in fields)
			{
				if(mapped.TryGetValue(
						field.FieldType,
						out Func<object, string, object> f
					)
					== false)
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
					uniqueName = $"{type.Name}_??";
				}
				
				var newVal = f.Invoke(field.GetValue(context), $"{field.Name}##{uniqueName}");
				field.SetValue(context, newVal);
			}
			
			var props  = type.GetProperties(flags);
			foreach(var prop in props)
			{
				if(prop.CanRead == false)
				{
					continue;
				}
				
				if(mapped.TryGetValue(
						prop.PropertyType,
						out Func<object, string, object> f
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
				} else if(context is Component cmp)
				{
					uniqueName = $"{type.Name}_??";
				}
				
				if(prop.CanWrite == false)
				{
					ImGui.BeginDisabled();
				}
				var newVal = f.Invoke(prop.GetValue(context), $"{prop.Name}##{uniqueName}");
				if(prop.CanWrite)
				{
					if(newVal != null)
					{
						prop.SetValue(context, newVal);
					}
				}
				else
				{
					ImGui.EndDisabled();
				}
			}
		}

		private object RenderField_Vector2(object position, string name)
		{
			var pos = (Vector2)position;
			if(ImGui.InputFloat2(name, ref pos))
			{
				return pos;
			}
			return null;
		}

		private object RenderField_String(object text, string name)
		{
			var str = (string)text;
			if(str == null)
			{
				str = string.Empty; 
			}
			if(ImGui.InputText(name, ref str, 64))
			{
				return str;
			}
			return null;
		}

		private object RenderField_Bool(object boolean, string name)
		{
			var b = (bool)boolean;
			if(ImGui.Checkbox(name, ref b))
			{
				return b;
			}
			return null;
		}

		private object RenderField_Int(object integer, string name)
		{
			var i = (int)integer;
			if(ImGui.InputInt(name, ref i))
			{
				return i;
			}
			return null;
		}
		
		private object RenderField_Float(object floating, string name)
		{
			var f = (float)floating;
			if(ImGui.InputFloat(name, ref f))
			{
				return f;
			}
			return null;
		}
		
		private object RenderField_Color(object color, string name)
		{
			var c = (Color)color;
			var newC = new Vector4(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f, c.A / 255.0f);
			if(ImGui.ColorEdit4(name, ref newC))
			{
				return new Color(newC.X, newC.Y, newC.Z, newC.W);
			}
			return null;
		}
		
		private object RenderField_Object(object obj, string name)
		{
			var o          = (PSMB.Object)obj;
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
			if(o != null)
			{
				var rnd = (random.Next().ToString());
				if(ImGui.Button($"{o.Name}##{name.Replace("??", rnd)}"))
				{
					hierarchySelectedObject = Object.All.IndexOf(o);
					inspectorCurrentTab = "Main Inspector";
				}
			}
			else
			{
				ImGui.Text("No parent!");
			}
			return null;
		}
	}
}