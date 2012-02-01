﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Chorus.FileTypeHanders;
using Chorus.FileTypeHanders.xml;
using Chorus.merge.xml.generic;
using FLEx_ChorusPlugin.Infrastructure;
using FLEx_ChorusPluginTests.Properties;
using NUnit.Framework;
using Palaso.IO;

namespace FLEx_ChorusPluginTests.Infrastructure.Handling
{
	/// <summary>
	/// Test the merge capabilities of the FieldWorksFileHandler implementation of the IChorusFileTypeHandler interface.
	/// </summary>
	[TestFixture]
	public class FieldWorksFileMergeTests
	{
		private IChorusFileTypeHandler _fileHandler;
		private TempFile _ourFile;
		private TempFile _theirFile;
		private TempFile _commonFile;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			_fileHandler = (from handler in ChorusFileTypeHandlerCollection.CreateWithInstalledHandlers().Handlers
							where handler.GetType().Name == "FieldWorksCommonFileHandler"
							   select handler).First();
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			_fileHandler = null;
		}

		[SetUp]
		public void TestSetup()
		{
			FieldWorksTestServices.SetupTempFilesWithExtension(".ClassData", out _ourFile, out _commonFile, out _theirFile);
		}

		[TearDown]
		public void TestTearDown()
		{
			FieldWorksTestServices.RemoveTempFiles(ref _ourFile, ref _commonFile, ref _theirFile);
		}

		[Test]
		public void CannotMergeNonexistantFile()
		{
			Assert.IsFalse(_fileHandler.CanMergeFile("bogusPathname"));
		}

		[Test]
		public void CannotMergeEmptyStringFile()
		{
			Assert.IsFalse(_fileHandler.CanMergeFile(String.Empty));
		}

		[Test]
		public void CanMergeGoodFwXmlFile()
		{
			var goodXmlPathname = Path.ChangeExtension(Path.GetTempFileName(), ".ClassData");
			try
			{
				File.WriteAllText(goodXmlPathname, TestResources.kXmlHeading + Environment.NewLine + TestResources.kClassDataEmptyTag);
				Assert.IsTrue(_fileHandler.CanMergeFile(goodXmlPathname));
			}
			finally
			{
				File.Delete(goodXmlPathname);
			}
		}

		[Test]
		public void CannotMergeNullFile()
		{
			Assert.IsFalse(_fileHandler.CanMergeFile(null));
		}

		[Test]
		public void Do3WayMergeThrowsOnNullInput()
		{
			Assert.Throws<ArgumentNullException>(() => _fileHandler.Do3WayMerge(null));
		}

		[Test]
		public void WinnerAndLoserEachAddedNewElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
</classdata>";
			var ourContent = commonAncestor.Replace("</classdata>", "<rt class='LexEntry' guid='newbieOurs'/></classdata>");
			var theirContent = commonAncestor.Replace("</classdata>", "<rt class='LexEntry' guid='newbieTheirs'/></classdata>");

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@guid=""newbieOurs""]", @"classdata/rt[@guid=""newbieTheirs""]" }, null,
				0, new List<Type>(),
				2, new List<Type> { typeof(XmlAdditionChangeReport), typeof(XmlAdditionChangeReport) });
		}

		[Test]
		public void WinnerAddedNewElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
</classdata>";
			var ourContent = commonAncestor.Replace("</classdata>", "<rt class='LexEntry' guid='newbieOurs'/></classdata>");
			const string theirContent = commonAncestor;

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@guid=""newbieOurs""]" }, null,
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlAdditionChangeReport) });
		}

		[Test]
		public void LoserAddedNewElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
</classdata>";
			const string ourContent = commonAncestor;
			var theirContent = commonAncestor.Replace("</classdata>", "<rt class='LexEntry' guid='newbieTheirs'/></classdata>");

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@guid=""newbieTheirs""]" }, null,
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlAdditionChangeReport) });
		}

		[Test]
		public void WinnerDeletedElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='goner'/>
</classdata>";
			var ourContent = commonAncestor.Replace("<rt class='LexEntry' guid='goner'/>", null);
			const string theirContent = commonAncestor;

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]" },
				new List<string> { @"classdata/rt[@guid=""goner""]" },
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlDeletionChangeReport) });
		}

		[Test]
		public void LoserDeletedElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='goner'/>
</classdata>";
			const string ourContent = commonAncestor;
			var theirContent = commonAncestor.Replace("<rt class='LexEntry' guid='goner'/>", null);

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]" },
				new List<string> { @"classdata/rt[@guid=""goner""]" },
				0, new List<Type>(),
				1, new List<Type> {typeof(XmlDeletionChangeReport) });
		}

		[Test]
		public void WinnerAndLoserBothDeletedElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='goner'/>
</classdata>";
			var ourContent = commonAncestor.Replace("<rt class='LexEntry' guid='goner'/>", null);
			var theirContent = commonAncestor.Replace("<rt class='LexEntry' guid='goner'/>", null);

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]" },
				new List<string> { @"classdata/rt[@guid=""goner""]" },
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlDeletionChangeReport) });
		}

		[Test]
		public void WinnerAndLoserBothMadeSameChangeToAttribute()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='dirtball' ownerguid='originalowner'/>
</classdata>";
			var ourContent = commonAncestor.Replace("originalowner", "newowner");
			var theirContent = commonAncestor.Replace("originalowner", "newowner");

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@ownerguid=""newowner""]" },
				new List<string> { @"classdata/rt[@ownerguid=""originalowner""]" },
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlAttributeBothMadeSameChangeReport) });
		}

		[Test]
		public void WinnerAndLoserBothChangedElementButInDifferentWays()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='dirtball' ownerguid='originalOwner'/>
</classdata>";
			var ourContent = commonAncestor.Replace("originalOwner", "newWinningOwner");
			var theirContent = commonAncestor.Replace("originalOwner", "newLosingOwner");

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> {@"classdata/rt[@guid=""oldie""]", @"classdata/rt[@ownerguid=""newWinningOwner""]"},
				new List<string> {@"classdata/rt[@ownerguid=""originalOwner""]", @"classdata/rt[@ownerguid=""newLosingOwner""]"},
				1, new List<Type> {typeof (BothEditedAttributeConflict)},
				0, new List<Type>());
		}

		[Test]
		public void WinnerChangedAttribute()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt		class='LexEntry' guid='oldie'/>
<rt
	class='LexEntry' guid='dirtball' ownerguid='originalowner'/>
</classdata>";
			var ourContent = commonAncestor.Replace("originalowner", "newowner");
			const string theirContent = commonAncestor;

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@ownerguid=""newowner""]" },
				new List<string> { @"classdata/rt[@ownerguid=""originalowner""]" },
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlAttributeChangedReport) });
		}

		[Test]
		public void LoserChangedAttribute()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='dirtball' ownerguid='originalowner'/>
</classdata>";
			const string ourContent = commonAncestor;
			var theirContent = commonAncestor.Replace("originalowner", "newowner");

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@ownerguid=""newowner""]" },
				new List<string> { @"classdata/rt[@ownerguid=""originalowner""]" },
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlAttributeChangedReport) });
		}

		[Test]
		public void WinnerEditedButLoserDeletedElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='dirtball' ownerguid='originalOwner'/>
</classdata>";
			var ourContent = commonAncestor.Replace("originalOwner", "newOwner");
			var theirContent = commonAncestor.Replace("<rt class='LexEntry' guid='dirtball' ownerguid='originalOwner'/>", null);

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@ownerguid=""newOwner""]" },
				new List<string> { @"classdata/rt[@ownerguid=""originalOwner""]" },
				1, new List<Type> { typeof(EditedVsRemovedElementConflict) },
				0, new List<Type>());
		}

		[Test]
		public void WinnerDeletedButLoserEditedElement()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='oldie'/>
<rt class='LexEntry' guid='dirtball' ownerguid='originalOwner'/>
</classdata>";
			var ourContent = commonAncestor.Replace("<rt class='LexEntry' guid='dirtball' ownerguid='originalOwner'/>", null);
			var theirContent = commonAncestor.Replace("originalOwner", "newOwner");

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt[@guid=""oldie""]", @"classdata/rt[@ownerguid=""newOwner""]" },
				new List<string> { @"classdata/rt[@ownerguid=""originalOwner""]" },
				1, new List<Type> { typeof(RemovedVsEditedElementConflict) },
				0, new List<Type>());
		}

		[Test]
		public void RemovePartOfMultiString()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variant </Run>
<Run ws='es'>variante</Run>
</AStr>
</Comment>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variant </Run>
</AStr>
</Comment>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variant </Run>
<Run ws='es'>variante</Run>
</AStr>
</Comment>
</rt>
</classdata>";

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Comment/AStr[@ws='en']", @"classdata/rt/Comment/AStr/Run[@ws='en']" },
				new List<string> { @"classdata/rt/Comment/AStr/Run[@ws='es']" },
				0, new List<Type>(),
				1, new List<Type> { typeof(XmlChangedRecordReport) }); // TODO: Let this keep failing, until new change reports are added in MergeAtomicElementService Run method.
		}

		[Test]
		public void EditDifferentPartsOfMultiStringGeneratesConflictReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variant </Run>
<Run ws='es'>variante</Run>
</AStr>
</Comment>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variantNew </Run>
<Run ws='es'>variante</Run>
</AStr>
</Comment>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variant </Run>
<Run ws='es'>varianteNew</Run>
</AStr>
</Comment>
</rt>
</classdata>";

			var result = FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Comment/AStr[@ws='en']",
					@"classdata/rt/Comment/AStr/Run[@ws='en']",
					@"classdata/rt/Comment/AStr/Run[@ws='es']" },
				null,
				1, new List<Type> { typeof(BothEditedTheSameElement) },
				0, new List<Type>());

			var doc = XDocument.Parse(result);
			var commentElement = doc.Element("classdata").Element(SharedConstants.RtTag).Element("Comment");
			var enAlt = commentElement.Element("AStr");
			var runs = enAlt.Descendants("Run");
			Assert.AreEqual("variantNew ", runs.ElementAt(0).Value);
			Assert.AreEqual("variante", runs.ElementAt(1).Value);
		}

		[Test]
		public void EditDifferentPartsOfMultiStringGeneratesConflictReportButNewAltAddedWithChangeReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variant </Run>
<Run ws='es'>variante</Run>
</AStr>
</Comment>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variantNew </Run>
<Run ws='es'>variante</Run>
</AStr>
</Comment>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='LexEntry' guid='9bffac9d-432a-43ce-a947-8e9f93074d65'>
<Comment>
<AStr ws='en'>
<Run ws='en'>variant </Run>
<Run ws='es'>varianteNew</Run>
</AStr>
<AStr ws='es'>
<Run ws='es'>variante</Run>
</AStr>
</Comment>
</rt>
</classdata>";

			var result = FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Comment/AStr[@ws='en']",
					@"classdata/rt/Comment/AStr[@ws='en']/Run[@ws='en']",
					@"classdata/rt/Comment/AStr[@ws='en']/Run[@ws='es']",
					@"classdata/rt/Comment/AStr[@ws='es']",
					@"classdata/rt/Comment/AStr[@ws='es']/Run[@ws='es']" },
				null,
				1, new List<Type> { typeof(BothEditedTheSameElement) }, // 1 conflict, since both edited the 'en' alternative.
				1, new List<Type> { typeof(XmlAdditionChangeReport) }); // 1 change, since 'they' added the new 'es' altenative.

			var doc = XDocument.Parse(result);
			var commentElement = doc.Element("classdata").Element(SharedConstants.RtTag).Element("Comment");
			var enAlt = commentElement.Element("AStr");
			var runs = enAlt.Descendants("Run");
			Assert.AreEqual("variantNew ", runs.ElementAt(0).Value);
			Assert.AreEqual("variante", runs.ElementAt(1).Value);
		}

		[Test]
		public void BothEditMultuUnicodePropertyGeneratesConflictReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmPossibilityList' guid='d72a1748-be3b-4164-9858-bc99de37e434' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
<Name>
<AUni ws='en'>Parts Of Speech</AUni>
<AUni ws='es'>Categorias Gramiticas</AUni>
<AUni ws='fr'>Parties du Discours</AUni>
</Name>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmPossibilityList' guid='d72a1748-be3b-4164-9858-bc99de37e434' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
<Name>
<AUni ws='en'>Parts Of Speech We Changed</AUni>
<AUni ws='es'>Categorias Gramiticas</AUni>
<AUni ws='fr'>Parties du Discours</AUni>
</Name>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmPossibilityList' guid='d72a1748-be3b-4164-9858-bc99de37e434' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
<Name>
<AUni ws='en'>Parts Of Speech They Changed</AUni>
<AUni ws='es'>Categorias Gramiticas</AUni>
<AUni ws='fr'>Parties du Discours</AUni>
</Name>
</rt>
</classdata>";

			var result = FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Name/AUni[@ws='en']",
					@"classdata/rt/Name/AUni[@ws='es']",
					@"classdata/rt/Name/AUni[@ws='fr']"},
				null,
				1, new List<Type> { typeof(XmlTextBothEditedTextConflict) }, // 1 conflict, since both edited the 'en' alternative.
				0, new List<Type>());

			var doc = XDocument.Parse(result);
			var nameElement = doc.Element("classdata").Element(SharedConstants.RtTag).Element("Name");
			var enAlt = (from auniElement in nameElement.Elements("AUni")
							where auniElement.Attribute("ws").Value == "en"
							select auniElement).First();
			Assert.AreEqual("Parts Of Speech We Changed", enAlt.Value);
		}

		[Test, Ignore("Sort this out, or jsut zap it, when old-style stuff goes away.")]
		public void EachDeletedOneAltWithOneChangeReported()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmPossibilityList' guid='d72a1748-be3b-4164-9858-bc99de37e434' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
<Name>
<AUni ws='en'>Parts Of Speech</AUni>
<AUni ws='es'>Categor�as Gram�ticas</AUni>
<AUni ws='fr'>Parties du Discours</AUni>
</Name>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmPossibilityList' guid='d72a1748-be3b-4164-9858-bc99de37e434' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
<Name>
<AUni ws='en'>Parts Of Speech</AUni>
<AUni ws='fr'>Parties du Discours</AUni>
</Name>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmPossibilityList' guid='d72a1748-be3b-4164-9858-bc99de37e434' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
<Name>
<AUni ws='en'>Parts Of Speech</AUni>
<AUni ws='es'>Categor�as Gram�ticas</AUni>
</Name>
</rt>
</classdata>";

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Name/AUni[@ws='en']" },
				new List<string> { @"classdata/rt/Name/AUni[@ws='es']",
					@"classdata/rt/Name/AUni[@ws='fr']" },
				0, new List<Type>(),
				2, new List<Type> { typeof(XmlTextDeletedReport), typeof(XmlTextDeletedReport) });
		}

		[Test]
		public void BothEditedTsStringWhichReturnsAConflictReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='PunctuationForm' guid='81bf4802-e411-42f7-98c7-319b13ed2e0b'>
	<Form>
		<Str>
			<Run ws='x-ezpi'>.</Run>
		</Str>
	</Form>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='PunctuationForm' guid='81bf4802-e411-42f7-98c7-319b13ed2e0b'>
	<Form>
		<Str>
			<Run ws='x-ezpi'>!</Run>
		</Str>
	</Form>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='PunctuationForm' guid='81bf4802-e411-42f7-98c7-319b13ed2e0b'>
	<Form>
		<Str>
			<Run ws='x-ezpi'>?</Run>
		</Str>
	</Form>
</rt>
</classdata>";

			var result = FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Form/Str/Run[@ws='x-ezpi']" },
				null,
				1, new List<Type> { typeof(BothEditedTheSameElement) },
				0, new List<Type>());

			var doc = XDocument.Parse(result);
			var runElement = doc.Element("classdata").Element(SharedConstants.RtTag).Element("Form").Element("Str").Element("Run");
			Assert.AreEqual("!", runElement.Value);
		}

		[Test]
		public void BothEditedTxtPropWhichReturnsAConflictReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='StStyle' guid='ee3c33fa-8141-4efe-a2c5-3e867c554b07' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
	<Rules>
		<Prop firstIndent='-36000' leadingIndent='9000' spaceBefore='1000' spaceAfter='2000' />
	</Rules>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='StStyle' guid='ee3c33fa-8141-4efe-a2c5-3e867c554b07' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
	<Rules>
		<Prop firstIndent='-36000' leadingIndent='10000' spaceBefore='1000' spaceAfter='2000' />
	</Rules>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='StStyle' guid='ee3c33fa-8141-4efe-a2c5-3e867c554b07' ownerguid='9719a466-2240-4dea-9722-9fe0746a30a6'>
	<Rules>
		<Prop firstIndent='-36000' leadingIndent='9000' spaceBefore='1000' spaceAfter='2000' bold='true' />
	</Rules>
</rt>
</classdata>";

			var result = FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Rules/Prop" },
				null,
				1, new List<Type> { typeof(BothEditedTheSameElement) },
				0, new List<Type>());

			var doc = XDocument.Parse(result);
			var propElement = doc.Element("classdata").Element(SharedConstants.RtTag).Element("Rules").Element("Prop");
			Assert.AreEqual("10000", propElement.Attribute("leadingIndent").Value);
			Assert.IsNull(propElement.Attribute("bold"));
		}

		[Test]
		public void BothEditedAtomicReferenceProducesConflictReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmTranslation' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Type>
<objsur t='r' guid='original' />
</Type>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmTranslation' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Type>
<objsur t='r' guid='ourNew' />
</Type>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmTranslation' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Type>
<objsur t='r' guid='theirNew' />
</Type>
</rt>
</classdata>";

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Type/objsur[@guid='ourNew']" },
				new List<string> { @"classdata/rt/Type/objsur[@guid='original']", @"classdata/rt/Type/objsur[@guid='theirNew']" },
				1, new List<Type> { typeof(BothEditedAttributeConflict) },
				0, new List<Type>());
		}

		[Test]
		public void BothEditedReferenceSequenceGeneratesConflictReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='Segment' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Analyses>
<objsur t='r' guid='original1' />
<objsur t='r' guid='original2' />
<objsur t='r' guid='original3' />
</Analyses>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='Segment' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Analyses>
<objsur t='r' guid='ourNew1' />
<objsur t='r' guid='ourNew2' />
</Analyses>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='Segment' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Analyses>
<objsur t='r' guid='theirNew1' />
</Analyses>
</rt>
</classdata>";

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Analyses/objsur[@guid='ourNew1']", @"classdata/rt/Analyses/objsur[@guid='ourNew2']" },
				new List<string> { @"classdata/rt/Analyses/objsur[@guid='original1']", @"classdata/rt/Analyses/objsur[@guid='original2']", @"classdata/rt/Analyses/objsur[@guid='original3']",
					@"classdata/rt/Analyses/objsur[@guid='theirNew1']" },
				1, new List<Type> { typeof(BothEditedTheSameElement) },
				0, new List<Type>());
		}

		[Test]
		public void BothEditedOwningSequenceGeneratesConflictReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='Segment' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Notes>
<objsur t='r' guid='original1' />
<objsur t='r' guid='original2' />
<objsur t='r' guid='original3' />
</Notes>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='Segment' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Notes>
<objsur t='r' guid='ourNew1' />
<objsur t='r' guid='ourNew2' />
</Notes>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='Segment' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<Notes>
<objsur t='r' guid='theirNew1' />
</Notes>
</rt>
</classdata>";

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/Notes/objsur[@guid='ourNew1']", @"classdata/rt/Notes/objsur[@guid='ourNew2']" },
				new List<string> { @"classdata/rt/Notes/objsur[@guid='original1']", @"classdata/rt/Notes/objsur[@guid='original2']", @"classdata/rt/Notes/objsur[@guid='original3']",
					@"classdata/rt/Notes/objsur[@guid='theirNew1']" },
				1, new List<Type> { typeof(BothEditedTheSameElement) },
				0, new List<Type>());
		}

		[Test, Ignore("Sort this out. Or, prhapos delete it, once old-style files go away.")]
		public void BothEditedOwningCollectionGeneratesChangeReport()
		{
			const string commonAncestor =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmFolder' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<SubFolders>
<objsur t='r' guid='original1' />
<objsur t='r' guid='original2' />
<objsur t='r' guid='original3' />
</SubFolders>
</rt>
</classdata>";
			const string ourContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmFolder' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<SubFolders>
<objsur t='r' guid='ourNew1' />
<objsur t='r' guid='ourNew2' />
</SubFolders>
</rt>
</classdata>";
			const string theirContent =
@"<?xml version='1.0' encoding='utf-8'?>
<classdata>
<rt class='CmFolder' guid='8e81ab31-31be-49e9-84ee-72a29f6ac50b' ownerguid='d9bc88e2-eeb3-4d99-91e4-99517ab1f9d4'>
<SubFolders>
<objsur t='r' guid='original1' />
<objsur t='r' guid='theirNew1' />
</SubFolders>
</rt>
</classdata>";

			FieldWorksTestServices.DoMerge(
				_fileHandler,
				_ourFile, ourContent,
				_commonFile, commonAncestor,
				_theirFile, theirContent,
				new List<string> { @"classdata/rt/SubFolders/objsur[@guid='ourNew1']", @"classdata/rt/SubFolders/objsur[@guid='ourNew2']", @"classdata/rt/SubFolders/objsur[@guid='theirNew1']" },
				new List<string> { @"classdata/rt/SubFolders/objsur[@guid='original1']", @"classdata/rt/SubFolders/objsur[@guid='original2']", @"classdata/rt/SubFolders/objsur[@guid='original3']" },
				0, new List<Type>(),
				2, new List<Type> { typeof(XmlChangedRecordReport), typeof(XmlChangedRecordReport) });
		}
	}
}