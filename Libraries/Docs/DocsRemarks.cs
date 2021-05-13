using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsRemarks : DocsTextElement
    {
        public DocsTextFormat Format { get; private set; }

        public DocsRemarks(XElement xeRemarks) : base(xeRemarks)
        {
        }

        protected override IEnumerable<XNode> ParseNodes(IEnumerable<XNode> nodes) =>
            base.ParseNodes(nodes).Select(CheckForMarkdown);

        private XNode CheckForMarkdown(XNode node)
        {
            if (node is XElement element && element.Name == "format")
            {
                if (element.Attribute("type")?.Value == "text/markdown")
                {
                    Format = DocsTextFormat.Markdown;
                }

                if (element.FirstNode is XCData cdata)
                {
                    return new XText(cdata.Value);
                }

                return element;
            }

            return node;
        }

        protected override IQueryable<string> ParseTextLines(IQueryable<string> lines) =>
            RemoveRemarksHeader(base.ParseTextLines(lines));

        private static readonly Regex RemarksHeaderPattern = new(@"^##\s*Remarks\s*$", RegexOptions.IgnoreCase);

        private static IQueryable<string> RemoveRemarksHeader(IQueryable<string> lines) =>
            lines.Where(l => !RemarksHeaderPattern.IsMatch(l));
    }
}
