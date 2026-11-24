using System.IO.Compression;
using System.Security;
using System.Text;

namespace PeopleHub.Web.Services;

/// <summary>Minimal .xlsx writer (one sheet, inline strings, numbers detected). No third-party packages.</summary>
public static class XlsxWriter
{
    private static string Col(int i) { var s = ""; for (i++; i > 0; i = (i - 1) / 26) s = (char)('A' + (i - 1) % 26) + s; return s; }

    public static byte[] Build(string sheet, List<string> columns, List<List<string>> rows)
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView rightToLeft=\"0\"/></sheetViews><sheetData>");
        void Row(int n, IEnumerable<string> cells, bool head)
        {
            sb.Append($"<row r=\"{n}\">"); var c = 0;
            foreach (var v in cells)
            {
                var r = Col(c++) + n;
                if (!head && decimal.TryParse(v.Replace(",", ""), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) && !v.StartsWith('0') || !head && v == "0")
                    sb.Append($"<c r=\"{r}\"><v>{decimal.Parse(v.Replace(",", ""), System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture)}</v></c>");
                else sb.Append($"<c r=\"{r}\" t=\"inlineStr\"{(head ? " s=\"1\"" : "")}><is><t xml:space=\"preserve\">{SecurityElement.Escape(v)}</t></is></c>");
            }
            sb.Append("</row>");
        }
        Row(1, columns, true);
        for (var i = 0; i < rows.Count; i++) Row(i + 2, rows[i], false);
        sb.Append("</sheetData></worksheet>");
        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            void Add(string name, string xml) { var e = z.CreateEntry(name); using var w = new StreamWriter(e.Open(), new UTF8Encoding(false)); w.Write(xml); }
            Add("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
            Add("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add("xl/workbook.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"{SecurityElement.Escape(new string(sheet.Where(char.IsLetterOrDigit).ToArray()) is { Length: > 0 } n ? n : "Report")}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add("xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            Add("xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"2\"><xf/><xf fontId=\"1\" applyFont=\"1\"/></cellXfs></styleSheet>");
            Add("xl/worksheets/sheet1.xml", sb.ToString());
        }
        return ms.ToArray();
    }
}
