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
        private static int[] UpperBoundaries = new[]
        {
            (int)SyntaxKind.RegionDirectiveTrivia
        };

        public static int[] LowerBoundaries = new[]
        {
            (int)SyntaxKind.IfDirectiveTrivia
        };

        public static SyntaxNode ApplyXmlComments(SyntaxNode node, IEnumerable<SyntaxTrivia> xmlComments)
        {
            SyntaxTriviaList leading = node.GetLeadingTrivia();
            SyntaxTriviaList xmlTrivia = new();
            SyntaxTrivia indentation = SyntaxFactory.Whitespace("");

            if (leading.Any() && leading.Last().IsKind(SyntaxKind.WhitespaceTrivia))
            {
                // If the last trivia before the declaration is whitespace
                // then this represents indentation for the declaration
                // and we'll match that indentation for the XML comments
                indentation = leading.Last();
            }

            // Create the indented XML comment lines
            foreach (var xmlComment in xmlComments)
            {
                xmlTrivia = xmlTrivia.AddRange(new[] { indentation, xmlComment, SyntaxFactory.CarriageReturnLineFeed });
            }

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

            // Now that we know the position of where to insert our XML comments
            // We replace that trivia with the XML trivia plus that trivia (to retain it)
            return node.ReplaceTrivia(leading[position], xmlTrivia.Add(leading[position]));
        }
    }
}
