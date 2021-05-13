using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsRemarks : DocsTextElement
    {
        public DocsTextFormat OriginalFormat { get; private set; }
        public DocsTextFormat ParsedFormat { get; private set; }

        public DocsRemarks(XElement xeRemarks) : base(xeRemarks)
        {
        }

        protected override XNode? ParseNode(XNode node) =>
            CheckForMarkdown(base.ParseNode(node));

        private static readonly Regex RemarksHeaderPattern = new(@"^\s*##\s*Remarks\s*$", RegexOptions.IgnoreCase);

        protected override string? ParseTextLine(string line)
        {
            if (RemarksHeaderPattern.IsMatch(line))
            {
                return null;
            }

            return line;
        }

        private XNode? CheckForMarkdown(XNode? node)
        {
            if (node is XElement element && element.Name == "format")
            {
                if (element.Attribute("type")?.Value == "text/markdown")
                {
                    OriginalFormat = DocsTextFormat.Markdown;
                }

                if (element.FirstNode is XCData cdata)
                {
                    return new XText(cdata.Value);
                }

                return element;
            }

            return node;
        }


    }
}
