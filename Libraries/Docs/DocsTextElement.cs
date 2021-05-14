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

        public string RawText { get; private init; }

        public IEnumerable<XNode> RawNodes { get; private init; }

        public IEnumerable<string> ParsedNodes { get; private init; }

        public string ParsedText { get; private init; }

        public DocsTextElement(XElement element)
        {
            Element = element;
            RawNodes = element.Nodes();
            RawText = string.Join("", RawNodes);

            // Clone the element for non-mutating parsing
            var cloned = XElement.Parse(Element.ToString()).Nodes();

            // Parse each node and filter out nulls, building a block of text
            ParsedNodes = cloned.Select(ParseNode).OfType<string>();
            var allNodeContent = string.Join("", ParsedNodes);

            var lines = allNodeContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            ParsedText = string.Join(Environment.NewLine, lines);
        }

        protected virtual string? ParseNode(XNode node) =>
            RewriteDocReferences(node).ToString();

        protected static XNode RewriteDocReferences(XNode node)
        {
            if (node is XElement element && element.Name == "see")
            {
                var cref = element.Attribute("cref");

                if (cref is not null)
                {
                    var apiReference = new DocsApiReference(cref.Value);
                    cref.SetValue(apiReference.Api);
                }
            }

            return node;
        }
    }
}
