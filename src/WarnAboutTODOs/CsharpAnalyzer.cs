using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WarnAboutTODOs
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CsharpAnalyzer : WarnAboutTodosAnalyzer
    {
        private readonly char[] csTrimChars = new[] { '/', '*', ' ' };

        internal override void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            try
            {
                List<Term> terms = null;

                List<Term> CachedTerms(SyntaxTreeAnalysisContext cntxt)
                {
                    return terms ?? (terms = GetTerms(cntxt));
                }

                SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);

                string comment;

                foreach (var node in root.DescendantTrivia())
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.SingleLineCommentTrivia:

                            comment = node.ToString().TrimStart(csTrimChars);

                            ReportIfUsesTerms(comment, CachedTerms(context), context, node.GetLocation());

                            break;

                        case SyntaxKind.SingleLineDocumentationCommentTrivia:
                        case SyntaxKind.MultiLineCommentTrivia:

                            comment = node.ToString();

                            foreach (var commentLine in comment.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                ReportIfUsesTerms(commentLine.TrimStart(csTrimChars), CachedTerms(context), context, node.GetLocation());
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
