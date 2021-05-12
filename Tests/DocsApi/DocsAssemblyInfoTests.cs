using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsAssemblyInfoTests
    {
        [Fact]
        public void ExtractsAssemblyName()
        {
            var assembly = new DocsAssemblyInfo(XElement.Parse(@"
                <AssemblyInfo>
                    <AssemblyName>MyAssembly</AssemblyName>
                </AssemblyInfo>"
            ));

            Assert.Equal("MyAssembly", assembly.AssemblyName);
        }

        [Fact]
        public void ExtractsOneAssemblyVersion()
        {
            var assembly = new DocsAssemblyInfo(XElement.Parse(@"
                <AssemblyInfo>
                    <AssemblyVersion>4.0.0.0</AssemblyVersion>
                </AssemblyInfo>"
            ));

            Assert.Equal(new string[] { "4.0.0.0" }, assembly.AssemblyVersions);
        }

        [Fact]
        public void ExtractsMultipleAssemblyVersions()
        {
            var assembly = new DocsAssemblyInfo(XElement.Parse(@"
                <AssemblyInfo>
                    <AssemblyVersion>4.0.0.0</AssemblyVersion>
                    <AssemblyVersion>5.0.0.0</AssemblyVersion>
                </AssemblyInfo>"
            ));

            Assert.Equal(new string[] { "4.0.0.0", "5.0.0.0" }, assembly.AssemblyVersions);
        }
    }
}
