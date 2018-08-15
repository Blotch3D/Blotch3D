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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Blotch
{
	/// <summary>
	/// This static class holds the debug flags.
	/// Many flags are initialized according to whether its a Debug build or Release build.
	/// Some flags enable exceptions for probable errors, and many flags cause warning messages to be sent
	/// to the console window, if there is one. For this reason you should first test your app as a debug build console app.
	/// </summary>
	public static class BlDebug
	{
		/// <summary>
		/// If true, this enables informational messages and warnings about threads. Default is false. 
		/// </summary>
		public static bool ShowThreadInfo = false;

		/// <summary>
		/// If true, this causes warnings related to thread issues to appear. For example, it will cause warnings to appear when
		/// certain Blotch3D objects are not disposed by the same thread that created them. Default is true
		/// for debug, false for release build. 
		/// </summary>
		public static bool ShowThreadWarnings = true;

		static BlDebug()
		{
#if !DEBUG
			BlDebug.ShowThreadInfo = false;
			BlDebug.ShowThreadWarnings = false;
#endif
		}
		/// <summary>
		/// Display a debug message that includes the ThreadId, DateTime, SrcFile, SrcLineNumber, CallingMethod, and Message.
		/// 
		/// Call it like this, for example:
		/// BlDebug.Message(String.Format("MyInfo: {0}", Info));
		/// </summary>
		/// <param name="message">The text message to log</param>
		public static void Message(
			string message,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			// We'll add text to this variable according to the message and various debug flags
			string msg = "";

			// This will cause the managed thread ID to be included in the text
			//if (includeThreadId)
				msg += String.Format("{0:00000} ", Thread.CurrentThread.ManagedThreadId);

			// This will cause the date to be included in the text
			//if (includeDate)
				//msg += String.Format("{0} ", DateTime.Now.ToString("yyyy:mm:dd "));

			// This will cause the time (accurate to 100s of nanoseconds) to be included in the text
			//if (includeTime)
				msg += String.Format("{0} ", DateTime.Now.ToString("hh:mm:ss:fffffff "));

			// This will cause the source file name of the calling code to be included in the text
			//if (includeSourceFileName)
			{
				var parts = sourceFilePath.Split(Path.DirectorySeparatorChar);

				//if (includeLineNumber)
					msg += String.Format("{0}:{1} ", parts[parts.Length - 1], sourceLineNumber);
				//else
					//msg += String.Format("{0} ", parts[parts.Length - 1]);
			}

			// This will cause the calling function name to be included in the text
			//if (includeCallingFunction)
				msg += String.Format("{0} ", memberName);

			// now tack-on the actual message
			msg += message;

			// also writing to log file?
			/*
			if (writeToLogFile)
			{
				using (var stream = new StreamWriter(logFilePath,true))
				{
					stream.WriteLine(msg);
				}
			}
			*/
			// send message to console
			Console.WriteLine(msg);
		}

	}
}
