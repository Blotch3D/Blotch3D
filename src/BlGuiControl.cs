using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Blotch
{
	/// <summary>
	/// A 2D GUI control. To create a GUI control: instantiate one of these, set its initial Texture (remember to create it in the
	/// 3D thread context), window position, and delegate, and then add it to BlWindow3D#GuiControls. (Any member can be
	/// dynamically changed.) The texture will be
	/// displayed, and then each frame the mouse is over it the delegate will be called.  The delegate typically would examine the
	/// current mouse state (Mouse.GetState()) and the #PrevMouseState member to detect button changes, etc. and perform an action.
	/// The delegate is called in the context of the window's 3D thread after the BlWindow3D#FrameProc method. You can use
	/// BlGraphicsDeviceManager#TextToTexture to create a textual textures, or just load a texture from a content file. Remember
	/// to Dispose textures when you are done with them.
	/// </summary>
	public class BlGuiControl
	{
		/// <summary>
		/// The texture to display for this control. Don't forget to dispose it when done.
		/// </summary>
		public Texture2D Texture = null;

		/// <summary>
		/// The pixel position in the BlWindow3D of this control
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
		/// The previous mouse state. A delegte typiclly uses this along with the current mouse state to make a decision.
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
