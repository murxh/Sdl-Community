﻿using System.Windows;
using System.Windows.Controls;

namespace GoogleTranslatorProvider.Behaviours
{
	public static class WebBrowserContentSource
	{
		public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
			"Html",
			typeof(string),
			typeof(WebBrowserContentSource),
			new FrameworkPropertyMetadata(OnHtmlChanged));

		[AttachedPropertyBrowsableForType(typeof(WebBrowser))]
		public static string GetHtml(WebBrowser webBrowser)
		{
			return (string)webBrowser.GetValue(HtmlProperty);
		}

		public static void SetHtml(WebBrowser webBrowser, string value)
		{
			webBrowser.SetValue(HtmlProperty, value);
		}

		static void OnHtmlChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var eNewValue = e.NewValue.ToString();
			if (obj is WebBrowser webBrowser
				&& !string.IsNullOrEmpty(eNewValue))
			{
				webBrowser.NavigateToString(eNewValue);
			}
		}
	}
}