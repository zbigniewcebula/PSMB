using Raylib_cs;

namespace PSMB
{
	public static class DebugShortcuts
	{
		private static Dictionary<KeyboardKey, Action> registered = new();
		
		public static void RegisterCallback(KeyboardKey key, Action callback)
		{
			if(registered.TryAdd(key, callback) == false)
			{
				//In case of existing calls, just encapsulate older ones with new one
				var old = registered[key];
				registered[key] = () => { old?.Invoke(); };
			}
		}

		public static void Update()
		{
			foreach(var (key, act) in registered)
			{
				if(Raylib.IsKeyReleased(key))
				{
					act?.Invoke();
				}
			}
		}
	}
}