/*
Blotch3D Copyright 1999-2018 Kelly Loum

Blotch3D is a C# 3D graphics library that notably simplifies 3D development.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
	/// it to the Mipmap member of a BlSprite. Note that this is a software mipmap (i.e. it isn't implemented
	/// in the 3D hardware). That is, only one resolution texture is used at time.
	/// </summary>
	public class BlMipmap: List<Texture2D>,IDisposable
	{
		BlGraphicsDeviceManager Graphics;

		/// <summary>
		/// Creates the mipmaps.
		/// </summary>
		/// <param name="graphics">Graphics device (typically the one owned by your BlWindow3D)</param>
		/// <param name="tex">Texture from which to create mipmaps, typically gotten from BlGraphics.LoadFromImageFile.</param>
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
