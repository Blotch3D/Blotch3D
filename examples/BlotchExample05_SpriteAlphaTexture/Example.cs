﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using Blotch;
using System.IO;

namespace BlotchExample
{
	/// <summary>
	/// The 3D window. This must inherit from BlWindow3D. See BlWindow3D for details.
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
		/// Same as BasicEffect, but also does alpha test (and has a AlphaTestThreshold variable)
		/// Note that this does NOT inherit from BasicEffect and cannot be used in its place
		/// </summary>
		BlBasicEffect BlBasicEffectAlphaTest;

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		protected override void Setup()
		{
			// Any type of content (3D models, fonts, images, etc.) can be converted to an XNB file by downloading and
			// using the mgcb-editor (see Blotch3D.chm for details). XNB files are then normally added to the project
			// and loaded as shown here. 'Content', here, is the folder that contains the XNB files or subfolders with
			// XNB files. We need to create one ContentManager object for each top-level content folder we'll be loading
			// XNB files from. You can create multiple content managers if content is spread over diverse folders. Some
			// content can also be loaded in its native format using platform specific code (may not be portable) or
			// certain Blotch3D/Monogame methods, like BlGraphicsDeviceManager.LoadFromImageFile.
			var MyContent = new ContentManager(Services, "Content");

			// The font we will use to draw the menu on the screen.
			// "Arial14" is the pathname to the font file
			Font = MyContent.Load<SpriteFont>("Arial14");

			// The model of the toroid
			var TorusModel = MyContent.Load<Model>("torus");
						
			// The sprite we draw in this window
			Torus = new BlSprite(Graphics,"Torus");

			// We use a custom effect rather than the default effect
			BlBasicEffectAlphaTest = new BlBasicEffect(Graphics.GraphicsDevice, "BlBasicEffectAlphaTestOGL.mgfxo");
			BlBasicEffectAlphaTest.Parameters["AlphaTestThreshold"].SetValue(.5f);

			// See Blotch3D documentation for details on translucency
			Torus.SetEffect = (s,effect) =>
			{
				s.SetupBasicEffect(BlBasicEffectAlphaTest);	

				return BlBasicEffectAlphaTest;
			};

			Torus.LODs.Add(TorusModel);

			// Load the image into a Texture2D
			MyTexture = Graphics.LoadFromImageFile("Content/image_with_alpha.png");

			// Set the sprite's mipmap
			// NOTE: The texture mapping is up to the model designer, because
			// the texture coordinates for each vertex are embedded in the model file.
			Torus.Mipmap = new BlMipmap(Graphics, MyTexture);
		}

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		/// <param name="timeInfo">Provides a snapshot of timing values.</param>
		protected override void FrameDraw(GameTime timeInfo)
		{
			//
			// Put your periodic code here
			//

			// Handle the standard mouse and keystroke functions. (This is very configurable)
			Graphics.DoDefaultGui();

			//
			// Draw things here using BlSprite.Draw(), graphics.DrawText(), etc.
			//

			Torus.Draw();

			// handle undrawable characters for the specified font (like the infinity symbol)
			try
			{
				var MyHud = $@"
Camera controls:
Dolly  -  Wheel
Zoom   -  Left-CTRL-wheel
Truck  -  Left-drag 
Rotate -  Right-drag
Pan    -  Left-ALT-left-drag
Reset  -  Esc
Fine control  -  Left-Shift

Eye: {Graphics.Eye}
LookAt: {Graphics.LookAt}
MaxDistance: {Graphics.MaxCamDistance}
MinDistance: {Graphics.MinCamDistance}
ViewAngle: {Graphics.Zoom}
ModelLod: {Torus.LodTarget}
ModelApparentSize: {Torus.ApparentSize}";

				Graphics.DrawText(MyHud, Font, new Vector2(50, 50));
			}
			catch { }
		}
	}
}