using Microsoft.Xna.Framework;
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
		/// This will be the Plane model we draw in the window
		/// </summary>
		BlSprite Plane;

		/// <summary>
		/// This will be the font for the help menu we draw in the window
		/// </summary>
		SpriteFont Font;

		/// <summary>
		/// Texture for the Plane
		/// </summary>
		Texture2D MyTexture;

		/// <summary>
		/// Dynamic texture translation
		/// </summary>
		float TexTrans = 0;

		/// <summary>
		/// Dynamic texture transform angle
		/// </summary>
		float TexRotAngle = 0;

		/// <summary>
		/// Same as BasicEffect, but also does alpha test (and has a AlphaTestThreshold variable)
		/// Note that this does NOT inherit from BasicEffect and cannot be used in its place
		/// </summary>
		BlBasicEffect BlBasicEffectXformTexture;

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
			var PlaneModel = MyContent.Load<Model>("plane");
						
			// The sprite we draw in this window
			Plane = new BlSprite(Graphics,"Plane");

			// We use a custom effect rather than the default effect
			BlBasicEffectXformTexture = new BlBasicEffect(Graphics.GraphicsDevice, "BlBasicEffectAlphaTestXformTexOGL.mgfxo");
			BlBasicEffectXformTexture.Parameters["AlphaTestThreshold"].SetValue(.5f);

			// Set the delegate
			Plane.SetEffect = (s,effect) =>
			{
				// Rotate the texture.
				TexRotAngle += .0234f;

				// avoid eventual visible floating point errors
				if (TexRotAngle > Math.PI)
					TexRotAngle -= (float)(2 * Math.PI);
				if (TexRotAngle < 0)
					TexRotAngle += (float)(2 * Math.PI);

				// We create a 2x2 matrix by using only the first two rows and columns of a 4x4 matrix
				// to demonstrate it can be done easily. Of course, this could be done more efficiently
				// with explicit 2x2 math.
				// (Also note that we pass the four elements as a Vector4 because there is no Matrix2x2)
				var m = Matrix.CreateRotationZ(TexRotAngle);
				BlBasicEffectXformTexture.Parameters["TextureTransform"].SetValue(new Vector4(m.M11, m.M12, m.M21, m.M22));

				// Let's also change the texture offset.
				TexTrans += .01f;

				// avoid eventual visible floating point errors
				if (TexTrans > 1)
					TexTrans -= (int)TexTrans;
				if (TexTrans < 0)
					TexTrans += (int)-TexTrans + 1;

				BlBasicEffectXformTexture.Parameters["TextureTranslate"].SetValue(new Vector2(TexTrans, 2*TexTrans));

				s.SetupBasicEffect(BlBasicEffectXformTexture);	

				return BlBasicEffectXformTexture;
			};

			Plane.LODs.Add(PlaneModel);

			// Load the image into a Texture2D
			MyTexture = Graphics.LoadFromImageFile("Content/image_with_alpha.png");

			// Set the sprite's mipmap
			// NOTE: The texture mapping is up to the model designer, because
			// the texture coordinates for each vertex are embedded in the model file.
			Plane.Mipmap = new BlMipmap(Graphics, MyTexture);
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

			Plane.Draw();

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
ModelLod: {Plane.LodTarget}
ModelApparentSize: {Plane.ApparentSize}";

				Graphics.DrawText(MyHud, Font, new Vector2(50, 50));
			}
			catch { }
		}
	}
}