using System;
using System.IO;
using System.Xml;
using Chorus.merge.xml.generic;
using Chorus.sync;
using Chorus.VcsDrivers;
using Chorus.VcsDrivers.Mercurial;
using FLEx_ChorusPluginTests.Properties;
using NUnit.Framework;
using Palaso.IO;
using Palaso.Progress.LogBox;
using Palaso.TestUtilities;

namespace FLEx_ChorusPluginTests.BorrowedCode
{
	/// <summary>
	/// Provides temporary directories,files, and repositories.  Provides operations on them, to simulate a user.
	/// </summary>
	/// <remarks>
	/// Any test doing high-level testing such that this is useful should expressly Not be interested in details of the files,
	/// so no methods are provided to control the contents of the files.
	/// </remarks>
	internal class RepositoryWithFilesSetup :IDisposable
	{
		internal ProjectFolderConfiguration ProjectConfiguration;
		private readonly StringBuilderProgress _stringProgress = new StringBuilderProgress();
		internal IProgress Progress;
		internal TemporaryFolder RootFolder;
		internal TemporaryFolder ProjectFolder;
		internal TempFile UserFile;
		internal Synchronizer Synchronizer;
		internal RepositoryAddress RepoPath;
		private HgRepository _repository;

		internal static RepositoryWithFilesSetup CreateWithLiftFile(string userName)
		{
		   const string entriesXml = @"<entry id='one' guid='F169EB3D-16F2-4eb0-91AA-FDB91636F8F6'>
						<lexical-unit>
							<form lang='a'>
								<text>original</text>
							</form>
						</lexical-unit>
					 </entry>";
		   string liftContents = string.Format("<?xml version='1.0' encoding='utf-8'?><lift version='{0}'>{1}</lift>", "0.00", entriesXml);
			return new RepositoryWithFilesSetup(userName, "test.lift", liftContents);
		}

		internal string ProgressString
		{
			get { return _stringProgress.Text; }
		}

		internal HgRepository Repository
		{
			get { return _repository; }
		}

		internal RepositoryWithFilesSetup(string userName, string fileName, string fileContents)
		{
			Progress = new MultiProgress(new IProgress[] { new ConsoleProgress(), _stringProgress });
			RootFolder = new TemporaryFolder("ChorusTest-" + userName);
			ProjectFolder = new TemporaryFolder(RootFolder, "foo project");
			Console.WriteLine(TestResources.kTestRepoCreated, RootFolder.Path);
			var p = ProjectFolder.Combine(fileName);
			File.WriteAllText(p, fileContents);
			UserFile = TempFile.TrackExisting(p);

			RepositorySetup.MakeRepositoryForTest(ProjectFolder.Path, userName,Progress);
			Init(userName);
			var options = new SyncOptions
							{
								DoPullFromOthers = false,
								DoMergeWithOthers = false,
								DoSendToOthers = false
							};
			Synchronizer.SyncNow(options);
		}

		internal static RepositoryWithFilesSetup CreateByCloning(string userName, RepositoryWithFilesSetup cloneFromUser)
		{
			return new RepositoryWithFilesSetup(userName,cloneFromUser);
		}

		private RepositoryWithFilesSetup(string userName, RepositoryWithFilesSetup cloneFromUser)
		{
			Progress= new MultiProgress(new IProgress[] { new ConsoleProgress(), _stringProgress });
			RootFolder = new TemporaryFolder("ChorusTest-" + userName);
			Console.WriteLine(TestResources.kTestRepoCloned, RootFolder.Path);
			var pathToProject = RootFolder.Combine(Path.GetFileName(cloneFromUser.ProjectFolder.Path));
			//cloneFromUser.Synchronizer.MakeClone(pathToProject, true);
			HgHighLevel.MakeCloneFromLocalToLocal(cloneFromUser.Repository.PathToRepo, pathToProject, true, Progress);

			ProjectFolder = TemporaryFolder.TrackExisting(RootFolder.Combine("foo project"));
			var pathToOurLiftFile = ProjectFolder.Combine(Path.GetFileName(cloneFromUser.UserFile.Path));
			UserFile = TempFile.TrackExisting(pathToOurLiftFile);

			Init(userName);
		}

		private void Init(string userName)
		{
			ProjectConfiguration = new ProjectFolderConfiguration(ProjectFolder.Path);
			ProjectConfiguration.IncludePatterns.Add(UserFile.Path);
			ProjectConfiguration.FolderPath = ProjectFolder.Path;
			_repository = new HgRepository(ProjectFolder.Path,Progress);

			RepoPath = RepositoryAddress.Create(userName, ProjectFolder.Path, false);
			Synchronizer = Synchronizer.FromProjectConfiguration(ProjectConfiguration, Progress);
			Synchronizer.Repository.SetUserNameInIni(userName,Progress);
		}

		public void Dispose()
		{
			if (Repository != null)
			{
				Assert.IsFalse(Repository.GetHasLocks(), "A lock was left over, after the test.");
			}

			if (DoNotDispose)
			{
				Console.WriteLine(TestResources.kTestRepoNotDeleted, RootFolder.Path);
			}
			else
			{
				Console.WriteLine(TestResources.kTestRepoDeleted, RootFolder.Path);
				UserFile.Dispose();
				ProjectFolder.Dispose();
				RootFolder.Dispose();
			}
		}

		internal bool DoNotDispose { get; set; }

		internal void ReplaceSomething(string replacement)
		{
			File.WriteAllText(UserFile.Path, File.ReadAllText(UserFile.Path).Replace("original", replacement));
		}

		internal void WriteNewContentsToTestFile(string replacement)
		{
			File.WriteAllText(UserFile.Path, replacement);
		}

		internal SyncResults CheckinAndPullAndMerge(RepositoryWithFilesSetup syncWithUser)
		{
			var options = new SyncOptions
							{
								DoPullFromOthers = true,
								DoMergeWithOthers = true,
								DoSendToOthers = false
							};

			options.RepositorySourcesToTry.Add(syncWithUser.RepoPath);
			return Synchronizer.SyncNow(options);
		}

		internal void AddAndCheckIn()
		{
			var options = new SyncOptions
							{
								DoPullFromOthers = false,
								DoMergeWithOthers = false,
								DoSendToOthers = false
							};

			Synchronizer.SyncNow(options);
		}

		internal void WriteIniContents(string s)
		{
			var p = Path.Combine(Path.Combine(ProjectConfiguration.FolderPath, ".hg"), "hgrc");
			File.WriteAllText(p, s);
		}

		internal void EnsureNoHgrcExists()
		{
			var p = Path.Combine(Path.Combine(ProjectConfiguration.FolderPath, ".hg"), "hgrc");
			if(File.Exists(p))
				File.Delete(p);
		}

		internal void AssertSingleHead()
		{
			var actual = Synchronizer.Repository.GetHeads().Count;
			Assert.AreEqual(1, actual, "There should be on only one head, but there are "+actual);
		}

		internal void AssertHeadCount(int count)
		{
			var actual = Synchronizer.Repository.GetHeads().Count;
			Assert.AreEqual(count, actual, "Wrong number of heads");
		}

		internal void AssertSingleConflictType<TConflict>()
		{
			var cmlFile = ChorusNotesMergeEventListener.GetChorusNotesFilePath(UserFile.Path);
			Assert.IsTrue(File.Exists(cmlFile), "ChorusNotes file should have been in working set");
			Assert.IsTrue(Synchronizer.Repository.GetFileIsInRepositoryFromFullPath(cmlFile), "ChorusNotes file should have been in repository");

			var doc = new XmlDocument();
			doc.Load(cmlFile);
			Assert.AreEqual(1, doc.SafeSelectNodes("notes/annotation").Count);

		}

		internal HgRepository GetRepository()
		{
			return Synchronizer.Repository;
		}

		internal void AssertNoErrorsReported()
		{
			Assert.IsFalse(ProgressString.ToLower().Contains("error"));
		}

		internal void AssertFileExists(string relativePath)
		{
			Assert.IsTrue(File.Exists(ProjectFolder.Combine(relativePath)));
		}

		internal void AssertFileContents(string relativePath, string expectedContents)
		{
			Assert.AreEqual(expectedContents,File.ReadAllText(ProjectFolder.Combine(relativePath)));
		}

		/// <summary>
		/// Obviously, don't leave this in a unit test... it's only for debugging
		/// </summary>
		internal void ShowInTortoise()
		{
			var start = new System.Diagnostics.ProcessStartInfo("hgtk", "log")
							{
								WorkingDirectory = ProjectFolder.Path
							};
			System.Diagnostics.Process.Start(start);
		}
	}
}