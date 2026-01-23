using MyJournalProject.Common;
using static MyJournalProject.Services.DashboardService;

namespace MyJournalProject.Services;


public interface IDashboardService
{
    // Gets comprehensive dashboard statistics for a user
    // Parameters:
    // userId: ID of the user requesting dashboard data
    // Returns: ServiceResult containing DashboardStats with various metrics
    Task<ServiceResult<DashboardStats>> GetDashboardStatsAsync(int userId);

    // Gets mood distribution data for a specified time period
    // Parameters:
    // userId: ID of the user requesting mood data
    // period: Time period for analysis
    // Returns: ServiceResult containing MoodDistributionData with mood counts
    Task<ServiceResult<MoodDistributionData>> GetMoodDistributionAsync(int userId, string period);

    // Gets word count trend data for a specified time period
    // Parameters:
    // userId: ID of the user requesting word count data
    // period: Time period for analysis
    // Returns: ServiceResult containing WordCountTrendData for chart visualization
    Task<ServiceResult<WordCountTrendData>> GetWordCountTrendAsync(int userId, string period);

    // Gets the most frequently used tags for a user
    // Parameters:
    // userId: ID of the user requesting tag data
    // count: Number of top tags to return
    // Returns: ServiceResult containing a list of TopTag objects
    Task<ServiceResult<List<TopTag>>> GetTopTagsAsync(int userId, int count = 3);

    // Gets the user's most recent mood information
    // Parameters:
    // userId: ID of the user requesting current mood data
    // Returns: ServiceResult containing CurrentMoodInfo with mood and recency status
    Task<ServiceResult<CurrentMoodInfo>> GetCurrentMoodAsync(int userId);

    // Gets comprehensive analytics data for a custom date range
    // Parameters:
    // userId: ID of the user requesting analytics
    // startDate: Optional start date for the analysis period
    // endDate: Optional end date for the analysis period
    // Returns: ServiceResult containing AnalyticsData with various metrics
    Task<ServiceResult<AnalyticsData>> GetAnalyticsDataAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
}

public class DashboardStats
{
    // Number of consecutive days with journal entries (current streak)
    public int CurrentStreak { get; set; }

    // Longest consecutive days with journal entries (all-time record)
    public int LongestStreak { get; set; }

    // Number of days without journal entries in the current month
    public int MissedDaysThisMonth { get; set; }

    // Total number of journal entries created by the user
    public int TotalEntries { get; set; }
}


public class MoodDistributionData
{
    // Count of journal entries with "Positive" primary mood
    public int Positive { get; set; }

    // Count of journal entries with "Neutral" primary mood
    public int Neutral { get; set; }

    // Count of journal entries with "Negative" primary mood
    public int Negative { get; set; }
}


public class WordCountTrendData
{
    // Labels for the X-axis (typically dates or time periods)
    public List<string> Labels { get; set; } = new();

    // Word count values for the Y-axis
    public List<int> Data { get; set; } = new();
}

public class TopTag
{
    // Name of the tag
    public string Name { get; set; } = string.Empty;

    // Number of times the tag has been used
    public int Count { get; set; }

    // Percentage of total journal entries that use this tag
    public double Percentage { get; set; }
}


public class CurrentMoodInfo
{
    // The current mood value 
    public string Mood { get; set; } = string.Empty;

    // Icon name for displaying the mood 
    public string Icon { get; set; } = "neutral";

    // Indicates if the mood is from a recent journal entry 
    public bool IsRecent { get; set; }

    // Date of the journal entry that contains this mood
    public DateTime? Date { get; set; }
}