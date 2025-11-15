using System.Numerics;

namespace PSMB
{
	public class Follower : Component, IUpdatable
	{
		public Object FollowTarget { get; set; }
		
		public Vector2 Offset { get; set; }
		
		public void Update(float delta)
		{
			if(FollowTarget != null)
			{
				Parent.Position = FollowTarget.Position + Offset;
			}
		}
	}
}