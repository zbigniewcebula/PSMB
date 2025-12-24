using Raylib_cs;

namespace PSMB
{
	public static class ObjectUtils
	{
		public static void RenderAll(Renderer renderer)
		{
			var viewport = renderer.GetViewport();
			var renderable = Object.All
								.Where(o => IsInViewport(o, viewport))
								.SelectMany(o => o.GetAllComponents<IRenderable>());
			
			foreach(var obj in renderable)
			{
				obj.Render(renderer);
			}
		}
		
		public static void RenderDebugAll(Renderer renderer)
		{
			var viewport = renderer.GetViewport();
			var renderable = Object.All
								.Where(o => IsInViewport(o, viewport))
								.SelectMany(o => o.GetAllComponents<IRenderable>());
			
			foreach(var obj in renderable)
			{
				obj?.RenderDebug(renderer);
			}
		}

		private static bool IsInViewport(Object o, Rectangle viewport)
		{
			if(o.GetAllComponents<IRenderableOutsideViewport>().Any())
			{
				return true;
			}
			return o.Position.X > (viewport.Position.X - viewport.Width / 10)
				&& o.Position.X < (viewport.Position.X + viewport.Width);
		}

		public static void UpdateAll(float delta)
		{
			var updatable = GetObjectsWithComponent<IUpdatable>();
			foreach(var obj in updatable)
			{
				obj.Update(delta);
			}
		}
		
		public static IEnumerable<T> GetObjectsWithComponent<T>()
		{
			HashSet<T> components = new();
			foreach(var obj in Object.All)
			{
				components.UnionWith(GatherComponents<T>(obj));
			}
			
			return components;

			IEnumerable<K> GatherComponents<K>(Object parent)
			{
				HashSet<K> childComponents = new(parent.GetAllComponents<K>());
				if(parent.ChildCount > 0)
				{
					foreach(var child in parent)
					{
						childComponents.UnionWith(GatherComponents<K>(child));
					}
				}

				return childComponents;
			}
		}
	}
}