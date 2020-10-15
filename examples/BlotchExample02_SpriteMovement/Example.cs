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
		BlSprite Torus;

		/// <summary>
		/// This will be the font for the help menu we draw in the window
		/// </summary>
		SpriteFont Font;

		/// <summary>
		/// Rotation of the torus
		/// </summary>
		float Radians = 0;

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		protected override void Setup()
		{
			// Convert models, fonts, images, etc. using the mgcb_editor (see Blotch3D.chm for more info). Converted models,
			// fonts, images, etc. are kept in a 'content' folder. We need to create one ContentManager object for each top-level
			// content folder we'll be loading things from. Here "Content" is the most senior folder name of the content tree.
			// (Content [models, fonts, etc.] are added to the project with the Content utility. You can create multiple content
			// managers if content is spread over diverse folders.
			var MyContent = new ContentManager(Services, "Content");

			// The font we will use to draw the menu on the screen.
			// "Arial14" is the pathname to the font file
			Font = MyContent.Load<SpriteFont>("Arial14");

			// The model of the toroid
			var TorusModel = MyContent.Load<Model>("torus");

			// The sprite we draw in this window
			Torus = new BlSprite(Graphics,"Torus");
			Torus.LODs.Add(TorusModel);
		}

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		/// <param name="timeInfo">Provides a snapshot of timing values.</param>
		protected override void FrameDraw(GameTime timeInfo)
		{
			// Change the rotation of the torus
			Radians += .05f;

			// There are many static and instance methods of the Matrix class that let you define
			// various rotations, translations, etc.
			Torus.Matrix = Matrix.CreateRotationX(Radians);

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