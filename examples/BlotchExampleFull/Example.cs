using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using Blotch;
using Microsoft.Xna.Framework.Input;

namespace BlotchExample
{
	/// <summary>
	/// A 3D Window
	/// </summary>
	public class GameExample : BlWindow3D
	{
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

		BlSprite TopSprite = null;
		BlSprite TopHudSprite = null;
		BlSprite HudBackground = null;
		BlSprite Skybox = null;
		BlSprite Model;

		Matrix LastProjectionMatrix;
		Model SkyboxModel = null;
		float SkyboxDiameter = 1000000f;
		SpriteFont Font;

		public GameExample()
		{
		}

		/// <summary>
		/// 'Setup' is automatically called one time just after the object is created, by the 3D thread.
		/// You can load fonts, load models, and do other time consuming one-time things here that must be done
		/// by the object's thread..
		/// You can also load content later if necessary (like in the Update or Draw methods), but try
		/// to load them as few times as necessary because loading things takes time.
		/// </summary>
		protected override void Setup()
		{
			TopSprite = new BlSprite(Graphics, "topSprite");
			TopHudSprite = new BlSprite(Graphics, "topHudSprite");
			HudBackground = new BlSprite(Graphics, "HudBackground");
			Graphics.ClearColor = new Microsoft.Xna.Framework.Color();
			//graphics.AutoRotate = .002;

			Content = new ContentManager(Services, "Content");

			Font = Content.Load<SpriteFont>("Arial14");

			var floor = new BlSprite(Graphics, "floor");
			var plane = Content.Load<Model>("Plane");
			var sphere = Content.Load<Model>("uv_sphere_192x96");
			var MyTexture = Graphics.LoadFromImageFile("image_with_alpha.png");

			//
			// Create floor
			//
			floor.LODs.Add(plane);
			floor.Mipmap = new BlMipmap(Graphics, MyTexture);
			floor.SetAllMaterialBlack();
			floor.EmissiveColor = new Vector3(1, 1, 1);
			TopSprite.Add("floor", floor);

			//
			// Create parent
			//
			var modelParent = new BlSprite(Graphics, "parent");
			modelParent.Matrix *= Matrix.CreateTranslation(1,1,1);
			TopSprite.Add("modelParent", modelParent);

			//
			// Create model
			//
			Model = new BlSprite(Graphics, "model");
			Model.Mipmap = new BlMipmap(Graphics, MyTexture);
			Model.Matrix = Microsoft.Xna.Framework.Matrix.CreateScale(.12f);
			Model.SetAllMaterialBlack();
			Model.EmissiveColor = new Vector3(1, 1, 1);
			modelParent.Add("model", Model);

			var verts = new VertexPositionNormalTexture[6];
			var norm = new Vector3(0, 0, 1);
			verts[0].Position = new Vector3(-1, -1, 0);
			verts[0].TextureCoordinate = new Vector2(0, 0);
			verts[0].Normal = norm;

			verts[1].Position = new Vector3(-1, 1, 0);
			verts[1].TextureCoordinate = new Vector2(0, 1);
			verts[1].Normal = norm;

			verts[2].Position = new Vector3(1, -1, 0);
			verts[2].TextureCoordinate = new Vector2(1, 0);
			verts[2].Normal = norm;

			verts[3].Position = verts[1].Position;
			verts[3].TextureCoordinate = new Vector2(0, 1);
			verts[3].Normal = norm;

			verts[4].Position = new Vector3(1, 1, 0);
			verts[4].TextureCoordinate = new Vector2(1, 1);
			verts[4].Normal = norm;

			verts[5].Position = verts[2].Position;
			verts[5].TextureCoordinate = new Vector2(1, 0);
			verts[5].Normal = norm;

			Model.LODs.Add(sphere);
			Model.LODs.Add(verts);
			Model.LODs.Add(sphere);
			Model.LODs.Add(verts);
			Model.LODs.Add(null);
			Model.BoundSphere = new BoundingSphere(Vector3.Zero, 1);


			//
			// Create text
			//
			var text = new BlSprite(Graphics, "text");
			text.SphericalBillboard = true;
			text.ConstSize = true;
			modelParent.Add("text", text);

			// Note that in-world textures with alpha (like this one) really need to use
			// an alpha test to work correctly (see the SpriteAlphaTexture demo)
			// This one works because it is drawn last and there is no other alpha texture in front of it.
			var title = new BlSprite(Graphics, "title");
			title.LODs.Add(Content.Load<Model>("Plane"));
			title.Matrix = Matrix.CreateScale(.15f, .05f, .15f);
			title.Mipmap = new BlMipmap(Graphics,Graphics.TextToTexture("These words are\nin world space", Font, Microsoft.Xna.Framework.Color.Red, Microsoft.Xna.Framework.Color.Transparent));
			title.MipmapScale = -1000;
			title.SetAllMaterialBlack();
			title.EmissiveColor = new Vector3(1, 1, 1);
			title.PreDraw = (s) => 
			{
				// Disable depth testing
				Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateDisabled;
				return BlSprite.PreDrawCmd.Continue;
			};
			title.DrawCleanup = (s) =>
			{
				// Disable depth testing
				Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateEnabled;
			};
			text.Add("title", title);

			//
			// Create hud
			//
			HudBackground.IncludeInAutoClipping = false;
			HudBackground.ConstSize = true;
			TopHudSprite.Add("hudBackground", HudBackground);

			var myHud = new BlSprite(Graphics, "myHud");
			HudBackground.Add("myHud", myHud);

			myHud.Matrix = Matrix.CreateScale(.2f, .1f, .2f);
			myHud.Matrix *= Matrix.CreateTranslation(3, 1, 0);

			myHud.LODs.Add(Content.Load<Model>("Plane"));
			myHud.Mipmap = new BlMipmap(Graphics, Graphics.TextToTexture("HUD text", Font, Microsoft.Xna.Framework.Color.White, Microsoft.Xna.Framework.Color.Transparent),1);
			myHud.SetAllMaterialBlack();
			myHud.EmissiveColor = new Vector3(1, 1, 1);

			// Create skybox, with a FrameProc that keeps it centered on the camera
			Skybox = new BlSprite(Graphics, "Skybox", (s) =>
			{
				s.Matrix.Translation = Graphics.TargetEye;
			});
			Skybox.Mipmap = new BlMipmap(Graphics, Graphics.LoadFromImageFile("Skybox.jpg"),1);
			SkyboxModel = Content.Load<Model>("uv_sphere_192x96");
			Skybox.LODs.Add(SkyboxModel);

			// Exclude from auto-clipping
			Skybox.IncludeInAutoClipping = false;

			// The sphere model is rotated a bit to avoid distortion at the poles. So we have to rotate it back
			Skybox.Matrix = Matrix.CreateFromYawPitchRoll(.462f, 0, .4504f);

			Skybox.Matrix *= Matrix.CreateScale(SkyboxDiameter);
			Skybox.PreDraw = (s) =>
			{
				// Set inside facets to visible, rather than outside
				Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

				// Disable depth testing
				Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateDisabled;

				// Create a separate View matrix when drawing the skybox, which is the same as the current view matrix but with very high farclip
				LastProjectionMatrix = Graphics.Projection;
				Graphics.Projection = Microsoft.Xna.Framework.Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians((float)Graphics.Zoom), (float)Graphics.CurrentAspect, SkyboxDiameter / 100, SkyboxDiameter * 100);

				return BlSprite.PreDrawCmd.Continue;
			};
			Skybox.DrawCleanup = (s) =>
			{
				// retore default settings

				Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateEnabled;
				Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
				Graphics.Projection = LastProjectionMatrix;
			};
			Skybox.SpecularColor = Vector3.Zero;
			Skybox.Color = Vector3.Zero;
			Skybox.EmissiveColor = new Vector3(1, 1, 1);

			var guiCtrl = new BlGuiControl(this)
			{
				Texture = Graphics.TextToTexture("Click me for a console message", Font,Color.Green,Color.Transparent),
				Position = new Vector2(600, 100),
				OnMouseOver = (ctrl) => 
				{
					if(ctrl.PrevMouseState.LeftButton == ButtonState.Released && Mouse.GetState().LeftButton == ButtonState.Pressed)
						Console.WriteLine("GUI button was clicked");
				}
			};


			GuiControls.TryAdd("MyControl",guiCtrl);
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
			// handle the standard mouse and key commands for controlling the 3D view
			var mouseRay = Graphics.DoDefaultGui();

			if (Graphics.Zoom > 150)
				Graphics.Zoom = 150;

			// Did user CTRL-leftClick in the 3D display?
			if (mouseRay != null)
			{
				// search the sprite tree for sprites that had a radius within the selection ray
				var sprites = TopSprite.GetRayIntersections((Ray)mouseRay);
				foreach (var s in sprites)
					Console.WriteLine(s);
			}
		}

		/// <summary>
		/// 'FrameDraw' is automatically called once per frame if there is enough CPU. Otherwise its called more slowly.
		/// This is where you would typically draw the scene.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void FrameDraw(GameTime timeInfo)
		{
			Skybox.Draw();

			var hudDist = (float)-(Graphics.CurrentNearClip + Graphics.CurrentFarClip) / 2;
			HudBackground.Matrix = Matrix.CreateScale(.4f, .4f, .4f) * Matrix.CreateTranslation(0, 0, hudDist);

			TopSprite.Draw();

			Graphics.SetSpriteToCamera(TopHudSprite);
			Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.None;
			TopHudSprite.Draw();
			Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

			var MyMenuText = String.Format("{0}\nEye: {1}\nLookAt: {2}\nMaxDistance: {3}\nMinistance: {4}\nViewAngle: {5}\nModelLod: {6}\nModelApparentSize: {7}",
				Help,
				Graphics.Eye,
				Graphics.LookAt,
				Graphics.MaxCamDistance,
				Graphics.MinCamDistance,
				Graphics.Zoom,
				Model.LodTarget,
				Model.ApparentSize
			);

			try
			{
				// handle undrawable characters for the specified font(like the infinity symbol)
				try
				{
					Graphics.DrawText(MyMenuText, Font, new Vector2(50, 50));
				}
				catch { }
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			/*
			Console.WriteLine("{0}  {1}  {2}  {3}  {4}",
				graphics.CurrentNearClip,
				graphics.CurrentFarClip,
				graphics.MinCamDistance,
				graphics.MaxCamDistance,
				hudDist
			);
			*/
			//Console.WriteLine(model.LodCurrentIndex);
		}
	}
}