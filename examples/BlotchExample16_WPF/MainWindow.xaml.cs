using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;


namespace BlotchExample
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public List<Window3d> Windows = new List<Window3d>();
		public MainWindow()
		{
			InitializeComponent();
		}

		private void StartMonoGameWindow_Click(object sender, RoutedEventArgs e)
		{
			int numWins = Windows.Count;
			new Thread(() =>
			{
				var win = new Window3d();
				Console.WriteLine("Creating window {0}", win.Window.Handle);
				Windows.Add(win);
				win.Run();
				Windows.Remove(win);
				win.Dispose();

			}).Start();

			while (Windows.Count == numWins)
				Thread.Sleep(10);
		}

		private void SendWindowCmd_Click(object sender, RoutedEventArgs e)
		{
			foreach(var window in Windows)
			{
				window.EnqueueCommand((win) => { Console.WriteLine("This is a queued command to window {0}", win.Window.Handle); });
			}
		}
	}
}
