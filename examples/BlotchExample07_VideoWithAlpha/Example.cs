using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using Blotch;
using System.IO;
using Microsoft.Xna.Framework.Media;

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

		/// <summary>
		/// Same as BasicEffect, but also does alpha test (and has a AlphaTestThreshold variable)
		/// Note that this does NOT inherit from BasicEffect and cannot be used in its place
		/// </summary>
		BlBasicEffect BlBasicEffectClipColor;

		Video video;
		VideoPlayer player;

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		protected override void Setup()
		{
			// We need to create one ContentManager object for each top-level content folder we'll be
			// loading things from. Here "Content" is the most senior folder name of the content tree.
			// (Content [models, fonts, etc.] are added to the project with the Content utility. Double-click
			// 'Content.mgcb' in solution explorer.). You can create multiple content managers if content
			// is spread of diverse folders.
			var MyContent = new ContentManager(Services, "Content");

			byte[] bytes = File.ReadAllBytes("BlBasicEffectClipColor.mgfxo");
			BlBasicEffectClipColor = new BlBasicEffect(Graphics.GraphicsDevice, bytes);
			BlBasicEffectClipColor.Parameters["ClipColor"].SetValue(new Vector3());
			BlBasicEffectClipColor.Parameters["ClipColorTolerance"].SetValue(.1f);

			// The font we will use to draw the menu on the screen.
			// "Arial14" is the pathname to the font file
			Font = MyContent.Load<SpriteFont>("Arial14");

			// The model of the toroid
			var TorusModel = MyContent.Load<Model>("torus");
						
			// The sprite we draw in this window
			Torus = new BlSprite(Graphics,"Torus");

			// See Blotch3D documentation for details on translucency
			Torus.SetEffect = (s,effect) =>
			{
				s.SetupBasicEffect(BlBasicEffectClipColor);	

				return BlBasicEffectClipColor;
			};

			Torus.LODs.Add(TorusModel);

			// Load the image into a Texture2D
			MyTexture = Graphics.LoadFromImageFile("image.png");

			// Set the sprite's mipmap
			// NOTE: The texture mapping is up to the model designer, because
			// the texture coordinates for each vertex are embedded in the model file.
			Torus.Mipmap = new BlMipmap(Graphics, MyTexture);

			video = Content.Load<Video>("Content/sample_video");
			player = new VideoPlayer();

		}

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		/// <param name="timeInfo"></param>
		protected override void FrameProc(GameTime timeInfo)
		{
			//
			// Put your periodic code here
			//
			if (player.State == MediaState.Stopped)
			{
				player.Play(video);
			}
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
			var tex = player.GetTexture();
			Torus.Mipmap = tex;
			Torus.Draw();
			tex.Dispose();

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