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
        public static SyntaxNode ApplyXmlComments(SyntaxNode node, IEnumerable<SyntaxTrivia> xmlComments)
        {
            var leading = node.GetLeadingTrivia();
            SyntaxTriviaList xmlTrivia = new();

            if (leading.Any() && leading.Last().IsKind(SyntaxKind.WhitespaceTrivia))
            {
                SyntaxTrivia indentation = leading.Last();

                foreach (var xmlComment in xmlComments)
                {
                    xmlTrivia = xmlTrivia.AddRange(new[] { indentation, xmlComment, SyntaxFactory.CarriageReturnLineFeed });
                }

                return node.ReplaceTrivia(indentation, xmlTrivia.Add(indentation));
            }
            foreach (var xmlComment in xmlComments)
            {
                xmlTrivia = xmlTrivia.AddRange(new[] { xmlComment, SyntaxFactory.CarriageReturnLineFeed });
            }

            return node.WithLeadingTrivia(leading.AddRange(xmlTrivia));
        }
    }
}
