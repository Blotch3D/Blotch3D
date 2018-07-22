using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using Blotch;

namespace BlotchExample
{
	/// <summary>
	/// The 3D window. This must inherit from BlWindow3D. See BlWindow3D for detils.
	/// </summary>
	public class Example : BlWindow3D
	{
		/// <summary>
		/// This will be the torus model we draw in the window
		/// </summary>
		BlSprite Torus;

		/// <summary>
		/// This will be the font for the help menu we draw in the window
		/// </summary>
		SpriteFont Font;

		/// <summary>
		/// Texture for the torus
		/// </summary>
		Texture2D MyTexture;

		/// <summary>
		/// The help menu text that we draw in the window
		/// </summary>
		string Help = @"
Camera controls:
Wheel          - Dolly
CTRL-wheel     - Zoom
Left-drag      - Truck 
Right-drag     - Rotate
CTRL-left-drag - Pan
Esc            - Reset
Shift          - Fine control
";

		AlphaTestEffect AlphaTestEffect;

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		protected override void Setup()
		{
			AlphaTestEffect = new AlphaTestEffect(GraphicsDevice);
			AlphaTestEffect.AlphaFunction = CompareFunction.GreaterEqual;
			AlphaTestEffect.ReferenceAlpha = 128;

			// We need to create one ContentManager object for each top-level content folder we'll be
			// loading things from. Here "Content" is the most senior folder name of the content tree.
			// (Content [models, fonts, etc.] are added to the project with the Content utility. Double-click
			// 'Content.mgcb' in solution explorer.). You can create multiple content managers if content
			// is spread of diverse folders.
			var MyContent = new ContentManager(Services, "Content");

			// The font we will use to draw the menu on the screen.
			// "Arial14" is the pathname to the font file
			Font = MyContent.Load<SpriteFont>("Arial14");

			// The model of the toroid
			var TorusModel = MyContent.Load<Model>("torus");
						
			// The sprite we draw in this window
			Torus = new BlSprite(Graphics,"Torus");

			Torus.PreDraw = (s) => 
			{
				var graphicsDevice = s.Graphics.GraphicsDevice;
				//graphicsDevice.BlendState = BlendState.Opaque;
				//graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
				//graphicsDevice.DepthStencilState = DepthStencilState.None;
				graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

				return BlSprite.PreDrawCmd.Continue;
			};

			Torus.DrawCleanup=(s)=>
			{
				var graphicsDevice = s.Graphics.GraphicsDevice;

				//graphicsDevice.DepthStencilState = DepthStencilState.Default;
				//graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
			};

			// Unfortunately, MonoGame's stock AlphaTestEffect does not have full lighting support.
			// It only supports ambient and fog. To make an alpha testing effect with full lighting support
			// you'd need to copy the source to BasicEffect and then add the alpha test, which is not trivial.
			// (Custom shaders files (*.fx) can be added the same way as other resources--via double-clicking
			// Content.mgcb and then loading them as an effect)
			// Note: You can also just neglect the AlphaTestEffect stuff in this demo and use the 
			// BasicEffect like all the other demos but with an alpha channel in the texture. That will give you a
			// "poor man's" alpha texture (which will have certain fairly subtle artifacts) but with full lighting support.
			Torus.SetMeshEffect = (s,mesh) =>
			{
				foreach(var part in mesh.MeshParts)
				{
					AlphaTestEffect.World = s.AbsoluteMatrix;
					AlphaTestEffect.View = s.Graphics.View;
					AlphaTestEffect.Projection = s.Graphics.Projection;
					AlphaTestEffect.Texture = s.GetMipmapLod();

					part.Effect = AlphaTestEffect;

					AlphaTestEffect.Techniques[2].Passes[0].Apply();
				}
				return BlSprite.SetMeshEffectCmd.Continue;
			};

			Torus.LODs.Add(TorusModel);

			// Load the image into a Texture2D
			MyTexture = Graphics.LoadFromImageFile("image.png");

			// Set the sprite's mipmap
			// NOTE: The texture mapping is up to the model designer, because
			// the texture coordinates for each vertex are embedded in the model file.
			Torus.Mipmap = new BlMipmap(Graphics, MyTexture);
		}

		/// <summary>
		/// See BlWindow3D for detils.
		/// </summary>
		/// <param name="timeInfo"></param>
		protected override void FrameProc(GameTime timeInfo)
		{
			//
			// Put your periodic code here
			//

			// Handle the standard mouse and keystroke functions. (Don't call this if you want some other behavior
			// of mouse and keys.)
			Graphics.DoDefaultGui();
		}
		/// <summary>
		/// See BlWindow3D for detils.
		/// </summary>
		/// <param name="timeInfo">Provides a snapshot of timing values.</param>
		protected override void FrameDraw(GameTime timeInfo)
		{
			//
			// Draw things here using BlSprite.Draw(), graphics.DrawText(), etc.
			//

			Torus.Draw();

			var MyMenuText = String.Format("{0}\nEye: {1}\nLookAt: {2}\nMaxDistance: {3}\nMinistance: {4}\nViewAngle: {5}\nModelLod: {6}\nModelApparentSize: {7}",
				Help,
				Graphics.Eye,
				Graphics.LookAt,
				Graphics.MaxCamDistance,
				Graphics.MinCamDistance,
				Graphics.Zoom,
				Torus.LodTarget,
				Torus.ApparentSize
			);

			// handle undrawable characters for the specified font(like the infinity symbol)
			try
			{
				Graphics.DrawText(MyMenuText, Font, new Vector2(50, 50));
			}
			catch { }
		}
	}
}