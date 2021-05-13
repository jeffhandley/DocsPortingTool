#nullable enable
using Libraries.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Libraries.RoslynTripleSlash
{
    /*
    The following triple slash comments section:

        /// <summary>
        /// My summary.
        /// </summary>
        /// <param name="paramName">My param description.</param>
        /// <remarks>My remarks.</remarks>
        public ...

    translates to this syntax tree structure:

    PublicKeyword (SyntaxToken) -> The public keyword including its trivia.
        Lead: EndOfLineTrivia -> The newline char before the 4 whitespace chars before the triple slash comments.
        Lead: WhitespaceTrivia -> The 4 whitespace chars before the triple slash comments.
        Lead: SingleLineDocumentationCommentTrivia (SyntaxTrivia)
            SingleLineDocumentationCommentTrivia (DocumentationCommentTriviaSyntax) -> The triple slash comments, excluding the first 3 slash chars.
                XmlText (XmlTextSyntax)
                    XmlTextLiteralToken (SyntaxToken) -> The space between the first triple slash and <summary>.
                        Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> The first 3 slash chars.

                XmlElement (XmlElementSyntax) -> From <summary> to </summary>. Excludes the first 3 slash chars, but includes the second and third trios.
                    XmlElementStartTag (XmlElementStartTagSyntax) -> <summary>
                        LessThanToken (SyntaxToken) -> <
                        XmlName (XmlNameSyntax) -> summary
                            IdentifierToken (SyntaxToken) -> summary
                        GreaterThanToken (SyntaxToken) -> >
                    XmlText (XmlTextSyntax) -> Everything after <summary> and before </summary>
                        XmlTextLiteralNewLineToken (SyntaxToken) -> endline after <summary>
                        XmlTextLiteralToken (SyntaxToken) -> [ My summary.]
                            Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> endline after summary text
                        XmlTextLiteralNewToken (SyntaxToken) -> Space between 3 slashes and </summary>
                            Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> whitespace + 3 slashes before the </summary>
                    XmlElementEndTag (XmlElementEndTagSyntax) -> </summary>
                        LessThanSlashToken (SyntaxToken) -> </
                        XmlName (XmlNameSyntax) -> summary
                            IdentifierToken (SyntaxToken) -> summary
                        GreaterThanToken (SyntaxToken) -> >
                XmlText -> endline + whitespace + 3 slahes before <param
                    XmlTextLiteralNewLineToken (XmlTextSyntax) -> endline after </summary>
                    XmlTextLiteralToken (XmlTextLiteralToken) -> space after 3 slashes and before <param
                        Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> whitespace + 3 slashes before the space and <param

                XmlElement -> <param name="...">...</param>
                    XmlElementStartTag -> <param name="...">
                        LessThanToken -> <
                        XmlName -> param
                            IdentifierToken -> param
                        XmlNameAttribute (XmlNameAttributeSyntax) -> name="paramName"
                            XmlName -> name
                                IdentifierToken -> name
                                    Lead: WhitespaceTrivia -> space between param and name
                            EqualsToken -> =
                            DoubleQuoteToken -> opening "
                            IdentifierName -> paramName
                                IdentifierToken -> paramName
                            DoubleQuoteToken -> closing "
                        GreaterThanToken -> >
                    XmlText -> My param description.
                        XmlTextLiteralToken -> My param description.
                    XmlElementEndTag -> </param>
                        LessThanSlashToken -> </
                        XmlName -> param
                            IdentifierToken -> param
                        GreaterThanToken -> >
                XmlText -> newline + 4 whitespace chars + /// before <remarks>

                XmlElement -> <remarks>My remarks.</remarks>
                XmlText -> new line char after </remarks>
                    XmlTextLiteralNewLineToken -> new line char after </remarks>
                EndOfDocumentationCommentToken (SyntaxToken) -> invisible

        Lead: WhitespaceTrivia -> The 4 whitespace chars before the public keyword.
        Trail: WhitespaceTrivia -> The single whitespace char after the public keyword.
    */
    internal class TripleSlashSyntaxRewriter : CSharpSyntaxRewriter
    {
        private DocsCommentsContainer DocsComments { get; }
        private SemanticModel Model { get; }

        public TripleSlashSyntaxRewriter(DocsCommentsContainer docsComments, SemanticModel model) : base(visitIntoStructuredTrivia: true)
        {
            DocsComments = docsComments;
            Model = model;
        }

        #region Visitor overrides

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitClassDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node) =>
            VisitBaseMethodDeclaration(node);

        public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitDelegateDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitEnumDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) =>
            VisitMemberDeclaration(node);

        public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node) =>
            VisitVariableDeclaration(node);

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node) =>
            VisitVariableDeclaration(node);

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitInterfaceDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node) =>
            VisitBaseMethodDeclaration(node);

        public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node) =>
            VisitBaseMethodDeclaration(node);

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = XmlDocComments.GetSummary(member, leadingWhitespace);
            SyntaxTriviaList value = XmlDocComments.GetValue(member, leadingWhitespace);
            SyntaxTriviaList exceptions = XmlDocComments.GetExceptions(member.Exceptions, leadingWhitespace);
            SyntaxTriviaList remarks = XmlDocComments.GetRemarks(member, leadingWhitespace);
            SyntaxTriviaList seealsos = XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = XmlDocComments.GetAltMembers(member.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = XmlDocComments.GetRelateds(member.Relateds, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, value, exceptions, remarks, seealsos, altmembers, relateds);
        }

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitRecordDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitStructDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        #endregion

        #region Visit helpers

        private SyntaxNode? VisitType(SyntaxNode? node, ISymbol? symbol)
        {
            if (node == null || symbol == null)
            {
                return node;
            }

            string? docId = symbol.GetDocumentationCommentId();
            if (string.IsNullOrWhiteSpace(docId))
            {
                Log.Warning($"DocId is null or empty.");
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            if (!TryGetType(symbol, out DocsType? type))
            {
                return node;
            }

            SyntaxTriviaList summary = XmlDocComments.GetSummary(type, leadingWhitespace);
            SyntaxTriviaList typeParameters = XmlDocComments.GetTypeParameters(type, leadingWhitespace);
            SyntaxTriviaList parameters = XmlDocComments.GetParameters(type, leadingWhitespace);
            SyntaxTriviaList remarks = XmlDocComments.GetRemarks(type, leadingWhitespace);
            SyntaxTriviaList seealsos = XmlDocComments.GetSeeAlsos(type.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = XmlDocComments.GetAltMembers(type.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = XmlDocComments.GetRelateds(type.Relateds, leadingWhitespace);


            return GetNodeWithTrivia(leadingWhitespace, node, summary, typeParameters, parameters, remarks, seealsos, altmembers, relateds);
        }

        private SyntaxNode? VisitBaseMethodDeclaration(BaseMethodDeclarationSyntax node)
        {
            // The Docs files only contain docs for public elements,
            // so if no comments are found, we return the node unmodified
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = XmlDocComments.GetSummary(member, leadingWhitespace);
            SyntaxTriviaList typeParameters = XmlDocComments.GetTypeParameters(member, leadingWhitespace);
            SyntaxTriviaList parameters = XmlDocComments.GetParameters(member, leadingWhitespace);
            SyntaxTriviaList returns = XmlDocComments.GetReturns(member, leadingWhitespace);
            SyntaxTriviaList exceptions = XmlDocComments.GetExceptions(member.Exceptions, leadingWhitespace);
            SyntaxTriviaList remarks = XmlDocComments.GetRemarks(member, leadingWhitespace);
            SyntaxTriviaList seealsos = XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = XmlDocComments.GetAltMembers(member.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = XmlDocComments.GetRelateds(member.Relateds, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, typeParameters, parameters, returns, exceptions, remarks, seealsos, altmembers, relateds);
        }

        private SyntaxNode? VisitMemberDeclaration(MemberDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = XmlDocComments.GetSummary(member, leadingWhitespace);
            SyntaxTriviaList exceptions = XmlDocComments.GetExceptions(member.Exceptions, leadingWhitespace);
            SyntaxTriviaList remarks = XmlDocComments.GetRemarks(member, leadingWhitespace);
            SyntaxTriviaList seealsos = XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = XmlDocComments.GetAltMembers(member.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = XmlDocComments.GetRelateds(member.Relateds, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, exceptions, remarks, seealsos, altmembers, relateds);
        }

        private SyntaxNode? VisitVariableDeclaration(BaseFieldDeclarationSyntax node)
        {
            // The comments need to be extracted from the underlying variable declarator inside the declaration
            VariableDeclarationSyntax declaration = node.Declaration;

            // Only port docs if there is only one variable in the declaration
            if (declaration.Variables.Count == 1)
            {
                if (!TryGetMember(declaration.Variables.First(), out DocsMember? member))
                {
                    return node;
                }

                SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

                SyntaxTriviaList summary = XmlDocComments.GetSummary(member, leadingWhitespace);
                SyntaxTriviaList remarks = XmlDocComments.GetRemarks(member, leadingWhitespace);
                SyntaxTriviaList seealsos = XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
                SyntaxTriviaList altmembers = XmlDocComments.GetAltMembers(member.AltMembers, leadingWhitespace);
                SyntaxTriviaList relateds = XmlDocComments.GetRelateds(member.Relateds, leadingWhitespace);

                return GetNodeWithTrivia(leadingWhitespace, node, summary, remarks, seealsos, altmembers, relateds);
            }

            return node;
        }

        private bool TryGetMember(SyntaxNode node, [NotNullWhen(returnValue: true)] out DocsMember? member)
        {
            member = null;
            if (Model.GetDeclaredSymbol(node) is ISymbol symbol)
            {
                string? docId = symbol.GetDocumentationCommentId();
                if (!string.IsNullOrWhiteSpace(docId))
                {
                    member = DocsComments.Members.FirstOrDefault(m => m.DocId == docId);
                }
            }

            return member != null;
        }

        private bool TryGetType(ISymbol symbol, [NotNullWhen(returnValue: true)] out DocsType? type)
        {
            type = null;

            string? docId = symbol.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                type = DocsComments.Types.FirstOrDefault(t => t.DocId == docId);
            }

            return type != null;
        }

        #endregion

        #region Syntax manipulation

        private static SyntaxNode GetNodeWithTrivia(SyntaxTriviaList leadingWhitespace, SyntaxNode node, params SyntaxTriviaList[] trivias)
        {
            SyntaxTriviaList leadingDoubleSlashComments = GetLeadingDoubleSlashComments(node, leadingWhitespace);

            SyntaxTriviaList finalTrivia = new();
            foreach (SyntaxTriviaList t in trivias)
            {
                finalTrivia = finalTrivia.AddRange(t);
            }
            finalTrivia = finalTrivia.AddRange(leadingDoubleSlashComments);

            if (finalTrivia.Count > 0)
            {
                finalTrivia = finalTrivia.AddRange(leadingWhitespace);

                var leadingTrivia = node.GetLeadingTrivia();
                if (leadingTrivia.Any())
                {
                    if (leadingTrivia[0].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        // Ensure the endline that separates nodes is respected
                        finalTrivia = new SyntaxTriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed)
                            .AddRange(finalTrivia);
                    }
                }

                return node.WithLeadingTrivia(finalTrivia);
            }

            // If there was no new trivia, return untouched
            return node;
        }

        // Finds the last set of whitespace characters that are to the left of the public|protected keyword of the node.
        private static SyntaxTriviaList GetLeadingWhitespace(SyntaxNode node)
        {
            SyntaxTriviaList triviaList = GetLeadingTrivia(node);

            if (triviaList.Any() &&
                triviaList.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)) is SyntaxTrivia last)
            {
                return new(last);
            }

            return new();
        }

        private static SyntaxTriviaList GetLeadingDoubleSlashComments(SyntaxNode node, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList triviaList = GetLeadingTrivia(node);

            SyntaxTriviaList doubleSlashComments = new();

            foreach (SyntaxTrivia trivia in triviaList)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                {
                    doubleSlashComments = doubleSlashComments
                                            .AddRange(leadingWhitespace)
                                            .Add(trivia)
                                            .Add(SyntaxFactory.CarriageReturnLineFeed);
                }
            }

            return doubleSlashComments;
        }

        private static SyntaxTriviaList GetLeadingTrivia(SyntaxNode node)
        {
            if (node is MemberDeclarationSyntax memberDeclaration)
            {
                if ((memberDeclaration.Modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.PublicKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword)) is SyntaxToken modifier) &&
                        !modifier.IsKind(SyntaxKind.None))
                {
                    return modifier.LeadingTrivia;
                }

                return node.GetLeadingTrivia();
            }

            return new();
        }

        #endregion
    }
}
