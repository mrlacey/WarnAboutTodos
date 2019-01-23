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
                var config = GetConfig(context);

                if (config.ExcludesFile(context.Tree.FilePath))
                {
                    return;
                }

                List<Term> terms = config.Terms;

                SyntaxNode root = context.Tree.GetCompilationUnitRoot(context.CancellationToken);

                foreach (var node in root.DescendantTrivia())
                {
                    string comment;

                    switch (node.Kind())
                    {
                        case SyntaxKind.SingleLineCommentTrivia:

                            comment = node.ToString().TrimStart(csTrimChars);

                            ReportIfUsesTerms(comment, terms, context, node.GetLocation());

                            break;

                        case SyntaxKind.SingleLineDocumentationCommentTrivia:
                        case SyntaxKind.MultiLineCommentTrivia:

                            comment = node.ToString();

                            foreach (var commentLine in comment.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                ReportIfUsesTerms(commentLine.TrimStart(csTrimChars), terms, context, node.GetLocation());
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
