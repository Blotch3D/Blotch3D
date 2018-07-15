using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Blotch;

namespace BlotchExample17_WinForms
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			new Thread(() =>
			{
				var Win = new Example();
				Win.Run();
				Win.Dispose();
			}).Start();

			Application.Run(new Form1());
		}
	}
}
