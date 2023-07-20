using System;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Reflector.UI
{
	internal class SearchService : IReflecService
	{
		public readonly static SearchService Instance;

		public IReflector _App
		{
			get;
			set;
		}

		public string Id
		{
			get
			{
				return "Search";
			}
		}

		static SearchService()
		{
			SearchService.Instance = new SearchService();
		}

		private SearchService()
		{
		}

		public object Exec(string name, params object[] args)
		{
			if (name != "Search.Show")
			{
				throw new InvalidOperationException(name);
			}
			Search search = new Search();
			IReflecWindow win = this._App.CreateWindow(search);
			win.Initialize(true);
			win.ShowDocument();
			win.Activate();
			return null;
		}

		public object GetProp(string name)
		{
			throw new InvalidOperationException(name);
		}

		public void LoadSettings(XmlNode node)
		{
		}

		public void SaveSettings(XmlDocument doc, XmlNode node)
		{
		}

		public void SetProp(string name, object value)
		{
			throw new InvalidOperationException(name);
		}
	}
}