﻿// --------------------------------------------------------------------------------------------
// Copyright (C) 2010-2013 SIL International. All rights reserved.
//
// Distributable under the terms of the MIT License, as specified in the license.rtf file.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Chorus.VcsDrivers.Mercurial;
using Palaso.IO;
using Palaso.Progress;
using Palaso.Xml;
using SIL.LiftBridge.Infrastructure;
using SIL.LiftBridge.Services;
using TriboroughBridge_ChorusPlugin;
using TriboroughBridge_ChorusPlugin.Infrastructure;
using TriboroughBridge_ChorusPlugin.Properties;
using LibTriboroughBridgeChorusPlugin;
using LibTriboroughBridgeChorusPlugin.Infrastructure;

namespace SIL.LiftBridge.Infrastructure.ActionHandlers
{
	/// <summary>
	/// This IObtainProjectStrategy implementation handles the Lift type of repo that the user selected in a generic 'obtain' call.
	/// </summary>
	[Export(typeof(IObtainProjectStrategy))]
	public class ObtainProjectStrategyLift : IObtainProjectStrategy
	{
		[Import]
		private ICreateProjectFromLift _liftprojectCreator;
		private string _liftFolder;

		#region Other methods

		private static float GetLiftVersionNumber(string repoLocation)
		{
			// Return 0.13 if there is no lift file or it has no 'version' attr on the main 'lift' element.
			var firstLiftFile = FileAndDirectoryServices.GetPathToFirstLiftFile(repoLocation);
			if (firstLiftFile == null)
				return float.MaxValue;

			using (var reader = XmlReader.Create(firstLiftFile, CanonicalXmlSettings.CreateXmlReaderSettings()))
			{
				reader.MoveToContent();
				reader.MoveToAttribute("version");
				return float.Parse(reader.Value);
			}
		}

		internal static void UpdateToTheCorrectBranchHeadIfPossible(string cloneLocation,
			string desiredBranchName, ActualCloneResult cloneResult)
		{
			if (!new UpdateBranchHelperLift().UpdateToTheCorrectBranchHeadIfPossible(
				desiredBranchName, cloneResult, cloneLocation))
					{
				cloneResult.Message = CommonResources.kFlexUpdateRequired;
			}
		}

		private string RemoveAppendedLiftIfNeeded(string cloneLocation)
		{
			cloneLocation = cloneLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (!cloneLocation.EndsWith("_LIFT"))
				return cloneLocation;

			var cloneLocationSansSuffix = cloneLocation.Substring(0, cloneLocation.LastIndexOf("_LIFT", StringComparison.InvariantCulture));
			var possiblyAdjustedCloneLocation = DirectoryUtilities.GetUniqueFolderPath(cloneLocationSansSuffix);
			DirectoryUtilities.MoveDirectorySafely(cloneLocation, possiblyAdjustedCloneLocation);
			return possiblyAdjustedCloneLocation;
		}

		internal static void MakeLocalClone(string sourceFolder, string targetFolder)
		{
			var parentFolder = Directory.GetParent(targetFolder).FullName;
			if (!Directory.Exists(parentFolder))
				Directory.CreateDirectory(parentFolder);

			// Do a clone of the lift repo into the new home.
			var oldRepo = new HgRepository(sourceFolder, new NullProgress());
			oldRepo.CloneLocalWithoutUpdate(targetFolder);

			// Now copy the original hgrc file into the new location.
			File.Copy(Path.Combine(sourceFolder, TriboroughBridge_ChorusPlugin.Utilities.hg, "hgrc"),
				Path.Combine(targetFolder, TriboroughBridge_ChorusPlugin.Utilities.hg, "hgrc"), true);

			// Move the import failure notification file, if it exists.
			var roadblock = Path.Combine(sourceFolder, LiftUtilties.FailureFilename);
			if (File.Exists(roadblock))
				File.Copy(roadblock, Path.Combine(targetFolder, LiftUtilties.FailureFilename), true);
		}

		#endregion Other methods

		#region IObtainProjectStrategy impl

		public bool ProjectFilter(string repositoryLocation)
		{
			var hgDataFolder = TriboroughBridge_ChorusPlugin.Utilities.HgDataFolder(repositoryLocation);
			return Directory.Exists(hgDataFolder) && Directory.GetFiles(hgDataFolder, "*.lift.i").Any();
		}

		public string HubQuery { get { return "*.lift"; } }

		public bool IsRepositoryEmpty(string repositoryLocation)
		{
			return !Directory.GetFiles(repositoryLocation, "*" + LiftUtilties.LiftExtension).Any();
		}

		public void FinishCloning(Dictionary<string, string> commandLineArgs, string cloneLocation, string expectedPathToClonedRepository)
		{
			// "obtain"
			//		'cloneLocation' will be a new folder at the $fwroot main project location, such as $fwroot\foo.
			//		Move the lift repo down into $fwroot\foo\OtherRepositories\foo_LIFT folder

			// Check for Lift version compatibility.
			cloneLocation = RemoveAppendedLiftIfNeeded(cloneLocation);
			var otherReposDir = Path.Combine(cloneLocation, SharedConstants.OtherRepositories);
			if (!Directory.Exists(otherReposDir))
			{
				Directory.CreateDirectory(otherReposDir);
			}
			_liftFolder = TriboroughBridge_ChorusPlugin.Utilities.LiftOffset(cloneLocation);

			var actualCloneResult = new ActualCloneResult();

			// Move the repo from its temp home in cloneLocation into new home.
			// The original location, may not be on the same device, so it may be a copy+delete, rather than a formal move.
			// At the end of the day, cloneLocation and its parent temp folder need to be deleted. MakeLocalCloneAndRemoveSourceParentFolder aims to do all of it.
			MakeLocalClone(cloneLocation, _liftFolder);
			actualCloneResult.ActualCloneFolder = _liftFolder;
			actualCloneResult.FinalCloneResult = FinalCloneResult.Cloned;

			// Update to the head of the desired branch, if possible.
			UpdateToTheCorrectBranchHeadIfPossible(_liftFolder,
				"LIFT" + commandLineArgs["-liftmodel"], actualCloneResult);

			switch (actualCloneResult.FinalCloneResult)
			{
				case FinalCloneResult.ExistingCloneTargetFolder:
					MessageBox.Show(CommonResources.kFlexProjectExists, CommonResources.kObtainProject, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					Directory.Delete(cloneLocation, true);
					_liftFolder = null;
					break;
				case FinalCloneResult.FlexVersionIsTooOld:
					MessageBox.Show(CommonResources.kFlexUpdateRequired, CommonResources.kObtainProject, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					Directory.Delete(cloneLocation, true);
					_liftFolder = null;
					break;
			}

			// Delete all old repo folders and files from 'cloneLocation'.
			foreach (var dir in Directory.GetDirectories(cloneLocation).Where(directory => !directory.Contains(SharedConstants.OtherRepositories)))
			{
				Directory.Delete(dir, true);
			}
			foreach (var file in Directory.GetFiles(cloneLocation))
			{
				File.Delete(file);
			}
		}

		public void TellFlexAboutIt()
		{
			_liftprojectCreator.CreateProjectFromLift(FileAndDirectoryServices.GetPathToFirstLiftFile(_liftFolder)); // PathToFirstLiftFile may be null, which is fine.
			//Caller does it. _connectionHelper.SignalBridgeWorkComplete(false);
		}

		public ActionType SupportedActionType
		{
			get { return ActionType.ObtainLift; }
		}

		#endregion
	}
}
