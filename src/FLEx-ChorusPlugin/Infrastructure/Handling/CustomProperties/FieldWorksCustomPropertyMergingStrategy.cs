using System.Collections.Generic;
using System.Xml;
using Chorus.merge;
using Chorus.merge.xml.generic;

namespace FLEx_ChorusPlugin.Infrastructure.Handling.CustomProperties
{
	/// <summary>
	/// A merge strategy for FieldWorks 7.0 custom properties data.
	/// </summary>
	internal sealed class FieldWorksCustomPropertyMergingStrategy : IMergeStrategy
	{
		private const string CustomField = "CustomField";
		private readonly XmlMerger _merger;

		/// <summary>
		/// Constructor.
		/// </summary>
		internal FieldWorksCustomPropertyMergingStrategy(MergeSituation mergeSituation)
		{
			_merger = new XmlMerger(mergeSituation);

			// Custom property declaration.
			var strategy = new ElementStrategy(false)
							{
								MergePartnerFinder = new FindByMultipleKeyAttributes(new List<string> { SharedConstants.Name, SharedConstants.Class }),
								ContextDescriptorGenerator = new FieldWorksCustomPropertyContextGenerator()
							};
			_merger.MergeStrategies.SetStrategy(CustomField, strategy);
		}

		#region Implementation of IMergeStrategy

		/// <summary>
		/// Do a 3-way merge of two CustomField XmlNodes ('CustomField' element nodes).
		/// </summary>
		/// <returns>XML of the merged object.</returns>
		public string MakeMergedEntry(IMergeEventListener eventListener, XmlNode ourEntry, XmlNode theirEntry, XmlNode commonEntry)
		{
			return ourEntry == null
					? theirEntry.OuterXml
					: (theirEntry == null
						? ourEntry.OuterXml
						: _merger.Merge(eventListener, ourEntry, theirEntry, commonEntry).OuterXml);
		}

		#endregion
	}
}