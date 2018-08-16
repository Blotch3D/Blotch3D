/*
Blotch3D (formerly GWin3D) Copyright (c) 1999-2018 Kelly Loum, all rights reserved except those granted in the following license.

Microsoft Public License (MS-PL)
This license governs use of the accompanying software. If you use the software, you
accept this license. If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the
same meaning here as under U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blotch
{
	/// <summary>
	/// A BlSprite is a single 3D object. Each sprite can also hold any number of subsprites, so you can make
	/// a sprite tree (a scene graph). In that case the child sprites 'follow' the orientation and position of the parent
	/// sprite. That is, they exist in the coordinate system of the parent sprite. The location and orientation of a
	/// sprite in its parent's coordinate system is defined by the sprite's #Matrix member. Subsprites, #LODs, and #Mipmap are NOT disposed
	/// when the sprite is disposed, so you can assign the same one to multiple sprites.
	/// </summary>
	public class BlSprite : Dictionary<string, BlSprite> , IComparable, IDisposable
	{
		/// <summary>
		/// This is proportional to the apparent 2D size of the sprite. (Calculated from the last
		/// Draw operation that occurred, but before any effect of ConstSize)
		/// </summary>
		public double ApparentSize { get; private set; }

		/// <summary>
		/// The Flags field can be used by callbacks of #Draw (#PreDraw, #PreSubsprites, #PreLocal, and #SetMeshEffect) to
		/// indicate various user attributes of the sprite. Also, #GetRayIntersections won't hit if the bitwise AND of this value
		/// and the flags argument passed to it is zero.
		/// </summary>
		public ulong Flags = 0xFFFFFFFFFFFFFFFF;

		/// <summary>
		/// The objects (levels of detail) to draw for this sprite. Only one element is drawn
		/// depending on the ApparentSize. Each element can be a Model, a triangle list
		/// (VertexPositionNormalTexture[]), or null (indicating nothing should be drawn for that LOD). Elements with lower indices are
		/// higher LODs. So index 0 has the highest detail, index 1 is second highest, etc. LOD decreases (the index increases) for
		/// every halving of the object's apparent size. You can adjust how close the LODs must be to the camera with
		/// #LodScale. When the calculated LOD index is higher than the last element,
		/// then the last element is used. So the simplest way to use this is to add a single object to the list.
		/// You can add multiple references of the same object so multiple consecutive LODs draw the same object.
		/// You can set an element to null so it doesn't draw anything, which is typically the last element.
		/// A model can be assigned to multiple sprites. These are NOT disposed when the sprite is disposed.
		/// </summary>
		public List<object> LODs = new List<object>();

		/// <summary>
		/// Defines the LOD scaling. The higher this value, the closer you
		/// must be to see a given LOD. A value of 9 (default) indicates that the highest LOD (LODs[0]) occurs when an
		/// object with a diameter of 1 roughly fills the window.
		/// Set to a large negative value, like -1000, to disable LODs (i.e. always use the highest resolution LOD).
		/// </summary>
		public double LodScale = 9;

		/// <summary>
		/// BlMipmap to apply to the model, or a single texture (Texture2D). The model must include texture coordinates. It must
		/// also include normals if lighting other than 'emissive' is desired. If BlMipmap, it will work the same as
		/// LODs (see LODs for more information). Specifically, the mipmap texture applied
		/// depends on the apparent size of the model. The next higher mipmap is used for every doubling
		/// of model size, where element zero is the highest resolution, used when the apparent size is largest.
		/// If a mipmap is not available for the apparent
		/// size, the next higher available on is used.
		/// Most graphics subsystems do support mipmaps, but these are software mipmaps, so only one image is used
		/// over a model for a given model apparent size rather than nearer portions of the
		/// model showing higher-level mipmaps.
		/// This is NOT disposed when the sprite is disposed, so a given BlMipmap or Texture2D may be assigned
		/// to multiple sprites.
		/// </summary>
		public object Mipmap = null;

		/// <summary>
		/// Defines the mipmap (Textures) scaling. The higher this value, the closer you must be to see a given mipmap.
		/// Set to a large negative value, like -1000, to disable mipmaps (i.e. always use the highest resolution mipmap).
		/// </summary>
		public double MipmapScale = 5;

		/// <summary>
		/// This read-only value is the log of the reciprocal of #ApparentSize. It is used in the calculation of the LOD and the mipmap level.
		/// See #LODs and #Mipmap for more information.
		/// </summary>
		public double LodTarget { get; private set; }

		/// <summary>
		/// The bounding sphere for this sprite. This is automatically updated when a model is drawn, but not if vertices
		/// are drawn. In that case you should set/update it explicitly if any of the internal functions may need it to be
		/// roughly correct,
		/// like if auto-clipping is enabled or a mouse selection or ray may hit the sprite and the hit be properly detected.
		/// </summary>
		public BoundingSphere? BoundSphere = null;

		Effect VerticesEffect = null;

		/// <summary>
		/// Spherically billboard the model. Specifically, keep the model's 'forward' direction pointing at the camera and keep
		/// its 'Up' direction pointing in the same direction as the camera's 'Up' direction. Also see #CylindricalBillboardX,
		/// #CylindricalBillboardY, #CylindricalBillboardZ, and #ConstSize.
		/// </summary>
		public bool SphericalBillboard = false;

		/// <summary>
		/// If non-zero, this is the rotation vector and magnitude of cylindrical billboarding where the angle calculation assumes
		/// this vector is the X axis, even though it may not be. The more this varies from that axis, the more eccentric the
		/// billboarding behavior. The amount of billboarding is equal to: 2*mag^2 - 1/mag^2. So if this vector's magnitude is
		/// unity (1), then full cylindrical billboarding occurs. A vector magnitude of 0.605 produces double reverse cylindrical
		/// billboarding. Also see #SphericalBillboard, #CylindricalBillboardY, #CylindricalBillboardZ, and #ConstSize.
		/// </summary>
		public Vector3 CylindricalBillboardX = Vector3.Zero;

		/// <summary>
		/// If non-zero, this is the rotation vector and magnitude of cylindrical billboarding where the angle calculation assumes
		/// this vector is the Y axis, even though it may not be. The more this varies from that axis, the more eccentric the
		/// billboarding behavior. The amount of billboarding is equal to: 2*mag^2 - 1/mag^2. So if this vector's magnitude is
		/// unity (1), then full cylindrical billboarding occurs. A vector magnitude of 0.605 produces double reverse cylindrical
		/// billboarding. Also see #SphericalBillboard, #CylindricalBillboardX, #CylindricalBillboardZ, and #ConstSize.
		/// </summary>
		public Vector3 CylindricalBillboardY = Vector3.Zero;

		/// <summary>
		/// If non-zero, this is the rotation vector and magnitude of cylindrical billboarding where the angle calculation assumes
		/// this vector is the Z axis, even though it may not be. The more this varies from that axis, the more eccentric the
		/// billboarding behavior. The amount of billboarding is equal to: 2*mag^2 - 1/mag^2. So if this vector's magnitude is
		/// unity (1), then full cylindrical billboarding occurs. A vector magnitude of 0.605 produces double reverse cylindrical
		/// billboarding. Also see #SphericalBillboard, #CylindricalBillboardX, #CylindricalBillboardY, and #ConstSize.
		/// </summary>
		public Vector3 CylindricalBillboardZ = Vector3.Zero;

		/// <summary>
		/// If true, maintain a constant apparent size for the sprite regardless of camera distance or zoom. This is typically
		/// used along with one of the Billboarding effects (see #SphericalBillboard, #CylindricalBillboardX, etc.).
		/// Note that if ConstSize is true, ApparentSize, LodScale, and MipmapScale still act as if it is false, and therefore in that case you
		/// may want to disable them (set them to large negative values). If both #ConstSize
		/// and any Billboarding is enabled and you have asymmetric scaling (different scaling for each dimension), then you'll
		/// need to separate those operations into different levels of the sprite tree to obtain the desired behavior. You'll also
		/// probably want to disable the depth stencil buffer and control which sprite is drawn first so that certain sprites are
		/// 'always on top'. See the examples.
		/// </summary>
		public bool ConstSize = false;

		/// <summary>
		/// Distance to the camera.
		/// </summary>
		public double CamDistance { get; private set; }

		/// <summary>
		/// The #Draw method takes an incoming 'world' matrix parameter which is the coordinate system of its parent. #AbsoluteMatrix
		/// is that incoming world matrix parameter times the #Matrix member and altered according to Billboarding and #ConstSize.
		/// This is not read-only because a callback (see #PreDraw, #PreSubsprites, #PreLocal, and #SetMeshEffect) may need to
		/// change it from within the #Draw method. This is the matrix that is also passed to subsprites as their 'world' matrix.
		/// </summary>
		public Matrix AbsoluteMatrix = Matrix.Identity;

		/// <summary>
		/// The matrix for this sprite. This defines the sprite's orientation and position relative to the parent coordinate system.
		/// For more detailed information, see #AbsoluteMatrix. 
		/// </summary>
		public Matrix Matrix = Matrix.Identity;

		/// <summary>
		/// Current incoming graphics parameter to the #Draw method. Typically this would be of interest to a callback function (see
		/// #PreDraw, #PreSubsprites, #PreLocal, and #SetMeshEffect).
		/// </summary>
		public BlGraphicsDeviceManager Graphics = null;

		/// <summary>
		/// Current incoming world matrix parameter to the #Draw method. Typically this would be of interest to a callback function (see
		/// #PreDraw, #PreSubsprites, #PreLocal, and #SetMeshEffect).
		/// </summary>
		public Matrix? LastWorldMatrix = null;

		/// <summary>
		/// Wwhether to participate in autoclipping calculations, when they are enabled.
		/// </summary>
		public bool IncludeInAutoClipping = true;

		/// <summary>
		/// Current incoming flags parameter to the Draw method. Typically this would be of interest to a callback function (see
		/// #PreDraw, #PreSubsprites, #PreLocal, and #SetMeshEffect).
		/// </summary>
		public ulong FlagsParameter = 0;

		/// <summary>
		/// The color of the material. This is lit by both diffuse and ambient light. If null, MonoGame's default color is kept.
		/// </summary>
		public Vector3? Color = new Vector3(.5f, .5f, 1);

		/// <summary>
		/// The emissive color. If null, MonoGame's default is kept.
		/// </summary>
		
		public Vector3? EmissiveColor = new Vector3(.1f, .1f, .2f);
		/// <summary>
		/// The specular color. If null, MonoGame's default is kept.
		/// </summary>
		
		public Vector3? SpecularColor = null;
		/// <summary>
		/// If a specular color is specified, this is the specular power.
		/// </summary>
		public float SpecularPower = 8;

		/// <summary>
		/// See #FrameProc
		/// </summary>
		/// <param name="sprite"></param>
		public delegate void FrameProcType(BlSprite sprite);

		FrameProcType FrameProc = null;

		/// <summary>
		/// Execute the FrameProc, if it was specified in the BlSprite constructor.
		/// (Normally you wouldn't need to call this because its automatically called 
		/// by the BlWindow.)
		/// </summary>
		public void ExecuteFrameProc()
		{
			if(FrameProc!=null)
				FrameProc(this);
		}

		/// <summary>
		/// Return code from #PreDraw callback. This tells #Draw what to do next.
		/// </summary>
		public enum PreDrawCmd
		{
			/// <summary>
			/// Continue Draw method execution
			/// </summary>
			Continue,
			/// <summary>
			/// Draw should immediately return
			/// </summary>
			Abort,
			/// <summary>
			/// Continue Draw method execution, but don't bother re-calculating AbsoluteMatrix. One would typically return this
			/// if, for example, its known that AbsoluteMatrix will not change from its current value because the Draw parameters
			/// will be the same as they were the last time Draw was called. This happens, for example, when multiple calls are
			/// being made in the same draw iteration for graphic operations that require multiple passes.
			/// </summary>
			UseCurrentAbsoluteMatrix
		}

		/// <summary>
		/// See #PreDraw
		/// </summary>
		/// <param name="sprite"></param>
		/// <returns></returns>
		public delegate PreDrawCmd PreDrawType(BlSprite sprite);

		/// <summary>
		/// If not null, #Draw method calls this at the beginning before doing anything else. From this function one might
		/// examine and/or alter any public writable BlSprite field, and/or control the further execution of the Draw method.
		/// </summary>
		public PreDrawType PreDraw = null;

		/// <summary>
		/// Return code from #PreSubsprites callback. This tells #Draw what to do next.
		/// </summary>
		public enum PreSubspritesCmd
		{
			/// <summary>
			/// Continue Draw method execution
			/// </summary>
			Continue,
			/// <summary>
			/// Draw should immediately return
			/// </summary>
			Abort,
			/// <summary>
			/// Skip drawing subsprites
			/// </summary>
			DontDrawSubsprites
		}

		/// <summary>
		/// See #PreSubsprites
		/// </summary>
		/// <param name="sprite"></param>
		/// <returns></returns>
		public delegate PreSubspritesCmd PreSubspritesType(BlSprite sprite);
		
		/// <summary>
		/// If not null, #Draw method calls this after the matrix calculations for AbsoluteMatrix (including billboards, CamDistance,
		/// ConstSize, etc.) but before drawing the subsprites or local model. From this function one might examine and/or alter
		/// any public writable BlSprite field.
		/// </summary>
		public PreSubspritesType PreSubsprites = null;

		/// <summary>
		/// Return code from #PreSubsprites callback. This tells #Draw what to do next.
		/// </summary>
		public enum SetEffectCmd
		{
			/// <summary>
			/// Continue Draw method execution for the mesh
			/// </summary>
			Continue,
			/// <summary>
			/// Draw should immediately return
			/// </summary>
			Abort,
			/// <summary>
			/// Draw should skip the current mesh
			/// </summary>
			Skip,
		}

		/// <summary>
		/// See #SetMeshEffect
		/// </summary>
		/// <param name="sprite"></param>
		/// <param name="effect"></param>
		/// <returns></returns>
		public delegate Effect SetMeshEffectType(BlSprite sprite, Effect effect);

		/// <summary>
		/// If this not null, then the #Draw method executes this delegate for each model mesh effect instead using the
		/// default BasicEffects. See the SpriteAlphaTexture for an example. The return value is the new or altered effect.
		/// If this is called when the thing to draw is a VertexPositionNormalTexture, then the effect parameter passed in
		/// is a null.
		/// </summary>
		public SetMeshEffectType SetEffect = null;

		/// <summary>
		/// Return code from #PreSubsprites callback. This tells #Draw what to do next.
		/// </summary>
		public enum PreLocalCmd
		{
			/// <summary>
			/// Continue Draw method execution
			/// </summary>
			Continue,
			/// <summary>
			/// Draw should immediately return
			/// </summary>
			Abort,
		}

		/// <summary>
		/// See #PreLocal
		/// </summary>
		/// <param name="sprite"></param>
		/// <returns></returns>
		public delegate PreLocalCmd PreLocalType(BlSprite sprite);
		
		/// <summary>
		/// If not null, #Draw method calls this after drawing subsprites (if appropriate) but before drawing the local model. From this
		/// function one might examine and/or alter any public writable BlSprite field, and/or abort the #Draw method.
		/// </summary>
		public PreLocalType PreLocal = null;

		/// <summary>
		/// See #DrawCleanup
		/// </summary>
		/// <param name="sprite"></param>
		public delegate void DrawCleanupType(BlSprite sprite);
		
		/// <summary>
		/// If not null, #Draw method calls this at the end.
		/// </summary>
		public DrawCleanupType DrawCleanup = null;
		
		/// <summary>
		/// The name of the BlSprite
		/// </summary>
		public string Name;

		/// <summary>
		/// Constructs a sprite
		/// </summary>
		/// <param name="graphicsIn">The BlGraphicsDeviceManager that operates on this sprite</param>
		/// <param name="name">The name of the sprite (must be unique among other sprites in the same subsprite list)</param>
		/// <param name="frameProc">The delegate to run every frame</param>
		public BlSprite(BlGraphicsDeviceManager graphicsIn, string name,FrameProcType frameProc=null)
		{
			FrameProc = frameProc;
			if(FrameProc!=null)
				graphicsIn.Window.FrameProcSpritesAdd(this);
			CreationThread = Thread.CurrentThread.ManagedThreadId;
			Graphics = graphicsIn;
			Name = name;
		}

		/// <summary>
		/// Add a subsprite. (A BlSprite inherits from a Dictionary of BlSprites. This wrapper method to the dictionary's Add
		/// method simply adds the sprite where the key is the sprite's #Name.)
		/// </summary>
		/// <param name="s"></param>
		public void Add(BlSprite s)
		{
			this[s.Name] = s;
		}
		/// <summary>
		/// Returns the current 2D view coordinates of the sprite (for passing to DrawText, for example),
		/// or null if it's behind the camera.
		/// </summary>
		/// <returns>The view coords of the sprite</returns>
		public Vector2? GetViewCoords()
		{
			if (BlDebug.ShowThreadWarnings && CreationThread != Thread.CurrentThread.ManagedThreadId)
				BlDebug.Message(String.Format("BlGraphicsDeviceManager.GetViewCoords() was called by thread {0} instead of thread {1}", Thread.CurrentThread.ManagedThreadId, CreationThread));

			Vector4 position = new Vector4(AbsoluteMatrix.Translation, 1);
			Matrix worldViewProjection = (Matrix)LastWorldMatrix * Graphics.View * Graphics.Projection;
			Vector4 result = Vector4.Transform(position, worldViewProjection);
			result /= result.W;

			if (result.Z >= 1)
				return null;

			Viewport vp = Graphics.GraphicsDevice.Viewport;
			Matrix invClient = Matrix.Invert(Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, -1, 1));
			Vector2 clientResult = Vector2.Transform(new Vector2(result.X, result.Y), invClient);

			return clientResult;
		}
		/// <summary>
		/// Sets all material colors to black.
		/// </summary>
		public void SetAllMaterialBlack()
		{
			Color = Vector3.Zero;
			EmissiveColor = Vector3.Zero;
			SpecularColor = Vector3.Zero;
			SpecularPower = 9.5f;
		}

		/// <summary>
		/// Returns the point on the line between point1 and point2 that is nearest to nearPoint
		/// </summary>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		/// <param name="nearPoint"></param>
		/// <returns>Point on the line nearest to nearPoint</returns>
		public static Vector3 NearestPointOnLine(Vector3 point1, Vector3 point2, Vector3 nearPoint)
		{
			var lineDir = (point2 - point1);
			lineDir.Normalize();//this needs to be a unit vector
			var v = nearPoint - point1;
			var d = Vector3.Dot(v, lineDir);
			return point1 + lineDir * d;
		}
		
		/// <summary>
		/// Returns the distance along the ray to the first point the ray enters the bounding sphere
		/// (BoundSphere), or null if it doesn't enter the sphere.
		/// </summary>
		/// <param name="ray"></param>
		/// <returns>How far along the ray till the first intersection, or null oif it didn't intersect</returns>
		public double? DoesRayIntersect(Ray ray)
		{
			if (BoundSphere == null)
				return null;

			return ray.Intersects((BoundingSphere)BoundSphere);
		}

		/// <summary>
		/// Returns a list of subsprites that the ray hit (i.e. those that were within their radius of the ray)
		/// </summary>
		/// <param name="ray">The ray we are searching</param>
		/// <param name="flags">Check for a hit only if flags & BlSprite#Flags is non-zero</param>
		/// <param name="sprites">An existing sprite list to load. If null, then this allocates a new sprite list.</param>
		/// <returns>A list of subsprites that the ray hit</returns>
		public List<BlSprite> GetRayIntersections(Ray ray, ulong flags=0xFFFFFFFFFFFFFFFF,List<BlSprite> sprites=null)
		{
			if(sprites == null)
				sprites = new List<BlSprite>();

			if((Flags & flags) != 0)
			{
				var dist = DoesRayIntersect(ray);
				if (dist != null)
					sprites.Add(this);
			}

			foreach (var s in this)
			{
				s.Value.GetRayIntersections(ray, flags, sprites);
			}

			return sprites;
		}

		/// <summary>
		/// Draws the sprite and the subsprites.
		/// </summary>
		/// <param name="worldMatrixIn">Defines the position and orientation of the sprite</param>
		/// <param name="flagsIn">Copied to LastFlags for use by any callback of Draw (PreDraw, PreSubspriteDraw, PreLocalDraw,
		/// and SetMeshEffect) that wants it</param>
		public void Draw(Matrix? worldMatrixIn = null, ulong flagsIn = 0xFFFFFFFFFFFFFFFF)
		{
			if (BlDebug.ShowThreadWarnings && CreationThread != Thread.CurrentThread.ManagedThreadId)
				BlDebug.Message(String.Format("BlGraphicsDeviceManager.Draw() was called by thread {0} instead of thread {1}", Thread.CurrentThread.ManagedThreadId, CreationThread));

			if (worldMatrixIn == null)
				worldMatrixIn = Matrix.Identity;

			// save incoming parameters for anyone that needs them (like callbacks)
			LastWorldMatrix = worldMatrixIn;
			FlagsParameter = flagsIn;

#if !DEBUG
			try
			{
#endif
				DrawInternal();
#if !DEBUG
			}
			catch (Exception e)
			{
				Console.WriteLine("Recovered from exception in EsSprite.Draw method:\n\n{0}", e);
			}
#endif
		}

		/// <summary>
		/// This does the meat of Drawing
		/// </summary>
		void DrawInternal()
		{
			CalcApparentSize();

			// get view
			Matrix view = Graphics.View;

			// Call PreDraw if present
			if (PreDraw != null)
			{
				var ret = PreDraw(this);

				if (ret == PreDrawCmd.Abort)
					return;

				if (ret != PreDrawCmd.UseCurrentAbsoluteMatrix)
					CalcMatrix();
			}
			else
				CalcMatrix();

			// Call PreSubsprites if present
			if (PreSubsprites != null)
			{
				var ret = PreSubsprites(this);

				if (ret == PreSubspritesCmd.Abort)
					return;

				if (ret != PreSubspritesCmd.DontDrawSubsprites)
					DrawSubsprites();
			}
			else
				DrawSubsprites();

			// Call PreLocal if present
			if (PreLocal != null)
			{
				var ret = PreLocal(this);

				if (ret == PreLocalCmd.Abort)
					return;
			}

			ProcessModel();

			// Call PreLocal if present
			if (DrawCleanup != null)
				DrawCleanup(this);

		}
		void DrawSubsprites()
		{
			//
			// Draw subsprites
			//
			foreach (var s in this)
			{
				s.Value.Draw(AbsoluteMatrix, FlagsParameter);
			}
		}
		object GetLod()
		{
			if (LODs.Count < 1)
				return null;

			var i = LodScale + LodTarget;

			if (i >= LODs.Count)
				i = LODs.Count - 1;

			if (i < 0)
				i=0;

			return LODs[(int)i];
		}
		/// <summary>
		/// If Mipmap is a BlMipmap, this returns the mimap texture that should currently be applied to the sprite.
		/// If Mipmap is a Texture2D, then that texture is returned.
		/// </summary>
		/// <returns></returns>
		public Texture2D GetCurrentTexture()
		{
			if(Mipmap is BlMipmap)
			{
				var mipmap = Mipmap as BlMipmap;
				if (mipmap == null || mipmap.Count < 1)
					return null;

				var i = MipmapScale + LodTarget;

				if (i >= mipmap.Count)
					i = mipmap.Count - 1;

				if (i < 0)
					i = 0;

				return mipmap[(int)i];
			}
			else
			{
				return Mipmap as Texture2D;
			}
		}
		/// <summary>
		/// Called by DrawInternal
		/// </summary>
		void ProcessModel()
		{
			var obj = GetLod();

			BoundingSphere? boundSphere = null;

			if (obj != null)
			{
				if (obj is Model)
				{
					var Model = obj as Model;

					foreach (ModelMesh mesh in Model.Meshes)
					{
						if (boundSphere == null)
							boundSphere = mesh.BoundingSphere;
						else
							boundSphere = BoundingSphere.CreateMerged((BoundingSphere)boundSphere, mesh.BoundingSphere);

						//
						// Draw the model
						//
						if (SetEffect != null)
						{
							foreach (var part in mesh.MeshParts)
							{
								var ret = SetEffect(this, part.Effect);
								if (ret == null)
									continue;
								part.Effect = ret;
							}
						}
						else
						{
							foreach (var effect in mesh.Effects)
							{
								SetupBasicEffect((BasicEffect)effect);
							}
						}

						mesh.Draw();
					}
				}

				if (obj is VertexPositionNormalTexture[])
				{
					var Vertices = obj as VertexPositionNormalTexture[];

					if (SetEffect != null)
					{
						var ret = SetEffect(this, null);
						if (ret == null)
							return;
					}
					else
					{
						if (VerticesEffect == null)
						{
							VerticesEffect = new BasicEffect(Graphics.GraphicsDevice);
						}
						SetupBasicEffect((BasicEffect)VerticesEffect);
					}

					//VerticesEffect.Techniques[0].Passes[0].Apply();

					Vector3 avg = Vector3.Zero;
					foreach (var pass in VerticesEffect.CurrentTechnique.Passes)
					{
						pass.Apply();

						Graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, Vertices.Length / 3);
					}
				}

				if (boundSphere != null)
				{
					boundSphere = boundSphere.Value.Transform(AbsoluteMatrix);

					BoundSphere = boundSphere;
				}

				if (BoundSphere!=null && IncludeInAutoClipping)
				{
					Graphics.ExtendClippingTo(this);
				}
			}
			Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateEnabled;
		}

		void CalcApparentSize()
		{
			ApparentSize = 1.0 / (CamDistance * Math.Tan(Graphics.Zoom * Math.PI / 360));

			var winSize = Math.Sqrt(Graphics.PreferredBackBufferWidth * Graphics.PreferredBackBufferHeight);

			if (ApparentSize <= 0)
			{
				ApparentSize*=-1;
			}
			if(ApparentSize>10000000 || Double.IsInfinity(ApparentSize) || Double.IsNaN(ApparentSize))
			{
				ApparentSize = 10000000;
			}
			
			LodTarget = Math.Log(1/(winSize*ApparentSize));
		}

		/// <summary>
		/// Called by DrawInternal
		/// </summary>
		void CalcMatrix()
		{
			Matrix view = Graphics.View;

			// get world matrix
			if (LastWorldMatrix == null)
				AbsoluteMatrix = Matrix;
			else
			{
				AbsoluteMatrix = (Matrix)LastWorldMatrix;
				// collapse it
				AbsoluteMatrix = Matrix.Multiply(Matrix, AbsoluteMatrix);
			}

			// calc cam distance
			var pointOnCameraForward = NearestPointOnLine(Graphics.Eye, Graphics.LookAt, AbsoluteMatrix.Translation);
			var distVec = pointOnCameraForward - Graphics.Eye;
			CamDistance = distVec.Length();
			if (Graphics.CameraForward.X * distVec.X < 0 || Graphics.CameraForward.Y * distVec.Y < 0 || Graphics.CameraForward.Z * distVec.Z < 0)
				CamDistance = -CamDistance;

			CalcApparentSize();

			// constant size?
			if (ConstSize)
			{
				AbsoluteMatrix = Matrix.Multiply(Matrix.CreateScale((float)(1.0/ApparentSize)), AbsoluteMatrix);
				//AbsoluteMatrix.M11 *= scale;
				//AbsoluteMatrix.M22 *= scale;
				//AbsoluteMatrix.M33 *= scale;
			}

			// billboard?
			if (SphericalBillboard)
			{
				var m = Matrix.Identity;
				m.M11 = view.M11;
				m.M12 = view.M21;
				m.M13 = view.M31;
				m.M21 = view.M12;
				m.M22 = view.M22;
				m.M23 = view.M32;
				m.M31 = view.M13;
				m.M32 = view.M23;
				m.M33 = view.M33;
				AbsoluteMatrix = Matrix.Multiply(m, AbsoluteMatrix);
			}

			if (CylindricalBillboardX != Vector3.Zero)
			{
				var v1 = AbsoluteMatrix.Up;
				var v2 = Graphics.Eye - AbsoluteMatrix.Translation;
				v1.Normalize();
				v2.Normalize();

				var angle = Math.Atan2(v1.Y, v1.Z) - Math.Atan2(v2.Y, v2.Z);

				var mag = CylindricalBillboardX.LengthSquared();
				mag = 2 * mag - 1 / mag;

				var m = Matrix.CreateFromAxisAngle(CylindricalBillboardX, (float)(angle * mag));

				AbsoluteMatrix = Matrix.Multiply(m, AbsoluteMatrix);
			}

			if (CylindricalBillboardY != Vector3.Zero)
			{
				var v1 = AbsoluteMatrix.Forward;
				var v2 = Graphics.Eye - AbsoluteMatrix.Translation;
				v1.Normalize();
				v2.Normalize();

				var angle = Math.Atan2(v2.X, v2.Z) - Math.Atan2(v1.X, v1.Z);

				var mag = CylindricalBillboardY.LengthSquared();
				mag = 2 * mag - 1 / mag;

				var m = Matrix.CreateFromAxisAngle(CylindricalBillboardY, (float)(angle * mag));

				AbsoluteMatrix = Matrix.Multiply(m, AbsoluteMatrix);
			}

			if (CylindricalBillboardZ != Vector3.Zero)
			{
				var v1 = AbsoluteMatrix.Right;
				var v2 = Graphics.Eye - AbsoluteMatrix.Translation;
				v1.Normalize();
				v2.Normalize();

				var angle = Math.Atan2(v1.X, v1.Y) - Math.Atan2(v2.X, v2.Y);

				var mag = CylindricalBillboardZ.LengthSquared();
				mag = 2 * mag - 1 / mag;

				var m = Matrix.CreateFromAxisAngle(CylindricalBillboardZ, (float)(angle * mag));

				AbsoluteMatrix = Matrix.Multiply(m, AbsoluteMatrix);
			}
		}
		/// <summary>
		/// Sets up in the specified BasicEffect all matrices and lighting parameters for this sprite.
		/// BlSprite#DrawInternal calls this for the BasicEffects embedded in the LOD models.
		/// For BlBasicEffect objects, see the overload of this method.
		/// </summary>
		public void SetupBasicEffect(BasicEffect effect)
		{
			effect.Projection = Graphics.Projection;
			effect.View = Graphics.View;
			effect.World = AbsoluteMatrix;

			effect.LightingEnabled = true;
			do
			{
				if (Graphics.Lights.Count < 1)
				{
					effect.DirectionalLight0.Enabled = false;
					effect.DirectionalLight1.Enabled = false;
					effect.DirectionalLight2.Enabled = false;
					break;
				}

				var light = Graphics.Lights[0];
				if (light.LightDirection != null)
				{
					effect.DirectionalLight0.DiffuseColor = light.LightDiffuseColor;
					effect.DirectionalLight0.SpecularColor = light.LightSpecularColor;
					effect.DirectionalLight0.Direction = (Vector3)light.LightDirection;
					effect.DirectionalLight0.Enabled = true;
				}
				else
					effect.DirectionalLight0.Enabled = false;

				if (Graphics.Lights.Count < 2)
				{
					effect.DirectionalLight1.Enabled = false;
					effect.DirectionalLight2.Enabled = false;
					break;
				}

				light = Graphics.Lights[1];
				if (light.LightDirection != null)
				{
					effect.DirectionalLight1.DiffuseColor = light.LightDiffuseColor;
					effect.DirectionalLight1.SpecularColor = light.LightSpecularColor;
					effect.DirectionalLight1.Direction = (Vector3)light.LightDirection;
					effect.DirectionalLight1.Enabled = true;
				}
				else
					effect.DirectionalLight1.Enabled = false;

				if (Graphics.Lights.Count < 3)
				{
					effect.DirectionalLight2.Enabled = false;
					break;
				}

				light = Graphics.Lights[2];
				if (light.LightDirection != null)
				{
					effect.DirectionalLight2.DiffuseColor = light.LightDiffuseColor;
					effect.DirectionalLight2.SpecularColor = light.LightSpecularColor;
					effect.DirectionalLight2.Direction = (Vector3)light.LightDirection;
					effect.DirectionalLight2.Enabled = true;
				}
				else
					effect.DirectionalLight2.Enabled = false;

			}
			while (false);

			if (Color != null)
				effect.DiffuseColor = (Vector3)Color;

			if (EmissiveColor != null)
				effect.EmissiveColor = (Vector3)EmissiveColor;

			if (Graphics.AmbientLightColor != null)
				effect.AmbientLightColor = (Vector3)Graphics.AmbientLightColor;

			if (SpecularColor != null)
			{
				effect.SpecularColor = (Vector3)SpecularColor;
				effect.SpecularPower = SpecularPower;
			}

			if (Graphics.FogColor != null)
			{
				effect.FogColor = (Vector3)Graphics.FogColor;
				effect.FogEnabled = true;
				effect.FogStart = Graphics.fogStart;
				effect.FogEnd = Graphics.fogEnd;
			}

			var Texture = GetCurrentTexture();
			if (Texture != null)
			{
				effect.TextureEnabled = true;
				effect.Texture = Texture;
			}
		}
		/// <summary>
		/// Sets up in the specified BlBasicEffect with all matrices and lighting parameters for this sprite.
		/// App code might call this from a SetEffect delegate if, for example, it is using one of the
		/// BlBasicEffectxxx effects, like the BlBasicEffectWithAlphaTest.
		/// </summary>
		public void SetupBasicEffect(BlBasicEffect effect)
		{
			effect.Projection = Graphics.Projection;
			effect.View = Graphics.View;
			effect.World = AbsoluteMatrix;

			effect.LightingEnabled = true;
			do
			{
				if (Graphics.Lights.Count < 1)
				{
					effect.DirectionalLight0.Enabled = false;
					effect.DirectionalLight1.Enabled = false;
					effect.DirectionalLight2.Enabled = false;
					break;
				}

				var light = Graphics.Lights[0];
				if (light.LightDirection != null)
				{
					effect.DirectionalLight0.DiffuseColor = light.LightDiffuseColor;
					effect.DirectionalLight0.SpecularColor = light.LightSpecularColor;
					effect.DirectionalLight0.Direction = (Vector3)light.LightDirection;
					effect.DirectionalLight0.Enabled = true;
				}
				else
					effect.DirectionalLight0.Enabled = false;

				if (Graphics.Lights.Count < 2)
				{
					effect.DirectionalLight1.Enabled = false;
					effect.DirectionalLight2.Enabled = false;
					break;
				}

				light = Graphics.Lights[1];
				if (light.LightDirection != null)
				{
					effect.DirectionalLight1.DiffuseColor = light.LightDiffuseColor;
					effect.DirectionalLight1.SpecularColor = light.LightSpecularColor;
					effect.DirectionalLight1.Direction = (Vector3)light.LightDirection;
					effect.DirectionalLight1.Enabled = true;
				}
				else
					effect.DirectionalLight1.Enabled = false;

				if (Graphics.Lights.Count < 3)
				{
					effect.DirectionalLight2.Enabled = false;
					break;
				}

				light = Graphics.Lights[2];
				if (light.LightDirection != null)
				{
					effect.DirectionalLight2.DiffuseColor = light.LightDiffuseColor;
					effect.DirectionalLight2.SpecularColor = light.LightSpecularColor;
					effect.DirectionalLight2.Direction = (Vector3)light.LightDirection;
					effect.DirectionalLight2.Enabled = true;
				}
				else
					effect.DirectionalLight2.Enabled = false;

			}
			while (false);

			if (Color != null)
				effect.DiffuseColor = (Vector3)Color;

			if (EmissiveColor != null)
				effect.EmissiveColor = (Vector3)EmissiveColor;

			if (Graphics.AmbientLightColor != null)
				effect.AmbientLightColor = (Vector3)Graphics.AmbientLightColor;

			if (SpecularColor != null)
			{
				effect.SpecularColor = (Vector3)SpecularColor;
				effect.SpecularPower = SpecularPower;
			}

			if (Graphics.FogColor != null)
			{
				effect.FogColor = (Vector3)Graphics.FogColor;
				effect.FogEnabled = true;
				effect.FogStart = Graphics.fogStart;
				effect.FogEnd = Graphics.fogEnd;
			}

			var Texture = GetCurrentTexture();
			if (Texture != null)
			{
				effect.TextureEnabled = true;
				effect.Texture = Texture;
			}
		}

		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// This makes a Sort operation sort sprites far to near. That is, the nearer sprites are later in the list.
		/// For sorting near to far, use something like
		/// myList.Sort(new Comparison<EsSprite>((b, a) => a.CompareTo(b)));
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public int CompareTo(object obj)
		{
			var other = (BlSprite)obj;
			var dif = CamDistance - other.CamDistance;

			if (dif > 0)
				return -1;
			else if (dif < 0)
				return 1;
			return 0;
		}

		~BlSprite()
		{
			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("BlSprite {0} destructor",Name);

			Dispose();
		}

		int CreationThread = -1;
		/// <summary>
		/// Set when the object is Disposed.
		/// </summary>
		public bool IsDisposed = false;
		/// <summary>
		/// When finished with the object, you should call Dispose() from the same thread that created the object.
		/// You can call this multiple times, but once is enough. If it isn't called before the object
		/// becomes inaccessible, then the destructor will call it and, if BlDebug.EnableDisposeErrors is
		/// true (it is true by default for Debug builds), then it will get an exception saying that it
		/// wasn't called by the same thread that created it. This is because the platform's underlying
		/// 3D library (OpenGL, etc.) often requires 3D resources to be managed only by one thread.
		/// </summary>
		public void Dispose()
		{
			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("BlSprite {0} dispose", Name);
			if (IsDisposed)
				return;

			if (CreationThread != Thread.CurrentThread.ManagedThreadId && BlDebug.ShowThreadWarnings)
				BlDebug.Message(String.Format("BlSprite {0} Dispose() was called by thread {1} instead of thread {2}", Name, Thread.CurrentThread.ManagedThreadId, CreationThread));

			GC.SuppressFinalize(this);

			// Note: We do NOT dispose the models and mipmaps because we did not create them

			if (FrameProc != null)
				Graphics.Window.FrameProcSpritesRemove(this);

			// Dispose the VerticesEffect if we were the one who created it.
			if(VerticesEffect != null)
				VerticesEffect.Dispose();

			//base.Dispose();
			IsDisposed = true;

			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("end BlSprite {0} dispose", Name);
		}
	}
}
