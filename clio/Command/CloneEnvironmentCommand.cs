﻿using ATF.Repository.Providers;
using ATF.Repository;
using Clio.Common;
using CommandLine;
using CreatioModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Clio.UserEnvironment;
using DocumentFormat.OpenXml.Drawing;
using k8s.Models;
using YamlDotNet.Serialization;
using Clio.Command.PackageCommand;

namespace Clio.Command
{
	[Verb("show-diff", Aliases = new[] { "diff", "compare" },
		HelpText = "Show difference in settings for two Creatio intances")]
	internal class CloneEnvironmentOptions : ShowDiffEnvironmentsOptions
	{

		[Option("source", Required = true, HelpText = "Source environment name")]
		public string Source { get; internal set; }

		[Option("target", Required = true, HelpText = "Target environment name")]
		public string Target { get; internal set; }

		[Option("file", Required = false, HelpText = "Diff file name")]
		public string FileName { get; internal set; }

		[Option("overwrite", Required = false, HelpText = "Overwrite existing file", Default = true)]
		public bool Overwrite { get; internal set; }

	}

	internal class CloneEnvironmentCommand : BaseDataContextCommand<CloneEnvironmentOptions>
	{
		private readonly ShowDiffEnvironmentsCommand showDiffEnvironmentsCommand;
		private readonly ApplyEnvironmentManifestCommand applyEnvironmentManifestCommand;
		private readonly PullPkgCommand pullPkgCommand;
		private readonly PushPackageCommand pushPackageCommand;
		private readonly IEnvironmentManager environmentManager;

		public CloneEnvironmentCommand(ShowDiffEnvironmentsCommand showDiffEnvironmentsCommand,
			ApplyEnvironmentManifestCommand applyEnvironmentManifestCommand, PullPkgCommand pullPkgCommand, PushPackageCommand pushPackageCommand, IEnvironmentManager environmentManager,ILogger logger,
			IDataProvider provider)
			: base(provider, logger) {
			this.showDiffEnvironmentsCommand = showDiffEnvironmentsCommand;
			this.applyEnvironmentManifestCommand = applyEnvironmentManifestCommand;
			this.pullPkgCommand = pullPkgCommand;
			this.pushPackageCommand = pushPackageCommand;
			this.environmentManager = environmentManager;
		}


		public override int Execute(CloneEnvironmentOptions options) {
			showDiffEnvironmentsCommand.Execute(options);
			
			var diffManifest = environmentManager.LoadEnvironmentManifestFromFile(options.FileName);
			foreach(var package in diffManifest.Packages) {
				var pullPkgOptions = new PullPkgOptions() {
					Environment = options.Source
				};
				pullPkgOptions.Name = package.Name;
				pullPkgCommand.Execute(pullPkgOptions);
			}

			var pushPackageOptions = new PushPkgOptions() {
				Environment = options.Target
			};
			pushPackageCommand.Execute(pushPackageOptions);

			var applyEnvironmentManifestOptions = new ApplyEnvironmentManifestOptions() {
				Environment = options.Target,
				ManifestFilePath = options.FileName
			};
			applyEnvironmentManifestCommand.Execute(applyEnvironmentManifestOptions);
			_logger.WriteInfo("Done");
			return 0;
		}

	}
}