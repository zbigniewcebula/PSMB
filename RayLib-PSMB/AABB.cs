using System.Numerics;
using Box2D.NetStandard.Collision;
using Raylib_cs;

namespace PSMB
{
	public static class AABBExtensions
	{
		public static Vector2[] Rect2Points(this Rectangle rect)
		{	
			return new Vector2[]
			{
				new(rect.X, rect.Y),
				new(rect.X, rect.Y + rect.Height),
				new(rect.X + rect.Width, rect.Y),
				new(rect.X + rect.Width, rect.Y + rect.Height),
			};
		}
		
		public static bool Intersects(this Rectangle a, Rectangle b)
		{
			return Rect2Points(b).Any(p => a.Contains(p))
				|| Rect2Points(a).Any(p => b.Contains(p));
		}
		
		public static bool Contains(this Rectangle a, Vector2 point)
		{
			Vector2 A = new Vector2(a.X, a.Y);
			Vector2 C = new Vector2(a.X + a.Width, a.Y + a.Height);
			
			return (point.X > A.X  || Utils.Approximately(point.X, A.X))
				&& (point.X < C.X  || Utils.Approximately(point.X, C.X))
				&& (point.Y >= A.Y || Utils.Approximately(point.Y, A.Y))
				&& (point.Y <= C.Y || Utils.Approximately(point.Y, C.Y));
		}
	}
}