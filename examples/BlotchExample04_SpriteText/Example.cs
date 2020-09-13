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
			//
			// Put your periodic code here
			//

			// Handle the standard mouse and keystroke functions. (This is very configurable)
			Graphics.DoDefaultGui();

			//
			// Draw things here using BlSprite.Draw(), graphics.DrawText(), etc.
			//

			Torus.Draw();

			// If torus is in view, draw its text
			var coords = Torus.GetViewCoords();
			if(coords!=null)
			{
				// This is how you would draw dynamic text. If the text is constant, you can do it faster by
				// creating a texture of the text (see Graphics.TextToTexture) in Setup, and then drawing that texture here with
				// Graphics.DrawTexture.
				// (You can also make text for the sprite if you add a subsprite that is a 'plane' model, has a texture that
				// is text, has billboard enabled, and maybe has constant size enabled. In that case, the text can be
				// occluded by closer sprites unless depth testing is disabled for that sprite, and the text size will vary
				// with the window size. But that's  more complicated.)
				Graphics.DrawText("This is the model", Font, (Vector2)coords);
			}

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