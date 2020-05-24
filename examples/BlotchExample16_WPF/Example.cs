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
		/// 'FrameDraw' is automatically called once per frame if there is enough CPU. Otherwise its called more slowly.
		/// This is where you would typically draw the scene.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
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
Pan    -  Left-CTRL-left-drag
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