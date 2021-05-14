using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsExample : DocsMarkdownElement
    {
        public DocsExample(XElement xeExample) : base(xeExample)
        {
        }

        protected override bool TryParseMarkdown(string markdown, [NotNullWhen(true)] out string? parsed)
        {
            parsed = RemoveMarkdownHeading(markdown, "Examples?");

            return base.TryParseMarkdown(parsed, out parsed);
        }
    }
}
