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
                List<Term> terms = null;

                List<Term> CachedTerms(SyntaxTreeAnalysisContext cntxt)
                {
                    return terms ?? (terms = GetTerms(cntxt));
                }

                SyntaxNode root = context.Tree.GetCompilationUnitRoot();

                string comment;

                foreach (var node in root.DescendantTrivia())
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.CommentTrivia:

                            comment = node.ToString().TrimStart(vbTrimChars);

                            ReportIfUsesTerms(comment, CachedTerms(context), context, node.GetLocation());

                            break;

                        case SyntaxKind.DocumentationCommentTrivia:

                            comment = node.ToString();

                            foreach (var commentLine in comment.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                ReportIfUsesTerms(commentLine.TrimStart(vbTrimChars), CachedTerms(context), context, node.GetLocation());
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
