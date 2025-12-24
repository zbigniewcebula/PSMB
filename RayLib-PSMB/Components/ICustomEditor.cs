namespace PSMB
{
	public interface ICustomEditor
	{
		[HideInEditor]
		public bool OverrideEditor { get; }

		public void RenderEditor();
	}
}