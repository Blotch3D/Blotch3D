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
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blotch
{
	/// <summary>
	/// A mipmap of textures for a given BlSprite. You could load this from an image file and then assign
	/// it to a BlSprite#Mipmap. Note that this is a software mipmap (i.e. it isn't implemented
	/// in the 3D hardware). That is, only one resolution texture is used at time.
	/// </summary>
	public class BlMipmap: List<Texture2D>,IDisposable
	{
		BlGraphicsDeviceManager Graphics;

		/// <summary>
		/// Creates the mipmaps.
		/// </summary>
		/// <param name="graphics">Graphics device (typically the one owned by your BlWindow3D)</param>
		/// <param name="tex">Texture from which to create mipmaps, typically gotten from BlGraphics#LoadFromImageFile.</param>
		/// <param name="numMaps">Maximum number of mipmaps to create (none are created with lower resolution than 16x16)</param>
		/// <param name="reverseX">Whether to reverse pixels horizontally</param>
		/// <param name="reverseY">Whether to reverse pixels vertically</param>
		public BlMipmap(BlGraphicsDeviceManager graphics, Texture2D tex, int numMaps=999, bool reverseX = false, bool reverseY = false)
		{
			CreationThread = Thread.CurrentThread.ManagedThreadId;
			Graphics = graphics;
			CreateMipmaps(tex,numMaps,reverseX,reverseY);
		}

		void CreateMipmaps(Texture2D tex, int numMaps=999, bool reverseX=false, bool reverseY = false)
		{
			// this also clones the texture (even if no reversal is done), so the Dispose method should free even the highest res texture
			tex = ReverseTexture(tex, reverseX, reverseY);

			while (true)
			{
				numMaps--;

				Add(tex);

				if (tex.Width <= 16 || tex.Height <= 16 || numMaps <= 0)
					break;

				tex = CreateMipmap(tex);
			}
		}

		Texture2D ReverseTexture(Texture2D tex, bool reverseX = false, bool reverseY = false)
		{
			if (!reverseX && !reverseY)
				return Graphics.CloneTexture2D(tex);

			Color[] pixels = new Color[tex.Width * tex.Height];
			tex.GetData(pixels);

			var revTex = new Texture2D(Graphics.GraphicsDevice, tex.Width, tex.Height);

			Color[] rpixels = new Color[tex.Width * tex.Height];
			Parallel.For(0, tex.Width, (x) =>
			{
				var ox = x;
				if (reverseX)
					ox = tex.Width - 1 - x;
				for (var y = 0; y < tex.Height; y++)
				{
					var oy = y;
					if (reverseY)
						oy = tex.Width - 1 - y;

					rpixels[x + tex.Width * y] = pixels[ox + tex.Width * oy];
				}
			});
			
			revTex.SetData(rpixels);
			return revTex;
		}

		Texture2D CreateMipmap(Texture2D tex)
		{
			Color[] pixels = new Color[tex.Width * tex.Height];
			tex.GetData(pixels);

			var nwidth = tex.Width / 2;
			var nheight = tex.Height / 2;

			Color[] mpixels = new Color[nwidth * nheight];
			Parallel.For(0, (tex.Width - 1) / 2, (xd) =>
			   {
				   var x = xd * 2;
				   for (var y = 0; y <= tex.Height - 1; y += 2)
				   {
					   if (x + 1 >= tex.Width || y + 1 >= tex.Height)
						   break;

					   var p00 = pixels[x + tex.Width * y].ToVector4();
					   var p01 = pixels[x + 1 + tex.Width * y].ToVector4();
					   var p10 = pixels[x + tex.Width * (y + 1)].ToVector4();
					   var p11 = pixels[x + 1 + tex.Width * (y + 1)].ToVector4();

					// we assume a gamma of 2 (besides, its faster that way)

					var np = p00 * p00 + p01 * p01 + p10 * p10 + p11 * p11;
					   if (x / 2 < nwidth && y / 2 < nheight)
						   mpixels[(x / 2) + nwidth * (y / 2)] = new Color(
							   new Vector4(
								   (float)Math.Sqrt(np.X / 4f),
								   (float)Math.Sqrt(np.Y / 4f),
								   (float)Math.Sqrt(np.Z / 4f),
								   (float)Math.Sqrt(np.W / 4f)
								   ));
				   }
			   });
			var mipmap = new Texture2D(Graphics.GraphicsDevice,nwidth,nheight);

			mipmap.SetData(mpixels);

			return mipmap;
		}

		~BlMipmap()
		{
			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("BlMipmap destructor");

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
				Console.WriteLine("BlMipmap dispose");
			if (IsDisposed)
				return;

			if (CreationThread != Thread.CurrentThread.ManagedThreadId && BlDebug.ShowThreadWarnings)
				BlDebug.Message(String.Format("BlMipmap.Dispose() was called by thread {0} instead of thread {1}", Thread.CurrentThread.ManagedThreadId, CreationThread));

			GC.SuppressFinalize(this);

			foreach (var tex in this)
			{
				tex.Dispose();
			}

			//base.Dispose();
			IsDisposed = true;

			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("end BlMipmap dispose");
		}
	}
}
