using Reflector;
using Reflector.CodeModel;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Reflector.UI
{
	public interface IReflector : IFrameworkInputElement, IInputElement
	{
		ILanguage ActiveLanguage
		{
			get;
		}

		Reflector.UI.BarsManager BarsManager
		{
			get;
		}

		ICollection<ILanguage> Languages
		{
			get;
		}

		ITranslatorManager Translator
		{
			get;
		}

		IReflecWindow CreateWindow(ReflecWindow win);

		IReflecService GetService(string id);

		void Register(IReflecService win);
	}
}