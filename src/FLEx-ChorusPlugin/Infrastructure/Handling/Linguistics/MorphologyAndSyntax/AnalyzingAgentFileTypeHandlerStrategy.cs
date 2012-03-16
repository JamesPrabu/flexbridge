using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Chorus.FileTypeHanders;
using Chorus.VcsDrivers.Mercurial;
using Chorus.merge;
using Chorus.merge.xml.generic;
using FLEx_ChorusPlugin.Infrastructure.DomainServices;
using Palaso.IO;

namespace FLEx_ChorusPlugin.Infrastructure.Handling.Linguistics.MorphologyAndSyntax
{
	internal sealed class AnalyzingAgentFileTypeHandlerStrategy : IFieldWorksFileHandler
	{
		#region Implementation of IFieldWorksFileHandler

		public bool CanValidateFile(string pathToFile)
		{
			return FileUtils.CheckValidPathname(pathToFile, SharedConstants.Agents) &&
				   Path.GetFileName(pathToFile) == SharedConstants.AnalyzingAgentsFilename;
		}

		public string ValidateFile(string pathToFile)
		{
			try
			{
				var doc = XDocument.Load(pathToFile);
				var root = doc.Root;
				if (root.Name.LocalName != SharedConstants.AnalyzingAgents
					|| (root.Element(SharedConstants.Header) != null)
					|| !root.Elements(SharedConstants.CmAgent).Any())
				{
					return "Not valid analyzing agent file";
				}

				return root.Elements(SharedConstants.CmAgent)
					.Select(filterElement => CmObjectValidator.ValidateObject(MetadataCache.MdCache, filterElement)).FirstOrDefault(res => res != null);
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
			return Xml2WayDiffService.ReportDifferences(
				repository,
				parent,
				child,
				null,
				SharedConstants.CmAgent, SharedConstants.GuidStr);
		}

		public void Do3WayMerge(MetadataCache mdc, MergeOrder mergeOrder)
		{
			mdc.AddCustomPropInfo(mergeOrder); // NB: Must be done before FieldWorksCommonMergeStrategy is created.

			XmlMergeService.Do3WayMerge(
				mergeOrder,
				new FieldWorksCommonMergeStrategy(mergeOrder.MergeSituation, mdc),
				null,
				SharedConstants.CmAgent,
				SharedConstants.GuidStr,
				WritePreliminaryAnalyzingAgentInformation);
		}

		public string Extension
		{
			get { return SharedConstants.Agents; }
		}

		#endregion

		private static void WritePreliminaryAnalyzingAgentInformation(XmlReader reader, XmlWriter writer)
		{
			reader.MoveToContent();
			writer.WriteStartElement(SharedConstants.AnalyzingAgents);
			reader.Read();
		}
	}
}