﻿using System.Xml;
using FLEx_ChorusPlugin.Properties;

namespace FLEx_ChorusPlugin.Infrastructure.Handling.Anthropology
{
	/// <summary>
	/// Context generator for RnGenericRec elements. These are a root element, so we generate a label directly,
	/// without needing to look further up the chain. This also handles about 9 different StText possibilities.
	/// </summary>
	internal sealed class RnGenericRecContextGenerator : FieldWorkObjectContextGenerator
	{
		private const string Space = " ";

		protected override string GetLabel(XmlNode start)
		{
			return GetLabelForRnGenericRec(start);
		}

		string EntryLabel
		{
			get { return Resources.kRnGenericRecLabel; }
		}

		private string GetLabelForRnGenericRec(XmlNode text)
		{
			var form = text.SelectSingleNode("Title/Str");
			return form == null
				? EntryLabel
				: EntryLabel + Space + form.InnerText;
		}
	}
}
