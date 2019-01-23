using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WarnAboutTODOs
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public class VisualBasicAnalyzer : WarnAboutTodosAnalyzer
    {
        private readonly char[] vbTrimChars = new[] { '\'', '*', ' ' };

        internal override void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            try
            {
                var config = GetConfig(context);

                if (config.ExcludesFile(context.Tree.FilePath))
                {
                    return;
                }

                List<Term> terms = config.Terms;

                SyntaxNode root = context.Tree.GetCompilationUnitRoot();

                foreach (var node in root.DescendantTrivia())
                {
                    string comment;

                    switch (node.Kind())
                    {
                        case SyntaxKind.CommentTrivia:

                            comment = node.ToString().TrimStart(vbTrimChars);

                            ReportIfUsesTerms(comment, terms, context, node.GetLocation());

                            break;

                        case SyntaxKind.DocumentationCommentTrivia:

                            comment = node.ToString();

                            foreach (var commentLine in comment.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                ReportIfUsesTerms(commentLine.TrimStart(vbTrimChars), terms, context, node.GetLocation());
                            }

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
