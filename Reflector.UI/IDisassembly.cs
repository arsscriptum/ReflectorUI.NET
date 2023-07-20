using ICSharpCode.AvalonEdit;
using Reflector.CodeModel;
using System;

namespace Reflector.UI
{
	public interface IDisassembly
	{
		ILanguage CurrentLanguage
		{
			get;
		}

		object CurrentObject
		{
			get;
		}

		TextEditor Text
		{
			get;
		}

		void GoBack();

		void GoForward();

		void Refresh();

		void Stop();
	}
}