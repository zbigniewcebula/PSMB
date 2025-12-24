using System.Diagnostics;
using System.Numerics;
using ImGuiNET;

namespace PSMB
{
	public static class Logger
	{
		private static List<Entry> history = new(20);
		
		static Logger()
		{
			
		}

		public static void Clear()
		{
			history.Clear();
		}
		
		public static void Log(string tag, params object[] args)
		{
			Log(tag, string.Join(" ", args));
		}
		
		public static void Log(string tag, string msg)
		{
			Log($"[{tag}] {msg}");
		}

		public static void Log(string msg)
		{
			history.Add(new()
			{
				text = msg,
				type = Entry.Type.Log,
				stack = new(true)
			});
			Console.WriteLine(msg);
		}
		
		public static void Error(string tag, params object[] args)
		{
			var msg = string.Join(" ", args);
			Error(tag, msg);
		}
		
		public static void Error(string tag, string msg)
		{
			Error($"[{tag}] {msg}");
		}
		
		public static void Error(string msg)
		{
			history.Add(new()
			{
				text  = msg,
				type  = Entry.Type.Error,
				stack = new(true)
			});
			Console.Error.WriteLine(msg);
		}

		private static bool logVisible = true;
		private static bool errorVisible = true;
		public static bool RenderConsole(Renderer render)
		{
			Vector2 minSize  = new(500, 300);
			if(ImGui.Begin("Console", ImGuiWindowFlags.MenuBar) == false)
			{
				ImGui.End();
				ImGui.SetWindowCollapsed("Console", false);
				return false;
			}
			Vector2 currentSize = ImGui.GetWindowSize();
			if(currentSize.X < minSize.X)
			{
				currentSize.X = minSize.X;
				ImGui.SetWindowSize("Console", currentSize);
			}
			if(currentSize.Y < minSize.Y)
			{
				currentSize.Y = minSize.Y;
				ImGui.SetWindowSize("Console", currentSize);
			}

			ImGui.BeginMenuBar();
			{
				int log = 0, err = 0;
				foreach(var entry in history)
				{
					if(entry.type == Entry.Type.Log)
					{
						++log;
					}
					else if(entry.type == Entry.Type.Error)
					{
						++err;
					}
				}
				ImGui.Text($"Entries: {log + err}");
				ImGui.SameLine();
				ImGui.Separator();
				ImGui.SameLine();
				ImGui.Checkbox($"Log: {log}", ref logVisible);
				ImGui.SameLine();
				ImGui.Separator();
				ImGui.SameLine();
				ImGui.Checkbox($"Err: {err}", ref errorVisible);
				ImGui.SameLine();
				ImGui.Separator();
				ImGui.SameLine();
				if(ImGui.Button("Clear"))
				{
					Clear();
				}
				ImGui.SameLine();
				ImGui.Separator();
				ImGui.SameLine();
				if(ImGui.Button("Close"))
				{
					ImGui.EndMenuBar();
					ImGui.End();
					return false;
				}
			}
			ImGui.EndMenuBar();
			
			ImGui.BeginChild(
				"ConsoleContent",
				new(),
				ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY
				| ImGuiChildFlags.AlwaysAutoResize
			);
			var   lineSize  = ImGui.CalcTextSize("Hello World!");
			float height    = lineSize.Y;
			var   lines     = (int)Math.Floor(currentSize.Y / height);
			var   windowPos = ImGui.GetWindowPos();
			
			// -- // -- // -- // -- // -- // -- // -- // -- // -- 
			foreach(var entry in history)//.TakeLast(lines))
			{
				if((entry.type == Entry.Type.Log && logVisible == false)
				|| (entry.type == Entry.Type.Error && errorVisible == false)
				) {
					continue;
				}
				
				ImGui.PushStyleColor(ImGuiCol.Text, entry.type.ToColor());
				if(ImGui.Selectable($"{entry.text}##{entry.id}", false))
				{
					entry.preview = true;
				}
				ImGui.PopStyleColor();
				
				RenderEntry(entry);
			}
			// -- // -- // -- // -- // -- // -- // -- // -- // -- 
			ImGui.EndChild();
			ImGui.End();

			return true;
		}

		private static void RenderEntry(Entry context)
		{
			if(context.preview == false)
			{
				return;
			}

			var title = $"Entry - {context.type}##{context.id}";
			if(ImGui.Begin(title) == false)
			{
				ImGui.End();
				context.preview = false;
				ImGui.SetWindowCollapsed(title, false);
				return;
			}
			
			ImGui.TextColored(context.type.ToColor(), context.text);
			ImGui.Separator();
			foreach(var frame in context.stack.GetFrames())
			{
				var method = frame.GetMethod();
				if(method != null)
				{
					if(method.DeclaringType == typeof(PSMB.Logger))
					{
						continue;
					}
				}
				ImGui.Text(frame.ToString());
			}
			
			ImGui.End();
		}

		private class Entry
		{
			public readonly Guid id;
			
			public string text;
			public Type type;
			public StackTrace stack;
			public bool preview;

			public Entry()
			{
				id = Guid.NewGuid();
			}
			
			public enum Type
			{
				Log,
				Error
			}
		}
		
		private static Vector4 ToColor(this Entry.Type type)
		{
			var typeColor = new Vector4(
				1, 1, 1, 1
			);
			switch(type)
			{
				case Entry.Type.Error:
				{
					typeColor = new(
						1, 0, 0, 1
					);
					break;
				}
			}
			return typeColor;
		}
	}
}