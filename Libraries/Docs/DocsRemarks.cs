﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsRemarks : DocsTextElement
    {
        public DocsRemarks(XElement xeRemarks) : base(xeRemarks)
        {
        }

        public DocsExample? ExampleContent { get; private set; }

        private static readonly Regex RemarksHeaderPattern = new(@"^\s*##\s*Remarks\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex ExampleSectionPattern = new(@"^\s*##\s*Examples?\s*(?<examples>.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex IncludeFilePattern = new(@"\[!INCLUDE");
        private static readonly Regex CalloutPattern = new(@"\[!NOTE|\[!IMPORTANT|\[!TIP");
        private static readonly Regex CodeIncludePattern = new(@"\[!code-cpp|\[!code-csharp|\[!code-vb");

        private static readonly Regex UnparseableMarkdown = new(string.Join('|', new[] {
            IncludeFilePattern.ToString(),
            CalloutPattern.ToString(),
            CodeIncludePattern.ToString()
        }));

        protected override string? ParseTextLine(string line) =>
            RemarksHeaderPattern.IsMatch(line) ? null : line;

        protected override XNode? ParseNode(XNode node)
        {
            if (node is XElement element && element.Name == "format" && element.Attribute("type")?.Value == "text/markdown")
            {
                var formattedText = (element.FirstNode is XCData cdata) ? cdata.Value : element.Value;

                formattedText = ExtractExamples(formattedText);

                if (!UnparseableMarkdown.IsMatch(formattedText))
                {
                    node = new XText(formattedText);
                }
            }

            return base.ParseNode(node);
        }

        private string ExtractExamples(string remarks)
        {
            var match = ExampleSectionPattern.Match(remarks);

            if (match.Success)
            {
                string exampleContent = match.Groups["examples"].Value;
                string exampleXml = $@"<example><format type=""text/markdown""><![CDATA[
{exampleContent}
]]></format></example>";

                ExampleContent = new DocsExample(XElement.Parse(exampleXml));
                return remarks.Substring(0, match.Index);
            }

            return remarks;
        }
    }
}
