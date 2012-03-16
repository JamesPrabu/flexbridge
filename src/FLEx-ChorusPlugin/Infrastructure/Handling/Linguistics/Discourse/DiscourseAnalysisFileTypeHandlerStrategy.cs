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

namespace FLEx_ChorusPlugin.Infrastructure.Handling.Linguistics.Discourse
{
	internal sealed class DiscourseAnalysisFileTypeHandlerStrategy : IFieldWorksFileHandler
	{
		#region Implementation of IFieldWorksFileHandler

		public bool CanValidateFile(string pathToFile)
		{
			return FileUtils.CheckValidPathname(pathToFile, SharedConstants.DiscourseExt) &&
				   Path.GetFileName(pathToFile) == SharedConstants.DiscourseChartFilename;
		}

		public string ValidateFile(string pathToFile)
		{
			try
			{
				var doc = XDocument.Load(pathToFile);
				var root = doc.Root;
				if (root.Name.LocalName != SharedConstants.DiscourseRootFolder
					|| root.Element(SharedConstants.Header) == null
					|| root.Element(SharedConstants.Header).Element("DsDiscourseData") == null
					|| !root.Elements(SharedConstants.DsChart).Any())
				{
					return "Not valid discourse file.";
				}

				var result = CmObjectValidator.ValidateObject(MetadataCache.MdCache, root.Element(SharedConstants.Header).Element("DsDiscourseData"));
				if (result != null)
					return result;

				return root.Elements(SharedConstants.DsChart)
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
			return Xml2WayDiffService.ReportDifferences(repository, parent, child,
				SharedConstants.Header,
				SharedConstants.DsChart, SharedConstants.GuidStr);
		}

		public void Do3WayMerge(MetadataCache mdc, MergeOrder mergeOrder)
		{
			mdc.AddCustomPropInfo(mergeOrder); // NB: Must be done before FieldWorksReversalMergeStrategy is created.

			XmlMergeService.Do3WayMerge(mergeOrder,
				new FieldWorksHeaderedMergeStrategy(mergeOrder.MergeSituation, mdc),
				SharedConstants.Header,
				SharedConstants.DsChart, SharedConstants.GuidStr, WritePreliminaryDiscourseAnalysisInformation);
		}

		public string Extension
		{
			get { return SharedConstants.DiscourseExt; }
		}

		#endregion

		private static void WritePreliminaryDiscourseAnalysisInformation(XmlReader reader, XmlWriter writer)
		{
			reader.MoveToContent();
			writer.WriteStartElement(SharedConstants.DiscourseRootFolder);
			reader.Read();
		}
	}
}