using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WarnAboutTODOs
{
    public abstract class WarnAboutTodosAnalyzer : DiagnosticAnalyzer
    {
        protected static DiagnosticDescriptor ErrorRule = new DiagnosticDescriptor(
            "TODO",
            "TODO",
            "{0}",
            "Task List",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/mrlacey/WarnAboutTodos");

        protected static DiagnosticDescriptor WarningRule = new DiagnosticDescriptor(
            "TODO",
            "TODO",
            "{0}",
            "Task List",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/mrlacey/WarnAboutTodos");

        protected static DiagnosticDescriptor InfoRule = new DiagnosticDescriptor(
            "TODO",
            "TODO",
            "{0}",
            "Task List",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/mrlacey/WarnAboutTodos");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ErrorRule, WarningRule, InfoRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        protected static List<Term> GetTerms(SyntaxTreeAnalysisContext context)
        {
            var terms = new List<Term>();

            var additionalFiles = context.Options.AdditionalFiles;
            var termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).ToLowerInvariant().Equals("todo-warn.config"));

            if (termsFile != null)
            {
                var termsFileContents = termsFile.GetText(context.CancellationToken);

                foreach (var line in termsFileContents.Lines)
                {
                    var lineText = line.ToString();

                    if (lineText.StartsWith("[ERROR]", StringComparison.OrdinalIgnoreCase))
                    {
                        terms.Add(new Term { ReportLevel = ReportLevel.Error, Value = lineText.Substring("[ERROR]".Length) });
                    }
                    else if (lineText.StartsWith("[INFO]", StringComparison.OrdinalIgnoreCase))
                    {
                        terms.Add(new Term { ReportLevel = ReportLevel.Info, Value = lineText.Substring("[INFO]".Length) });
                    }
                    else if (lineText.StartsWith("[WARN]", StringComparison.OrdinalIgnoreCase))
                    {
                        terms.Add(new Term { ReportLevel = ReportLevel.Warning, Value = lineText.Substring("[WARN]".Length) });
                    }
                    else if (!string.IsNullOrWhiteSpace(line.ToString()))
                    {
                        terms.Add(new Term { ReportLevel = ReportLevel.Warning, Value = lineText });
                    }
                }
            }

            if (!terms.Any())
            {
                terms.Add(Term.Default);
            }

            return terms;
        }

        protected void ReportIfUsesTerms(string comment, List<Term> terms, SyntaxTreeAnalysisContext context, Location location)
        {
            foreach (var term in terms)
            {
                if (comment.ToLowerInvariant().StartsWith(term.Value.ToLowerInvariant()))
                {
                    var displayComment = comment.Substring(term.Value.Length).TrimStart(' ', ':');

                    switch (term.ReportLevel)
                    {
                        case ReportLevel.Warning:
                            context.ReportDiagnostic(Diagnostic.Create(WarningRule, location, displayComment));
                            break;
                        case ReportLevel.Error:
                            context.ReportDiagnostic(Diagnostic.Create(ErrorRule, location, displayComment));
                            break;
                        case ReportLevel.Info:
                            context.ReportDiagnostic(Diagnostic.Create(InfoRule, location, displayComment));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
            }
        }

        internal abstract void HandleSyntaxTree(SyntaxTreeAnalysisContext obj);
    }
}
