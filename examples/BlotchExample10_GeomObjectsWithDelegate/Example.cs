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
		/// This will be the geomodel we draw in the window
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
			// We need to create one ContentManager object for each top-level content folder we'll be
			// loading things from. Here "Content" is the most senior folder name of the content tree.
			// (Content [models, fonts, etc.] are added to the project with the Content utility. Double-click
			// 'Content.mgcb' in solution explorer.). You can create multiple content managers if content
			// is spread over diverse folders.
			var MyContent = new ContentManager(Services, "Content");

			// The font we will use to draw the menu on the screen.
			// "Arial14" is the pathname to the font file
			Font = MyContent.Load<SpriteFont>("Arial14");

			// The model resolution
			var numX = 512;
			var numY = 512;

			// tapered bent screw shape parameters
			var taperSize = .1;
			var numThreads = 5;
			var numTurns = 8; // (total for all threads)
			var threadDepth = .2;
			var bend = .001;

			// Create bending matrix (this could also include scale and translation)
			var matrix = Matrix.CreateRotationX((float)bend);

			// Create the tapered screw
			var triangles = BlGeometry.CreateCylindroid
			(
				(x,y) => threadDepth * Math.Sin(Math.PI * 2 * (numThreads * x / (double)numX + numTurns * y / (double)numY)) + 1,
				numX,
				numY,
				taperSize,
				false,
				true,
				true,
				matrix
			);

			// Uncomment this for facet normals
			//geoModel = BlGeometry.CalcFacetNormals(geoModel);

			// uncomment this to transform it
			//geoModel = BlGeometry.TransformVertices(geoModel, Matrix.CreateScale(1, 1, 2f));

			// Uncomment this to generate face normals (for example, if the previous transform totally flattened the model)
			//geoModel = BlGeometry.CalcFacetNormals(geoModel);

			// Uncomment this to set texture to planar
			// (note: to control the planar direction, transform the vertices, call this method, then transform them back)
			//geoModel = BlGeometry.SetTextureToXY(geoModel);

			// convert to vertex buffer
			var geoVertexBuffer = BlGeometry.TrianglesToVertexBuffer(Graphics.GraphicsDevice, triangles);
			
			// The sprite we draw in this window
			GeoObj = new BlSprite(Graphics, "geomodel");
			GeoObj.LODs.Add(geoVertexBuffer);
			GeoObj.BoundSphere = BlGeometry.GetBoundingSphere(triangles);
			GeoObj.Mipmap = new BlMipmap(Graphics, Graphics.LoadFromImageFile("Content/image.png"));

			/*
			// Uncomment this to make insides visible, also.
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
Pan    -  Left-CTRL-left-drag
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