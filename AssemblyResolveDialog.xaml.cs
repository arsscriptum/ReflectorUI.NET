using Microsoft.Win32;
using Mono.Cecil;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Reflector.UI
{
	public partial class AssemblyResolveDialog : Window
	{
		private string resolved;

		public AssemblyResolveDialog()
		{
			this.InitializeComponent();
		}

		private void browse_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog()
			{
				Filter = ".NET Assembly (*.exe; *.dll)|*.exe;*.dll|All Files|*.*"
			};
			if (ofd.ShowDialog().GetValueOrDefault())
			{
				this.path.Text = ofd.FileName;
			}
		}

		private void cancel_Click(object sender, RoutedEventArgs e)
		{
			base.DialogResult = new bool?(false);
		}

		private void ok_Click(object sender, RoutedEventArgs e)
		{
			string str;
			base.DialogResult = new bool?(true);
			string pth = this.path.Text;
			if (File.Exists(pth))
			{
				str = pth;
			}
			else
			{
				str = null;
			}
			this.resolved = str;
		}

		public static string Resolve(AssemblyNameReference refer)
		{
			AssemblyResolveDialog ret = new AssemblyResolveDialog();
			ret.fullName.Text = refer.ToString();
			if (!ret.ShowDialog().GetValueOrDefault())
			{
				return null;
			}
			return ret.resolved;
		}
	}
}