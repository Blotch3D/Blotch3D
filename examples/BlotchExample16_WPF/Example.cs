using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using Blotch;

namespace BlotchExample
{
	/// <summary>
	/// The 3D window. It must inherit from BlWindow3D.
	/// You can create one of these for each 3D window you want.
	/// </summary>
	public class Example : BlWindow3D
	{
		/// <summary>
		/// This will be the torus model we draw in this window
		/// </summary>
		BlSprite Torus;

		/// <summary>
		/// This will be the font for the help menu we draw in this window
		/// </summary>
		SpriteFont Font;

		/// <summary>
		/// The help menu text that we draw in this window
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
		/// 'Setup' is automatically called one time near the beginning of the program.
		/// You can load fonts, load models, and do other time consuming one-time things here.
		/// You can also load content later if necessary (like in the Update or Draw methods), but try
		/// to load them as few times as necessary because loading things takes time.
		/// </summary>
		protected override void Setup()
		{
			// We need to create one ContentManager object for each top-level content folder we'll be
			// loading things from. Here "Content" is the most senior folder name of a content tree.
			// (Content [models, fonts, etc.] are added to the project with the Content utility. Double-click
			// 'Content.mgcb' in solution explorer.)
			var MyContent = new ContentManager(Services, "Content");

			// The font we will use to draw the menu on the screen.
			// "CourierNew12" is the pathname to the font file
			Font = MyContent.Load<SpriteFont>("Arial14");

			// The model of the toroid
			var TorusModel = MyContent.Load<Model>("torus");

			// The sprite we draw in this window
			Torus = new BlSprite(Graphics,"Torus");
			Torus.LODs.Add(TorusModel);
		}

		/// <summary>
		/// 'FrameProc' is automatically called once per frame if at all possible. Here you put the things you definitely
		/// need to do periodically, like sprite movement that needs to be smooth, critical timing operations,
		/// etc. If you put that stuff in the FrameDraw() method, then behavior might be choppy because the FrameDraw()
		/// method is not called periodically if there isn't enough CPU.
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
		/// 'FrameDraw' is automatically called once per frame if there is enough CPU. Otherwise its called more slowly.
		/// This is where you would typically draw the scene.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
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