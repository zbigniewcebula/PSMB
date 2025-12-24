using System.Numerics;
using Raylib_cs;

namespace PSMB
{
	public class Camera: Component,
						IRenderable,
						IRenderableOutsideViewport
	{
		[HideInEditor]
		public Camera2D Camera2D => camera2D;

		public float Zoom
		{
			get => camera2D.Zoom;
			set => camera2D.Zoom = value;
		}

		public Vector2 Resolution
		{
			get => new(
				renderTexture.Texture.Dimensions.X,
				renderTexture.Texture.Dimensions.Y
			);
			set => ResizeRenderTexture(value);
		}

		private Camera2D camera2D;
		private RenderTexture2D renderTexture;

		private float freecamSpeed = 5000f;

		public override void OnCreate()
		{
			Zoom = 1.0f;

			ResizeRenderTexture(new Vector2(100, 100));

			Parent.OnPositionChanged += OnPositionChange;
			Parent.OnRotationChanged += OnRotationChange;
		}

		private void OnRotationChange(float oldRot, float newRot)
		{
			camera2D.Rotation = newRot;
		}

		private void OnPositionChange(Vector2 oldP, Vector2 newP)
		{
			camera2D.Offset = new(-newP.X, newP.Y);
		}

		private void ResizeRenderTexture(Vector2 value)
		{
			Raylib.UnloadRenderTexture(renderTexture);
			renderTexture = Raylib.LoadRenderTexture(
				(int)value.X,
				(int)value.Y
			);
		}

		public void Render(Renderer renderer)
		{
			Raylib.DrawTexture(
				renderTexture.Texture,
				0,
				0,
				Color.White
			);
		}

		public void RenderDebug(Renderer renderer)
		{
			// Raylib.DrawText(
			// 	$"({-camera2D.Offset.X:F2}; {camera2D.Offset.Y:F2}) x{Zoom:F2}",
			// 	((int)-camera2D.Offset.X) + 100,
			// 	((int)-camera2D.Offset.Y) + 40,
			// 	24,
			// 	Color.Black
			// );
		}
	}
}