// <copyright file="WarnAboutTODOsAnalyzer.cs" company="Matt Lacey Ltd.">
// Copyright (c) Matt Lacey Ltd. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace WarnAboutTODOs
{
    public abstract class WarnAboutTodosAnalyzer : DiagnosticAnalyzer
    {
        private const string Id = "TODO";
        private const string Title = "TODO";
        private const string MessageFormat = "{0}";
        private const string Category = "Task List";
        private const string HelpLinkUri = "https://github.com/mrlacey/WarnAboutTodos";

        private static readonly DiagnosticDescriptor ErrorRule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: HelpLinkUri);

        private static readonly DiagnosticDescriptor WarningRule = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLinkUri);

        private static readonly DiagnosticDescriptor InfoRule = new DiagnosticDescriptor(
            Id,
            Title,
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
            var termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).ToLowerInvariant().Equals(configFileName))
                            ?? UserConfigFile.FromApplicationData(configFileName);

            Term CreateTerm(ReportLevel level, string line)
            {
                var result = new Term { ReportLevel = level };

                const string startsGroup = "[STARTS(";
                const string containsGroup = "[CONTAINS(";
                const string notContainsGroup = "[DOESNOTCONTAIN(";
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

                if (!string.IsNullOrWhiteSpace(line) &&
                    string.IsNullOrWhiteSpace(result.StartsWith) &&
                    string.IsNullOrWhiteSpace(result.Contains) &&
                    string.IsNullOrWhiteSpace(result.DoesNotContain))
                {
                    result.StartsWith = line;
                }

                return result;
            }

            if (termsFile != null)
            {
                var termsFileContents = termsFile.GetText(context.CancellationToken);

                const string errorIndicator = "[ERROR]";
                const string infoIndicator = "[INFO]";
                const string warningIndicator = "[WARN]";
                const string exclusionIndicator = "[EXCLUDE]";

                foreach (var line in termsFileContents.Lines)
                {
                    var lineText = line.ToString();

                    if (lineText.StartsWith(exclusionIndicator, StringComparison.OrdinalIgnoreCase))
                    {
                        exclusions.Add(lineText.Substring(exclusionIndicator.Length).Trim());
                    }
                    else if (lineText.StartsWith(errorIndicator, StringComparison.OrdinalIgnoreCase))
                    {
                        terms.Add(CreateTerm(ReportLevel.Error, lineText.Substring(errorIndicator.Length)));
                    }
                    else if (lineText.StartsWith(infoIndicator, StringComparison.OrdinalIgnoreCase))
                    {
                        terms.Add(CreateTerm(ReportLevel.Info, lineText.Substring(infoIndicator.Length)));
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
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
