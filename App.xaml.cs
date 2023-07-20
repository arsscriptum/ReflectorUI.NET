using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Reflector.UI
{
	public partial class App : Application
	{
		public static IReflector Reflector
		{
			get;
			internal set;
		}

		public App()
		{
		}

		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(string.Format("Unhandled exception!!!!!!!!!!!!!!\r\nMessage: {0}\r\nStack Trace: {1}\r\nPlease report it!!!!!!!!!", e.Exception.Message, e.Exception.StackTrace));
			e.Handled = true;
		}
	}
}