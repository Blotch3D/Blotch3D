﻿using Microsoft.Xna.Framework;
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
			Graphics.Lights.Clear();
			var light = new BlGraphicsDeviceManager.Light();
			light.LightDiffuseColor = new Vector3(1, 1, .5f);
			light.LightDirection = new Vector3(1, 0, 0);
			Graphics.Lights.Add(light);

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

			// load the terrain image
			var terrain = Graphics.LoadFromImageFile("Content/terrain.png", true);

			// The vertices of the surface
			var SurfaceArray = BlGeometry.CreatePlanarSurface(terrain);

			// convert to vertex buffer
			var vertexBuf = BlGeometry.TrianglesToVertexBuffer(Graphics.GraphicsDevice, SurfaceArray);

			// The sprite we draw in this window
			Surface = new BlSprite(Graphics, "Surface");
			Surface.Mipmap = terrain;
			Surface.LODs.Add(vertexBuf);
			Surface.BoundSphere = new BoundingSphere(Vector3.Zero, 1);
			Surface.SetAllMaterialBlack();
			Surface.Color = new Vector3(1, 1, 1);
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