// --------------------------------------------------------------------------------------------
// Copyright (C) 2010-2013 SIL International. All rights reserved.
//
// Distributable under the terms of the MIT License, as specified in the license.rtf file.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Chorus.FileTypeHandlers;
using Chorus.VcsDrivers.Mercurial;
using Chorus.merge;
using FLEx_ChorusPlugin.Infrastructure.DomainServices;
using SIL.IO;

namespace FLEx_ChorusPlugin.Infrastructure.Handling.Linguistics.TextCorpus
{
	internal sealed class TextCorpusFileTypeHandlerStrategy : IFieldWorksFileHandler
	{
		private const string TextInCorpus = "TextInCorpus"; // NB: Not the same as what is in SharedSharedConstants.

		#region Implementation of IFieldWorksFileHandler

		public bool CanValidateFile(string pathToFile)
		{
			return FileUtils.CheckValidPathname(pathToFile, SharedConstants.TextInCorpus);
		}

		public string ValidateFile(string pathToFile)
		{
			try
			{
				var doc = XDocument.Load(pathToFile);
				var root = doc.Root;
				if (root.Name.LocalName != TextInCorpus
					|| root.Element(SharedConstants.Text) == null)
				{
					return "Not valid text corpus file";
				}
				return CmObjectValidator.ValidateObject(MetadataCache.MdCache, root.Element(SharedConstants.Text));
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		public IChangePresenter GetChangePresenter(IChangeReport report, HgRepository repository)
		{
			return FieldWorksChangePresenter.GetCommonChangePresenter(report, repository);
		}

		public IEnumerable<IChangeReport> Find2WayDifferences(FileInRevision parent, FileInRevision child, HgRepository repository)
		{
			return Xml2WayDiffService.ReportDifferences(repository, parent, child,
				null,
				SharedConstants.Text, SharedConstants.GuidStr);
		}

		public void Do3WayMerge(MetadataCache mdc, MergeOrder mergeOrder)
		{
			FieldWorksCommonFileHandler.Do3WayMerge(mergeOrder, mdc, true);
		}

		public string Extension
		{
			get { return SharedConstants.TextInCorpus; }
		}

		#endregion
	}
}