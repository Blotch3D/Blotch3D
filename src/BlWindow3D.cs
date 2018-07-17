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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace Blotch
{
	/// <summary>
	/// To create the 3D window, derive a class from BlWindow3D. Instantiate it and call its Run method from the same thread.
	/// When you instantiate it, it will create the 3D window and a separate thread we’ll
	/// call the “3D thread”. All model meshes, textures, fonts, etc. used by the 3D hardware must be created and accessed
	/// by the 3D thread, because supported hardware platforms require it. Its safest to assume all Blotch3D and MonoGame
	/// objects must be created and accessed in the 3D thread. Although it may apparently work in certain circumstances,
	/// do not have the window class constructor create or access any of these things, or have its instance initializers
	/// do it, because neither are executed by the 3D thread. To specify code to be executed in the context of the 3D
	/// thread, you can override the Setup, FrameProc, and/or FrameDraw methods, and other threads can pass a delegate
	/// to the EnqueueCommand and EnqueueCommandBlocking methods. When you override the Setup method it will be called
	/// once when the object is first created. You might put time-consuming overall initialization code in there like
	/// graphics setting initializations if different from the defaults, loading of persistent content (models, fonts,
	/// etc.), creation of persistent BlSprites, etc. Do not draw things in the 3D window from the setup method. When you
	/// override the FrameProc method it will be called once per frame (see BlGraphicsDeviceManager.FramePeriod). You can
	/// put code there that should be called periodically. This is typically code that must run at a constant rate, like
	/// code that implements smooth sprite and camera movement, etc. Do not draw things in the 3D window from the FrameProc
	/// method. When you override the FrameDraw method, the 3D thread calls PrepareDraw just before calling FrameDraw once
	/// per frame, but more rarely if CPU is being exhausted. This is where you put drawing code (BlSprite.Draw,
	/// BlGraphicsDeviceManager.DrawText, etc.). Finally, if you are developing a multithreaded app, when other threads
	/// need to create, change, or destroy 3D resources or otherwise do something in a thread-safe way with the 3D thread,
	/// they can queue a delegate to EnqueueCommand or EnqueueCommandBlocking, which makes sure the code is done by the 3D
	/// thread sequentially at the end of the current FrameProc. If user input to the 3D window needs to be conveyed back
	/// to app threads, you can create thread-safe queues for that as well. This inherits from MonoGame's "Game" class.
	/// </summary>
	public class BlWindow3D : Game
	{

		/// <summary>
		/// The BlGraphicsDeviceManager associated with this window. This is automatically created when you create
		/// the BlWindow3D.
		/// </summary>
		public BlGraphicsDeviceManager Graphics;

		/// <summary>
		/// Internal use only. Do not write.
		/// Holds the sprites that currently have a BlSprite.FrameProc defined.
		/// </summary>
		public List<BlSprite> FrameProcSprites = new List<BlSprite>();


		/// <summary>
		/// The GUI controls for this window. See BlGuiControl for details.
		/// </summary>
		public ConcurrentDictionary<string, BlGuiControl> GuiControls = new ConcurrentDictionary<string, BlGuiControl>();

		/// <summary>
		/// See EnqueueCommand, EnqueueCommandBlocking, and BlWindow3D for more info
		/// </summary>
		/// <param name="win">The BlWindow3D object</param>
		public delegate void Command(BlWindow3D win);
		class BlockingCommand
		{
			public Command command=null;
			public AutoResetEvent evnt = null;
		}
		BlockingCollection<BlockingCommand> Queue = new BlockingCollection<BlockingCommand>();

#if WINDOWS
		/// <summary>
		/// A WinForms wrapper to the window. (This is only present in Windows.)
		/// This gives you much more control of the window.
		/// </summary>
		public Form WindowForm = null;
#endif

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		public BlWindow3D()
		{
			CreationThread = Thread.CurrentThread.ManagedThreadId;

			Graphics = new BlGraphicsDeviceManager(this);
			Window.AllowUserResizing = true;
		}
		/// <summary>
		/// Since all operations accessing 3D resources must be done by the 3D thread,
		/// this allows other threads to
		/// send commands to execute in the 3D thread. For example, you might need another thread to be able to 
		/// create, move, and delete BlSprites. You can also use this for general thred safety of various operations.
		/// This method does not block.
		/// Also see BlWindow3D and the (blocking) EnqueueCommandBlocking for more details.
		/// </summary>
		/// <param name="cmd"></param>
		public void EnqueueCommand(Command cmd)
		{
			var Qcmd = new BlockingCommand()
			{
				command = cmd
			};

			Queue.Add(Qcmd);
		}
		/// <summary>
		/// Since all operations accessing 3D resources must be done by the 3D thread,
		/// this allows other threads to
		/// send commands to execute in the 3D thread. For example, you might need another thread to be able to 
		/// create, move, and delete BlSprites. You can also use this for general thred safety of various operations.
		/// This method blocks until the command has executed.
		/// Also see BlWindow3D and the (non-blocking) EnqueueCommand for more details.
		/// </summary>
		/// <param name="cmd"></param>
		public void EnqueueCommandBlocking(Command cmd)
		{
			var myEvent = new AutoResetEvent(false);
			var Qcmd = new BlockingCommand()
			{
				command = cmd,
				evnt = myEvent
			};

			Queue.Add(Qcmd);
			myEvent.WaitOne();
		}
#if WINDOWS
		void OnClose(object sender, FormClosingEventArgs e)
		{
			e.Cancel = !OnClosing();
		}
		/// <summary>
		/// This is only present in Windows. You can override this if you want to control what happens when an attempt is made to close the window.
		/// Return true to close the window, false to leave the window open. For example, you could hide the window with
		/// WindowForm.Hide(). 
		/// By default, this prompts the user for
		/// confirmation and if confirmed, kills the process.
		/// </summary>
		protected virtual bool OnClosing()
		{
			var result = MessageBox.Show("Really quit?", "", MessageBoxButtons.YesNo);
			if (result == DialogResult.Yes)
				Process.GetCurrentProcess().Kill();

			return false;
		}
#endif
		/// <summary>
		/// Used internally, Do NOT override. Use Setup instead.
		/// </summary>
		protected override void Initialize()
		{
			Graphics.Initialize();
			base.Initialize();

#if WINDOWS
			WindowForm = Form.FromHandle(Window.Handle) as Form;
			if (WindowForm != null)
			{
				WindowForm.FormClosing += OnClose;
			}
#endif

			Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateEnabled;
		}

		/// <summary>
		/// Used internally, Do NOT override. Use Setup instead.
		/// </summary>
		protected override void LoadContent()
		{
			base.LoadContent();
			Setup();
		}

		/// <summary>
		/// Override this and put all initialization and global content creation code in it.
		/// See BlWindow3D for details.
		/// </summary>
		protected virtual void Setup()
		{

		}

		/// <summary>
		/// Used internally, Do NOT override. Use FrameProc instead.
		/// </summary>
		/// <param name="timeInfo"></param>
		protected override void Update(GameTime timeInfo)
		{
			// Call derived class' FrameProc
			FrameProc(timeInfo);

			ServiceGuiControls();

			ExecutePendingCommands();

			ExecuteSpriteFrameProcs();
		}
		void ExecuteSpriteFrameProcs()
		{
			foreach (var s in FrameProcSprites)
			{
				s.FrameProc(s);
			}
		}
		void ExecutePendingCommands()
		{
			// execute any pending commands, in order.
			while (Queue.TryTake(out BlockingCommand bcmd, 0))
			{
				if (bcmd.command != null)
				{
					bcmd.command(this);
					if (bcmd.evnt != null)
						bcmd.evnt.Set();
				}
			}
		}

		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		/// <param name="timeInfo"></param>
		protected virtual void FrameProc(GameTime timeInfo)
		{

		}

		/// <summary>
		/// Used internally, Do NOT override. Use FrameDraw instead.
		/// </summary>
		/// <param name="timeInfo"></param>
		protected override void Draw(GameTime timeInfo)
		{
			Graphics.PrepareDraw();

			base.Draw(timeInfo);
			FrameDraw(timeInfo);

			DrawGuiControls();
		}
		void DrawGuiControls()
		{
			if (Graphics.MySpriteBatch == null)
				Graphics.MySpriteBatch = new SpriteBatch(Graphics.GraphicsDevice);

			Graphics.MySpriteBatch.Begin();
			foreach (var ctrl in GuiControls)
			{
				Graphics.MySpriteBatch.Draw(ctrl.Value.Texture, ctrl.Value.Position, Color.White);
			}
			Graphics.MySpriteBatch.End();
		}
		bool ServiceGuiControls()
		{
			bool hit = false;
			foreach (var ctrl in GuiControls)
			{
				if (ctrl.Value.HandleInput())
					hit = true;
			}
			return hit;
		}
		/// <summary>
		/// See BlWindow3D for details.
		/// </summary>
		/// <param name="timeInfo"></param>
		protected virtual void FrameDraw(GameTime timeInfo)
		{

		}

		~BlWindow3D()
		{
			if(BlDebug.ShowThreadInfo)
				Console.WriteLine("BlGame destructor");
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
		public new void Dispose()
		{
			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("BlGame dispose");
			if (IsDisposed)
				return;

			// We do NOT check for CreationThread, because it doesn't matter for this class
			//if (CreationThread != Thread.CurrentThread.ManagedThreadId && BlDebug.EnableThreadExceptions)
			//BlDebug.Message(String.Format("BlGame.Dispose() was called by thread {0} instead of thread {1}", Thread.CurrentThread.ManagedThreadId, CreationThread));

			GC.SuppressFinalize(this);

			base.Dispose();

			Queue.Dispose();
			Graphics.Dispose();
#if WINDOWS
			WindowForm.Dispose();
#endif
			IsDisposed = true;

			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("end BlGame dispose");
		}
	}
}
