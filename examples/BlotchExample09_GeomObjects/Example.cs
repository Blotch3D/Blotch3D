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
		/// This will be the geomodel model we draw in the window
		/// </summary>
		BlSprite GeoObj;

		/// <summary>
		/// This will be the font for the help menu we draw in the window
		/// </summary>
		SpriteFont Font;

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

			// The model
			var numX = 128;
			var numY = 2;

			var geoModel = BlGeometry.CreateCylindroid(numX, numY,0);

			// transform it
			geoModel = BlGeometry.TransformVertices(geoModel, Matrix.CreateScale(1, 1, 2f));

			// Uncomment this to generate face normals (for example, if the previous transform totally flattened the model)
			//geoModel = BlGeometry.CalcFacetNormals(geoModel);

			// Uncomment this to set texture to planar
			//geoModel = BlGeometry.SetTextureToXY(geoModel);

			// convert to vertex buffer
			var geoVertexBuffer = BlGeometry.TrianglesToVertexBuffer(Graphics.GraphicsDevice, geoModel);

			var tex = Graphics.LoadFromImageFile("Content/image.png");

			// The sprite we draw in this window
			GeoObj = new BlSprite(Graphics, "geomodel");
			GeoObj.LODs.Add(geoVertexBuffer);
			GeoObj.BoundSphere = new BoundingSphere(new Vector3(), 1);
			GeoObj.Mipmap = tex;

			/*
			// Uncomment this to show insides, also
			GeoObj.PreDraw = (s) =>
			{
				Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
				return BlSprite.PreDrawCmd.Continue;
			};
			GeoObj.DrawCleanup = (s) =>
			{
				Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
			};
			*/
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

			GeoObj.Draw();

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
ModelLod: {GeoObj.LodTarget}
ModelApparentSize: {GeoObj.ApparentSize}";

				Graphics.DrawText(MyHud, Font, new Vector2(50, 50));
			}
			catch { }
		}
	}
}