using Reflector.CodeModel;
using System;
using System.Globalization;
using System.IO;

namespace Reflector.UI
{
	internal class TextFormatter : IFormatter
	{
		private StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);

		private bool newLine;

		private int indent;

		public TextFormatter()
		{
		}

		private void ApplyIndent()
		{
			if (this.newLine)
			{
				for (int i = 0; i < this.indent; i++)
				{
					this.writer.Write("    ");
				}
				this.newLine = false;
			}
		}

		public override string ToString()
		{
			return this.writer.ToString();
		}

		public void Write(string text)
		{
			this.ApplyIndent();
			this.writer.Write(text);
		}

		private void WriteBold(string text)
		{
			this.ApplyIndent();
			this.writer.Write(text);
		}

		private void WriteColor(string text, int color)
		{
			this.ApplyIndent();
			this.writer.Write(text);
		}

		public void WriteComment(string text)
		{
			this.writer.Write(text);
		}

		public void WriteDefinition(string text)
		{
			this.ApplyIndent();
			this.writer.Write(text);
		}

		public void WriteDefinition(string text, object target)
		{
			this.ApplyIndent();
			this.writer.Write(text);
		}

		public void WriteIndent()
		{
			this.indent++;
		}

		public void WriteKeyword(string text)
		{
			this.writer.Write(text);
		}

		public void WriteLine()
		{
			this.writer.WriteLine();
			this.newLine = true;
		}

		public void WriteLiteral(string text)
		{
			this.writer.Write(text);
		}

		public void WriteOutdent()
		{
			this.indent--;
		}

		public void WriteProperty(string propertyName, string propertyValue)
		{
		}

		public void WriteReference(string text, string toolTip, object reference)
		{
			this.ApplyIndent();
			this.writer.Write(text);
		}
	}
}