using PeopleHub.Application.Services;
namespace PeopleHub.Tests;
public class SpreadsheetExportTests
{
 [Theory]
 [InlineData("=1+1")][InlineData("+SUM(1,1)")][InlineData("-1+1")][InlineData("@SUM(1,1)")][InlineData("   =1+1")][InlineData("\t=1+1")][InlineData("\r=1+1")][InlineData("line\n=1+1")]
 public void SpreadsheetCellsNeutralizeFormulaAndControlPrefixes(string value)=>Assert.Equal(PayrollService.Csv("'"+value),ReportService.SpreadsheetCell(value));
 [Theory][InlineData("Plain name")][InlineData("name, department")][InlineData("A \"quoted\" name")][InlineData("موظف تجريبي")][InlineData("")]
 public void OrdinaryDisplayTextOnlyUsesCsvEscaping(string value)=>Assert.Equal(PayrollService.Csv(value),ReportService.SpreadsheetCell(value));
 [Fact]public void HeadersAndRowsAreBothProtected(){var csv=ReportService.ToCsv(new("test",["=header"],[["+row"]]));Assert.Contains("\"'=header\"",csv);Assert.Contains("\"'+row\"",csv);}
 [Fact]public void BankInterchangeRemainsExactRatherThanSpreadsheetSanitized()=>Assert.Equal("\"+BANK-REF\"",PayrollService.Csv("+BANK-REF"));
}
