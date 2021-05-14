using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsExample : DocsMarkdownElement
    {
        public DocsExample(XElement xeExample) : base(xeExample)
        {
        }

        private static readonly Regex ExampleHeaderPattern = new(@"^\s*##\s*Examples?\s*$", RegexOptions.IgnoreCase);

        protected override string? ParseTextLine(string line) =>
            ExampleHeaderPattern.IsMatch(line) ? null : line;
    }
}
