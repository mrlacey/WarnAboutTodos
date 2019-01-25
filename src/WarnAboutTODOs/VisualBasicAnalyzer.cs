// <copyright file="VisualBasicAnalyzer.cs" company="Matt Lacey Ltd.">
// Copyright (c) Matt Lacey Ltd. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;

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
                var config = this.GetConfig(context);

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

                            comment = node.ToString().TrimStart(this.vbTrimChars);

                            this.ReportIfUsesTerms(comment, terms, context, node.GetLocation());

                            break;

                        case SyntaxKind.DocumentationCommentTrivia:

                            comment = node.ToString();

                            var baseLocation = node.GetLocation();

                            var offset = node.SpanStart;

                            foreach (var commentLine in comment.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var trimmedLine = commentLine.TrimStart(this.vbTrimChars);

                                var trimLength = commentLine.Length - trimmedLine.Length;

                                this.ReportIfUsesTerms(trimmedLine, terms, context, baseLocation, offset + trimLength);

                                offset = offset + commentLine.Length + Environment.NewLine.Length;
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
