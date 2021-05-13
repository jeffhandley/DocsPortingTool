using System.Xml.Linq;

namespace Libraries.Docs
{
    public abstract class DocsTextElement
    {
        private readonly XElement Element;

        public DocsTextFormat Format { get; private init; }

        public string RawText { get; private init; }

        public string ParsedText { get; private init; }

        public DocsTextElement(XElement element)
        {
            Element = element;
            Format = DocsTextFormat.PlainText;
            RawText = XmlHelper.GetNodesInPlainText(Element);
            ParsedText = RawText;
        }
    }
}
