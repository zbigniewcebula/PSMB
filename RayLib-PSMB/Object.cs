using System.Collections;
using System.Numerics;

namespace PSMB
{
	public class Object : IEnumerable<Object>
	{
		public static IReadOnlyList<Object> All => all; 
		protected static List<Object> all = new(); 
		
		public string Name { get; set; }
		public string Tag { get; set; }
		
		public Object Parent { get => parent; set => SetParent(value); }

		public bool Active
		{
			get => active;
			set
			{
				if(active != value)
				{
					OnActiveChanged?.Invoke(active = value);
				}
			}
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

		public Vector2 LocalPosition
		{
			get => localPosition;
			set
			{
				if(value != localPosition)
				{
					OnPositionChanged?.Invoke(localPosition, value);
					localPosition = value;
				}
			}
		}
		private Vector2 localPosition = Vector2.Zero;
		
		public float LocalRotation { get; set; } = 0f;
		public Vector2 LocalScale { get; set; } = Vector2.One;
		
		public int ChildCount => children.Count;

		public event Action<bool> OnActiveChanged;
		public event Action<Object, Object> OnParentChanged;
		public event Action<Component> OnComponentAdded;
		public event Action<Component> OnComponentRemoved;
		
		public event Action<Vector2, Vector2> OnPositionChanged;
		public event Action<Vector2, Vector2> OnScaleChanged;
		public event Action<float, float> OnRotationChanged;
		
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

			var oldParent = parent;
			if(parent != null)
			{
				parent.RemoveChild(this);
			}
			newParent.AddChild(this);
			
			parent = newParent;
			OnParentChanged?.Invoke(oldParent, newParent);
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
			
			component.OnCreate();
			return component;
		}

		public bool RemoveComponent<T>(T component) where T: Component, new()
		{
			bool result = components.Remove(component);
			if(result)
			{
				OnComponentRemoved?.Invoke(component);
			}

			component.OnDestroy();

			component
				.GetType()
				.GetProperty("Parent")
				?.SetValue(component, null, null);
			
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
}