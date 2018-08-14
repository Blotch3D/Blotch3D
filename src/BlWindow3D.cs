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
	/// To make a 3D window, you must derive a class from BlWindow3D and override the #Setup, #FrameProc, and #FrameDraw methods.
	/// When it comes time to open the 3D window, you instantiate that class and call its “Run” method from the same thread
	/// that instantiated it. The Run method will call the #Setup, #FrameProc, and #FrameDraw methods when appropriate, and not
	/// return until the window closes. All code that accesses 3D resources must be done in that thread, including code that
	/// creates and uses all Blotch3D and MonoGame objects. Note that this rule also applies to any code structure that may
	/// internally use other threads, as well. Do not use Parallel, async, etc. code structures that access 3D resources.
	/// Other threads that need to access 3D resources can do so by passing a delegate to #EnqueueCommand and
	/// #EnqueueCommandBlocking.
	/// </summary>
	public class BlWindow3D : Game
	{

		/// <summary>
		/// The BlGraphicsDeviceManager associated with this window. This is automatically created when you create
		/// the BlWindow3D.
		/// </summary>
		public BlGraphicsDeviceManager Graphics;



		List<BlSprite> FrameProcSprites = new List<BlSprite>();
		Mutex FrameProcSpritesMutex = new Mutex();


		/// <summary>
		/// The GUI controls for this window. See BlGuiControl for details.
		/// </summary>
		public ConcurrentDictionary<string, BlGuiControl> GuiControls = new ConcurrentDictionary<string, BlGuiControl>();

		/// <summary>
		/// See #EnqueueCommand, #EnqueueCommandBlocking, and BlWindow3D for more info
		/// </summary>
		/// <param name="win">The BlWindow3D object</param>
		public delegate void Command(BlWindow3D win);
		class QueueCommand
		{
			public Command command=null;
			public AutoResetEvent evnt = null;
		}
		Mutex QueueMutex = new Mutex();
		Queue<QueueCommand> Queue = new Queue<QueueCommand>();

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
		/// create, move, and delete BlSprites. You can also use this for general thread safety of various operations.
		/// This method does not block.
		/// Also see BlWindow3D and the (blocking) #EnqueueCommandBlocking for more details.
		/// </summary>
		/// <param name="cmd"></param>
		public void EnqueueCommand(Command cmd)
		{
			var Qcmd = new QueueCommand()
			{
				command = cmd
			};
			try
			{
				QueueMutex.WaitOne();
				Queue.Enqueue(Qcmd);
			}
			finally
			{
				QueueMutex.ReleaseMutex();
			}
		}
		/// <summary>
		/// Since all operations accessing 3D resources must be done by the 3D thread,
		/// this allows other threads to
		/// send commands to execute in the 3D thread. For example, you might need another thread to be able to 
		/// create, move, and delete BlSprites. You can also use this for general thread safety of various operations.
		/// This method blocks until the command has executed.
		/// Also see BlWindow3D and the (non-blocking) #EnqueueCommand for more details.
		/// </summary>
		/// <param name="cmd"></param>
		public void EnqueueCommandBlocking(Command cmd)
		{
			var myEvent = new AutoResetEvent(false);
			var Qcmd = new QueueCommand()
			{
				command = cmd,
				evnt = myEvent
			};

			try
			{
				QueueMutex.WaitOne();
				Queue.Enqueue(Qcmd);
			}
			finally
			{
				QueueMutex.ReleaseMutex();
			}
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
		/// <summary>
		/// Used internally
		/// </summary>
		/// <param name="s"></param>
		public void FrameProcSpritesAdd(BlSprite s)
		{
			try
			{
				FrameProcSpritesMutex.WaitOne();
				FrameProcSprites.Add(s);
			}
			finally
			{
				FrameProcSpritesMutex.ReleaseMutex();
			}
		}
		/// <summary>
		/// Used internally
		/// </summary>
		/// <param name="s"></param>
		public void FrameProcSpritesRemove(BlSprite s)
		{
			try
			{
				FrameProcSpritesMutex.WaitOne();
				FrameProcSprites.Remove(s);
			}
			finally
			{
				FrameProcSpritesMutex.ReleaseMutex();
			}
		}
		void ExecuteSpriteFrameProcs()
		{
			try
			{
				FrameProcSpritesMutex.WaitOne();

				foreach (var s in FrameProcSprites)
				{
					s.ExecuteFrameProc();
				}
			}
			finally
			{
				FrameProcSpritesMutex.ReleaseMutex();
			}
		}
		void ExecutePendingCommands()
		{
			// execute any pending commands, in order.
			QueueCommand bcmd;
			try
			{
				QueueMutex.WaitOne();
				while (Queue.Count > 0)
				{
					bcmd = Queue.Dequeue();
					if (bcmd.command != null)
					{
						bcmd.command(this);
						if (bcmd.evnt != null)
							bcmd.evnt.Set();
					}
				}
			}
			finally
			{
				QueueMutex.ReleaseMutex();
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
			if (Graphics.SpriteBatch == null)
				Graphics.SpriteBatch = new SpriteBatch(Graphics.GraphicsDevice);

			Graphics.SpriteBatch.Begin();
			foreach (var ctrl in GuiControls)
			{
				Graphics.SpriteBatch.Draw(ctrl.Value.Texture, ctrl.Value.Position, Color.White);
			}
			Graphics.SpriteBatch.End();
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

			QueueMutex.Dispose();
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
