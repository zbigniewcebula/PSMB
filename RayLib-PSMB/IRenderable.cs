namespace PSMB
{
	public interface IRenderable
	{
		public void Render(Renderer renderer);
		public void RenderDebug(Renderer renderer);
	}

	public interface IRenderableOutsideViewport {}
}