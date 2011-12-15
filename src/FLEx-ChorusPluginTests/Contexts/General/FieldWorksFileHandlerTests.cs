﻿using System.Linq;
using Chorus.FileTypeHanders;
using NUnit.Framework;

namespace FLEx_ChorusPluginTests.Contexts.General
{
	/// <summary>
	/// Test the FieldWorksFileHandler implementation of the IChorusFileTypeHandler interface.
	/// </summary>
	[TestFixture]
	public class FieldWorksFileHandlerTests
	{
		private IChorusFileTypeHandler m_fwFileHandler;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_fwFileHandler = (from handler in ChorusFileTypeHandlerCollection.CreateWithInstalledHandlers().Handlers
						 where handler.GetType().Name == "FieldWorksFileHandler"
						 select handler).First();
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			m_fwFileHandler = null;
		}

		[Test]
		public void DescribeInitialContents_Should_Have_Added_For_Label()
		{
			var initialContents = m_fwFileHandler.DescribeInitialContents(null, null);
			Assert.AreEqual(1, initialContents.Count());
			var onlyOne = initialContents.First();
			Assert.AreEqual("Added", onlyOne.ActionLabel);
		}

		[Test]
		public void GetExtensionsOfKnownTextFileTypes_Is_Xml()
		{
			var extensions = m_fwFileHandler.GetExtensionsOfKnownTextFileTypes().ToArray();
			Assert.AreEqual(1, extensions.Count(), "Wrong number of extensions.");
			Assert.AreEqual("ClassData", extensions[0]);
		}
	}
}
