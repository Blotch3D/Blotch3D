/*
Blotch3D (formerly GWin3D) Copyright (c) 1999-2020 Kelly Loum, all rights reserved except those granted in the following license.

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
using System.Drawing;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using Color = Microsoft.Xna.Framework.Color;

namespace Blotch
{
    /// <summary>
    /// To make a 3D window, you either create an instance with the BlWindow3d.Factory, or derive a class from
    /// BlWidnow3D. If you use the factory, at a minimum you must perform setup with a call to #EnqueueCommandBlocking,
    /// and then you can assign a BlWindow3d.#FrameDrawDelegate to perfom FrameDraw processing. Do not assign the
    /// BlWindow3d.#FrameDrawDelegate before the setup! If you derive from BlWindow3d, you must override at least the
    /// #FrameDraw method, and open it with a call to its “Run” method from the same thread that instantiated it. The
    /// Run method will call the #Setup, #FrameProc, and #FrameDraw methods when appropriate, and not return until the
    /// window closes. All code that accesses 3D resources must be done in that thread (i.e., one of the overrides),
    /// including code that creates and uses all Blotch3D and MonoGame objects. Note that this rule also applies to any
    /// code structure that may internally use other threads, as well. Other threads that need to access 3D resources or
    /// otheriwse do something in a thread-safe way with the 3D thread can do so by passing a delegate to
    /// #EnqueueCommand and #EnqueueCommandBlocking.
    /// </summary>
    public class BlWindow3D : Game
	{
		/// <summary>
        /// The BlGraphicsDeviceManager associated with this window. This is automatically created when you create the
        /// BlWindow3D.
        /// </summary>
		public BlGraphicsDeviceManager Graphics;

        /// <summary>
        /// A FrameDrawDelegate runs every frame. If you've overloaded FrameDraw, it runs immediatley after FrameDraw.
        /// You can use it instead of or in addition to FrameDraw to dynamically change the FrameDraw code. You can also
        /// use this directly in a BlWindow3d instance so you don't even need to derive from BlWindow3d (in which case
        /// you could use EnqueueCommandBlocking to perform your initialization code before assigning
        /// FrameDrawDelegate).
        /// </summary>
        public Action<BlWindow3D, GameTime> FrameDrawDelegate = null; 

        List<BlSprite> FrameProcSprites = new List<BlSprite>();
		Mutex FrameProcSpritesMutex = new Mutex();

        public Dictionary<string, Object> Objects = new Dictionary<string, object>();

        public Thread WinThread = null;

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
		object QueueMutex = new object();
		Queue<QueueCommand> Queue = new Queue<QueueCommand>();

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
        /// If you don't feel like to deriving from BlWindow, you can create a window using this factory and then use
        /// #EnqueueCommandBlocking and #FrameDrawDelegate to specify the Setup and FrameDraw code. Be sure to assign
        /// FrameDrawDelegate AFTER the EnqueueCommandBlocking call! See BlotchExample13_NoDerivation for an example.
        /// </summary>
        /// <returns>The BlWindiw3d instance</returns>
        public static BlWindow3D Factory()
        {
            BlWindow3D win = null;
            var thread = new Thread((p) =>
            {
                win = new BlWindow3D();
                win.Run();
            });
            thread.IsBackground = true;
            thread.Start();

            while(win==null) Thread.Sleep(100);

            return win;
        }

		/// <summary>
        /// Since all operations accessing 3D resources must be done by the 3D thread, this allows other threads to send
        /// commands to execute in the 3D thread. For example, you might need another thread to be able to create, move,
        /// and delete BlSprites. You can also use this for general thread safety of various operations. This method
        /// does not block. Also see BlWindow3D and the (blocking) #EnqueueCommandBlocking for more details.
        /// </summary>
        /// <param name="cmd">A command to perform in the window thread</param>
		public void EnqueueCommand(Command cmd)
		{
			var Qcmd = new QueueCommand()
			{
				command = cmd
			};

            // don't bother queuing it if we are already in the window thread
            if (Graphics.CreationThread == Thread.CurrentThread.ManagedThreadId)
            {
				if(cmd != null)
                {
					cmd(this);
				}
			}
            else // we have to queue it because we aren't in the window thread
            {
				try
				{
					lock (QueueMutex)
					{
						Queue.Enqueue(Qcmd);
					}
				}
				catch { }
            }
        }
        /// <summary>
        /// Since all operations accessing 3D resources must be done by the 3D thread, this allows other threads to send
        /// commands to execute in the 3D thread. For example, you might need another thread to be able to create, move,
        /// and delete BlSprites. You can also use this for general thread safety of various operations. This method
        /// blocks until the command has executed or the timeout has expired. Also see BlWindow3D and the (non-blocking)
        /// #EnqueueCommand for more details.
        /// </summary>
        /// <param name="cmd">A command to perform in the window thread, or null if you only want to wait a frame
        ///     </param>
        /// <returns>True if BlWindow completed command within timeoutMs milliseconds</returns>
        public bool EnqueueCommandBlocking(Command cmd = null, int timeoutMs = int.MaxValue)
		{
            // don't bother queuing it if we are already in the window thread
            if (Graphics.CreationThread == Thread.CurrentThread.ManagedThreadId)
            {
                cmd(this);
				return true;
            }
            else // we have to queue it because we aren't in the window thread
            {
                var myEvent = new AutoResetEvent(false);
                var Qcmd = new QueueCommand()
                {
                    command = cmd,
                    evnt = myEvent
                };

				try
				{
					lock (QueueMutex)
					{
						Queue.Enqueue(Qcmd);
					}
				}
				catch { }

				return myEvent.WaitOne(timeoutMs);
            }
        }

		/// <summary>
        /// Used internally, Do NOT override. Use Setup instead.
        /// </summary>
		protected override void Initialize()
		{
			Graphics.Initialize();
			base.Initialize();

			Graphics.GraphicsDevice.DepthStencilState = Graphics.DepthStencilStateEnabled;
		}

		protected void DrawTextList()
		{
            if (Graphics.SpriteBatch == null)
                Graphics.SpriteBatch = new SpriteBatch(Graphics.GraphicsDevice);
            Graphics.SpriteBatch.Begin();
            foreach (var txtInfo in Graphics.TextList)
            {
				try
				{
                    Graphics.SpriteBatch.DrawString(txtInfo.TextFont, txtInfo.Text, txtInfo.Coords, txtInfo.TextColor);
                } catch
				{
                    Graphics.SpriteBatch.DrawString(txtInfo.TextFont, "<txtErr>", txtInfo.Coords, txtInfo.TextColor);
                }
            }
            Graphics.TextList.Clear();
            Graphics.SpriteBatch.End();
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
        /// Override this and put all initialization and global content creation code in it. See BlWindow3D for details.
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
			if (Window.ClientBounds.Width < 2 || Window.ClientBounds.Height < 2)
			{
				return;
			}

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
			var tmpSpriteList = new List<BlSprite>();
			try
			{
				FrameProcSpritesMutex.WaitOne();

				foreach (var s in FrameProcSprites)
				{
					tmpSpriteList.Add(s);
				}

			}
			finally
			{
				FrameProcSpritesMutex.ReleaseMutex();
			}

			foreach (var s in tmpSpriteList)
			{
				s.ExecuteFrameProc();
			}
		}
		void ExecutePendingCommands()
		{
			// execute any pending commands, in order.
			QueueCommand bcmd;
			while (Queue.Count > 0)
			{
                lock (QueueMutex)
                {
					bcmd = Queue.Dequeue();
					if (bcmd.command != null)
					{
						bcmd.command(this);
					}
					if (bcmd.evnt != null)
					{
						bcmd.evnt.Set();
					}
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
			if (Window.ClientBounds.Width < 2 || Window.ClientBounds.Height < 2)
			{
				return;
			}

			Graphics.PrepareDraw();

			base.Draw(timeInfo);
			FrameDraw(timeInfo);

			if(FrameDrawDelegate!= null)
			{
                FrameDrawDelegate(this, timeInfo);
            }

            DrawTextList();

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
			throw new Exception(string.Format("BlWindow was garbage collected before its Dispose was called"));
		}

		int CreationThread = -1;
		/// <summary>
        /// Set when the object is Disposed.
        /// </summary>
		public bool IsDisposed = false;
		/// <summary>
        /// When finished with the object, you should call Dispose() from the same thread that created the object. You
        /// can call this multiple times, but once is enough. If it isn't called before the object becomes inaccessible,
        /// then the destructor will call it and, if BlDebug.EnableDisposeErrors is true (it is true by default for
        /// Debug builds), then it will get an exception saying that it wasn't called by the same thread that created
        /// it. This is because the platform's underlying 3D library (OpenGL, etc.) often requires 3D resources to be
        /// managed only by one thread.
        /// </summary>
		public new void Dispose()
		{
			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("BlGame dispose");

			if (IsDisposed)
				return;

			if (CreationThread != Thread.CurrentThread.ManagedThreadId)
				throw new Exception(String.Format("BlWindow.Dispose() was called by thread {0} instead of thread {1}", Thread.CurrentThread.ManagedThreadId, CreationThread));

			GC.SuppressFinalize(this);

			base.Dispose();

			Graphics.Dispose();
			IsDisposed = true;

			if (BlDebug.ShowThreadInfo)
				Console.WriteLine("end BlGame dispose");
		}
	}
}
