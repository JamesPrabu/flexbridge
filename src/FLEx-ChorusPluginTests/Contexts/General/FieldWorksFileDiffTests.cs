using System;
using System.IO;
using System.Linq;
using Chorus.FileTypeHanders;
using Chorus.FileTypeHanders.xml;
using LibChorus.Tests;
using LibChorus.Tests.merge.xml.generic;
using NUnit.Framework;
using Palaso.IO;

namespace FLEx_ChorusPluginTests.Contexts.General
{
	/// <summary>
	/// Test the FieldWorksFileHandler implementation of the IChorusFileTypeHandler interface.
	/// </summary>
	[TestFixture]
	public class FieldWorksFileDiffTests
	{
		private IChorusFileTypeHandler m_fwFileHandler;
		private string m_goodXmlPathname;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_fwFileHandler = (from handler in ChorusFileTypeHandlerCollection.CreateWithInstalledHandlers().Handlers
							   where handler.GetType().Name == "FieldWorksFileHandler"
							   select handler).First();
			m_goodXmlPathname = Path.ChangeExtension(Path.GetTempFileName(), ".ClassData");
// ReSharper disable LocalizableElement
			File.WriteAllText(m_goodXmlPathname, "<?xml version='1.0' encoding='utf-8'?>" + Environment.NewLine + "<classdata />");
// ReSharper restore LocalizableElement
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			m_fwFileHandler = null;
			if (File.Exists(m_goodXmlPathname))
				File.Delete(m_goodXmlPathname);
		}

		[Test]
		public void Cannot_Diff_Nonexistant_File()
		{
			Assert.IsFalse(m_fwFileHandler.CanDiffFile("bogusPathname"));
		}

		[Test]
		public void Cannot_Diff_Null_File()
		{
			Assert.IsFalse(m_fwFileHandler.CanDiffFile(null));
		}

		[Test]
		public void Cannot_Diff_Empty_String_File()
		{
			Assert.IsFalse(m_fwFileHandler.CanDiffFile(String.Empty));
		}

		[Test]
		public void Can_Diff_Good_Fw_Xml_File()
		{
			Assert.IsTrue(m_fwFileHandler.CanDiffFile(m_goodXmlPathname));
		}

		// This test is of a series that test:
		//	DONE: 1. No changes (Is this needed? If Hg can commit unchanged files, probably.)
		//	DONE: 2. <rt> element added to child.
		//	DONE: 3. <rt> element removed from child.
		//	DONE: 4. <rt> element changed in child.
		//	5. Custom field stuff.
		//	6. Model version same or different. (Child is lower number?)
		[Test]
		public void NewObjectInChild_Reported()
		{
			const string parent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			// Third <rt> element (3d9ba4a1-4a25-11df-9879-0800200c9a66) is new.
			const string child =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a1-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			using (var parentTempFile = new TempFile(parent))
			using (var childTempFile = new TempFile(child))
			{
				var listener = new ListenerForUnitTests();
				var differ = Xml2WayDiffer.CreateFromFiles(parentTempFile.Path, childTempFile.Path, listener,
					null,
															 "rt",
															 "guid");
				differ.ReportDifferencesToListener();
				listener.AssertExpectedChangesCount(1);
				listener.AssertFirstChangeType<XmlAdditionChangeReport>();
			}
		}

		[Test]
		public void GuidCaseDifference_Not_Reported()
		{
			const string parent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			// Third <rt> element (3d9ba4a1-4a25-11df-9879-0800200c9a66) is new.
			const string child =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3D9B7D90-4A25-11DF-9879-0800200C9A66'>
</rt>
</classdata>";
			using (var parentTempFile = new TempFile(parent))
			using (var childTempFile = new TempFile(child))
			{
				var listener = new ListenerForUnitTests();
				var differ = Xml2WayDiffer.CreateFromFiles(parentTempFile.Path, childTempFile.Path, listener,
					null,
															 "rt",
															 "guid");

				// TODO: It would be nice if case on a guid didn't matter,
				// TODO: but that would require the basic XML diff code to know the id's attribute,
				// TODO: and that it was a guid.
				differ.ReportDifferencesToListener();
				listener.AssertExpectedChangesCount(1);
				listener.AssertFirstChangeType<XmlChangedRecordReport>();
			}
		}

		[Test]
		public void ObjectDeletedInChild_Reported()
		{
			const string parent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a1-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			// Third <rt> element (3d9ba4a1-4a25-11df-9879-0800200c9a66) in parent is removed in child.
			const string child =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			using (var parentTempFile = new TempFile(parent))
			using (var childTempFile = new TempFile(child))
			{
				var listener = new ListenerForUnitTests();
				var differ = Xml2WayDiffer.CreateFromFiles(parentTempFile.Path, childTempFile.Path, listener,
					null,
															 "rt",
															 "guid");
				differ.ReportDifferencesToListener();
				listener.AssertExpectedChangesCount(1);
				listener.AssertFirstChangeType<XmlDeletionChangeReport>();
			}
		}

		[Test]
		public void ObjectChangedInChild_Reported()
		{
			const string parent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66' ownerguid='3d9ba4a2-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			const string child =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66' ownerguid='3d9ba4a3-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			using (var parentTempFile = new TempFile(parent))
			using (var childTempFile = new TempFile(child))
			{
				var listener = new ListenerForUnitTests();
				var differ = Xml2WayDiffer.CreateFromFiles(parentTempFile.Path, childTempFile.Path, listener,
					null,
															 "rt",
															 "guid");
				differ.ReportDifferencesToListener();
				listener.AssertExpectedChangesCount(1);
				listener.AssertFirstChangeType<XmlChangedRecordReport>();
			}
		}

		[Test]
		public void NoChangesInChild_Reported()
		{
			const string parent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a1-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			// <rt> elements reordered, but no changes in any of them.
			const string child =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9ba4a1-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			using (var parentTempFile = new TempFile(parent))
			using (var childTempFile = new TempFile(child))
			{
				var listener = new ListenerForUnitTests();
				var differ = Xml2WayDiffer.CreateFromFiles(parentTempFile.Path, childTempFile.Path, listener,
					null,
															 "rt",
															 "guid");
				differ.ReportDifferencesToListener();
				listener.AssertExpectedChangesCount(0);
			}
		}

		[Test]
		public void Find2WayDifferences_Reported_Three_Changes()
		{
			const string parent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9ba4a4-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a1-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a6-4a25-11df-9879-0800200c9a66' ownerguid='3d9ba4a7-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			// One deletion, one change, one insertion, and three reordered, but not changed.
			const string child =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt guid='3d9ba4a1-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a0-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9b7d90-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a5-4a25-11df-9879-0800200c9a66'>
</rt>
<rt guid='3d9ba4a6-4a25-11df-9879-0800200c9a66' ownerguid='3d9ba4a8-4a25-11df-9879-0800200c9a66'>
</rt>
</classdata>";
			using (var repositorySetup = new RepositorySetup("randy"))
			{
				repositorySetup.AddAndCheckinFile("fwtest.xml", parent);
				repositorySetup.ChangeFileAndCommit("fwtest.xml", child, "change it");
				var hgRepository = repositorySetup.Repository;
				var allRevisions = (from rev in hgRepository.GetAllRevisions()
									   orderby rev.Number.LocalRevisionNumber
									   select rev).ToList();
				var first = allRevisions[0];
				var second = allRevisions[1];
				var firstFiR = hgRepository.GetFilesInRevision(first).First();
				var secondFiR = hgRepository.GetFilesInRevision(second).First();
				var result = m_fwFileHandler.Find2WayDifferences(firstFiR, secondFiR, hgRepository);
				Assert.AreEqual(3, result.Count());
			}
		}
	}
}