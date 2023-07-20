using System;
using System.Xml;

namespace Reflector.UI
{
	public interface IReflecService
	{
		IReflector _App
		{
			get;
			set;
		}

		string Id
		{
			get;
		}

		object Exec(string name, params object[] args);

		object GetProp(string name);

		void LoadSettings(XmlNode node);

		void SaveSettings(XmlDocument doc, XmlNode node);

		void SetProp(string name, object value);
	}
}