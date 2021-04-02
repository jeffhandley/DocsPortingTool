#nullable enable
using Libraries.RoslynTripleSlash;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Tests.PortToTripleSlash
{
    public class LeadingTriviaRewriterTests
    {
        public struct LeadingTriviaTestFile
        {
            public SyntaxNode MyType;
            public SyntaxNode MyEnum;
            public SyntaxNode MyField;
            public SyntaxNode MyProperty;
            public SyntaxNode MyMethod;
        }

        private static (LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) LoadTestFiles(string test)
        {
            Func<string, LeadingTriviaTestFile> LoadTestFile = (fileName) =>
            {
                string testFolder = "../../../PortToTripleSlash/TestData/LeadingTrivia";
                string testContent = File.ReadAllText(Path.Combine(testFolder, fileName));

                IEnumerable<SyntaxNode> nodes = SyntaxFactory.ParseSyntaxTree(testContent).GetRoot().DescendantNodes();

                return new LeadingTriviaTestFile
                {
                    MyType = nodes.First(n => n.IsKind(SyntaxKind.ClassDeclaration)),
                    MyEnum = nodes.First(n => n.IsKind(SyntaxKind.EnumDeclaration)),
                    MyField = nodes.First(n => n.IsKind(SyntaxKind.FieldDeclaration)),
                    MyProperty = nodes.First(n => n.IsKind(SyntaxKind.PropertyDeclaration)),
                    MyMethod = nodes.First(n => n.IsKind(SyntaxKind.MethodDeclaration))
                };
            };

            LeadingTriviaTestFile original = LoadTestFile($"{test}.Original.cs");
            LeadingTriviaTestFile expected = LoadTestFile($"{test}.Expected.cs");

            return (original, expected);
        }

        public static IEnumerable<object[]> GetLeadingTriviaTests()
        {
            yield return new object[] { LoadTestFiles("WhitespaceOnly") };
        }

        private static IEnumerable<SyntaxTrivia> GetTestComments()
        {
            XmlTextSyntax summaryText = SyntaxFactory.XmlText();
            XmlElementSyntax summaryElement = SyntaxFactory.XmlSummaryElement(summaryText);
            DocumentationCommentTriviaSyntax summaryComment = SyntaxFactory.DocumentationComment(summaryElement);
            SyntaxTrivia summaryTrivia = SyntaxFactory.Trivia(summaryComment);

            XmlTextSyntax remarksText = SyntaxFactory.XmlText();
            XmlElementSyntax remarksElement = SyntaxFactory.XmlRemarksElement(remarksText);
            DocumentationCommentTriviaSyntax remarksComment = SyntaxFactory.DocumentationComment(remarksElement);
            SyntaxTrivia remarksTrivia = SyntaxFactory.Trivia(remarksComment);

            return new SyntaxTrivia[] { summaryTrivia, remarksTrivia };
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToClassDeclaration((LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyType,
                GetTestComments()
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyType.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToEnumDeclaration((LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyEnum,
                GetTestComments()
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyEnum.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToFieldDeclaration((LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyField,
                GetTestComments()
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyField.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToPropertyDeclaration((LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyProperty,
                GetTestComments()
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyProperty.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetLeadingTriviaTests))]
        public void AddsXmlToMethodDeclaration((LeadingTriviaTestFile Original, LeadingTriviaTestFile Expected) test)
        {
            var actual = LeadingTriviaRewriter.ApplyXmlComments(
                test.Original.MyMethod,
                GetTestComments()
            ).GetLeadingTrivia().ToFullString();

            var expected = test.Expected.MyMethod.GetLeadingTrivia().ToFullString();

            Assert.Equal(expected, actual);
        }
    }
}