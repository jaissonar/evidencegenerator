using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EvidenceGenerator.Api.Documents;

namespace EvidenceGenerator.Api.Excel;

public interface IExcelGenerator { byte[] Generate(EvidenceDocument document); }

// Patch dynamic OOXML parts while retaining the institutional styles and logos.
public sealed class TemplateExcelGenerator(string templatePath) : IExcelGenerator
{
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace D = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace P = "http://schemas.openxmlformats.org/package/2006/relationships";

    public byte[] Generate(EvidenceDocument document)
    {
        if (DocumentValidation.Validate(document, true) is { } error) throw new ArgumentException(error);
        using var output = new MemoryStream();
        using (var source = File.OpenRead(templatePath)) source.CopyTo(output);
        using (var archive = new ZipArchive(output, ZipArchiveMode.Update, true))
        {
            var sqlEnd = FillSheet(archive, 2, document, document.Sql);
            var oracleEnd = FillSheet(archive, 3, document, document.Oracle);
            var book = Read(archive, "xl/workbook.xml");
            var names = book.Root!.Element(S + "definedNames");
            if (names is null) { names = new XElement(S + "definedNames"); var calc = book.Root.Element(S + "calcPr"); if (calc is null) book.Root.Add(names); else calc.AddBeforeSelf(names); }
            foreach (var (index, end) in new[] { (1, sqlEnd), (2, oracleEnd) })
            {
                names.Elements(S + "definedName").Where(n => (string?)n.Attribute("name") == "_xlnm.Print_Area" && (int?)n.Attribute("localSheetId") == index).Remove();
                var sheetName = book.Descendants(S + "sheet").ElementAt(index).Attribute("name")!.Value;
                names.Add(new XElement(S + "definedName", new XAttribute("name", "_xlnm.Print_Area"), new XAttribute("localSheetId", index), $"'{sheetName}'!$A$1:${ColumnName(end.Column)}${end.Row}"));
            }
            Write(archive, "xl/workbook.xml", book);
            RemoveUnusedSamples(archive);
        }
        return output.ToArray();
    }

    private static (int Row, int Column) FillSheet(ZipArchive archive, int index, EvidenceDocument doc, EvidenceSection section)
    {
        var path = $"xl/worksheets/sheet{index}.xml";
        var sheet = Read(archive, path); var root = sheet.Root!;
        var data = root.Element(S + "sheetData")!;
        var prototype = data.Elements(S + "row").Where(r => Row(r) >= 63 && Row(r) <= 109).Select(r => new XElement(r)).ToList();
        data.Elements(S + "row").Where(r => Row(r) >= 63).Remove();
        var merges = root.Element(S + "mergeCells")!;
        merges.Elements().Where(m => AddressRow(m.Attribute("ref")!.Value.Split(':')[0]) >= 63).Remove();
        SetCell(data, "A5", $"Elaborado por: {doc.Author}");
        SetCell(data, "E5", $"Fecha de elaboración: {doc.Date:dd/MM/yyyy}");
        SetCell(data, "D9", section.Url); SetCell(data, "D10", section.Connection);
        SetCell(data, "A60", "Plan de Pruebas");
        var sheetRelsPath = $"xl/worksheets/_rels/sheet{index}.xml.rels";
        var sheetRels = Read(archive, sheetRelsPath);
        foreach (var link in sheetRels.Descendants(P + "Relationship").Where(r => ((string?)r.Attribute("Type"))?.EndsWith("/hyperlink") == true)) link.SetAttributeValue("Target", section.Url);
        foreach (var link in root.Descendants(S + "hyperlink")) link.SetAttributeValue("display", section.Url);
        Write(archive, sheetRelsPath, sheetRels);
        foreach (var validation in root.Descendants(S + "dataValidation")) validation.SetAttributeValue("showErrorMessage", "0");
        var drawingPath = $"xl/drawings/drawing{index}.xml";
        var drawing = Read(archive, drawingPath);
        drawing.Root!.Elements().Where(e => (int?)e.Element(D + "from")?.Element(D + "row") != 0).Remove();
        var drawingRelsPath = $"xl/drawings/_rels/drawing{index}.xml.rels";
        var relations = Read(archive, drawingRelsPath);
        var logoIds = drawing.Descendants(A + "blip").Select(b => (string?)b.Attribute(R + "embed")).ToHashSet();
        relations.Root!.Elements().Where(r => !logoIds.Contains((string?)r.Attribute("Id"))).Remove();
        var imageNumber = 100;
        var width = root.Element(S + "cols")!.Elements(S + "col").Sum(c =>
        {
            var count = Math.Max(0, Math.Min(14, (int)c.Attribute("max")!) - Math.Max(1, (int)c.Attribute("min")!) + 1);
            return count * (((double?)c.Attribute("width") ?? 8.43) * 7 + 5);
        });
        if (section.LoginImage is not null) AddImage(archive, drawing, relations, data, root, section.LoginImage, index, ++imageNumber, 14);
        var blocks = section.Items.SelectMany(e => e.Images.Count == 0
            ? new[] { (e.Description, Image: (EvidenceImage?)null) }
            : e.Images.Select((image, i) => (Description: e.Description + (e.Images.Count > 1 ? $" ({i + 1}/{e.Images.Count})" : ""), Image: (EvidenceImage?)image))).ToList();
        if (blocks.Count == 0) blocks.Add(($"{doc.Description}\nSin evidencias registradas para este motor.", null));
        var start = 63;
        foreach (var block in blocks)
        {
            var offset = start - 63;
            foreach (var templateRow in prototype)
            {
                var row = new XElement(templateRow); row.SetAttributeValue("r", Row(row) + offset);
                foreach (var cell in row.Elements(S + "c"))
                {
                    var address = cell.Attribute("r")!.Value;
                    cell.SetAttributeValue("r", Regex.Replace(address, "[0-9]+", (AddressRow(address) + offset).ToString(CultureInfo.InvariantCulture)));
                    cell.Elements().Remove(); cell.Attribute("t")?.Remove();
                }
                data.Add(row);
            }
            merges.Add(new XElement(S + "mergeCell", new XAttribute("ref", $"A{start}:N{start + 2}")));
            var description = start == 63 ? $"{doc.Requirement}: {doc.Description}\n\n{block.Description}" : block.Description;
            SetCell(data, $"A{start}", description);
            var lines = description.Split('\n').Sum(line => Math.Max(1, (int)Math.Ceiling(line.Length / Math.Max(30, width / 8))));
            var rowHeight = Math.Max(15, Math.Ceiling(lines * 15d / 3));
            for (var r = start; r < start + 3; r++)
            {
                var row = data.Elements(S + "row").First(x => Row(x) == r);
                row.SetAttributeValue("ht", rowHeight); row.SetAttributeValue("customHeight", "1");
            }
            if (block.Image is not null) AddImage(archive, drawing, relations, data, root, block.Image, index, ++imageNumber, start + 3);
            start += 47;
        }
        merges.SetAttributeValue("count", merges.Elements().Count());
        root.Element(S + "dimension")?.SetAttributeValue("ref", $"A1:AA{start - 1}");
        var sortedRows = data.Elements().OrderBy(Row).ToArray(); data.ReplaceNodes(sortedRows);
        Write(archive, path, sheet); Write(archive, drawingPath, drawing); Write(archive, drawingRelsPath, relations);
        var lastColumn = Math.Max(14, drawing.Root.Elements().Select(anchor =>
        {
            var to = anchor.Element(D + "to");
            return ((int?)to?.Element(D + "col") ?? 0) + ((long?)to?.Element(D + "colOff") > 0 ? 1 : 0);
        }).DefaultIfEmpty(14).Max());
        return (start - 1, lastColumn);
    }

    private static void AddImage(ZipArchive archive, XDocument drawing, XDocument rels, XElement data, XElement sheetRoot, EvidenceImage image, int sheet, int number, int row)
    {
        var bytes = Convert.FromBase64String(image.DataUrl[22..]);
        var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        const int imageRows = 42;
        var defaultHeight = (double?)sheetRoot.Element(S + "sheetFormatPr")?.Attribute("defaultRowHeight") ?? 15;
        // Size the image to the existing rows; never resize rows to fit a screenshot.
        var heightPoints = Enumerable.Range(row + 1, imageRows).Sum(position =>
            (double?)data.Elements(S + "row").FirstOrDefault(r => Row(r) == position)?.Attribute("ht") ?? defaultHeight);
        var cy = (long)Math.Round(heightPoints * 12700);
        var cx = (long)Math.Round((double)cy * width / height);
        var remainingWidth = cx / 9525d;
        var endColumn = 0;
        foreach (var column in Enumerable.Range(1, 16384))
        {
            var definition = sheetRoot.Element(S + "cols")!.Elements(S + "col")
                .FirstOrDefault(c => (int)c.Attribute("min")! <= column && (int)c.Attribute("max")! >= column);
            var defaultWidth = (double?)sheetRoot.Element(S + "sheetFormatPr")?.Attribute("defaultColWidth") ?? 8.43;
            var columnWidth = (string?)definition?.Attribute("hidden") == "1" ? 0 : (((double?)definition?.Attribute("width") ?? defaultWidth) * 7 + 5);
            if (remainingWidth < columnWidth) break;
            remainingWidth -= columnWidth;
            endColumn++;
        }
        var filename = $"evidence-{sheet}-{number}.png"; var id = $"evidence{number}";
        using (var stream = archive.CreateEntry($"xl/media/{filename}").Open()) stream.Write(bytes);
        rels.Root!.Add(new XElement(P + "Relationship", new XAttribute("Id", id), new XAttribute("Type", R.NamespaceName + "/image"), new XAttribute("Target", $"../media/{filename}")));
        drawing.Root!.Add(new XElement(D + "twoCellAnchor", new XAttribute("editAs", "twoCell"),
            new XElement(D + "from", new XElement(D + "col", 0), new XElement(D + "colOff", 0), new XElement(D + "row", row), new XElement(D + "rowOff", 0)),
            new XElement(D + "to", new XElement(D + "col", endColumn), new XElement(D + "colOff", (long)Math.Round(Math.Max(0, remainingWidth) * 9525)), new XElement(D + "row", row + imageRows), new XElement(D + "rowOff", 0)),
            new XElement(D + "pic",
                new XElement(D + "nvPicPr", new XElement(D + "cNvPr", new XAttribute("id", number), new XAttribute("name", filename)), new XElement(D + "cNvPicPr", new XElement(A + "picLocks", new XAttribute("noChangeAspect", 1)))),
                new XElement(D + "blipFill", new XElement(A + "blip", new XAttribute(R + "embed", id)), new XElement(A + "stretch", new XElement(A + "fillRect"))),
                new XElement(D + "spPr", new XElement(A + "xfrm", new XElement(A + "off", new XAttribute("x", 0), new XAttribute("y", 0)), new XElement(A + "ext", new XAttribute("cx", cx), new XAttribute("cy", cy))), new XElement(A + "prstGeom", new XAttribute("prst", "rect"), new XElement(A + "avLst")), new XElement(A + "ln", new XAttribute("w", 12700), new XElement(A + "solidFill", new XElement(A + "srgbClr", new XAttribute("val", "252932"))), new XElement(A + "prstDash", new XAttribute("val", "solid"))))),
            new XElement(D + "clientData")));
    }

    private static void RemoveUnusedSamples(ZipArchive archive)
    {
        var usedMedia = new HashSet<string>(); var strings = new HashSet<int>();
        foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".rels")).ToArray())
            foreach (var rel in Read(archive, entry.FullName).Descendants(P + "Relationship"))
                if (((string?)rel.Attribute("Type"))?.EndsWith("/image") == true) usedMedia.Add(Path.GetFileName((string)rel.Attribute("Target")!));
        foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("xl/media/") && !usedMedia.Contains(e.Name)).ToArray()) entry.Delete();
        foreach (var entry in archive.Entries.Where(e => Regex.IsMatch(e.FullName, @"^xl/worksheets/sheet\d+\.xml$")).ToArray())
            foreach (var cell in Read(archive, entry.FullName).Descendants(S + "c").Where(c => (string?)c.Attribute("t") == "s")) strings.Add((int)cell.Element(S + "v")!);
        var shared = Read(archive, "xl/sharedStrings.xml"); var index = 0;
        foreach (var item in shared.Root!.Elements(S + "si")) if (!strings.Contains(index++)) item.ReplaceNodes(new XElement(S + "t", ""));
        Write(archive, "xl/sharedStrings.xml", shared);
    }

    private static string ColumnName(int column)
    {
        var name = "";
        while (column > 0) { column--; name = (char)('A' + column % 26) + name; column /= 26; }
        return name;
    }

    private static int Row(XElement row) => (int)row.Attribute("r")!;
    private static int AddressRow(string address) => int.Parse(Regex.Match(address, "[0-9]+").Value, CultureInfo.InvariantCulture);
    private static void SetCell(XElement data, string address, string value)
    {
        var row = data.Elements(S + "row").First(r => Row(r) == AddressRow(address));
        var cell = row.Elements(S + "c").FirstOrDefault(c => (string?)c.Attribute("r") == address);
        if (cell is null) { cell = new XElement(S + "c", new XAttribute("r", address)); row.Add(cell); }
        cell.SetAttributeValue("t", "inlineStr");
        cell.ReplaceNodes(new XElement(S + "is", new XElement(S + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), value)));
    }
    private static XDocument Read(ZipArchive archive, string path) { using var stream = archive.GetEntry(path)!.Open(); return XDocument.Load(stream); }
    private static void Write(ZipArchive archive, string path, XDocument doc) { archive.GetEntry(path)?.Delete(); using var stream = archive.CreateEntry(path).Open(); doc.Save(stream); }
}

