using System;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;

namespace FLEx_ChorusPluginTests
{
	public class XmlTestHelper
	{
		public static void AssertXPathMatchesExactlyOne(string xml, string xpath)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			AssertXPathMatchesExactlyOneInner(doc, xpath);
		}
		public static void AssertXPathMatchesExactlyOne(XmlNode node, string xpath)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(node.OuterXml);
			AssertXPathMatchesExactlyOneInner(doc, xpath);
		}

		private static void AssertXPathMatchesExactlyOneInner(XmlDocument doc, string xpath)
		{
			XmlNodeList nodes = doc.SelectNodes(xpath);
			if (nodes == null || nodes.Count != 1)
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = true;
				settings.ConformanceLevel = ConformanceLevel.Fragment;
				XmlWriter writer = XmlTextWriter.Create(Console.Out, settings);
				doc.WriteContentTo(writer);
				writer.Flush();
				if (nodes != null && nodes.Count > 1)
				{
					Assert.Fail("Too Many matches for XPath: {0}", xpath);
				}
				else
				{
					Assert.Fail("No Match: XPath failed: {0}", xpath);
				}
			}
		}

		public static void AssertXPathNotNull(string documentPath, string xpath)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(documentPath);
			XmlNode node = doc.SelectSingleNode(xpath);
			if (node == null)
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = true;
				settings.ConformanceLevel = ConformanceLevel.Fragment;
				XmlWriter writer = XmlTextWriter.Create(Console.Out, settings);
				doc.WriteContentTo(writer);
				writer.Flush();
			}
			Assert.IsNotNull(node);
		}

		public static void AssertXPathIsNull(string xml, string xpath)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			XmlNode node = doc.SelectSingleNode(xpath);
			if (node != null)
			{
				XmlWriterSettings settings = new XmlWriterSettings
												{
													Indent = true,
													ConformanceLevel = ConformanceLevel.Fragment
												};
				XmlWriter writer = XmlTextWriter.Create(Console.Out, settings);
				doc.WriteContentTo(writer);
				writer.Flush();
			}
			Assert.IsNull(node);
		}

		public static void AssertXPathMatchesExactlyOne(string xml, string xpath, Dictionary<string, string> namespaces)
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			var namespaceManager = new XmlNamespaceManager(doc.NameTable);
			foreach (var namespaceKvp in namespaces)
				namespaceManager.AddNamespace(namespaceKvp.Key, namespaceKvp.Value);

			var nodes = doc.SelectNodes(xpath, namespaceManager);
			if (nodes != null && nodes.Count == 1)
				return;

			var settings = new XmlWriterSettings { Indent = true, ConformanceLevel = ConformanceLevel.Fragment };
			var writer = XmlTextWriter.Create(Console.Out, settings);
			doc.WriteContentTo(writer);
			writer.Flush();
			if (nodes != null && nodes.Count > 1)
			{
				Assert.Fail("Too Many matches for XPath: {0}", xpath);
			}
			else
			{
				Assert.Fail("No Match: XPath failed: {0}", xpath);
			}
		}

		public static void AssertXPathIsNull(string xml, string xpath, Dictionary<string, string> namespaces)
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			var namespaceManager = new XmlNamespaceManager(doc.NameTable);
			foreach (var namespaceKvp in namespaces)
				namespaceManager.AddNamespace(namespaceKvp.Key, namespaceKvp.Value);

			var node = doc.SelectSingleNode(xpath, namespaceManager);
			if (node != null)
			{
				var settings = new XmlWriterSettings
				{
					Indent = true,
					ConformanceLevel = ConformanceLevel.Fragment
				};
				var writer = XmlTextWriter.Create(Console.Out, settings);
				doc.WriteContentTo(writer);
				writer.Flush();
			}
			Assert.IsNull(node);
		}
	}
}