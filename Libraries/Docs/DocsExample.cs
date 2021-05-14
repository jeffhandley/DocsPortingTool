using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsExample : DocsTextElement
    {
        public DocsExample(XElement xeExample) : base(xeExample)
        {
        }

        private static readonly Regex ExampleHeaderPattern = new(@"^\s*##\s*Examples?\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex IncludeFilePattern = new(@"\[!INCLUDE");
        private static readonly Regex CalloutPattern = new(@"\[!NOTE|\[!IMPORTANT|\[!TIP");
        private static readonly Regex CodeIncludePattern = new(@"\[!code-cpp|\[!code-csharp|\[!code-vb");

        private static readonly Regex UnparseableMarkdown = new(string.Join('|', new[] {
            IncludeFilePattern.ToString(),
            CalloutPattern.ToString(),
            CodeIncludePattern.ToString()
        }));

        protected override string? ParseTextLine(string line) =>
            ExampleHeaderPattern.IsMatch(line) ? null : line;

        protected override XNode? ParseNode(XNode node)
        {
            if (node is XElement element && element.Name == "format" && element.Attribute("type")?.Value == "text/markdown")
            {
                var formattedText = (element.FirstNode is XCData cdata) ? cdata.Value : element.Value;

                if (!UnparseableMarkdown.IsMatch(formattedText))
                {
                    node = new XText(formattedText);
                }
            }

            return base.ParseNode(node);
        }
    }
}
