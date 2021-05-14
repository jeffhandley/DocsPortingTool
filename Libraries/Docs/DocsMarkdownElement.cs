using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public abstract class DocsMarkdownElement : DocsTextElement
    {
        public DocsMarkdownElement(XElement xeRemarks) : base(xeRemarks)
        {
        }

        private static readonly Regex IncludeFilePattern = new(@"\[!INCLUDE");
        private static readonly Regex CalloutPattern = new(@"\[!NOTE|\[!IMPORTANT|\[!TIP");
        private static readonly Regex CodeIncludePattern = new(@"\[!code-cpp|\[!code-csharp|\[!code-vb");

        private static readonly Regex UnparseableMarkdown = new(string.Join('|', new[] {
            IncludeFilePattern.ToString(),
            CalloutPattern.ToString(),
            CodeIncludePattern.ToString()
        }));

        protected override XNode? ParseNode(XNode node)
        {
            if (node is XElement element && element.Name == "format" && element.Attribute("type")?.Value == "text/markdown")
            {
                var markdown = (element.FirstNode is XCData cdata) ? cdata.Value : element.Value;

                if (TryParseMarkdown(markdown, out var parsedText))
                {
                    node = new XText(parsedText);
                }
            }

            return base.ParseNode(node);
        }

        protected virtual bool TryParseMarkdown(string markdown, [NotNullWhen(true)] out string? parsed)
        {
            if (UnparseableMarkdown.IsMatch(markdown))
            {
                parsed = null;
                return false;
            }

            parsed = markdown;
            return true;
        }
    }
}
