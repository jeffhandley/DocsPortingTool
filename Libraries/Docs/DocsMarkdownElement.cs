﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public abstract class DocsMarkdownElement : DocsTextElement
    {
        public DocsMarkdownElement(XElement xeRemarks) : base(xeRemarks)
        {
        }

        public IEnumerable<DocsParam>? Params { get; init; }
        public IEnumerable<DocsTypeParam>? TypeParams { get; init; }

        private static readonly Regex IncludeFilePattern = new(@"\[!INCLUDE");
        private static readonly Regex CalloutPattern = new(@"\[!NOTE|\[!IMPORTANT|\[!TIP");
        private static readonly Regex CodeIncludePattern = new(@"\[!code-cpp|\[!code-csharp|\[!code-vb");

        private static readonly Regex MarkdownLinkPattern = new(@"\[(?<linkText>.+)\]\((?<linkURL>(http|www)([A-Za-z0-9\-\._~:\/#\[\]\{\}@!\$&'\(\)\*\+,;\?=%])+)\)");
        private const string MarkdownLinkReplacement = "<a href=\"${linkURL}\">${linkText}</a>";

        private static readonly Regex MarkdownBoldPattern = new(@"\*\*(?<content>[A-Za-z0-9\-\._~:\/#\[\]@!\$&'\(\)\+,;%` ]+)\*\*");
        private const string MarkdownBoldReplacement = @"<b>${content}</b>";

        private static readonly Regex MarkdownCodeStartPattern = new(@"```(?<language>(cs|csharp|cpp|vb|visualbasic))(?<spaces>\s+)");
        private const string MarkdownCodeStartReplacement = "<code class=\"lang-${language}\">${spaces}";

        private static readonly Regex MarkdownCodeEndPattern = new(@"```(?<spaces>\s+)");
        private const string MarkdownCodeEndReplacement = "</code>${spaces}";


        private static readonly Regex UnparseableMarkdown = new(string.Join('|', new[] {
            IncludeFilePattern.ToString(),
            CalloutPattern.ToString(),
            CodeIncludePattern.ToString()
        }));

        protected override string? ParseNode(XNode node)
        {
            if (node is XElement element && element.Name == "format" && element.Attribute("type")?.Value == "text/markdown")
            {
                var markdown = (element.FirstNode is XCData cdata) ? cdata.Value : element.Value;

                if (TryParseMarkdown(markdown, out var parsedText))
                {
                    return parsedText;
                }
            }

            return base.ParseNode(node);
        }

        protected string RemoveMarkdownHeading(string markdown, string heading)
        {
            Regex HeadingPattern = new(@$"^\s*##\s*{heading}\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            return HeadingPattern.Replace(markdown, "", 1);
        }

        protected virtual bool TryParseMarkdown(string markdown, [NotNullWhen(true)] out string? parsed)
        {
            if (UnparseableMarkdown.IsMatch(markdown))
            {
                parsed = null;
                return false;
            }

            parsed = DocsApiReference.ReplaceMarkdownXrefWithSeeCref(markdown);
            parsed = MarkdownLinkPattern.Replace(parsed, MarkdownLinkReplacement);
            parsed = MarkdownBoldPattern.Replace(parsed, MarkdownBoldReplacement);
            parsed = MarkdownCodeStartPattern.Replace(parsed, MarkdownCodeStartReplacement);
            parsed = MarkdownCodeEndPattern.Replace(parsed, MarkdownCodeEndReplacement);
            parsed = ReplaceBacktickReferences(parsed);

            return true;
        }

        private static readonly string[] ReservedKeywords = new[] { "abstract", "async", "await", "false", "null", "sealed", "static", "true", "virtual" };

        private string ReplaceBacktickReferences(string markdown)
        {
            // langwords|parameters|typeparams and other type references within markdown backticks
            MatchCollection collection = Regex.Matches(markdown, @"(?<backtickContent>`(?<backtickedApi>[a-zA-Z0-9_\.]+(?<genericType>\<(?<typeParam>[a-zA-Z0-9_,]+)\>){0,1})`)");
            foreach (Match match in collection)
            {
                string backtickContent = match.Groups["backtickContent"].Value;
                string backtickedApi = match.Groups["backtickedApi"].Value;
                Group genericType = match.Groups["genericType"];
                Group typeParam = match.Groups["typeParam"];

                if (genericType.Success && typeParam.Success)
                {
                    backtickedApi = backtickedApi.Replace(genericType.Value, $"{{{typeParam.Value}}}");
                }

                if (ReservedKeywords.Any(x => x == backtickedApi))
                {
                    markdown = Regex.Replace(markdown, $"{backtickContent}", $"<see langword=\"{backtickedApi}\" />");
                }
                else if (TypeParams?.Any(x => x.Name == backtickedApi) == true)
                {
                    markdown = Regex.Replace(markdown, $"{backtickContent}", $"<typeparamref name=\"{backtickedApi}\" />");
                }
                else if (Params?.Any(x => x.Name == backtickedApi) == true)
                {
                    markdown = Regex.Replace(markdown, $"{backtickContent}", $"<paramref name=\"{backtickedApi}\" />");
                }
                else
                {
                    markdown = Regex.Replace(markdown, $"{backtickContent}", $"<see cref=\"{backtickedApi}\" />");
                }
            }

            return markdown;
        }
    }
}

