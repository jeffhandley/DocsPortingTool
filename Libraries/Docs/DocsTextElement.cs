using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public abstract class DocsTextElement
    {
        private readonly XElement Element;
        private IEnumerable<XNode>? _parsedNodes;
        private string? _parsedText;

        public string RawText { get; private init; }

        public IEnumerable<XNode> RawNodes { get; private init; }

        public IEnumerable<XNode> ParsedNodes
        {
            get
            {
                if (_parsedNodes is null)
                {
                    // Clone the element for non-mutating parsing
                    XElement cloned = XElement.Parse(Element.ToString());
                    _parsedNodes = ParseNodes(cloned.Nodes());
                }

                return _parsedNodes;
            }
        }

        public string ParsedText
        {
            get
            {
                if (_parsedText is null)
                {
                    var lines = JoinNodes(ParsedNodes).Split(Environment.NewLine, StringSplitOptions.TrimEntries).AsQueryable();
                    lines = ParseTextLines(lines);

                    _parsedText = JoinLines(lines);
                }

                return _parsedText;
            }
        }

        public DocsTextElement(XElement element)
        {
            Element = element;
            RawNodes = Element.Nodes();
            RawText = JoinNodes(RawNodes);
        }

        protected virtual IEnumerable<XNode> ParseNodes(IEnumerable<XNode> nodes) =>
            nodes.Select(FormatDocReference);

        protected virtual IQueryable<string> ParseTextLines(IQueryable<string> lines) =>
            lines.Where(line => !string.IsNullOrWhiteSpace(line));

        protected static string JoinNodes(IEnumerable<XNode> nodes) =>
            string.Join("", nodes);

        protected static string JoinLines(IEnumerable<string> lines) =>
            string.Join(Environment.NewLine, lines);

        private static XNode FormatDocReference(XNode node)
        {
            if (node is XElement element)
            {
                if (element.Name == "see")
                {
                    var cref = element.Attribute("cref");

                    if (cref is not null)
                    {
                        var apiReference = new DocsApiReference(cref.Value);
                        cref.SetValue(apiReference.Api);
                    }
                }
            }

            return node;
        }
    }
}
