using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Reflector.UI
{
	public class Bitmap : FrameworkElement
	{
		public readonly static DependencyProperty SourceProperty;

		private EventHandler _sourceDownloaded;

		private EventHandler<ExceptionEventArgs> _sourceFailed;

		private Point _pixelOffset;

		public BitmapSource Source
		{
			get
			{
				return (BitmapSource)base.GetValue(Bitmap.SourceProperty);
			}
			set
			{
				base.SetValue(Bitmap.SourceProperty, value);
			}
		}

		static Bitmap()
		{
			Bitmap.SourceProperty = DependencyProperty.Register("Source", typeof(BitmapSource), typeof(Bitmap), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(Bitmap.OnSourceChanged)));
		}

		public Bitmap()
		{
			this._sourceDownloaded = new EventHandler(this.OnSourceDownloaded);
			this._sourceFailed = new EventHandler<ExceptionEventArgs>(this.OnSourceFailed);
			base.LayoutUpdated += new EventHandler(this.OnLayoutUpdated);
		}

		private Point ApplyVisualTransform(Point point, Visual v, bool inverse)
		{
			bool success = true;
			return this.TryApplyVisualTransform(point, v, inverse, true, out success);
		}

		private bool AreClose(Point point1, Point point2)
		{
			if (!this.AreClose(point1.X, point2.X))
			{
				return false;
			}
			return this.AreClose(point1.Y, point2.Y);
		}

		private bool AreClose(double value1, double value2)
		{
			if (value1 == value2)
			{
				return true;
			}
			double delta = value1 - value2;
			if (delta >= 1.53E-06)
			{
				return false;
			}
			return delta > -1.53E-06;
		}

		private Point GetPixelOffset()
		{
			Point pixelOffset = new Point();
			PresentationSource ps = PresentationSource.FromVisual(this);
			if (ps != null)
			{
				Visual rootVisual = ps.RootVisual;
				pixelOffset = base.TransformToAncestor(rootVisual).Transform(pixelOffset);
				pixelOffset = this.ApplyVisualTransform(pixelOffset, rootVisual, false);
				pixelOffset = ps.CompositionTarget.TransformToDevice.Transform(pixelOffset);
				pixelOffset.X = Math.Round(pixelOffset.X);
				pixelOffset.Y = Math.Round(pixelOffset.Y);
				pixelOffset = ps.CompositionTarget.TransformFromDevice.Transform(pixelOffset);
				pixelOffset = this.ApplyVisualTransform(pixelOffset, rootVisual, true);
				pixelOffset = rootVisual.TransformToDescendant(this).Transform(pixelOffset);
			}
			return pixelOffset;
		}

		private Matrix GetVisualTransform(Visual v)
		{
			if (v == null)
			{
				return Matrix.Identity;
			}
			Matrix m = Matrix.Identity;
			Transform transform = VisualTreeHelper.GetTransform(v);
			if (transform != null)
			{
				m = Matrix.Multiply(m, transform.Value);
			}
			Vector offset = VisualTreeHelper.GetOffset(v);
			m.Translate(offset.X, offset.Y);
			return m;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			Size measureSize = new Size();
			BitmapSource bitmapSource = this.Source;
			if (bitmapSource != null)
			{
				measureSize = new Size((double)bitmapSource.PixelWidth, (double)bitmapSource.PixelHeight);
			}
			return measureSize;
		}

		private void OnLayoutUpdated(object sender, EventArgs e)
		{
			if (!this.AreClose(this.GetPixelOffset(), this._pixelOffset))
			{
				base.InvalidateVisual();
			}
		}

		protected override void OnRender(DrawingContext dc)
		{
			BitmapSource bitmapSource = this.Source;
			if (bitmapSource != null)
			{
				this._pixelOffset = this.GetPixelOffset();
				dc.DrawImage(bitmapSource, new Rect(this._pixelOffset, new Size((double)bitmapSource.PixelWidth, (double)bitmapSource.PixelHeight)));
			}
		}

		private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Bitmap bitmap = (Bitmap)d;
			BitmapSource oldValue = (BitmapSource)e.OldValue;
			BitmapSource newValue = (BitmapSource)e.NewValue;
			if (oldValue != null && bitmap._sourceDownloaded != null && !oldValue.IsFrozen && oldValue != null)
			{
				oldValue.DownloadCompleted -= bitmap._sourceDownloaded;
				oldValue.DownloadFailed -= bitmap._sourceFailed;
			}
			if (newValue != null && newValue != null && !newValue.IsFrozen)
			{
				newValue.DownloadCompleted += bitmap._sourceDownloaded;
				newValue.DownloadFailed += bitmap._sourceFailed;
			}
		}

		private void OnSourceDownloaded(object sender, EventArgs e)
		{
			base.InvalidateMeasure();
			base.InvalidateVisual();
		}

		private void OnSourceFailed(object sender, ExceptionEventArgs e)
		{
			this.Source = null;
			this.BitmapFailed(this, e);
		}

		private Point TryApplyVisualTransform(Point point, Visual v, bool inverse, bool throwOnError, out bool success)
		{
			success = true;
			if (v != null)
			{
				Matrix visualTransform = this.GetVisualTransform(v);
				if (inverse)
				{
					if (!throwOnError && !visualTransform.HasInverse)
					{
						success = false;
						return new Point(0, 0);
					}
					visualTransform.Invert();
				}
				point = visualTransform.Transform(point);
			}
			return point;
		}

		public event EventHandler<ExceptionEventArgs> BitmapFailed;
	}
}