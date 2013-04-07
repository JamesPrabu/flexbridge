﻿using System;
using System.IO;
using System.Linq;
using SIL.LiftBridge.Infrastructure;
using TriboroughBridge_ChorusPlugin;

namespace SIL.LiftBridge.Model
{
	/// <summary>
	/// Class that represents a Lift project.
	/// </summary>
	internal class LiftProject
	{
		internal LiftProject(string basePath)
		{
			BasePath = basePath; // fwroot/foo
		}

		internal string LiftPathname
		{
			get { return PathToFirstLiftFile(this); }
		}

		/// <summary>
		/// NOTE: BasePath is the main FLEx project folder, not the lift folder.
		/// </summary>
		private string BasePath { get; set; }

		internal string PathToProject
		{
			get
			{
				var flexProjName = Path.GetFileName(BasePath);
				var otherPath = Path.Combine(BasePath, Utilities.OtherRepositories);
				if (Directory.Exists(otherPath))
				{
					var extantLiftFolder = Directory.GetDirectories(otherPath).FirstOrDefault(subfolder => subfolder.EndsWith("_LIFT"));
					if (extantLiftFolder != null)
						return extantLiftFolder;
				}
				return Path.Combine(BasePath, Utilities.OtherRepositories, flexProjName + '_' + Utilities.LIFT);
			}
		}

		internal string ProjectName
		{
			get { return Path.GetFileNameWithoutExtension(PathToFirstFwFile(BasePath)); }
		}

		private static string PathToFirstFwFile(string basePath)
		{
			var fwFiles = Directory.GetFiles(basePath, "*" + Utilities.FwXmlExtension).ToList();
			if (fwFiles.Count == 0)
				fwFiles = Directory.GetFiles(basePath, "*" + Utilities.FwDb4oExtension).ToList();
			return fwFiles.Count == 0 ? null : (from file in fwFiles
												  where HasOnlyOneDot(file)
												  select file).FirstOrDefault();
		}

		private static string PathToFirstLiftFile(LiftProject project)
		{
			var liftFiles = Directory.GetFiles(project.PathToProject, "*" + LiftUtilties.LiftExtension).ToList();
			return liftFiles.Count == 0 ? null : (from file in liftFiles
												  where HasOnlyOneDot(file)
												  select file).FirstOrDefault();
		}

		private static bool HasOnlyOneDot(string pathname)
		{
			var filename = Path.GetFileName(pathname);
			return filename.IndexOf(".", StringComparison.InvariantCulture) == filename.LastIndexOf(".", StringComparison.InvariantCulture);
		}
	}
}
