using System.Collections.Generic;
using System.Linq;
using EstimateApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace EstimateApp.Services;

public class ExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GeneratePdf(string filePath, IEnumerable<EstimateItem> items, decimal grandTotal)
    {
        // Групуємо та сортуємо
        var groupedItems = items
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .GroupBy(i => i.Category);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("ТЕХНІЧНИЙ РОЗРАХУНОК").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{System.DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).Italic();
                    });
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Name
                            columns.RelativeColumn();  // Factor
                            columns.RelativeColumn();  // Price
                            columns.RelativeColumn();  // Total
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Найменування / Категорія");
                            header.Cell().Element(HeaderStyle).Text("К-сть");
                            header.Cell().Element(HeaderStyle).Text("Ціна");
                            header.Cell().Element(HeaderStyle).Text("Всього");

                            static IContainer HeaderStyle(IContainer container) => 
                                container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var group in groupedItems)
                        {
                            // Рядок категорії
                            table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten3).Padding(5).Text(group.Key).FontSize(12).Bold();

                            foreach (var item in group)
                            {
                                table.Cell().Element(CellStyle).PaddingLeft(10).Text(item.Name);
                                table.Cell().Element(CellStyle).Text($"{item.Factor} шт.");
                                table.Cell().Element(CellStyle).Text($"{item.Price:N2}");
                                table.Cell().Element(CellStyle).Text($"{item.Total:N2}");

                                static IContainer CellStyle(IContainer container) => 
                                    container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
                            }
                        }
                    });

                    // Підсумок ВІДРАЗУ під таблицею
                    column.Item().PaddingTop(20).AlignRight().Column(col =>
                    {
                        col.Item().Text("ЗАГАЛЬНА СУМА:").FontSize(12).SemiBold();
                        col.Item().Text($"{grandTotal:N2} грн").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().PaddingTop(5).Text("* Ціни з урахуванням ПДВ").FontSize(9).Italic();
                    });
                });
            });
        }).GeneratePdf(filePath);
    }

    public void ExportToExcel(string filePath, IEnumerable<EstimateItem> items, decimal grandTotal)
    {
        var groupedItems = items
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .GroupBy(i => i.Category);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Розрахунок");

        ws.Cell("A1").Value = "ТЕХНІЧНИЙ РОЗРАХУНОК ОБЛАДНАННЯ";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 18;
        ws.Range("A1:D1").Merge();

        int row = 3;
        ws.Cell(row++, 1).Value = $"Сформовано: {System.DateTime.Now:dd.MM.yyyy HH:mm}";
        row++;

        // Headers
        var headers = new[] { "Найменування", "К-сть", "Ціна", "Сума" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
            ws.Cell(row, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        row++;

        foreach (var group in groupedItems)
        {
            // Category Header
            var catCell = ws.Cell(row, 1);
            catCell.Value = group.Key;
            catCell.Style.Font.Bold = true;
            catCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            ws.Range(row, 1, row, 4).Merge().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;

            foreach (var item in group)
            {
                ws.Cell(row, 1).Value = item.Name;
                ws.Cell(row, 2).Value = item.Factor;
                ws.Cell(row, 3).Value = item.Price;
                ws.Cell(row, 4).Value = item.Total;
                
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Range(row, 1, row, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                row++;
            }
        }

        row++;
        ws.Cell(row, 3).Value = "ЗАГАЛЬНА СУМА:";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = grandTotal;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00 \"грн\"";

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
