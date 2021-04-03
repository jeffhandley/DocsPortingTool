using Libraries.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Libraries.RoslynTripleSlash
{
    internal static class XmlDocComments
    {
        private static readonly string[] ReservedKeywords = new[] { "abstract", "async", "await", "false", "null", "sealed", "static", "true", "virtual" };

        private static readonly Dictionary<string, string> PrimitiveTypes = new()
        {
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.Object", "object" }, // Ambiguous: could be 'object' or 'dynamic' https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
            { "System.SByte", "sbyte" },
            { "System.Single", "float" },
            { "System.String", "string" },
            { "System.UInt16", "ushort" },
            { "System.UInt32", "uint" },
            { "System.UInt64", "ulong" },
            { "System.Void", "void" }
        };

        // Note that we need to support generics that use the ` literal as well as the escaped %60
        private static readonly string ValidRegexChars = @"[A-Za-z0-9\-\._~:\/#\[\]\{\}@!\$&'\(\)\*\+,;]|(%60|`)\d+";
        private static readonly string ValidExtraChars = @"\?=";

        private static readonly string RegexDocIdPattern = @"(?<prefix>[A-Za-z]{1}:)?(?<docId>(" + ValidRegexChars + @")+)(?<overload>%2[aA])?(?<extraVars>\?(" + ValidRegexChars + @")+=(" + ValidRegexChars + @")+)?";
        private static readonly string RegexXmlCrefPattern = "cref=\"" + RegexDocIdPattern + "\"";
        private static readonly string RegexMarkdownXrefPattern = @"(?<xref><xref:" + RegexDocIdPattern + ">)";

        private static readonly string RegexMarkdownBoldPattern = @"\*\*(?<content>[A-Za-z0-9\-\._~:\/#\[\]@!\$&'\(\)\+,;%` ]+)\*\*";
        private static readonly string RegexXmlBoldReplacement = @"<b>${content}</b>";

        private static readonly string RegexMarkdownLinkPattern = @"\[(?<linkValue>.+)\]\((?<linkURL>(http|www)(" + ValidRegexChars + "|" + ValidExtraChars + @")+)\)";
        private static readonly string RegexHtmlLinkReplacement = "<a href=\"${linkURL}\">${linkValue}</a>";

        private static readonly string RegexMarkdownCodeStartPattern = @"```(?<language>(cs|csharp|cpp|vb|visualbasic))(?<spaces>\s+)";
        private static readonly string RegexXmlCodeStartReplacement = "<code class=\"lang-${language}\">${spaces}";

        private static readonly string RegexMarkdownCodeEndPattern = @"```(?<spaces>\s+)";
        private static readonly string RegexXmlCodeEndReplacement = "</code>${spaces}";
        private static readonly string[] MarkdownUnconvertableStrings = new[] { "](~/includes", "[!INCLUDE" };

        private static readonly string[] MarkdownCodeIncludes = new[] { "[!code-cpp", "[!code-csharp", "[!code-vb", };

        private static readonly string[] MarkdownExamples = new[] { "## Examples", "## Example" };

        private static readonly string[] MarkdownHeaders = new[] { "[!NOTE]", "[!IMPORTANT]", "[!TIP]" };

        public static SyntaxTriviaList GetSummary(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Summary.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Summary, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlSummaryElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static SyntaxTriviaList GetRemarks(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Remarks.IsDocsEmpty())
            {
                return GetFormattedRemarks(api, leadingWhitespace);
            }

            return new();
        }

        public static SyntaxTriviaList GetValue(DocsMember api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Value.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Value, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlValueElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static SyntaxTriviaList GetParameter(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlParamElement(name, contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static SyntaxTriviaList GetParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList parameters = new();
            foreach (SyntaxTriviaList parameterTrivia in api.Params
                    .Where(param => !param.Value.IsDocsEmpty())
                    .Select(param => GetParameter(param.Name, param.Value, leadingWhitespace)))
            {
                parameters = parameters.AddRange(parameterTrivia);
            }
            return parameters;
        }

        public static SyntaxTriviaList GetTypeParam(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                var attribute = new SyntaxList<XmlAttributeSyntax>(SyntaxFactory.XmlTextAttribute("name", name));
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                return GetXmlTrivia("typeparam", attribute, contents, leadingWhitespace);
            }

            return new();
        }

        public static SyntaxTriviaList GetTypeParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList typeParameters = new();
            foreach (SyntaxTriviaList typeParameterTrivia in api.TypeParams
                        .Where(typeParam => !typeParam.Value.IsDocsEmpty())
                        .Select(typeParam => GetTypeParam(typeParam.Name, typeParam.Value, leadingWhitespace)))
            {
                typeParameters = typeParameters.AddRange(typeParameterTrivia);
            }
            return typeParameters;
        }

        public static SyntaxTriviaList GetReturns(DocsMember api, SyntaxTriviaList leadingWhitespace)
        {
            // Also applies for when <returns> is empty because the method return type is void
            if (!api.Returns.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Returns, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlReturnsElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static SyntaxTriviaList GetException(string cref, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                cref = RemoveCrefPrefix(cref);
                TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref));
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlExceptionElement(crefSyntax, contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static SyntaxTriviaList GetExceptions(List<DocsException> docsExceptions, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList exceptions = new();
            if (docsExceptions.Any())
            {
                foreach (SyntaxTriviaList exceptionsTrivia in docsExceptions.Select(
                    exception => GetException(exception.Cref, exception.Value, leadingWhitespace)))
                {
                    exceptions = exceptions.AddRange(exceptionsTrivia);
                }
            }
            return exceptions;
        }

        public static SyntaxTriviaList GetSeeAlso(string cref, SyntaxTriviaList leadingWhitespace)
        {
            cref = RemoveCrefPrefix(cref);
            TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref));
            XmlEmptyElementSyntax element = SyntaxFactory.XmlSeeAlsoElement(crefSyntax);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        public static SyntaxTriviaList GetSeeAlsos(List<string> docsSeeAlsoCrefs, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList seealsos = new();
            if (docsSeeAlsoCrefs.Any())
            {
                foreach (SyntaxTriviaList seealsoTrivia in docsSeeAlsoCrefs.Select(
                    s => GetSeeAlso(s, leadingWhitespace)))
                {
                    seealsos = seealsos.AddRange(seealsoTrivia);
                }
            }
            return seealsos;
        }

        public static SyntaxTriviaList GetAltMember(string cref, SyntaxTriviaList leadingWhitespace)
        {
            cref = RemoveCrefPrefix(cref);
            XmlAttributeSyntax attribute = SyntaxFactory.XmlTextAttribute("cref", cref);
            XmlEmptyElementSyntax emptyElement = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName(SyntaxFactory.Identifier("altmember")), new SyntaxList<XmlAttributeSyntax>(attribute));
            return GetXmlTrivia(emptyElement, leadingWhitespace);
        }

        public static SyntaxTriviaList GetAltMembers(List<string> docsAltMembers, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList altMembers = new();
            if (docsAltMembers.Any())
            {
                foreach (SyntaxTriviaList altMemberTrivia in docsAltMembers.Select(
                    s => GetAltMember(s, leadingWhitespace)))
                {
                    altMembers = altMembers.AddRange(altMemberTrivia);
                }
            }
            return altMembers;
        }

        public static SyntaxTriviaList GetRelated(string articleType, string href, string value, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlAttributeSyntax> attributes = new();

            attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("type", articleType));
            attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("href", href));

            XmlTextSyntax contents = GetTextAsCommentedTokens(value, leadingWhitespace);
            return GetXmlTrivia("related", attributes, contents, leadingWhitespace);
        }

        public static SyntaxTriviaList GetRelateds(List<DocsRelated> docsRelateds, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList relateds = new();
            if (docsRelateds.Any())
            {
                foreach (SyntaxTriviaList relatedsTrivia in docsRelateds.Select(
                    s => GetRelated(s.ArticleType, s.Href, s.Value, leadingWhitespace)))
                {
                    relateds = relateds.AddRange(relatedsTrivia);
                }
            }
            return relateds;
        }

        private static XmlTextSyntax GetTextAsCommentedTokens(string text, SyntaxTriviaList leadingWhitespace, bool wrapWithNewLines = false)
        {
            text = CleanCrefs(text);

            // collapse newlines to a single one
            string whitespace = Regex.Replace(leadingWhitespace.ToFullString(), @"(\r?\n)+", "");
            SyntaxToken whitespaceToken = SyntaxFactory.XmlTextNewLine(Environment.NewLine + whitespace);

            SyntaxTrivia leadingTrivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, string.Empty);
            SyntaxTriviaList leading = SyntaxTriviaList.Create(leadingTrivia);

            string[] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var tokens = new List<SyntaxToken>();

            if (wrapWithNewLines)
            {
                tokens.Add(whitespaceToken);
            }

            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                string line = lines[lineNumber];

                SyntaxToken token = SyntaxFactory.XmlTextLiteral(leading, line, line, default);
                tokens.Add(token);

                if (lines.Length > 1 && lineNumber < lines.Length - 1)
                {
                    tokens.Add(whitespaceToken);
                }
            }

            if (wrapWithNewLines)
            {
                tokens.Add(whitespaceToken);
            }

            XmlTextSyntax xmlText = SyntaxFactory.XmlText(tokens.ToArray());
            return xmlText;
        }

        private static SyntaxTriviaList GetXmlTrivia(XmlNodeSyntax node, SyntaxTriviaList leadingWhitespace)
        {
            DocumentationCommentTriviaSyntax docComment = SyntaxFactory.DocumentationComment(node);
            SyntaxTrivia docCommentTrivia = SyntaxFactory.Trivia(docComment);

            return leadingWhitespace
                .Add(docCommentTrivia)
                .Add(SyntaxFactory.CarriageReturnLineFeed);
        }

        // Generates a custom SyntaxTrivia object containing a triple slashed xml element with optional attributes.
        // Looks like below (excluding square brackets):
        // [    /// <element attribute1="value1" attribute2="value2">text</element>]
        private static SyntaxTriviaList GetXmlTrivia(string name, SyntaxList<XmlAttributeSyntax> attributes, XmlTextSyntax contents, SyntaxTriviaList leadingWhitespace)
        {
            XmlElementStartTagSyntax start = SyntaxFactory.XmlElementStartTag(
                SyntaxFactory.Token(SyntaxKind.LessThanToken),
                SyntaxFactory.XmlName(SyntaxFactory.Identifier(name)),
                attributes,
                SyntaxFactory.Token(SyntaxKind.GreaterThanToken));

            XmlElementEndTagSyntax end = SyntaxFactory.XmlElementEndTag(
                SyntaxFactory.Token(SyntaxKind.LessThanSlashToken),
                SyntaxFactory.XmlName(SyntaxFactory.Identifier(name)),
                SyntaxFactory.Token(SyntaxKind.GreaterThanToken));

            XmlElementSyntax element = SyntaxFactory.XmlElement(start, new SyntaxList<XmlNodeSyntax>(contents), end);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private static string WrapInRemarks(string acum)
        {
            string wrapped = Environment.NewLine + "<format type=\"text/markdown\"><![CDATA[" + Environment.NewLine;
            wrapped += acum;
            wrapped += Environment.NewLine + "]]></format>" + Environment.NewLine;
            return wrapped;
        }

        private static string WrapCodeIncludes(string[] splitted, ref int n)
        {
            string acum = string.Empty;
            while (n < splitted.Length && splitted[n].ContainsStrings(MarkdownCodeIncludes))
            {
                acum += Environment.NewLine + splitted[n];
                if ((n + 1) < splitted.Length && splitted[n + 1].ContainsStrings(MarkdownCodeIncludes))
                {
                    n++;
                }
                else
                {
                    break;
                }
            }
            return WrapInRemarks(acum);
        }

        private static SyntaxTriviaList GetFormattedRemarks(IDocsAPI api, SyntaxTriviaList leadingWhitespace)
        {

            string remarks = RemoveUnnecessaryMarkdown(api.Remarks);
            string example = string.Empty;

            XmlNodeSyntax contents;
            if (remarks.ContainsStrings(MarkdownUnconvertableStrings))
            {
                contents = GetTextAsFormatCData(remarks, leadingWhitespace);
            }
            else
            {
                string[] splitted = remarks.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                string updatedRemarks = string.Empty;
                for (int n = 0; n < splitted.Length; n++)
                {
                    string acum;
                    string line = splitted[n];
                    if (line.ContainsStrings(MarkdownHeaders))
                    {
                        acum = line;
                        n++;
                        while (n < splitted.Length && splitted[n].StartsWith(">"))
                        {
                            acum += Environment.NewLine + splitted[n];
                            if ((n + 1) < splitted.Length && splitted[n + 1].StartsWith(">"))
                            {
                                n++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        updatedRemarks += WrapInRemarks(acum);
                    }
                    else if (line.ContainsStrings(MarkdownCodeIncludes))
                    {
                        updatedRemarks += WrapCodeIncludes(splitted, ref n);
                    }
                    // When an example is found, everything after the header is considered part of that section
                    else if (line.Contains("## Example"))
                    {
                        n++;
                        while (n < splitted.Length)
                        {
                            line = splitted[n];
                            if (line.ContainsStrings(MarkdownCodeIncludes))
                            {
                                example += WrapCodeIncludes(splitted, ref n);
                            }
                            else
                            {
                                example += Environment.NewLine + line;
                            }
                            n++;
                        }
                    }
                    else
                    {
                        updatedRemarks += ReplaceMarkdownWithXmlElements(Environment.NewLine + line, api.Params, api.TypeParams);
                    }
                }

                contents = GetTextAsCommentedTokens(updatedRemarks, leadingWhitespace);
            }

            XmlElementSyntax remarksXml = SyntaxFactory.XmlRemarksElement(contents);
            SyntaxTriviaList result = GetXmlTrivia(remarksXml, leadingWhitespace);

            if (!string.IsNullOrWhiteSpace(example))
            {
                SyntaxTriviaList exampleTriviaList = GetFormattedExamples(api, example, leadingWhitespace);
                result = result.AddRange(exampleTriviaList);
            }

            return result;
        }

        private static SyntaxTriviaList GetFormattedExamples(IDocsAPI api, string example, SyntaxTriviaList leadingWhitespace)
        {
            example = ReplaceMarkdownWithXmlElements(example, api.Params, api.TypeParams);
            XmlNodeSyntax exampleContents = GetTextAsCommentedTokens(example, leadingWhitespace);
            XmlElementSyntax exampleXml = SyntaxFactory.XmlExampleElement(exampleContents);
            SyntaxTriviaList exampleTriviaList = GetXmlTrivia(exampleXml, leadingWhitespace);
            return exampleTriviaList;
        }

        private static XmlNodeSyntax GetTextAsFormatCData(string text, SyntaxTriviaList leadingWhitespace)
        {
            XmlTextSyntax remarks = GetTextAsCommentedTokens(text, leadingWhitespace, wrapWithNewLines: true);

            XmlNameSyntax formatName = SyntaxFactory.XmlName("format");
            XmlAttributeSyntax formatAttribute = SyntaxFactory.XmlTextAttribute("type", "text/markdown");
            var formatAttributes = new SyntaxList<XmlAttributeSyntax>(formatAttribute);

            var formatStart = SyntaxFactory.XmlElementStartTag(formatName, formatAttributes);
            var formatEnd = SyntaxFactory.XmlElementEndTag(formatName);

            XmlCDataSectionSyntax cdata = SyntaxFactory.XmlCDataSection(remarks.TextTokens);
            var cdataList = new SyntaxList<XmlNodeSyntax>(cdata);

            XmlElementSyntax contents = SyntaxFactory.XmlElement(formatStart, cdataList, formatEnd);

            return contents;
        }

        private static string RemoveUnnecessaryMarkdown(string text)
        {
            text = Regex.Replace(text, @"<!\[CDATA\[(\r?\n)*[\t ]*", "");
            text = Regex.Replace(text, @"\]\]>", "");
            text = Regex.Replace(text, @"##[ ]?Remarks(\r?\n)*[\t ]*", "");
            return text;
        }

        private static string ReplaceMarkdownWithXmlElements(string text, List<DocsParam> docsParams, List<DocsTypeParam> docsTypeParams)
        {
            text = CleanXrefs(text);

            // commonly used url entities
            text = Regex.Replace(text, @"%23", "#");
            text = Regex.Replace(text, @"%28", "(");
            text = Regex.Replace(text, @"%29", ")");
            text = Regex.Replace(text, @"%2C", ",");

            // hyperlinks
            text = Regex.Replace(text, RegexMarkdownLinkPattern, RegexHtmlLinkReplacement);

            // bold
            text = Regex.Replace(text, RegexMarkdownBoldPattern, RegexXmlBoldReplacement);

            // code snippet
            text = Regex.Replace(text, RegexMarkdownCodeStartPattern, RegexXmlCodeStartReplacement);
            text = Regex.Replace(text, RegexMarkdownCodeEndPattern, RegexXmlCodeEndReplacement);

            // langwords|parameters|typeparams
            MatchCollection collection = Regex.Matches(text, @"(?<backtickedParam>`(?<paramName>[a-zA-Z0-9_]+)`)");
            foreach (Match match in collection)
            {
                string backtickedParam = match.Groups["backtickedParam"].Value;
                string paramName = match.Groups["paramName"].Value;
                if (ReservedKeywords.Any(x => x == paramName))
                {
                    text = Regex.Replace(text, $"{backtickedParam}", $"<see langword=\"{paramName}\" />");
                }
                else if (docsParams.Any(x => x.Name == paramName))
                {
                    text = Regex.Replace(text, $"{backtickedParam}", $"<paramref name=\"{paramName}\" />");
                }
                else if (docsTypeParams.Any(x => x.Name == paramName))
                {
                    text = Regex.Replace(text, $"{backtickedParam}", $"<typeparamref name=\"{paramName}\" />");
                }
            }

            return text;
        }

        // Removes the one letter prefix and the following colon, if found, from a cref.
        private static string RemoveCrefPrefix(string cref)
        {
            if (cref.Length > 2 && cref[1] == ':')
            {
                return cref[2..];
            }
            return cref;
        }

        private static string ReplacePrimitives(string text)
        {
            foreach ((string key, string value) in PrimitiveTypes)
            {
                text = Regex.Replace(text, key, value);
            }
            return text;
        }

        private static string ReplaceDocId(Match m)
        {
            string docId = m.Groups["docId"].Value;
            string overload = string.IsNullOrWhiteSpace(m.Groups["overload"].Value) ? "" : "O:";
            docId = ReplacePrimitives(docId);
            docId = Regex.Replace(docId, @"%60", "`");
            docId = Regex.Replace(docId, @"`\d", "{T}");
            return overload + docId;
        }

        private static string CrefEvaluator(Match m)
        {
            string docId = ReplaceDocId(m);
            return "cref=\"" + docId + "\"";
        }

        private static string CleanCrefs(string text)
        {
            text = Regex.Replace(text, RegexXmlCrefPattern, CrefEvaluator);
            return text;
        }

        private static string XrefEvaluator(Match m)
        {
            string docId = ReplaceDocId(m);
            return "<see cref=\"" + docId + "\" />";
        }

        private static string CleanXrefs(string text)
        {
            text = Regex.Replace(text, RegexMarkdownXrefPattern, XrefEvaluator);
            return text;
        }
    }
}
