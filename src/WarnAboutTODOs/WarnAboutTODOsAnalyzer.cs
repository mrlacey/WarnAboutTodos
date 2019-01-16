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
        protected static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "TODO",
            "TODO",
            "{0}",
            "Task List",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/mrlacey/WarnAboutTodos");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        protected static List<string> GetTerms(SyntaxTreeAnalysisContext context)
        {
            var terms = new List<string>();

            var additionalFiles = context.Options.AdditionalFiles;
            var termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).ToLowerInvariant().Equals("todo-warn.config"));

            if (termsFile != null)
            {
                var termsFileContents = termsFile.GetText(context.CancellationToken);

                foreach (var term in termsFileContents.Lines)
                {
                    if (!string.IsNullOrWhiteSpace(term.ToString()))
                    {
                        terms.Add(term.ToString());
                    }
                }
            }

            if (!terms.Any())
            {
                terms.Add("TODO");
            }

            return terms;
        }

        protected void ReportIfUsesTerms(string comment, List<string> terms, SyntaxTreeAnalysisContext context, Location location)
        {
            foreach (var term in terms)
            {
                if (comment.ToLowerInvariant().StartsWith(term.ToLowerInvariant()))
                {
                    var displayComment = comment.Substring(term.Length).TrimStart(' ', ':');

                    context.ReportDiagnostic(Diagnostic.Create(Rule, location, displayComment));
                    break;
                }
            }
        }

        internal abstract void HandleSyntaxTree(SyntaxTreeAnalysisContext obj);
    }
}
