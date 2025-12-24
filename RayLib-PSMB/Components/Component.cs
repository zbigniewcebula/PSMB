using System.Linq.Expressions;
using System.Reflection;

namespace PSMB
{
	public class Component
	{
		[HideInEditor] public Guid ID { get; internal set; }
		[HideInEditor] public Object Parent { get; protected set; }

		public event Action OnDestroyed;

		internal Component()
		{
			ID =  Guid.NewGuid();
		}

		virtual public void OnCreate()
		{
		}

		virtual public void OnDestroy()
		{
			OnDestroyed?.Invoke();
			OnDestroyed = null!;
		}
		
		public static class Factory<T> where T : Component
		{
			public static readonly Func<PSMB.Object, T> Create;

			static Factory()
			{
				var ownerParam = Expression.Parameter(typeof(PSMB.Object), "owner");

				// Require parameterless constructor
				var ctor = typeof(T).GetConstructor(Type.EmptyTypes)
					?? throw new InvalidOperationException($"{typeof(T).Name} must have a parameterless constructor.");

				// Find Parent property (internal/private allowed)
				var parentProp = typeof(T).GetProperty(
						"Parent",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					?? throw new InvalidOperationException($"{typeof(T).Name} must define a Parent property.");

				var setter = parentProp.SetMethod
					?? throw new InvalidOperationException($"{typeof(T).Name} must have a setter for Parent property.");

				// new T()
				var newExpr = Expression.New(ctor);

				// instance variable
				var instanceVar = Expression.Variable(typeof(T), "instance");

				// instance = new T();
				var assignInstance = Expression.Assign(instanceVar, newExpr);

				// instance.Parent = owner;
				var assignParent = Expression.Call(instanceVar, setter, ownerParam);

				// return instance
				var block = Expression.Block(
					new[] { instanceVar },
					assignInstance,
					assignParent,
					instanceVar);

				Create = Expression.Lambda<Func<PSMB.Object, T>>(block, ownerParam).Compile();
			}
		}
	}
}