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

        private static readonly Regex ExampleSectionPattern = new(@"^\s*##\s*Examples?\s*(?<examples>.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        protected override bool TryParseMarkdown(string markdown, [NotNullWhen(true)] out string? parsed)
        {
            markdown = RemoveMarkdownHeading(markdown, "Remarks");
            markdown = ExtractExamples(markdown);

            return base.TryParseMarkdown(markdown, out parsed);
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
