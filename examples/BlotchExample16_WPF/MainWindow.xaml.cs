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
		Example Win = null;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void StartMonoGameWindow_Click(object sender, RoutedEventArgs e)
		{
			new Thread(() =>
			{
				Win = new Example();
				Console.WriteLine("Creating window {0}", Win.Window.Handle);
				Win.Run();
				Win.Dispose();
			}).Start();
		}

		private void SendWindowCmd_Click(object sender, RoutedEventArgs e)
		{
			Win.EnqueueCommand((win) => { Console.WriteLine("This is a queued command to window {0}", win.Window.Handle); });
		}
	}
}
