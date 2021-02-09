using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using Blotch;

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
		BlSprite MySprite;

		/// <summary>
		/// This will be the font for the help menu we draw in the window
		/// </summary>
		SpriteFont Font;

		/// <summary>
		/// Texture for the torus
		/// </summary>
		Texture2D MyTexture;

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
			var particleContent = MyContent.Load<Model>("torus");

			// Load the image into a Texture2D
			MyTexture = Graphics.LoadFromImageFile("Content/image.png");

			// The starting matrix for the first particle 
			var FirstMatrix = Matrix.CreateTranslation(.5f,0,0);

			// How to alter FirstMatrix each time a new particle is created
			var ConsecutiveMatrix = Matrix.CreateRotationZ(1f);

			// How to alter a particle with particle age
			var ChangeMatrix = Matrix.CreateTranslation(.03f, 0, .03f);
//			ChangeMatrix *= Matrix.CreateScale(.98f);

			// Maximum number of particles
			long MaxParticles = 30;

			// How much to change the overall alpha with particle age
			double AlphaChange = -.004;

			// how many frames to wait between particle creation
			double ParticlesPerSecond = 6;

			//
			// The top sprite that holds the particle sprites
			//

			// next particle to be created (and its name)
			long particleNum = 0;
			int framesPerParticle = (int)(1 / (Graphics.FramePeriod * ParticlesPerSecond));
			int frameCntSinceLastParticle = framesPerParticle;
			MySprite = new BlSprite(Graphics,"Torus",(s)=>
			{
				// Delete any expired particle
				if(s.Count > MaxParticles)
				{
					var oldName = (particleNum - MaxParticles - 1).ToString();
					var p = s[oldName];
					s.Remove(oldName);
					p.Dispose();
				}

				// create new particle?
				if(frameCntSinceLastParticle >= framesPerParticle)
				{
					frameCntSinceLastParticle = 0;

					var particleName = particleNum.ToString();

					var part = new BlSprite(Graphics, particleName, (p) =>
					{
						p.Matrix = Matrix.Multiply(ChangeMatrix, p.Matrix);
						p.Alpha += AlphaChange;
						if (p.Alpha > 1) p.Alpha = 1;
						if (p.Alpha < 0) p.Alpha = 0;
					});

					part.PreDraw = (p) =>
					{
						Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateDisabled;
						return BlSprite.PreDrawCmd.Continue;
					};

					part.DrawCleanup = (p) =>
					{
						Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateEnabled;
					};


					part.LODs.Add(particleContent);
					part.Matrix = FirstMatrix;

					FirstMatrix *= ConsecutiveMatrix;

					part.Mipmap = MyTexture;

					s.Add(part);

					particleNum++;
				}

				frameCntSinceLastParticle++;
			});
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

			MySprite.Draw();

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
ModelLod: {MySprite.LodTarget}
ModelApparentSize: {MySprite.ApparentSize}";

				Graphics.DrawText(MyHud, Font, new Vector2(50, 50));
			}
			catch { }
		}
	}
}