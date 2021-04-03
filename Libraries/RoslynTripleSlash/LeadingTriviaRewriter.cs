using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.RoslynTripleSlash
{
    public static class LeadingTriviaRewriter
    {
        private static int[] TriviaAboveDocComments = new[]
        {
            (int)SyntaxKind.RegionDirectiveTrivia,
            (int)SyntaxKind.PragmaWarningDirectiveTrivia,
            (int)SyntaxKind.IfDirectiveTrivia,
            (int)SyntaxKind.EndIfDirectiveTrivia,
        };

        public static int[] TriviaBelowDocComments = new[]
        {
            (int)SyntaxKind.SingleLineCommentTrivia,
            (int)SyntaxKind.MultiLineCommentTrivia
        };

        private static bool IsDocumentationCommentTrivia(this SyntaxTrivia trivia) =>
            trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);

        private static bool IsDocumentationCommentTriviaContinuation(this SyntaxTrivia trivia) =>
            trivia.IsDocumentationCommentTrivia() ||
            trivia.IsKind(SyntaxKind.EndOfLineTrivia) ||
            trivia.IsKind(SyntaxKind.WhitespaceTrivia);

        public static SyntaxTriviaList WithoutDocumentationComments(this SyntaxTriviaList trivia)
        {
            return trivia.WithoutDocumentationComments(out int? _);
        }

        public static SyntaxTriviaList WithoutDocumentationComments(this SyntaxTriviaList trivia, out int? existingDocsPosition)
        {
            int i = 0;
            existingDocsPosition = null;

            while (i < trivia.Count)
            {
                if (trivia[i].IsDocumentationCommentTrivia())
                {
                    while (i < trivia.Count && trivia[i].IsDocumentationCommentTriviaContinuation())
                    {
                        trivia = trivia.RemoveAt(i);
                    }

                    existingDocsPosition = i;
                }

                i++;
            }

            return trivia;
        }

        public static SyntaxNode ApplyXmlComments(SyntaxNode node, IEnumerable<SyntaxTrivia> xmlComments)
        {
            if (!node.HasLeadingTrivia)
            {
                return node.WithLeadingTrivia(GetXmlCommentLines(xmlComments));
            }

            SyntaxTriviaList leading = node.GetLeadingTrivia().WithoutDocumentationComments(out int? docsPosition);

            if (docsPosition is null)
            {
                // We will determine the position at which to insert the XML
                // comments. We want to find the spot closest to the declaration
                // that makes sense, so we walk upward through the leading trivia
                // until we find nodes we need to stay beneath. Then, we walk back
                // downward until we find the first node to stay above.
                docsPosition = leading.Count;

                while (docsPosition > 0 && !TriviaAboveDocComments.Contains(leading[docsPosition.Value - 1].RawKind))
                {
                    docsPosition--;
                }

                while (docsPosition < leading.Count && !TriviaBelowDocComments.Contains(leading[docsPosition.Value].RawKind))
                {
                    docsPosition++;
                }
            }

            // Given the intended position of the docs, we now walk backwards through any whitespace
            // We will insert at the beginning of the line, but match the whitespace for indentation
            SyntaxTriviaList indentation = new();

            while (docsPosition > 0 && leading[docsPosition.Value - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                docsPosition--;
                indentation = indentation.Insert(0, leading[docsPosition.Value]);
            }

            // Insert the XML comment lines at the docs position, indenting each line to match
            return node.WithLeadingTrivia(
                leading.InsertRange(docsPosition.Value, GetXmlCommentLines(xmlComments, indentation))
            );
        }

        public static SyntaxTriviaList GetXmlCommentLines(IEnumerable<SyntaxTrivia> xmlComments, SyntaxTriviaList indentation = new())
        {
            SyntaxTriviaList xmlTrivia = new();

            foreach (var xmlComment in xmlComments)
            {
                xmlTrivia = xmlTrivia
                    .AddRange(indentation)
                    .Add(xmlComment)
                    .Add(SyntaxFactory.CarriageReturnLineFeed);
            }

            return xmlTrivia;
        }
    }
}
