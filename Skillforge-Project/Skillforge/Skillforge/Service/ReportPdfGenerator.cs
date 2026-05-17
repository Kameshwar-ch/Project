using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Skillforge.Dto;

namespace Skillforge.Service;

/// <summary>
/// Generates a professionally styled PDF for a SkillForge report
/// using the QuestPDF library.
/// </summary>
public class ReportPdfGenerator
{
    // Brand colours
    private static readonly string PrimaryColor   = "#1E3A5F";  // dark navy
    private static readonly string AccentColor    = "#2E86DE";  // blue
    private static readonly string LightBg        = "#F4F6F9";  // light grey
    private static readonly string TextColor      = "#2C3E50";  // dark text
    private static readonly string MutedColor     = "#7F8C8D";  // muted grey
    private static readonly string WhiteColor     = "#FFFFFF";

    public byte[] Generate(ReportMetrics metrics)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontColor(TextColor).FontSize(10));

                page.Header().Element(c => ComposeHeader(c, metrics));
                page.Content().Element(c => ComposeContent(c, metrics));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    // ── Header ───────────────────────────────────────────────────────────────────

    private void ComposeHeader(IContainer container, ReportMetrics metrics)
    {
        container.Column(col =>
        {
            // Top banner
            col.Item().Background(PrimaryColor).Padding(16).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item()
                         .Text("SKILLFORGE")
                         .FontSize(22).Bold().FontColor(WhiteColor);

                    inner.Item()
                         .Text("Learning & Development Platform")
                         .FontSize(10).FontColor("#A8C4E0");
                });

                row.ConstantItem(120).AlignRight().AlignMiddle().Column(inner =>
                {
                    inner.Item()
                         .Text($"{metrics.Scope.ToUpper()} REPORT")
                         .FontSize(11).Bold().FontColor(AccentColor);

                    inner.Item()
                         .Text(metrics.GeneratedAt.ToString("dd MMM yyyy, hh:mm tt") + " IST")
                         .FontSize(8).FontColor("#A8C4E0");
                });
            });

            // Accent stripe
            col.Item().Height(4).Background(AccentColor);
        });
    }

    // ── Content ──────────────────────────────────────────────────────────────────

    private void ComposeContent(IContainer container, ReportMetrics metrics)
    {
        container.PaddingTop(20).Column(col =>
        {
            col.Spacing(16);

            // Section label
            col.Item().Element(c => SectionLabel(c, "Executive Summary"));

            // Metric cards
            col.Item().Element(c => ComposeMetricCards(c, metrics));

            // Divider
            col.Item().Height(1).Background(AccentColor);

            // Details table
            col.Item().Element(c => SectionLabel(c, "Detailed Breakdown"));
            col.Item().Element(c => ComposeDetailsTable(c, metrics));

            // Insights
            col.Item().Element(c => SectionLabel(c, "Insights"));
            col.Item().Element(c => ComposeInsights(c, metrics));
        });
    }

    // ── Metric cards ─────────────────────────────────────────────────────────────

    private void ComposeMetricCards(IContainer container, ReportMetrics metrics)
    {
        var cards = BuildCardData(metrics);
        const int maxPerRow = 4;

        container.Column(col =>
        {
            col.Spacing(8);
            for (int i = 0; i < cards.Count; i += maxPerRow)
            {
                var rowCards = cards.Skip(i).Take(maxPerRow).ToList();
                col.Item().Row(row =>
                {
                    foreach (var (label, value, color) in rowCards)
                    {
                        row.RelativeItem().Padding(4).Background(LightBg).Border(1).BorderColor("#DDE3EA")
                           .Padding(12).Column(inner =>
                           {
                               inner.Item().Text(value).FontSize(22).Bold().FontColor(color);
                               inner.Item().PaddingTop(2).Text(label).FontSize(8).FontColor(MutedColor);
                           });
                    }
                });
            }
        });
    }

    private List<(string Label, string Value, string Color)> BuildCardData(ReportMetrics m)
    {
        var cards = new List<(string, string, string)>();

        if (m.TotalEnrollments.HasValue)
            cards.Add(("Total Enrollments", m.TotalEnrollments.Value.ToString(), AccentColor));

        if (m.TotalCourses.HasValue)
            cards.Add(("Total Courses", m.TotalCourses.Value.ToString(), PrimaryColor));

        if (m.ActiveCourses.HasValue)
            cards.Add(("Active Courses", m.ActiveCourses.Value.ToString(), AccentColor));

        if (m.TotalEmployees.HasValue)
            cards.Add(("Total Employees", m.TotalEmployees.Value.ToString(), PrimaryColor));

        if (m.TotalManagers.HasValue)
            cards.Add(("Managers", m.TotalManagers.Value.ToString(), "#8E44AD"));

        if (m.TotalTrainers.HasValue)
            cards.Add(("Trainers", m.TotalTrainers.Value.ToString(), AccentColor));

        if (m.TotalHRs.HasValue)
            cards.Add(("HR Staff", m.TotalHRs.Value.ToString(), "#16A085"));

        if (m.ActiveCertifications.HasValue)
            cards.Add(("Active Certifications", m.ActiveCertifications.Value.ToString(), "#27AE60"));

        if (m.ComplianceRate.HasValue)
        {
            var rate = m.ComplianceRate.Value;
            var color = rate >= 80 ? "#27AE60" : rate >= 50 ? "#F39C12" : "#E74C3C";
            cards.Add(("Compliance Rate", $"{rate:0.00}%", color));
        }

        if (m.TotalSkillGaps.HasValue)
            cards.Add(("Skill Gaps", m.TotalSkillGaps.Value.ToString(), "#E74C3C"));

        return cards;
    }

    // ── Details table ─────────────────────────────────────────────────────────────

    private void ComposeDetailsTable(IContainer container, ReportMetrics metrics)
    {
        var rows = BuildTableRows(metrics);

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3);
                c.RelativeColumn(2);
            });

            // Header row
            table.Header(header =>
            {
                header.Cell().Background(PrimaryColor).Padding(8)
                      .Text("Metric").FontColor(WhiteColor).Bold().FontSize(9);
                header.Cell().Background(PrimaryColor).Padding(8)
                      .Text("Value").FontColor(WhiteColor).Bold().FontSize(9);
            });

            bool isAlt = false;
            foreach (var (label, value) in rows)
            {
                var bg = isAlt ? LightBg : WhiteColor;
                table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDE3EA")
                     .Padding(8).Text(label).FontSize(9);
                table.Cell().Background(bg).BorderBottom(1).BorderColor("#DDE3EA")
                     .Padding(8).Text(value).FontSize(9).Bold();
                isAlt = !isAlt;
            }
        });
    }

    private List<(string, string)> BuildTableRows(ReportMetrics m)
    {
        var rows = new List<(string, string)>();

        void Add(string label, int? val, string suffix = "") { if (val.HasValue) rows.Add((label, val.Value + suffix)); }
        void AddD(string label, double? val, string suffix = "") { if (val.HasValue) rows.Add((label, $"{val.Value:0.00}{suffix}")); }

        Add("Total Enrollments", m.TotalEnrollments);
        Add("Total Courses", m.TotalCourses);
        Add("Active Courses", m.ActiveCourses);
        Add("Total Employees", m.TotalEmployees);
        Add("Total Managers", m.TotalManagers);
        Add("Total Trainers", m.TotalTrainers);
        Add("Total HR Staff", m.TotalHRs);
        Add("Certified Employees", m.CertifiedEmployees);
        Add("Compliant Employees", m.CompliantEmployees);
        Add("Non-Compliant Employees", m.NonCompliantEmployees);
        Add("Active Certifications", m.ActiveCertifications);
        AddD("Compliance Rate", m.ComplianceRate, "%");
        Add("Total Skill Gaps Identified", m.TotalSkillGaps);

        return rows;
    }

    // ── Insights ──────────────────────────────────────────────────────────────────

    private void ComposeInsights(IContainer container, ReportMetrics metrics)
    {
        var points = BuildInsights(metrics);

        container.Background(LightBg).Border(1).BorderColor("#DDE3EA").Padding(14).Column(col =>
        {
            foreach (var point in points)
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(14).Text("•").FontColor(AccentColor).Bold();
                    row.RelativeItem().Text(point).FontSize(9).FontColor(TextColor);
                });
                col.Item().Height(4);
            }
        });
    }

    private List<string> BuildInsights(ReportMetrics m)
    {
        var insights = new List<string>();

        if (m.ComplianceRate.HasValue)
        {
            if (m.ComplianceRate.Value >= 80)
                insights.Add($"Compliance rate is strong at {m.ComplianceRate.Value:0.00}%. Keep maintaining current certification standards.");
            else if (m.ComplianceRate.Value >= 50)
                insights.Add($"Compliance rate is moderate at {m.ComplianceRate.Value:0.00}%. Consider targeted certification drives.");
            else
                insights.Add($"Compliance rate is low at {m.ComplianceRate.Value:0.00}%. Immediate action is required to address certification gaps.");
        }

        if (m.TotalSkillGaps.HasValue && m.TotalSkillGaps.Value > 0)
            insights.Add($"{m.TotalSkillGaps.Value} skill gap(s) identified. Recommend reviewing course enrollment for affected employees.");

        if (m.ActiveCertifications.HasValue && m.TotalEnrollments.HasValue && m.TotalEnrollments.Value > 0)
        {
            double certRate = (double)m.ActiveCertifications.Value / m.TotalEnrollments.Value * 100;
            insights.Add($"Certification conversion rate is {certRate:0.00}% (certifications vs. enrollments).");
        }

        if (m.TotalCourses.HasValue && m.ActiveCourses.HasValue)
        {
            int inactive = m.TotalCourses.Value - m.ActiveCourses.Value;
            if (inactive > 0)
                insights.Add($"{inactive} course(s) are currently inactive and may require review or archiving.");
        }

        if (m.NonCompliantEmployees.HasValue && m.NonCompliantEmployees.Value > 0)
            insights.Add($"{m.NonCompliantEmployees.Value} employee(s) are non-compliant. HR should follow up with targeted certification plans.");

        if (m.TotalManagers.HasValue && m.TotalEmployees.HasValue && m.TotalManagers.Value > 0)
        {
            double ratio = (double)m.TotalEmployees.Value / m.TotalManagers.Value;
            insights.Add($"Manager-to-employee ratio is 1:{ratio:0.0} across the organisation.");
        }

        if (insights.Count == 0)
            insights.Add("All metrics are within expected ranges. No immediate action required.");

        return insights;
    }

    // ── Footer ────────────────────────────────────────────────────────────────────

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor("#DDE3EA").PaddingTop(6).Row(row =>
        {
            row.RelativeItem()
               .Text("SkillForge · Confidential · For Internal Use Only")
               .FontSize(8).FontColor(MutedColor);

            row.ConstantItem(100).AlignRight()
               .Text(text =>
               {
                   text.Span("Page ").FontSize(8).FontColor(MutedColor);
                   text.CurrentPageNumber().FontSize(8).FontColor(MutedColor);
                   text.Span(" of ").FontSize(8).FontColor(MutedColor);
                   text.TotalPages().FontSize(8).FontColor(MutedColor);
               });
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private void SectionLabel(IContainer container, string title)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(11).Bold().FontColor(PrimaryColor);
            col.Item().Height(2).Background(AccentColor);
            col.Item().Height(6);
        });
    }
}
