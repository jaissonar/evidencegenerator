using System.IO.Compression;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using EvidenceGenerator.Api.Documents;
using EvidenceGenerator.Api.Excel;

namespace EvidenceGenerator.Tests;

public class DocumentTests
{
    private static string Template => Path.Combine(AppContext.BaseDirectory, "Templates", "PruebasUnitarias.v1.xlsx");
    private const string Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aWQAAAABJRU5ErkJggg==";
    public static EvidenceDocument Sample(int count = 2) => new(Guid.NewGuid(), 0, "MD 12345", "Validación de traducción", "Responsable", "Cliente", new DateOnly(2026, 10, 7),
        new("https://example.com/sql", "SqlTest", new(Png), Enumerable.Range(1, count).Select(i => new Evidence(Guid.NewGuid().ToString(), $"Evidencia nueva {i}", [new(Png)])).ToList()),
        new("https://example.com/oracle", "OracleTest", null, []), new("Nicolás", "", "", "", 11, "", "", null));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(17)]
    public void ExportPreservesInstitutionalPartsAndProducesValidOpenXml(int count)
    {
        var result = new TemplateExcelGenerator(Template).Generate(Sample(count));
        using var source = ZipFile.OpenRead(Template);
        using var generated = new ZipArchive(new MemoryStream(result));
        Assert.Equal(Bytes(source, "xl/worksheets/sheet1.xml"), Bytes(generated, "xl/worksheets/sheet1.xml"));
        Assert.Equal(Bytes(source, "xl/styles.xml"), Bytes(generated, "xl/styles.xml"));
        Assert.Equal(Bytes(source, "xl/drawings/drawing1.xml"), Bytes(generated, "xl/drawings/drawing1.xml"));
        var xml = System.Text.Encoding.UTF8.GetString(Bytes(generated, "xl/worksheets/sheet2.xml"));
        Assert.Contains("MD 12345", xml); Assert.Contains("https://example.com/sql", xml);
        Assert.Contains("Validación de traducción", xml);
        if (count > 0) Assert.Contains($"Evidencia nueva {count}", xml);
        var shared = System.Text.Encoding.UTF8.GetString(Bytes(generated, "xl/sharedStrings.xml"));
        Assert.DoesNotContain("Novelties Freight", shared);
        using var originalPackage = SpreadsheetDocument.Open(Template, false);
        using var package = SpreadsheetDocument.Open(new MemoryStream(result), false);
        var validator = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Microsoft365);
        var baseline = validator.Validate(originalPackage).Select(e => e.Description).ToHashSet();
        var errors = validator.Validate(package).Where(e => !baseline.Contains(e.Description)).Select(e => $"{e.Path?.XPath}: {e.Description}").ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors));
        XNamespace d = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
        var drawing = XDocument.Load(new MemoryStream(Bytes(generated, "xl/drawings/drawing2.xml")));
        Assert.Equal(count + 2, drawing.Root!.Elements().Count());
        Assert.Equal(count + 2, drawing.Root.Elements(d + "twoCellAnchor").Count());
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(new MemoryStream(Bytes(generated, "xl/worksheets/sheet2.xml")));
        Assert.Equal("Plan de Pruebas", sheet.Descendants(s + "c").Single(c => (string?)c.Attribute("r") == "A60").Value);
        foreach (var anchor in drawing.Root.Elements().Skip(1))
        {
            var from = (int)anchor.Element(d + "from")!.Element(d + "row")!;
            var to = (int)anchor.Element(d + "to")!.Element(d + "row")!;
            Assert.Equal(42, to - from);
            Assert.Equal(0, (int)anchor.Element(d + "to")!.Element(d + "rowOff")!);
            var line = anchor.Descendants(a + "ln").Single();
            Assert.Equal(12700, (int)line.Attribute("w")!);
            Assert.Equal("252932", (string?)line.Descendants(a + "srgbClr").Single().Attribute("val"));
            var original = XDocument.Load(new MemoryStream(Bytes(source, "xl/worksheets/sheet2.xml")));
            var measuredHeight = AssertOriginalRowHeights(original, sheet, from) * 12700;
            var extent = anchor.Descendants(a + "ext").Single();
            Assert.InRange(Math.Abs(measuredHeight - (long)extent.Attribute("cy")!), 0, 1);
        }
    }

    [Fact]
    public void SeparateEvidenceBlocksTreatTextAsTextAndRejectMultipleImages()
    {
        var doc = Sample(1);
        doc.Sql.Items[0] = doc.Sql.Items[0] with { Images = [new(Png), new(Png)] };
        Assert.NotNull(DocumentValidation.Validate(doc));
        doc.Sql.Items[0] = doc.Sql.Items[0] with { Images = [new(Png)] };
        doc.Sql.Items.Add(new("second", "=HYPERLINK(\"https://example.org\")", [new(Png)]));
        using var generated = new ZipArchive(new MemoryStream(new TemplateExcelGenerator(Template).Generate(doc)));
        XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheet = XDocument.Load(new MemoryStream(Bytes(generated, "xl/worksheets/sheet2.xml")));
        var second = sheet.Descendants(s + "c").Single(c => (string?)c.Attribute("r") == "A110");
        Assert.Equal("inlineStr", (string?)second.Attribute("t"));
        Assert.Empty(second.Elements(s + "f")); Assert.Contains("=HYPERLINK", second.Value);
    }

    [Theory]
    [InlineData("iVBORw0KGgoAAAANSUhEUgAAAAQAAAABCAIAAAB2XpiaAAAADklEQVR4nGOUiE5mgAEACQEA2KxF1voAAAAASUVORK5CYII=", 4d)]
    [InlineData("iVBORw0KGgoAAAANSUhEUgAAAAEAAAAECAIAAADAusJtAAAAEUlEQVR4nGOQiE5mYmBggGEAC6kA3Sje4b8AAAAASUVORK5CYII=", 0.25d)]
    public void OracleImagesFill42OriginalRowsForWideOrTallImages(string png, double ratio)
    {
        var doc = Sample(0);
        var image = new EvidenceImage("data:image/png;base64," + png);
        doc.Oracle.Items.Add(new Evidence("oracle", "Validación Oracle", [image]));
        doc = doc with { Oracle = doc.Oracle with { LoginImage = image } };
        using var archive = new ZipArchive(new MemoryStream(new TemplateExcelGenerator(Template).Generate(doc)));
        XNamespace d = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var drawing = XDocument.Load(new MemoryStream(Bytes(archive, "xl/drawings/drawing3.xml")));
        var sheet = XDocument.Load(new MemoryStream(Bytes(archive, "xl/worksheets/sheet3.xml")));
        using var source = ZipFile.OpenRead(Template);
        var original = XDocument.Load(new MemoryStream(Bytes(source, "xl/worksheets/sheet3.xml")));
        Assert.Equal("Plan de Pruebas", sheet.Descendants(s + "c").Single(c => (string?)c.Attribute("r") == "A60").Value);
        foreach (var anchor in drawing.Root!.Elements().Skip(1))
        {
            Assert.Equal(42, (int)anchor.Element(d + "to")!.Element(d + "row")! - (int)anchor.Element(d + "from")!.Element(d + "row")!);
            var size = anchor.Descendants(a + "ext").Single();
            var from = (int)anchor.Element(d + "from")!.Element(d + "row")!;
            var expectedHeight = AssertOriginalRowHeights(original, sheet, from) * 12700;
            Assert.Equal((long)Math.Round(expectedHeight), (long)size.Attribute("cy")!);
            Assert.Equal((long)Math.Round(expectedHeight * ratio), (long)size.Attribute("cx")!);
            Assert.Equal(1, (int)anchor.Descendants(a + "picLocks").Single().Attribute("noChangeAspect")!);
            var to = anchor.Element(d + "to")!;
            var endColumn = (int)to.Element(d + "col")!;
            double measuredWidth = (long)to.Element(d + "colOff")!;
            foreach (var column in Enumerable.Range(1, endColumn))
            {
                var definition = sheet.Root!.Element(s + "cols")!.Elements().FirstOrDefault(c =>
                    (int)c.Attribute("min")! <= column && (int)c.Attribute("max")! >= column);
                var defaultWidth = (double?)sheet.Root.Element(s + "sheetFormatPr")?.Attribute("defaultColWidth") ?? 8.43;
                measuredWidth += (string?)definition?.Attribute("hidden") == "1" ? 0 : (((double?)definition?.Attribute("width") ?? defaultWidth) * 7 + 5) * 9525;
            }
            Assert.InRange(Math.Abs(measuredWidth - (long)size.Attribute("cx")!), 0, 1);
            if (ratio > 1) Assert.True(endColumn >= 14);
            Assert.Equal(12700, (int)anchor.Descendants(a + "ln").Single().Attribute("w")!);
        }
        var book = XDocument.Load(new MemoryStream(Bytes(archive, "xl/workbook.xml")));
        var area = book.Descendants(s + "definedName").Single(n => (string?)n.Attribute("name") == "_xlnm.Print_Area" && (int?)n.Attribute("localSheetId") == 2).Value;
        var lastColumnName = System.Text.RegularExpressions.Regex.Match(area, @":\$([A-Z]+)\$").Groups[1].Value;
        var lastColumn = lastColumnName.Aggregate(0, (value, letter) => value * 26 + letter - 'A' + 1);
        foreach (var anchor in drawing.Root.Elements().Skip(1))
        {
            var to = anchor.Element(d + "to")!;
            Assert.True(lastColumn >= (int)to.Element(d + "col")! + ((long)to.Element(d + "colOff")! > 0 ? 1 : 0));
        }
    }

    [Fact]
    public void StoreRejectsStaleRevisionsAndRecoversImages()
    {
        var directory = Path.Combine(Path.GetTempPath(), "evidence-test-" + Guid.NewGuid());
        try
        {
            var store = new DocumentStore(directory); var doc = Sample();
            var first = store.Save(doc)!;
            Assert.Equal(1, first.Revision); Assert.Null(store.Save(doc));
            var next = store.Save(first with { Description = "Cambio" })!;
            Assert.Equal(2, next.Revision); Assert.Null(store.Save(first));
            var recovered = new DocumentStore(directory).Get(doc.Id)!;
            Assert.Equal("Cambio", recovered.Description); Assert.Equal(Png, recovered.Sql.Items[0].Images[0].DataUrl);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/secret")]
    [InlineData("not-a-url")]
    public void UnsafeSitesAreRejected(string url) => Assert.NotNull(DocumentValidation.Validate(Sample() with { Sql = Sample().Sql with { Url = url } }));

    [Fact]
    public void BrokenImagesAndExcessiveEvidenceAreRejected()
    {
        Assert.NotNull(DocumentValidation.Validate(Sample(51)));
        Assert.False(DocumentValidation.ValidImage(new("data:image/png;base64,garbage")));
        Assert.NotNull(DocumentValidation.Validate(Sample() with { Mail = Sample().Mail with { FontSize = 99 } }));
    }
    private static double AssertOriginalRowHeights(XDocument original, XDocument generated, int from)
    {
        XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var defaultHeight = (double?)original.Root!.Element(s + "sheetFormatPr")?.Attribute("defaultRowHeight") ?? 15;
        double total = 0;
        foreach (var position in Enumerable.Range(from + 1, 42))
        {
            var sourcePosition = position < 63 ? position : 63 + (position - 63) % 47;
            var sourceRow = original.Descendants(s + "row").SingleOrDefault(r => (int)r.Attribute("r")! == sourcePosition);
            var outputRow = generated.Descendants(s + "row").SingleOrDefault(r => (int)r.Attribute("r")! == position);
            Assert.Equal((string?)sourceRow?.Attribute("ht"), (string?)outputRow?.Attribute("ht"));
            Assert.Equal((string?)sourceRow?.Attribute("customHeight"), (string?)outputRow?.Attribute("customHeight"));
            total += (double?)sourceRow?.Attribute("ht") ?? defaultHeight;
        }
        return total;
    }

    private static byte[] Bytes(ZipArchive zip, string path) { using var stream = zip.GetEntry(path)!.Open(); using var buffer = new MemoryStream(); stream.CopyTo(buffer); return buffer.ToArray(); }
}

