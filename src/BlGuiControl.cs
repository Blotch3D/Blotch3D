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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blotch
{
	/// <summary>
	/// A 2D GUI control. To create a GUI control: instantiate one of these, set its initial Texture, window position, and
	/// delegate, and then add it to BlWindow3D#GuiControls. (Any member can be
	/// dynamically changed.) The texture will be
	/// displayed, and then each frame the mouse is over it the delegate will be called.  The delegate typically would examine the
	/// current mouse state (Mouse.GetState()) and the #PrevMouseState member to detect button changes, etc. and perform an action.
	/// The delegate is called in the context of the window's 3D thread after the BlWindow3D#FrameProc method.
	/// </summary>
	public class BlGuiControl
	{
		/// <summary>
		/// The texture to display for this control. Don't forget to dispose it when done.
		/// </summary>
		public Texture2D Texture = null;

		/// <summary>
		/// The pixel position of this control in the BlWindow3D
		/// </summary>
		public Vector2 Position = Vector2.Zero;

		/// <summary>
		/// Delegates for a BlGuiControl are of this type
		/// </summary>
		/// <param name="guiCtrl"></param>
		public delegate void OnMouseChangeDelegate(BlGuiControl guiCtrl);
		/// <summary>
		/// The delegate to call each frame (from the 3D thread) when the mouse is over the control. A typical delegate would make a decision
		/// according to #PrevMouseState and the current mouse state (Mouse.GetState).
		/// </summary>
		public OnMouseChangeDelegate OnMouseOver = null;

		/// <summary>
		/// The previous mouse state. A delegate typically uses this along with the current mouse state to make a decision.
		/// </summary>
		public MouseState PrevMouseState = new MouseState();

		/// <summary>
		/// The window this BlGuiControl is in.
		/// </summary>
		public BlWindow3D Window = null;

		public BlGuiControl(BlWindow3D window)
		{
			Window = window;
		}

		/// <summary>
		/// Periodically called by BlWindow3D. You shouldn't need to call this. 
		/// </summary>
		/// <returns>True if mouse is over any control, false otherwise.</returns>
		public bool HandleInput()
		{
			var mouseState = Mouse.GetState();
			if (
				mouseState.X >= Position.X
				&&
				mouseState.X < Position.X + Texture.Width
				&&
				mouseState.Y >= Position.Y
				&&
				mouseState.Y < Position.Y + Texture.Height
				/*
				&&
				(
					mouseState.X != PrevMouseState.X
					||
					mouseState.Y != PrevMouseState.Y
				)
				*/
				)
			{
				if (OnMouseOver != null)
					OnMouseOver(this);
				PrevMouseState = Mouse.GetState();
				return true;
			}
			return false;
		}
	}
}
