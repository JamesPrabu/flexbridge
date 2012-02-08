﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Chorus.VcsDrivers.Mercurial;
using FieldWorksBridge.Properties;
using FLEx_ChorusPlugin.Controller;
using FLEx_ChorusPlugin.Properties;
using FLEx_ChorusPlugin.View;
using Palaso.Reporting;

namespace FieldWorksBridge
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			ExceptionHandler.Init();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Is mercurial set up?
			var s = HgRepository.GetEnvironmentReadinessMessage("en");
			if (!string.IsNullOrEmpty(s))
			{
				MessageBox.Show(s, Resources.kFieldWorksBridge, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			var options = ParseCommandLineArgs(args);
			options["-u"] = "King of France";
			options["-p"] = "C:/FW-WW/DistFiles/Projects/benice/benice.fwdata";
			options["-v"] = "start";
			using (var controller = new FwBridgeController(options))
			{
				if(!options.ContainsKey("-v"))
				{
					Application.Run(controller.MainForm);
				}
				else if(options["-v"] != null)
				{
					switch (options["-v"])
					{
						case "obtain":
						case "start":
						case "send_receive":
							var syncProj = new SynchronizeProject();
							syncProj.SynchronizeFieldWorksProject(controller);
							break;
						case "view_notes": //view the conflict\notes report
							break;
						default:
							//display options dialog
							break;
					}
				}
			}

			Settings.Default.Save();
		}

		static Dictionary<string, string> ParseCommandLineArgs(string[] args)
		{
			Dictionary<string, string> options = new Dictionary<string, string>();
			if(args != null && args.Length > 0)
			{
				string currentKey = null;
				foreach (string arg in args)
				{
					//not all options are followed by input, so just add them as a key
					if(arg.StartsWith("-") || arg.StartsWith("/"))
					{
						currentKey = arg;
						options[currentKey] = null;
					}
					else //this is input which apparently follows an option, added it as the value in the dictionary
					{
						if(currentKey != null && options[currentKey] == null)
						{
							//this option goes with the flag that came before it
							options[currentKey] = arg;
						}
						else //there was no flag before this option.
						{
							//This is an unparsable command line
							Console.WriteLine("Invalid command line options. Please launch from FLEx or run the executable without arguments.");
							//Signal FLEx or other apps
							throw new ApplicationException("Invalid command line options.");
						}
					}
				}
			}
			return options;
		}
	}
}
