using MyJournalProject.Common;
using static MyJournalProject.Services.DashboardService;

namespace MyJournalProject.Services;

public interface IDashboardService
{
    Task<ServiceResult<DashboardStats>> GetDashboardStatsAsync(int userId);
    Task<ServiceResult<MoodDistributionData>> GetMoodDistributionAsync(int userId, string period);
    Task<ServiceResult<WordCountTrendData>> GetWordCountTrendAsync(int userId, string period);
    Task<ServiceResult<List<TopTag>>> GetTopTagsAsync(int userId, int count = 3);
    Task<ServiceResult<CurrentMoodInfo>> GetCurrentMoodAsync(int userId);
    Task<ServiceResult<AnalyticsData>> GetAnalyticsDataAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
}

// Move these classes here or keep them in DashboardService.cs
public class DashboardStats
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int MissedDaysThisMonth { get; set; }
    public int TotalEntries { get; set; }
}

public class MoodDistributionData
{
    public int Positive { get; set; }
    public int Neutral { get; set; }
    public int Negative { get; set; }
}

public class WordCountTrendData
{
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
}

public class TopTag
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class CurrentMoodInfo
{
    public string Mood { get; set; } = string.Empty;
    public string Icon { get; set; } = "neutral";
    public bool IsRecent { get; set; }
    public DateTime? Date { get; set; }
}