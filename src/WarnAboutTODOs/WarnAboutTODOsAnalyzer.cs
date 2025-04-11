// <copyright file="WarnAboutTODOsAnalyzer.cs" company="Matt Lacey Ltd.">
// Copyright (c) Matt Lacey Ltd. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace WarnAboutTODOs
{
	public abstract class WarnAboutTodosAnalyzer : DiagnosticAnalyzer
	{
		private const string IdError = "TODOE1";
		private const string IdError2 = "TODOE2";
		private const string IdError3 = "TODOE3";
		private const string IdWarning = "TODOW1";
		private const string IdWarning2 = "TODOW2";
		private const string IdWarning3 = "TODOW3";
		private const string IdInfo = "TODOI";
		private const string TitleError = "TODO-ERROR";
		private const string TitleWarning = "TODO-WARN";
		private const string TitleInfo = "TODO-INFO";
		private const string MessageFormat = "{0}";
		private const string Category = "Task List";
		private const string HelpLinkUri = "https://github.com/mrlacey/WarnAboutTodos";

		private static readonly DiagnosticDescriptor ErrorRule = new DiagnosticDescriptor(
			IdError,
			TitleError,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			helpLinkUri: HelpLinkUri);

		private static readonly DiagnosticDescriptor ErrorRule2 = new DiagnosticDescriptor(
			IdError2,
			TitleError,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTags.Build);

		private static readonly DiagnosticDescriptor ErrorRule3 = new DiagnosticDescriptor(
			IdError3,
			TitleError,
			MessageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			helpLinkUri: HelpLinkUri,
            customTags: WellKnownDiagnosticTags.Build);

		private static readonly DiagnosticDescriptor WarningRule = new DiagnosticDescriptor(
			IdWarning,
			TitleWarning,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: HelpLinkUri);

		private static readonly DiagnosticDescriptor WarningRule2 = new DiagnosticDescriptor(
			IdWarning2,
			TitleWarning,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: HelpLinkUri);

		private static readonly DiagnosticDescriptor WarningRule3 = new DiagnosticDescriptor(
			IdWarning3,
			TitleWarning,
			MessageFormat,
			Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: HelpLinkUri);

		private static readonly DiagnosticDescriptor InfoRule = new DiagnosticDescriptor(
			IdInfo,
			TitleInfo,
			MessageFormat,
			Category,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			helpLinkUri: HelpLinkUri);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ErrorRule, WarningRule, InfoRule);

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxTreeAction(this.HandleSyntaxTree);
		}

		internal abstract void HandleSyntaxTree(SyntaxTreeAnalysisContext obj);

		protected WatConfig GetConfig(SyntaxTreeAnalysisContext context)
		{
			var result = new WatConfig();

			(result.Terms, result.Exclusions) = this.GetTermsAndExclusions(context);

			return result;
		}

#pragma warning disable SA1008 // Opening parenthesis must not be preceded by a space
		protected (List<Term>, List<string>) GetTermsAndExclusions(SyntaxTreeAnalysisContext context)
#pragma warning restore SA1008 // Opening parenthesis must not be preceded by a space
		{
			var terms = new List<Term>();
			var exclusions = new List<string>();

			const string configFileName = "todo-warn.config";
			var additionalFiles = context.Options.AdditionalFiles;
			var termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).ToLowerInvariant().Equals(configFileName));

			// If no file in the project, look for one in the same directory as the sln file
			// TODO: See if can cache the results of this to avoid excessive disk IO
			if (termsFile is null)
			{
				var currentFile = context.Tree.FilePath;

				if (currentFile is not null)
				{
					var foundSlnFile = false;

					var dirOfInterest = Path.GetDirectoryName(currentFile);

					while (!foundSlnFile)
					{
						if (Directory.GetFiles(dirOfInterest, "*.sln").Any())
						{
							foundSlnFile = true;
							if (File.Exists(Path.Combine(dirOfInterest, configFileName)))
							{
								termsFile = SolutionConfigFile.FromFilePath(Path.Combine(dirOfInterest, configFileName));
							}
							break;
						}
						else
						{
							dirOfInterest = Path.GetDirectoryName(dirOfInterest);
						}

						// if at root of drive or no more directories to check
						if (Path.GetPathRoot(dirOfInterest) == dirOfInterest
							|| string.IsNullOrWhiteSpace(dirOfInterest)
							|| dirOfInterest.Length <= 3)
						{
							break;
						}
					}
				}
			}

			// If still not found a config file, look in the user's app data folder
			if (termsFile is null)
			{
				termsFile = UserConfigFile.FromApplicationData(configFileName);
			}

			Term CreateTerm(ReportLevel level, string line)
			{
				var result = new Term { ReportLevel = level };

				const string startsGroup = "[STARTS(";
				const string containsGroup = "[CONTAINS(";
				const string notContainsGroup = "[DOESNOTCONTAIN(";
				const string matchesRegexGroup = "[MATCHESREGEX(";
				const string closeGroup = ")]";

				if (line.StartsWith(startsGroup, StringComparison.OrdinalIgnoreCase))
				{
					var closeIndex = line.IndexOf(closeGroup, StringComparison.Ordinal);

					if (closeIndex > 0)
					{
						var startsLen = startsGroup.Length;

						result.StartsWith = line.Substring(startsLen, closeIndex - startsLen);
						line = line.Substring(closeIndex + closeGroup.Length);
					}
				}

				if (line.StartsWith(containsGroup, StringComparison.OrdinalIgnoreCase))
				{
					var closeIndex = line.IndexOf(closeGroup, StringComparison.Ordinal);

					if (closeIndex > 0)
					{
						var containsLen = containsGroup.Length;

						result.Contains = line.Substring(containsLen, closeIndex - containsLen);
						line = line.Substring(closeIndex + closeGroup.Length);
					}
				}

				if (line.StartsWith(notContainsGroup, StringComparison.OrdinalIgnoreCase))
				{
					var closeIndex = line.IndexOf(closeGroup, StringComparison.Ordinal);

					if (closeIndex > 0)
					{
						var containsLen = notContainsGroup.Length;

						result.DoesNotContain = line.Substring(containsLen, closeIndex - containsLen);
						line = line.Substring(closeIndex + closeGroup.Length);
					}
				}

				if (line.StartsWith(matchesRegexGroup, StringComparison.OrdinalIgnoreCase))
				{
					var closeIndex = line.IndexOf(closeGroup, StringComparison.Ordinal);

					if (closeIndex > 0)
					{
						var containsLen = matchesRegexGroup.Length;

						result.MatchesRegex = line.Substring(containsLen, closeIndex - containsLen);
						line = line.Substring(closeIndex + closeGroup.Length);
					}
				}

				if (!string.IsNullOrWhiteSpace(line) &&
					string.IsNullOrWhiteSpace(result.StartsWith) &&
					string.IsNullOrWhiteSpace(result.Contains) &&
					string.IsNullOrWhiteSpace(result.DoesNotContain) &&
					string.IsNullOrWhiteSpace(result.MatchesRegex))
				{
					result.StartsWith = line;
				}

				return result;
			}

			if (termsFile != null)
			{
				var termsFileContents = termsFile.GetText(context.CancellationToken);

				const string errorIndicator = "[ERROR]";
				const string errorIndicator2 = "[ERROR2]";
				const string errorIndicator3 = "[ERROR3]";
				const string infoIndicator = "[INFO]";
				const string warningIndicator = "[WARN]";
				const string warningIndicator2 = "[WARN2]";
				const string warningIndicator3 = "[WARN3]";
				const string exclusionIndicator = "[EXCLUDE]";

				foreach (var line in termsFileContents.Lines)
				{
					var lineText = line.ToString();

					if (lineText.StartsWith(exclusionIndicator, StringComparison.OrdinalIgnoreCase))
					{
						exclusions.Add(lineText.Substring(exclusionIndicator.Length).Trim());
					}
                    //NOTE: The order - ERROR2, ERROR3, ERROR - to properly match all cases and still be performant
					else if (lineText.StartsWith(errorIndicator2, StringComparison.OrdinalIgnoreCase))
					{
						terms.Add(CreateTerm(ReportLevel.Error2, lineText.Substring(errorIndicator2.Length)));
					}
					else if (lineText.StartsWith(errorIndicator3, StringComparison.OrdinalIgnoreCase))
					{
						terms.Add(CreateTerm(ReportLevel.Error3, lineText.Substring(errorIndicator2.Length)));
					}
					else if (lineText.StartsWith(errorIndicator, StringComparison.OrdinalIgnoreCase))
					{
						terms.Add(CreateTerm(ReportLevel.Error, lineText.Substring(errorIndicator.Length)));
					}
					else if (lineText.StartsWith(infoIndicator, StringComparison.OrdinalIgnoreCase))
					{
						terms.Add(CreateTerm(ReportLevel.Info, lineText.Substring(infoIndicator.Length)));
					}
                    //NOTE: The order first we check for WARNING2, WARNING3, WARNING - to properly match all cases and still be performant
					else if (lineText.StartsWith(warningIndicator2, StringComparison.OrdinalIgnoreCase))
					{
						terms.Add(CreateTerm(ReportLevel.Warning2, lineText.Substring(warningIndicator.Length)));
					}
					else if (lineText.StartsWith(warningIndicator3, StringComparison.OrdinalIgnoreCase))
					{
						terms.Add(CreateTerm(ReportLevel.Warning3, lineText.Substring(warningIndicator.Length)));
					}
					else if (lineText.StartsWith(warningIndicator, StringComparison.OrdinalIgnoreCase))
					{
						terms.Add(CreateTerm(ReportLevel.Warning, lineText.Substring(warningIndicator.Length)));
					}
					else if (!string.IsNullOrWhiteSpace(line.ToString()))
					{
						terms.Add(CreateTerm(ReportLevel.Warning, lineText));
					}
				}
			}

			if (!terms.Any())
			{
				terms.Add(Term.Default);
			}

			return (terms, exclusions);
		}

		protected void ReportIfUsesTerms(string comment, List<Term> terms, SyntaxTreeAnalysisContext context, Location location, int startOffset = -1)
		{
			var displayComment = string.Empty;
			var displayOffset = 0;

			foreach (var term in terms)
			{
				var report = false;

				if (!string.IsNullOrWhiteSpace(term.StartsWith))
				{
					if (comment.ToLowerInvariant().StartsWith(term.StartsWith.ToLowerInvariant()))
					{
						displayComment = comment.Substring(term.StartsWith.Length).TrimStart(' ', ':');
						displayOffset = comment.IndexOf(term.StartsWith, StringComparison.OrdinalIgnoreCase);
						report = true;
					}
					else
					{
						continue;
					}
				}

				if (!string.IsNullOrWhiteSpace(term.Contains))
				{
					if (comment.ToLowerInvariant().Contains(term.Contains.ToLowerInvariant()))
					{
						report = true;
					}
					else
					{
						continue;
					}
				}

				if (!string.IsNullOrWhiteSpace(term.DoesNotContain))
				{
					if (!comment.ToLowerInvariant().Contains(term.DoesNotContain.ToLowerInvariant()))
					{
						report = true;
					}
					else
					{
						continue;
					}
				}

				if (!string.IsNullOrWhiteSpace(term.MatchesRegex))
				{
					if (new Regex(term.MatchesRegex).IsMatch(comment))
					{
						report = true;
					}
					else
					{
						continue;
					}
				}

				if (report)
				{
					if (string.IsNullOrWhiteSpace(displayComment))
					{
						displayComment = comment;
					}

					var locationToUse = location;

					if (startOffset >= 0)
					{
						locationToUse = Location.Create(
							location.SourceTree,
							new TextSpan(startOffset + displayOffset, comment.Length - displayOffset));
					}

					switch (term.ReportLevel)
					{
						case ReportLevel.Warning:
							context.ReportDiagnostic(Diagnostic.Create(WarningRule, locationToUse, displayComment));
							break;
						case ReportLevel.Error:
							context.ReportDiagnostic(Diagnostic.Create(ErrorRule, locationToUse, displayComment));
							break;
						case ReportLevel.Info:
							context.ReportDiagnostic(Diagnostic.Create(InfoRule, locationToUse, displayComment));
							break;
						case ReportLevel.Error2:
							context.ReportDiagnostic(Diagnostic.Create(ErrorRule2, locationToUse, displayComment));
							break;
						case ReportLevel.Error3:
							context.ReportDiagnostic(Diagnostic.Create(ErrorRule3, locationToUse, displayComment));
							break;
						case ReportLevel.Warning2:
							context.ReportDiagnostic(Diagnostic.Create(WarningRule2, locationToUse, displayComment));
							break;
						case ReportLevel.Warning3:
							context.ReportDiagnostic(Diagnostic.Create(WarningRule3, locationToUse, displayComment));
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}
	}
}
