using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using Blotch;
using System.Threading.Tasks;

namespace BlotchExample
{
	/// <summary>
	/// The 3D window. This must inherit from BlWindow3D. See BlWindow3D for details.
	/// </summary>
	public class Example : BlWindow3D
	{
		/// <summary>
		/// This will be the model we draw in the window
		/// </summary>
		BlSprite Surface;

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

			var width = 128;
			var height = 128;

			// Create the surface triangles
			var SurfaceArray = BlGeometry.CreatePlanarSurface
			(
				(x,y)=> .06*Math.Sin(((x - width / 2) * (y - height / 2)) / 300.0),
				width,
				height
			);

			// convert to vertex buffer
			var vertexBuf = BlGeometry.TrianglesToVertexBuffer(Graphics.GraphicsDevice, SurfaceArray);

			// The sprite we draw in this window
			Surface = new BlSprite(Graphics, "Surface");
			Surface.BoundSphere = new BoundingSphere(Vector3.Zero, 1);
			Surface.LODs.Add(vertexBuf);
			Surface.SetAllMaterialBlack();
			Surface.Color = new Vector3(1, 1, 1);

			// When drawing this sprite, don't cull any polygons so that we can see both sides of them
			Surface.PreDraw = (s) =>
			{
				Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
				return BlSprite.PreDrawCmd.Continue;
			};

			// Undo the previous 'CullNone' after the sprite is drawn so that other sprites use the default mode
			// (although there are no other sprites in this example, so this isn't technically necessary here)
			Surface.DrawCleanup = (s) =>
			{
				Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
			};
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

			Surface.Draw();

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
ModelLod: {Surface.LodTarget}
ModelApparentSize: {Surface.ApparentSize}";

				Graphics.DrawText(MyHud, Font, new Vector2(50, 50));
			}
			catch { }
		}
	}
}