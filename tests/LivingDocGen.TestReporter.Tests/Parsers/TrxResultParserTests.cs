using LivingDocGen.TestReporter.Parsers;
using LivingDocGen.TestReporter.Models;
using System.Xml;
using Xunit;

namespace LivingDocGen.TestReporter.Tests.Parsers;

public class TrxResultParserTests
{
    [Fact]
    public void Parse_ValidTrxFile_ReturnsTestResults()
    {
        // Arrange
        var parser = new TrxResultParser();
        var trxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"">
  <Times start=""2026-01-22T10:00:00"" finish=""2026-01-22T10:05:00"" />
  <TestDefinitions>
    <UnitTest id=""test1"" name=""LoginSuccessful"">
      <TestMethod className=""LoginFeature"" />
    </UnitTest>
    <UnitTest id=""test2"" name=""LoginFailed"">
      <TestMethod className=""LoginFeature"" />
    </UnitTest>
    <UnitTest id=""test3"" name=""LoginSkipped"">
      <TestMethod className=""LoginFeature"" />
    </UnitTest>
  </TestDefinitions>
  <Results>
    <UnitTestResult testId=""test1"" testName=""LoginSuccessful"" outcome=""Passed"" duration=""00:00:01.234"" />
    <UnitTestResult testId=""test2"" testName=""LoginFailed"" outcome=""Failed"" duration=""00:00:00.567"">
      <Output>
        <ErrorInfo>
          <Message>Expected true but got false</Message>
        </ErrorInfo>
      </Output>
    </UnitTestResult>
    <UnitTestResult testId=""test3"" testName=""LoginSkipped"" outcome=""NotExecuted"" />
  </Results>
</TestRun>";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, trxContent);

        try
        {
            // Act
            var report = parser.Parse(tempFile);

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.Features);
            Assert.Equal(TestFramework.MSTest, report.Framework);
            
            var feature = report.Features.First();
            Assert.Equal(3, feature.Scenarios.Count);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_EmptyTrxFile_ReturnsEmptyReport()
    {
        // Arrange
        var parser = new TrxResultParser();
        var trxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestRun xmlns=""http://microsoft.com/schemas/VisualStudio/TeamTest/2010"">
  <Times start=""2026-01-22T10:00:00"" finish=""2026-01-22T10:05:00"" />
  <Results>
  </Results>
</TestRun>";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, trxContent);

        try
        {
            // Act
            var report = parser.Parse(tempFile);

            // Assert
            Assert.NotNull(report);
            Assert.Empty(report.Features);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_InvalidXml_ThrowsException()
    {
        // Arrange
        var parser = new TrxResultParser();
        var invalidContent = "This is not XML";
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, invalidContent);

        try
        {
            // Act & Assert
            Assert.Throws<XmlException>(() => parser.Parse(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_NonExistentFile_ThrowsException()
    {
        // Arrange
        var parser = new TrxResultParser();
        var nonExistentFile = "nonexistent.trx";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => parser.Parse(nonExistentFile));
    }
}
