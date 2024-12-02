// <copyright file="UserConfigFile.cs" company="Matt Lacey Ltd.">
// Copyright (c) Matt Lacey Ltd. All rights reserved.
// </copyright>

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WarnAboutTODOs
{
	internal class SolutionConfigFile : AdditionalText
	{
		private SolutionConfigFile(string path)
		{
			this.Path = path;
		}

		public override string Path { get; }

		public static SolutionConfigFile FromFilePath(string configFileName)
		{
			return File.Exists(configFileName) ? new SolutionConfigFile(configFileName) : null;
		}

		public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
		{
			return SourceText.From(File.ReadAllText(this.Path));
		}
	}
}
