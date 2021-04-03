﻿using Microsoft.CodeAnalysis;
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
        private static int[] UpperBoundaries = new[]
        {
            (int)SyntaxKind.RegionDirectiveTrivia,
            (int)SyntaxKind.PragmaWarningDirectiveTrivia,
            (int)SyntaxKind.IfDirectiveTrivia,
            (int)SyntaxKind.EndIfDirectiveTrivia,
        };

        public static int[] LowerBoundaries = new[]
        {
            (int)SyntaxKind.SingleLineCommentTrivia
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
            return trivia.WithoutDocumentationComments(out bool _);
        }

        public static SyntaxTriviaList WithoutDocumentationComments(this SyntaxTriviaList trivia, out bool removed)
        {
            int i = 0;
            removed = false;

            while (i < trivia.Count)
            {
                if (trivia[i].IsDocumentationCommentTrivia())
                {
                    while (i < trivia.Count && trivia[i].IsDocumentationCommentTriviaContinuation())
                    {
                        trivia = trivia.RemoveAt(i);
                    }

                    removed = true;
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

            SyntaxTriviaList leading = node.GetLeadingTrivia().WithoutDocumentationComments();

            // We will determine the position at which to insert the XML
            // comments. We want to find the spot closest to the declaration
            // that makes sense, so we walk upward through the leading trivia
            // until we find nodes we need to stay beneath. Then, we walk back
            // downward until we find the first node to stay above.
            int position = leading.Count;

            while (position > 0 && !UpperBoundaries.Contains(leading[position - 1].RawKind))
            {
                position--;
            }

            while (position < leading.Count - 1 && !LowerBoundaries.Contains(leading[position].RawKind))
            {
                position++;
            }

            // Now we know the trivia we need to remain above. Walk backward through any whitespace;
            while (position > 0 && leading[position - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                position--;
            }

            // Now we will construct the XML trivia. If the position we'll use is whitespace,
            // then we use that to indent the XML comments too.
            SyntaxTriviaList xmlTrivia;

            if (leading[position].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                xmlTrivia = GetXmlCommentLines(xmlComments, leading[position]);
            }
            else
            {
                xmlTrivia = GetXmlCommentLines(xmlComments);
            }

            leading = leading.InsertRange(position, xmlTrivia);

            return node.WithLeadingTrivia(leading);
        }

        public static SyntaxTriviaList GetXmlCommentLines(IEnumerable<SyntaxTrivia> xmlComments)
        {
            SyntaxTriviaList xmlTrivia = new();

            foreach (var xmlComment in xmlComments)
            {
                xmlTrivia = xmlTrivia.AddRange(new[] { xmlComment, SyntaxFactory.CarriageReturnLineFeed });
            }

            return xmlTrivia;
        }

        public static SyntaxTriviaList GetXmlCommentLines(IEnumerable<SyntaxTrivia> xmlComments, SyntaxTrivia indentation)
        {
            SyntaxTriviaList xmlTrivia = new();

            foreach (var xmlComment in xmlComments)
            {
                xmlTrivia = xmlTrivia.AddRange(new[] { indentation, xmlComment, SyntaxFactory.CarriageReturnLineFeed });
            }

            return xmlTrivia;
        }
    }
}
