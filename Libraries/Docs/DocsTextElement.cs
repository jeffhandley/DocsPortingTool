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
                    var cloned = XElement.Parse(Element.ToString()).Nodes();

                    // Parse each node and filter out nulls
                    _parsedNodes = cloned.Select(ParseNode).OfType<XNode>();
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
                    IEnumerable<string> lines = JoinNodes(ParsedNodes).Split(Environment.NewLine);

                    // Parse each line and filter out nulls
                    lines = ParseTextLines(lines.Select(ParseTextLine));

                    _parsedText = string.Join(Environment.NewLine, lines);
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

        protected virtual XNode? ParseNode(XNode node) =>
            RewriteDocReferences(node);

        protected virtual IEnumerable<string> ParseTextLines(IEnumerable<string?> lines) =>
            lines.Where(line => !string.IsNullOrWhiteSpace(line)).OfType<string>().Select(line => line.Trim());

        protected virtual string? ParseTextLine(string line) => line;

        private static string JoinNodes(IEnumerable<XNode> nodes) =>
            string.Join("", nodes);

        //public IEnumerable<DocsTextBlock> Parse(XNode node)
        //{
        //    DocsTextFormat format = DocsTextFormat.PlainText;
        //    string text;

        //    if (node is XElement element && element.Name == "format")
        //    {
        //        if (element.Attribute("type")?.Value == "text/markdown")
        //        {
        //            format = DocsTextFormat.Markdown;
        //        }

        //        text = (element.FirstNode is XCData cdata) ? cdata.Value : element.Value;
        //    }
        //    else
        //    {
        //        text = node.ToString();
        //    }

        //    return (format == DocsTextFormat.Markdown) ? ParseMarkdown(text) : ParseText(text);
        //}

        protected static XNode RewriteDocReferences(XNode node)
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
