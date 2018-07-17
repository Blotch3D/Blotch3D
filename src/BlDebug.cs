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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Blotch
{
	/// <summary>
	/// This static class holds the debug flags.
	/// Many flags are initialized according to whether its a Debug build or Release build.
	/// Some flags enable exceptions for probable errors, and many flags cause warning messages to be sent
	/// to the console window, if it exist. For this reason you should first test your app as a debug build console app.
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
		/// Display a debug message and save it to the logfile if writeToLogFile is true.
		/// Call it like this, for example:
		/// if (Debug.drawables) Debug.Message(String.Format("Drawables: {0}", runModeDrawables));
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
				using (var stream = new StreamWriter(logFilePath))
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
