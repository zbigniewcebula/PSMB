using System.Collections;
using System.Numerics;

namespace PSMB
{
	public class Object : IEnumerable<Object>
	{
		public static IReadOnlyList<Object> All => all; 
		private static List<Object> all = new(); 
		
		public string Name { get; set; }
		public string Tag { get; set; }
		
		public Object Parent { get => parent; set => SetParent(value); }

		public bool Active
		{
			get => active; set => OnActiveChanged?.Invoke(active = value);
		}
		
		public Vector2 Position
		{
			get => GetGlobalPosition(); set => SetGlobalPosition(value);
		}

		public float Rotation
		{
			get => GetGlobalRotation(); set => SetGlobalRotation(value);
		}

		public Vector2 Scale
		{
			get => GetGlobalScale(); set => SetGlobalScale(value);
		}
		
		public Vector2 LocalPosition { get; set; } = Vector2.Zero;
		public float LocalRotation { get; set; } = 0f;
		public Vector2 LocalScale { get; set; } = Vector2.One;
		
		public int ChildCount => children.Count;

		public event Action<bool> OnActiveChanged;
		public event Action<Object> OnParentChanged;
		public event Action<Component> OnComponentAdded;
		public event Action<Component> OnComponentRemoved;
		
		private Object parent = null;
		
		private bool active = true;
		private List<Component> components = new();
		private HashSet<Object> children = new();

		public Object(string name)
		{
			Name = name;
			
			all.Add(this);
		}

		~Object()
		{
			all.Remove(this);
		}
		
		private void SetParent(Object newParent)
		{
			if(newParent == this)
			{
				return;
			}

			if(parent != null)
			{
				parent.RemoveChild(this);
			}
			newParent.AddChild(this);
			
			parent = newParent;
			OnParentChanged?.Invoke(newParent);
		}

		private void AddChild(Object child)
		{
			children.Add(child);
		}
		
		private void RemoveChild(Object child)
		{
			children.Remove(child);
		}

		public T AddComponent<T>() where T: Component
		{
			var component = Component.Factory<T>.Create(this);
			components.Add(component);
			OnComponentAdded?.Invoke(component);

			return component;
		}

		public bool RemoveComponent<T>(T component) where T: Component, new()
		{
			bool result = components.Remove(component);
			if(result)
			{
				OnComponentRemoved?.Invoke(component);
			}

			return result;
		}

		public int RemoveAllComponents<T>() where T: Component, new()
		{
			var found = components
			.Where(c => c.GetType() == typeof(T));
			int counter = 0;
			foreach(T component in found)
			{
				if(RemoveComponent<T>(component))
				{
					++counter;
				}
			}

			return counter;
		}

		public T GetComponent<T>() where T: Component
		{
			foreach(var component in components)
			{
				if(component is T tComponent)
				{
					return tComponent;
				}
			}

			return default;
		}

		public IEnumerable<T> GetAllComponents<T>()
		{
			return components.Where(c => c is T).Cast<T>();
		}

		private void SetGlobalPosition(Vector2 value)
		{
			if(Parent == null)
			{
				LocalPosition = value;

				return;
			}

			var parent = Parent.GetGlobalPosition();
			LocalPosition = value - parent;
		}

		private Vector2 GetGlobalPosition()
		{
			if(Parent != null)
			{
				return Parent.GetGlobalPosition() + LocalPosition * Parent.GetGlobalScale();
			}

			return LocalPosition;
		}

		private void SetGlobalRotation(float value)
		{
			if(Parent == null)
			{
				LocalRotation = value;

				return;
			}

			var parent = Parent.GetGlobalRotation();
			LocalRotation = value - parent;
		}

		private float GetGlobalRotation()
		{
			if(Parent != null)
			{
				return Parent.GetGlobalRotation() + LocalRotation;
			}

			return LocalRotation;
		}

		private void SetGlobalScale(Vector2 value)
		{
			if(Parent == null)
			{
				LocalScale = value;

				return;
			}

			var parent = Parent.GetGlobalScale();
			LocalScale = value - parent;
		}

		private Vector2 GetGlobalScale()
		{
			if(Parent != null)
			{
				return Parent.GetGlobalScale() * LocalScale;
			}

			return LocalScale;
		}

		public IEnumerator<Object> GetEnumerator() => children.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
	
	public static class ObjectUtils
	{
		public static void RenderAll(Renderer renderer)
		{
			var viewport = renderer.GetViewport();
			var renderable = Object.All
								.Where(IsInViewport)
								.SelectMany(o => o.GetAllComponents<IRenderable>());
			
			foreach(var obj in renderable)
			{
				obj.Render(renderer);
			}

			return;

			bool IsInViewport(Object o)
			{
				return o.Position.X > (viewport.Position.X - viewport.Width / 10)
					&& o.Position.X < (viewport.Position.X + viewport.Width);
			}
		}
		public static void RenderDebugAll(Renderer renderer)
		{
			var viewport = renderer.GetViewport();
			var renderable = Object.All.Where(o => 
				o.Position.X > viewport.Position.X
				&& o.Position.X < viewport.Width
			).SelectMany(o => o.GetAllComponents<IRenderable>());
			
			foreach(var obj in renderable)
			{
				obj.RenderDebug(renderer);
			}
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