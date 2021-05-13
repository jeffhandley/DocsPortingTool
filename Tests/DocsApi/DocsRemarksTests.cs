using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsRemarksTests
    {
        [Theory]
        [InlineData(
            @"<remarks>Remarks.</remarks>",
            @"Remarks.")]
        [InlineData(
            @"<remarks>Remarks referencing <see cref=""T:System.Int32"" />.</remarks>",
            @"Remarks referencing <see cref=""T:System.Int32"" />.")]
        [InlineData(
            @"<remarks>
                Multiline
                Remarks
                Referencing
                <see cref=""T:System.Int32"" />.
            </remarks>",
            @"
                Multiline
                Remarks
                Referencing
                <see cref=""T:System.Int32"" />.
            ")]
        public void GetsRawText(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.RawText);
        }

        [Theory]
        [InlineData(
            @"<remarks>Remarks.</remarks>",
            @"Remarks.")]
        [InlineData(
            @"<remarks>Remarks referencing <see cref=""T:System.Int32"" />.</remarks>",
            @"Remarks referencing <see cref=""int"" />.")]
        [InlineData(
            @"<remarks>
                Multiline

                Remarks

                Referencing

                <see cref=""T:System.Int32"" />

                With Blank Lines.
            </remarks>",
            @"Multiline
Remarks
Referencing
<see cref=""int"" />
With Blank Lines.")]
        [InlineData(
            @"<remarks>
                <format type=""text/markdown""><![CDATA[
                ## Remarks
                Markdown remarks
                ]]></format>
            </remarks>",
            @"Markdown remarks")]
        public void GetsParsedText(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ParsedText);
        }

        [Fact]
        public void GetsNodes()
        {
            var xml = @"<remarks>Remarks referencing a <see cref=""T:System.Type"" />.</remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Remarks referencing a "),
                XElement.Parse(@"<see cref=""T:System.Type"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), remarks.RawNodes.ToArray().Select(x => x.ToString()));
        }

        [Fact]
        public void CanIncludeSeeElements()
        {
            var xml = @"<remarks><see cref=""T:System.Type"" /></remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));
            var see = remarks.RawNodes.Single();

            Assert.Equal(XmlNodeType.Element, see.NodeType);
        }

        [Fact]
        public void CanExposeRawSeeElements()
        {
            var xml = @"<remarks><see cref=""T:System.Type"" /></remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));
            var see = remarks.RawNodes.Single();

            Assert.Equal("see", ((XElement)see).Name);
        }

        [Fact]
        public void CanExposeRawSeeCrefValues()
        {
            var xml = @"<remarks><see cref=""T:System.Type"" /></remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));
            var see = remarks.RawNodes.Single();

            Assert.Equal("T:System.Type", ((XElement)see).Attribute("cref").Value);
        }

        [Fact]
        public void ParsesNodes()
        {
            var xml = @"<remarks>Remarks referencing a <see cref=""T:System.Collections.Generic.IEnumerable`1"" />.</remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Remarks referencing a "),
                XElement.Parse(@"<see cref=""System.Collections.Generic.IEnumerable{T}"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), remarks.ParsedNodes.ToArray().Select(x => x.ToString()));
        }
    }
}
