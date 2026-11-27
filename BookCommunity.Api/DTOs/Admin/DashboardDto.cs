using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Api.DTOs.Admin;

public class DashboardDto
{
    // Threshold constants (server-side)
    public const int MinReviewsForHighestRated = 3;
    public const int MinReportsForMostReportedBooks = 2;
    public const int MinReportsForMostReportedReviewers = 2;

    // Context strip
    public int VisibleBooks { get; set; }
    public int HiddenBooks { get; set; }
    public int TotalGenres { get; set; }
    public int ActiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int VisibleReviews { get; set; }

    // Needs attention row
    public int PendingReports { get; set; }
    public int OldestPendingReportDays { get; set; }
    public bool OldestPendingOver3Days => OldestPendingReportDays > 3;
    public int NewReportsLast7Days { get; set; }
    public int HiddenRemovedReviewsLast7Days { get; set; }

    // Previous period comparisons (computed server-side)
    public int PreviousPeriodPendingReports { get; set; }
    public int PreviousPeriodNewReports { get; set; }
    public int PreviousPeriodHiddenRemovedReviews { get; set; }

    // Trends
    public List<DailyTrend> NewReviewsPerDay { get; set; } = new();
    public List<DailyTrend> NewUsersPerDay { get; set; } = new();
    public List<ReasonCount> ReportsByReason { get; set; } = new();
    public ReportStatusBreakdown StatusBreakdown { get; set; } = new();

    // Catalog insights
    public List<GenreBookCount> BooksPerGenre { get; set; } = new();
    public List<string> GenresWithZeroVisibleBooks { get; set; } = new();
    public List<BookSummary> Top5MostReviewed { get; set; } = new();
    public List<BookSummary> Top5HighestRated { get; set; } = new();
    public List<BookSummary> Top5MostSaved { get; set; } = new();
    public int BooksMissingCoverOrDescription { get; set; }
    public List<BookSummary> BooksMissingCoverOrDescriptionSample { get; set; } = new();

    // Moderation insights
    public List<ReportedItemSummary> Top5MostReportedBooks { get; set; } = new();
    public List<ReportedItemSummary> Top5MostReportedReviewers { get; set; } = new();
}

public class DailyTrend
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReasonCount
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ReportStatusBreakdown
{
    public int Pending { get; set; }
    public int Dismissed { get; set; }
    public int Resolved { get; set; }
    public int Total => Pending + Dismissed + Resolved;
    public double PendingPercent => Total > 0 ? Math.Round((double)Pending / Total * 100, 1) : 0;
    public double DismissedPercent => Total > 0 ? Math.Round((double)Dismissed / Total * 100, 1) : 0;
    public double ResolvedPercent => Total > 0 ? Math.Round((double)Resolved / Total * 100, 1) : 0;
}

public class GenreBookCount
{
    public string GenreName { get; set; } = string.Empty;
    public int BookCount { get; set; }
}

public class BookSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Count { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class ReportedItemSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ReportCount { get; set; }
}

// Trends endpoint DTOs
public class DashboardTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string TimeZone { get; set; } = "Asia/Manila";
    public int EarliestYear { get; set; }
    public List<WeekTrend> Weeks { get; set; } = new();
}

public class WeekTrend
{
    public int Index { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NewUsers { get; set; }
    public int NewReviews { get; set; }
}
