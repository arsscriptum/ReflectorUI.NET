using AvalonDock;
using System;
using System.Runtime.CompilerServices;

namespace Reflector.UI
{
	public interface IReflecWindow
	{
		ReflecWindow Content
		{
			get;
		}

		bool IsVisible
		{
			get;
		}

		void Activate();

		void Close();

		void Dock(AnchorStyle anchor);

		void Initialize(bool isDock);

		void Show();

		void ShowDocument();

		event EventHandler Closed;
	}
}