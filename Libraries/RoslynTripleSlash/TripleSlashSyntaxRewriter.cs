﻿#nullable enable
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

            List<XmlNodeSyntax?> xmlComments = new();
            xmlComments.Add(XmlDocComments.GetSummary(member));
            xmlComments.Add(XmlDocComments.GetValue(member));
            xmlComments.AddRange(XmlDocComments.GetExceptions(member.Exceptions));
            xmlComments.Add(XmlDocComments.GetRemarks(member));
            xmlComments.AddRange(XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs));
            xmlComments.AddRange(XmlDocComments.GetAltMembers(member.AltMembers));
            xmlComments.AddRange(XmlDocComments.GetRelateds(member.Relateds));

            return LeadingTriviaRewriter.ApplyXmlComments(node, xmlComments.Where(c => c is not null)!);
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

            if (!TryGetType(symbol, out DocsType? type))
            {
                return node;
            }

            List<XmlNodeSyntax?> xmlComments = new();
            xmlComments.Add(XmlDocComments.GetSummary(type));
            xmlComments.AddRange(XmlDocComments.GetTypeParameters(type));
            xmlComments.AddRange(XmlDocComments.GetParameters(type));
            xmlComments.Add(XmlDocComments.GetRemarks(type));
            xmlComments.AddRange(XmlDocComments.GetSeeAlsos(type.SeeAlsoCrefs));
            xmlComments.AddRange(XmlDocComments.GetAltMembers(type.AltMembers));
            xmlComments.AddRange(XmlDocComments.GetRelateds(type.Relateds));

            return LeadingTriviaRewriter.ApplyXmlComments(node, xmlComments.Where(c => c is not null)!);
        }

        private SyntaxNode? VisitBaseMethodDeclaration(BaseMethodDeclarationSyntax node)
        {
            // The Docs files only contain docs for public elements,
            // so if no comments are found, we return the node unmodified
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            List<XmlNodeSyntax?> xmlComments = new();
            xmlComments.Add(XmlDocComments.GetSummary(member));
            xmlComments.AddRange(XmlDocComments.GetTypeParameters(member));
            xmlComments.AddRange(XmlDocComments.GetParameters(member));
            xmlComments.Add(XmlDocComments.GetReturns(member));
            xmlComments.AddRange(XmlDocComments.GetExceptions(member.Exceptions));
            xmlComments.Add(XmlDocComments.GetRemarks(member));
            xmlComments.AddRange(XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs));
            xmlComments.AddRange(XmlDocComments.GetAltMembers(member.AltMembers));
            xmlComments.AddRange(XmlDocComments.GetRelateds(member.Relateds));

            return LeadingTriviaRewriter.ApplyXmlComments(node, xmlComments.Where(c => c is not null)!);
        }

        private SyntaxNode? VisitMemberDeclaration(MemberDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            List<XmlNodeSyntax?> xmlComments = new();
            xmlComments.Add(XmlDocComments.GetSummary(member));
            xmlComments.AddRange(XmlDocComments.GetExceptions(member.Exceptions));
            xmlComments.Add(XmlDocComments.GetRemarks(member));
            xmlComments.AddRange(XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs));
            xmlComments.AddRange(XmlDocComments.GetAltMembers(member.AltMembers));
            xmlComments.AddRange(XmlDocComments.GetRelateds(member.Relateds));

            return LeadingTriviaRewriter.ApplyXmlComments(node, xmlComments.Where(c => c is not null)!);
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

                List<XmlNodeSyntax?> xmlComments = new();
                xmlComments.Add(XmlDocComments.GetSummary(member));
                xmlComments.Add(XmlDocComments.GetRemarks(member));
                xmlComments.AddRange(XmlDocComments.GetSeeAlsos(member.SeeAlsoCrefs));
                xmlComments.AddRange(XmlDocComments.GetAltMembers(member.AltMembers));
                xmlComments.AddRange(XmlDocComments.GetRelateds(member.Relateds));

                return LeadingTriviaRewriter.ApplyXmlComments(node, xmlComments.Where(c => c is not null)!);
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
    }
}
