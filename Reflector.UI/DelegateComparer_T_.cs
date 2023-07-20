using System;
using System.Collections.Generic;

namespace Reflector.UI
{
	internal class DelegateComparer<T> : IComparer<T>
	{
		private Comparison<T> comp;

		public DelegateComparer(Comparison<T> comp)
		{
			this.comp = comp;
		}

		public int Compare(T x, T y)
		{
			return this.comp(x, y);
		}
	}
}