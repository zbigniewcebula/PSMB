using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public static class Utils
	{
		public const float Epsilon = 0.0001f;
		public const float EpsilonSqr = 0.00001f;
		
		public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
		{
			int i = 0;
			foreach(T element in self)
			{
				if(Equals(element, elementToFind))
				{
					return i;
				}
				i++;
			}
			return -1;
		}
		
		public static bool IsZero(this Rectangle rect)
		{
			return rect.Position == Vector2.Zero && rect.Size == Vector2.Zero;
		}
		
		public static Vector2 RestrictPoint(this Rectangle rect, Vector2 point)
		{
			if(point.X < rect.X)
			{
				point.X = rect.X;
			}
			if(point.Y < rect.Y)
			{
				point.Y = rect.Y;
			}
			float maxX = rect.X + rect.Width;
			if(point.X > maxX)
			{
				point.X = maxX;
			}
			float maxY = rect.Y + rect.Height;
			if(point.Y > maxY)
			{
				point.Y = maxY;
			}

			return point;
		}

		public static LineSegment[] ExtractLines(this Rectangle rect)
		{
			var secondCorner = new Vector2(rect.X + rect.Width, rect.Y + rect.Height);
			var lines = new LineSegment[]
			{
				new(rect.Position, new(rect.X + rect.Width, rect.Y)), 
				new(rect.Position, new(rect.X, rect.Y + rect.Height)),
				new(new(rect.X + rect.Width, rect.Y), secondCorner),
				new(new(rect.X, rect.Y + rect.Height), secondCorner),
			};

			foreach(var segment in lines)
			{
				var middle   = (segment.start + segment.end) / 2;
				var outwards = middle - rect.Center;
				segment.normal = outwards / outwards.Length();
			}

			return lines;
		}

		public class LineSegment
		{
			public Vector2 start;
			public Vector2 end;
			public Vector2 normal;
			
			public LineSegment(Vector2 start, Vector2 end)
			{
				this.start = start;
				this.end = end;
			}
		}
		
		public static Vector2[] ExtractVertices(this Rectangle rect)
		{
			return new Vector2[]
			{
				rect.Position,
				new(rect.X + rect.Width, rect.Y), 
				new Vector2(rect.X + rect.Width, rect.Y + rect.Height),
				new(rect.X, rect.Y + rect.Height),
			};
		}

		public static bool Approximately(float a, float b)
		{
			return Math.Abs(a - b) < Epsilon;
		}

		public static Vector2? IntersectionCheck(
			Vector2 Astart, Vector2 Aend, Vector2 Bstart, Vector2 Bend
		) {
			// Direction vectors
			var r = Aend - Astart;
			var s = Bend - Bstart;

			float rxs  = r.X * s.Y - r.Y * s.X;
			float qpxr = (Bstart.X - Astart.X) * r.Y - (Bstart.Y - Astart.Y) * r.X;

			// If r × s == 0 and (C - A) × r == 0, then the lines are collinear.
			if(Approximately(rxs, 0f) && Approximately(qpxr, 0f))
			{
				return null; // collinear => treat as non-intersecting (you can change this behavior)}
			}

			// If r × s == 0 and (C - A) × r != 0, then lines are parallel.
			if(Approximately(rxs, 0f) && Approximately(qpxr, 0f) == false)
			{
				return null;
			}

			float t = ((Bstart.X - Astart.X) * s.Y - (Bstart.Y - Astart.Y) * s.X) / rxs;
			float u = ((Bstart.X - Astart.X) * r.Y - (Bstart.Y - Astart.Y) * r.X) / rxs;

			// For line *segments*: intersection occurs if t and u are between 0 and 1
			if(t >= 0f && t <= 1f && u >= 0f && u <= 1f)
			{
				return Astart + t * r; // intersection point
			}

			return null; // no intersection within the segments
		}

		/*
		public static Vector2? IntersectionCheck(
			Rectangle rect,
			Vector2 intersectingLineStart,
			Vector2 intersectingLineEnd
		)
		{
			var lines  = rect.ExtractLines();
			var intersectionPoints = new Vector2?[4];
			ushort count           = 0;
			foreach(var rectLine in lines)
			{
				var intersection = Utils.IntersectionCheck(
					intersectingLineStart, intersectingLineEnd,
					rectLine.start, rectLine.end
				);
				if(intersection != null)
				{
					intersectionPoints[count] = intersection.Value;
					++count;
				}
			}

			if(count == 0)
			{
				return null;
			}

			var sortedIntersectionPoints = intersectionPoints
										.Where(p => p != null)
										.OrderBy(p => Vector2.Distance(p.Value, rect.Center));
			return sortedIntersectionPoints.FirstOrDefault();
		}
		*/
		
		public static Vector2 SlideAlong(Vector2 inVec, Vector2 normal)
		{
			normal = normal / normal.Length();
			float dot = Vector2.Dot(inVec, normal);
			if(dot > 0)
			{
				return inVec;
			}
			return inVec - dot * normal;
		}
		
		public static (LineSegment line, Vector2 point) IntersectionCheck(
			Rectangle rect,
			Vector2 intersectingLineStart,
			Vector2 intersectingLineEnd
		)
		{
			var    lines              = rect.ExtractLines();
			var    intersectionLines = new (LineSegment, Vector2)[4];
			ushort count              = 0;
			foreach(var rectLine in lines)
			{
				var intersection = Utils.IntersectionCheck(
					intersectingLineStart, intersectingLineEnd,
					rectLine.start, rectLine.end
				);
				if(intersection != null)
				{
					intersectionLines[count] = (rectLine, intersection.Value);
					++count;
				}
			}

			if(count == 0)
			{
				return (null, Vector2.Zero);
			}

			var sortedIntersectionPoints = intersectionLines
										.Take(count)
										.OrderBy(p => Vector2.Distance(p.Item2, intersectingLineStart));
			return sortedIntersectionPoints.FirstOrDefault();
		}
	}
}