using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EstimateApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;

namespace EstimateApp.Services;

public class ExportMetadata
{
    public DateTime Date { get; set; } = DateTime.Now;
    public string AuthorName { get; set; } = "";
    public string AuthorPhone { get; set; } = "";
    public string AuthorEmail { get; set; } = "";
    public string Website { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string HeaderImagePath { get; set; } = "";
}

public class ExportService
{
    private byte[]? _cachedDefaultLogo;

    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GeneratePdf(string filePath, IEnumerable<EstimateItem> items, decimal grandTotal, ExportMetadata meta)
    {
        var groupedItems = items
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .GroupBy(i => i.Category);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                page.Header().Column(headerCol =>
                {
                    // КАРТИНКА ХЕДЕРА
                    var logoBytes = GetLogoBytes(meta.HeaderImagePath);
                    if (logoBytes != null)
                    {
                        headerCol.Item().AlignCenter().MaxHeight(80).Image(logoBytes, ImageScaling.FitArea);
                    }

                    headerCol.Item().PaddingTop(10).Row(row =>
                    {
                        // ЛІВА ЧАСТИНА: Клієнт та Пропозиція
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().PaddingTop(5).Text(meta.ClientName).FontSize(14).Bold();
                            col.Item().Text("Пропозиція").FontSize(12).SemiBold().FontColor(Colors.Grey.Medium);
                            col.Item().Text($"{meta.Date:dd MMMM yyyy}").FontSize(11);
                        });

                        // ПРАВА ЧАСТИНА: Контакти автора
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            if (!string.IsNullOrWhiteSpace(meta.Website)) col.Item().Text(meta.Website).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrWhiteSpace(meta.AuthorName)) col.Item().Text(meta.AuthorName).Bold();
                            if (!string.IsNullOrWhiteSpace(meta.AuthorPhone)) col.Item().Text(meta.AuthorPhone);
                            if (!string.IsNullOrWhiteSpace(meta.AuthorEmail)) col.Item().Text(meta.AuthorEmail);
                        });
                    });
                });

                page.Content().PaddingVertical(15).Column(column =>
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
                            header.Cell().Element(HeaderStyle).Text("Найменування");
                            header.Cell().Element(HeaderStyle).AlignCenter().Text("К-сть");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Ціна");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Сума");

                            static IContainer HeaderStyle(IContainer container) => 
                                container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                        });

                        foreach (var group in groupedItems)
                        {
                            table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten3).Padding(5).Text(group.Key).FontSize(11).Bold();

                            foreach (var item in group)
                            {
                                table.Cell().Element(CellStyle).PaddingLeft(5).Text(item.Name);
                                table.Cell().Element(CellStyle).AlignCenter().Text($"{item.Factor} шт.");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Price:N2} ₴");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Total:N2} ₴");

                                static IContainer CellStyle(IContainer container) => 
                                    container.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten4);
                            }
                        }
                    });

                    column.Item().PaddingTop(15).AlignRight().Column(col =>
                    {
                        col.Item().Text(t => {
                            t.Span("ЗАГАЛЬНА СУМА: ").FontSize(12).SemiBold();
                            t.Span($"{grandTotal:N2} ₴").FontSize(18).Bold();
                        });
                        col.Item().PaddingTop(2).Text("* Ціни вказані у національній валюті").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    });
                });
                
                page.Footer().AlignCenter().Text(x => {
                    x.Span("Стор. ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf(filePath);
    }

    public void ExportToExcel(string filePath, IEnumerable<EstimateItem> items, decimal grandTotal, ExportMetadata meta)
    {
        var groupedItems = items
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .GroupBy(i => i.Category);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Розрахунок");

        int currentRow = 1;

        // КАРТИНКА (приблизно)
        var logoBytes = GetLogoBytes(meta.HeaderImagePath);
        if (logoBytes != null)
        {
            using var ms = new MemoryStream(logoBytes);
            var picture = ws.AddPicture(ms)
                .MoveTo(ws.Cell(currentRow, 1))
                .WithPlacement(XLPicturePlacement.FreeFloating);
            picture.Height = 80;
            currentRow += 5;
        }

        // МЕТАДАНІ
        ws.Cell(currentRow, 1).Value = meta.ClientName;
        ws.Cell(currentRow, 1).Style.Font.Bold = true;
        ws.Cell(currentRow, 1).Style.Font.FontSize = 14;

        ws.Cell(currentRow, 4).Value = meta.Website;
        ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        currentRow++;

        ws.Cell(currentRow, 1).Value = "Пропозиція";
        ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.Gray;

        ws.Cell(currentRow, 4).Value = meta.AuthorName;
        ws.Cell(currentRow, 4).Style.Font.Bold = true;
        ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        currentRow++;

        ws.Cell(currentRow, 1).Value = meta.Date.ToString("dd MMMM yyyy");

        ws.Cell(currentRow, 4).Value = meta.AuthorPhone;
        ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        currentRow++;

        ws.Cell(currentRow, 4).Value = meta.AuthorEmail;
        ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        currentRow += 2;

        // Headers
        var headers = new[] { "Найменування", "К-сть", "Ціна", "Сума" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(currentRow, i + 1).Value = headers[i];
            ws.Cell(currentRow, i + 1).Style.Font.Bold = true;
            ws.Cell(currentRow, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            ws.Cell(currentRow, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        currentRow++;

        foreach (var group in groupedItems)
        {
            // Category Header
            ws.Cell(currentRow, 1).Value = group.Key;
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FAFAFA");
            ws.Range(currentRow, 1, currentRow, 4).Merge().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            currentRow++;

            foreach (var item in group)
            {
                ws.Cell(currentRow, 1).Value = item.Name;
                ws.Cell(currentRow, 2).Value = item.Factor;
                ws.Cell(currentRow, 3).Value = item.Price;
                ws.Cell(currentRow, 4).Value = item.Total;
                
                ws.Cell(currentRow, 3).Style.NumberFormat.Format = "#,##0.00 \"₴\"";
                ws.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00 \"₴\"";
                ws.Range(currentRow, 1, currentRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                currentRow++;
            }
        }

        currentRow++;
        ws.Cell(currentRow, 3).Value = "ЗАГАЛЬНА СУМА:";
        ws.Cell(currentRow, 3).Style.Font.Bold = true;
        ws.Cell(currentRow, 4).Value = grandTotal;
        ws.Cell(currentRow, 4).Style.Font.Bold = true;
        ws.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00 \"₴\"";

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = 50; // Назва може бути довгою
        workbook.SaveAs(filePath);
    }

    private byte[]? GetLogoBytes(string customPath)
    {
        // 1. Спробувати завантажити зовнішній файл
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
        {
            try { return File.ReadAllBytes(customPath); } catch { }
        }

        // 2. Повернути вбудований логотип
        if (_cachedDefaultLogo == null)
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Iris;component/maxeffectshow.jpg");
                var info = System.Windows.Application.GetResourceStream(uri);
                if (info != null)
                {
                    using var ms = new MemoryStream();
                    info.Stream.CopyTo(ms);
                    _cachedDefaultLogo = ms.ToArray();
                }
            }
            catch { }
        }

        return _cachedDefaultLogo;
    }
}
