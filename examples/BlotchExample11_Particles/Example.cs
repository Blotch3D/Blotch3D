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
		/// See BlWindow3D for details.
		/// </summary>
		protected override void Setup()
		{
			// We need to create one ContentManager object for each top-level content folder we'll be
			// loading things from. Here "Content" is the most senior folder name of the content tree.
			// (Content [models, fonts, etc.] are added to the project with the Content utility. Double-click
			// 'Content.mgcb' in solution explorer.). You can create multiple content managers if content
			// is spread over diverse folders.
			var MyContent = new ContentManager(Services, "Content");

			// The font we will use to draw the menu on the screen.
			// "Arial14" is the pathname to the font file
			Font = MyContent.Load<SpriteFont>("Arial14");

			// The model of the toroid
			var TorusModel = MyContent.Load<Model>("torus");

			// Load the image into a Texture2D
			MyTexture = Graphics.LoadFromImageFile("image.png");

			// The starting matrix for the first particle 
			var FirstMatrix = Matrix.CreateTranslation(.5f,0,0);

			// How to alter FirstMatrix each time a new particle is created
			var ConsecutiveMatrix = Matrix.CreateRotationY(.1f);

			// How to alter a particle with particle age
			var ChangeMatrix = Matrix.CreateTranslation(.03f, 0, .03f);
			ChangeMatrix *= Matrix.CreateScale(.98f);

			// Maximum number of particles
			long MaxParticles = 30;

			// How much to change the overall alpha with particle age
			double AlphaChange = -.06;

			// how many frames to wait between particle creation
			double ParticlesPerSecond = 60;

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
						p.Matrix *= ChangeMatrix;
						p.Alpha += AlphaChange;
						if (p.Alpha > 1) p.Alpha = 1;
						if (p.Alpha < 0) p.Alpha = 0;
					});

					part.LODs.Add(TorusModel);
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
		/// See BlWindow3D for details.
		/// </summary>
		/// <param name="timeInfo">Provides a snapshot of timing values.</param>
		protected override void FrameDraw(GameTime timeInfo)
		{
			//
			// Draw things here using BlSprite.Draw(), graphics.DrawText(), etc.
			//

			MySprite.Draw();

			var MyMenuText = String.Format("{0}\nEye: {1}\nLookAt: {2}\nMaxDistance: {3}\nMinistance: {4}\nViewAngle: {5}\nModelLod: {6}\nModelApparentSize: {7}",
				Help,
				Graphics.Eye,
				Graphics.LookAt,
				Graphics.MaxCamDistance,
				Graphics.MinCamDistance,
				Graphics.Zoom,
				MySprite.LodTarget,
				MySprite.ApparentSize
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