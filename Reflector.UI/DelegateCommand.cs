using System;
using System.Windows.Input;

namespace Reflector.UI
{
	public class DelegateCommand : ICommand
	{
		private Func<object, bool> canExe;

		private Action<object> exe;

		private string name;

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public DelegateCommand(string name, Func<object, bool> canExe, Action<object> exe)
		{
			this.name = name;
			this.canExe = canExe;
			this.exe = exe;
		}

		public bool CanExecute(object parameter)
		{
			return this.canExe(parameter);
		}

		public void Execute(object parameter)
		{
			this.exe(parameter);
		}

		public event EventHandler CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested += value;
			}
			remove
			{
				CommandManager.RequerySuggested -= value;
			}
		}
	}
}