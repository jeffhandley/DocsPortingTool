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

        public IEnumerable<XNode> ParsedNodes { get; private init; }

        public string ParsedText { get; private init; }

        public DocsTextElement(XElement element)
        {
            Element = element;
            RawNodes = element.Nodes();
            RawText = JoinNodes(RawNodes);

            // Clone the element for non-mutating parsing
            var cloned = XElement.Parse(Element.ToString()).Nodes();

            // Parse each node and filter out nulls, building a block of text
            ParsedNodes = cloned.Select(ParseNode).OfType<XNode>();
            var allNodeContent = JoinNodes(ParsedNodes);
            
            // Parse each line, filter out blank lines, and then trim each line of content
            IEnumerable<string> lines = allNodeContent
                .Split(Environment.NewLine)
                .Select(ParseTextLine)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line!.Trim());

            ParsedText = string.Join(Environment.NewLine, lines);
        }

        protected virtual XNode? ParseNode(XNode node) =>
            RewriteDocReferences(node);

        protected virtual string? ParseTextLine(string line) => line;

        private static string JoinNodes(IEnumerable<XNode> nodes) =>
            string.Join("", nodes);

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
