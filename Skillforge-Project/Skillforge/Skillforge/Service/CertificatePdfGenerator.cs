using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Skillforge.Dto;

namespace Skillforge.Service;

/// <summary>Generates a printable PDF certificate for a completed course.</summary>
public class CertificatePdfGenerator
{
    private static readonly string PrimaryColor = "#1E3A5F";
    private static readonly string AccentColor  = "#2E86DE";
    private static readonly string GoldColor    = "#D4A017";
    private static readonly string LightGold    = "#FBF3E0";
    private static readonly string TextColor    = "#2C3E50";
    private static readonly string MutedColor   = "#7F8C8D";
    private static readonly string WhiteColor   = "#FFFFFF";

    public byte[] Generate(CertificationResponseDto cert)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(50);
                page.MarginVertical(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontColor(TextColor));

                page.Content().Element(c => ComposeCertificate(c, cert));
            });
        }).GeneratePdf();
    }

    private void ComposeCertificate(IContainer container, CertificationResponseDto cert)
    {
        container
            .Border(6).BorderColor(GoldColor)
            .Background(WhiteColor)
            .Padding(32)
            .Column(col =>
            {
                col.Spacing(0);

                // ── Top brand strip ──────────────────────────────────────────
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("SKILLFORGE")
                             .FontSize(26).Bold().FontColor(PrimaryColor);
                        inner.Item().Text("Learning & Development Platform")
                             .FontSize(9).FontColor(MutedColor);
                    });
                    row.ConstantItem(140).AlignRight().Column(inner =>
                    {
                        inner.Item().Text("CERTIFICATE OF")
                             .FontSize(9).FontColor(MutedColor).AlignRight();
                        inner.Item().Text("COMPLETION")
                             .FontSize(14).Bold().FontColor(AccentColor).AlignRight();
                    });
                });

                // Gold divider
                col.Item().PaddingTop(12).Height(3).Background(GoldColor);
                col.Item().PaddingBottom(24);

                // ── Main body ────────────────────────────────────────────────
                col.Item().AlignCenter().Text("This is to certify that")
                   .FontSize(13).FontColor(MutedColor);

                col.Item().PaddingTop(10).AlignCenter()
                   .Text(cert.EmployeeName ?? "Employee")
                   .FontSize(28).Bold().FontColor(PrimaryColor);

                col.Item().PaddingTop(8).AlignCenter()
                   .Text("has successfully completed all modules of the course")
                   .FontSize(13).FontColor(MutedColor);

                col.Item().PaddingTop(10).AlignCenter()
                   .Text(cert.CourseName ?? "Course")
                   .FontSize(22).Bold().FontColor(AccentColor);

                if (!string.IsNullOrWhiteSpace(cert.CourseDescription))
                {
                    col.Item().PaddingTop(6).AlignCenter()
                       .Text(cert.CourseDescription)
                       .FontSize(10).FontColor(MutedColor).Italic();
                }

                // ── Thin gold divider ────────────────────────────────────────
                col.Item().PaddingTop(24).Height(1).Background(GoldColor);
                col.Item().PaddingBottom(16);

                // ── Footer row with dates ────────────────────────────────────
                col.Item().Row(row =>
                {
                    row.RelativeItem().AlignCenter().Column(inner =>
                    {
                        inner.Item().Text("Issue Date")
                             .FontSize(9).FontColor(MutedColor);
                        inner.Item().Text(cert.IssueDate.ToString("dd MMMM yyyy"))
                             .FontSize(12).Bold();
                    });

                    row.RelativeItem().AlignCenter().Column(inner =>
                    {
                        inner.Item().Text("Valid Until")
                             .FontSize(9).FontColor(MutedColor);
                        inner.Item().Text(cert.ExpiryDate.ToString("dd MMMM yyyy"))
                             .FontSize(12).Bold();
                    });

                    row.RelativeItem().AlignCenter().Column(inner =>
                    {
                        inner.Item().Text("Certificate ID")
                             .FontSize(9).FontColor(MutedColor);
                        inner.Item().Text($"CERT-{cert.CertificationId:D6}")
                             .FontSize(12).Bold().FontColor(AccentColor);
                    });
                });

                col.Item().PaddingTop(18).AlignCenter()
                   .Text("SkillForge · For Internal Use Only")
                   .FontSize(8).FontColor(MutedColor);
            });
    }
}
