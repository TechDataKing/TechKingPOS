using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace TechKingPOS.App.Services
{
    public static class PdfExporter
    {
        static PdfExporter()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static void Export(
            string title,
            string period,
            string[] headers,
            List<string[]> rows)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"{title.Replace(" ", "_")}.pdf"
            };

            if (dialog.ShowDialog() != true)
                return;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    page.Header().Column(col =>
                    {
                        col.Item().Text(title).FontSize(20).Bold();
                        col.Item().Text(period).FontSize(10).Italic();
                        col.Item().LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            foreach (var _ in headers)
                                cols.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                            {
                                header.Cell()
                                    .Background(QuestPDF.Helpers.Colors.Grey.Lighten2)
                                    .Padding(6)
                                    .Text(h)
                                    .Bold();
                            }
                        });

                        foreach (var row in rows)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell()
                                    .Padding(6)
                                    .Text(cell ?? "");
                            }
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");
                });
            })
            .GeneratePdf(dialog.FileName);
        }
    }
}
