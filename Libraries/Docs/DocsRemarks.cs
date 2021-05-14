using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsRemarks : DocsMarkdownElement
    {
        public DocsRemarks(XElement xeRemarks) : base(xeRemarks)
        {
        }

        public DocsExample? ExampleContent { get; private set; }

        private static readonly Regex RemarksHeaderPattern = new(@"^\s*##\s*Remarks\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex ExampleSectionPattern = new(@"^\s*##\s*Examples?\s*(?<examples>.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        protected override string? ParseTextLine(string line) =>
            RemarksHeaderPattern.IsMatch(line) ? null : line;

        protected override bool TryParseMarkdown(string markdown, [NotNullWhen(true)] out string? parsed)
        {
            var remarks = ExtractExamples(markdown);

            return base.TryParseMarkdown(remarks, out parsed);
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
