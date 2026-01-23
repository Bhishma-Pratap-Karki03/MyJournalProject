using MyJournalProject.Common;
using MyJournalProject.Data;
using MyJournalProject.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MyJournalProject.Services;

// Service for providing dashboard analytics and statistics
// Calculates streaks, mood distributions, word count trends, and other user metrics
public class DashboardService : IDashboardService
{
    // Database context for accessing journal data
    private readonly AppDbContext _context;

    // Constructor - injects database context dependency
    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    // Gets comprehensive dashboard statistics for a user
    public async Task<ServiceResult<DashboardStats>> GetDashboardStatsAsync(int userId)
    {
        try
        {
            // Get all journals for the user, ordered by date 
            var journals = await _context.Journals
                .Where(j => j.UserId == userId)
                .OrderBy(j => j.EntryDate)
                .ToListAsync();

            // If no journals exist, return empty stats
            if (!journals.Any())
            {
                return ServiceResult<DashboardStats>.SuccessResult(new DashboardStats
                {
                    CurrentStreak = 0,
                    LongestStreak = 0,
                    MissedDaysThisMonth = 0,
                    TotalEntries = 0
                });
            }

            // Calculate streaks (current and longest)
            var (currentStreak, longestStreak) = CalculateStreaks(journals);

            // Calculate missed days in current month
            var missedDays = CalculateMissedDaysThisMonth(journals);

            // Return complete dashboard statistics
            return ServiceResult<DashboardStats>.SuccessResult(new DashboardStats
            {
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                MissedDaysThisMonth = missedDays,
                TotalEntries = journals.Count
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<DashboardStats>.FailureResult($"Error getting dashboard stats: {ex.Message}");
        }
    }

    // Gets mood distribution data for a specified time period
    public async Task<ServiceResult<MoodDistributionData>> GetMoodDistributionAsync(int userId, string period)
    {
        try
        {
            var endDate = DateTime.Today;
            var startDate = period switch
            {
                "7" => endDate.AddDays(-6),      // Last 7 days
                "30" => endDate.AddDays(-29),    // Last 30 days
                "120" => endDate.AddDays(-119),  // Last 120 days
                _ => endDate.AddDays(-29)        // Default: 30 days
            };

            // Get journals within the specified date range
            var journals = await _context.Journals
                .Where(j => j.UserId == userId && j.EntryDate >= startDate && j.EntryDate <= endDate)
                .ToListAsync();

            // Initialize mood counters
            var moodCounts = new Dictionary<string, int>
            {
                { "Positive", 0 },
                { "Neutral", 0 },
                { "Negative", 0 }
            };

            // Count occurrences of each primary mood
            foreach (var journal in journals)
            {
                var mood = journal.PrimaryMood;
                if (moodCounts.ContainsKey(mood))
                {
                    moodCounts[mood]++;
                }
            }

            // Create and return mood distribution data (actual counts, not percentages)
            var distribution = new MoodDistributionData
            {
                Positive = moodCounts["Positive"],
                Neutral = moodCounts["Neutral"],
                Negative = moodCounts["Negative"]
            };

            return ServiceResult<MoodDistributionData>.SuccessResult(distribution);
        }
        catch (Exception ex)
        {
            return ServiceResult<MoodDistributionData>.FailureResult($"Error getting mood distribution: {ex.Message}");
        }
    }

    // Gets word count trend data for a specified time period
    public async Task<ServiceResult<WordCountTrendData>> GetWordCountTrendAsync(int userId, string period)
    {
        try
        {
            var endDate = DateTime.Today;
            var startDate = period switch
            {
                "7" => endDate.AddDays(-6),      // Last 7 days
                "30" => endDate.AddDays(-29),    // Last 30 days
                "120" => endDate.AddDays(-119),  // Last 120 days
                _ => endDate.AddDays(-6)         // Default: 7 days
            };

            // Get journals within the specified date range, ordered by date
            var journals = await _context.Journals
                .Where(j => j.UserId == userId && j.EntryDate >= startDate && j.EntryDate <= endDate)
                .OrderBy(j => j.EntryDate)
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<int>();

            // Generate data based on selected period
            if (period == "7")
            {
                // Daily data for 7 days
                for (int i = 0; i < 7; i++)
                {
                    var date = endDate.AddDays(-i);
                    var journal = journals.FirstOrDefault(j => j.EntryDate.Date == date.Date);
                    labels.Insert(0, date.ToString("ddd"));  // Day abbreviation (Mon, Tue, etc.)
                    data.Insert(0, journal?.WordCount ?? 0);
                }
            }
            else if (period == "30")
            {
                // Weekly data for 4 weeks
                for (int i = 0; i < 4; i++)
                {
                    var weekStart = endDate.AddDays(-(i + 1) * 7 + 1);
                    var weekEnd = endDate.AddDays(-i * 7);

                    var weekJournals = journals
                        .Where(j => j.EntryDate >= weekStart && j.EntryDate <= weekEnd)
                        .ToList();

                    labels.Insert(0, $"Week {4 - i}");
                    data.Insert(0, weekJournals.Sum(j => j.WordCount));
                }
            }
            else
            {
                // Monthly data for 4 months
                for (int i = 0; i < 4; i++)
                {
                    var monthStart = new DateTime(endDate.Year, endDate.Month - i, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthJournals = journals
                        .Where(j => j.EntryDate >= monthStart && j.EntryDate <= monthEnd)
                        .ToList();

                    labels.Insert(0, monthStart.ToString("MMM"));  // Month abbreviation (Jan, Feb, etc.)
                    data.Insert(0, monthJournals.Sum(j => j.WordCount));
                }
            }

            return ServiceResult<WordCountTrendData>.SuccessResult(new WordCountTrendData
            {
                Labels = labels,
                Data = data
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<WordCountTrendData>.FailureResult($"Error getting word count trend: {ex.Message}");
        }
    }

    // Gets the most frequently used tags for a user
    public async Task<ServiceResult<List<TopTag>>> GetTopTagsAsync(int userId, int count = 3)
    {
        try
        {
            // Get all journals for the user
            var journals = await _context.Journals
                .Where(j => j.UserId == userId)
                .ToListAsync();

            var tagCounts = new Dictionary<string, int>();

            // Count tag occurrences across all journals
            foreach (var journal in journals)
            {
                // Deserialize JSON tags string to list
                var tags = JsonSerializer.Deserialize<List<string>>(journal.TagsJson ?? "[]") ?? new List<string>();
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        if (tagCounts.ContainsKey(tag))
                            tagCounts[tag]++;
                        else
                            tagCounts[tag] = 1;
                    }
                }
            }

            // Get top N tags by usage count
            var topTags = tagCounts
                .OrderByDescending(t => t.Value)
                .Take(count)
                .Select(t => new TopTag
                {
                    Name = t.Key,
                    Count = t.Value
                })
                .ToList();

            return ServiceResult<List<TopTag>>.SuccessResult(topTags);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<TopTag>>.FailureResult($"Error getting top tags: {ex.Message}");
        }
    }

    // Gets comprehensive analytics data for a custom date range
    public async Task<ServiceResult<AnalyticsData>> GetAnalyticsDataAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            // Set default date range if not provided (last month)
            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;

            // Get journals within date range, ordered by date
            var journals = await _context.Journals
                .Where(j => j.UserId == userId && j.EntryDate >= start && j.EntryDate <= end)
                .OrderBy(j => j.EntryDate)
                .ToListAsync();

            // If no journals found, return empty analytics data
            if (!journals.Any())
            {
                return ServiceResult<AnalyticsData>.SuccessResult(new AnalyticsData());
            }

            // Calculate mood distribution
            var moodDistribution = new MoodDistributionData();
            foreach (var journal in journals)
            {
                switch (journal.PrimaryMood.ToLower())
                {
                    case "positive":
                        moodDistribution.Positive++;
                        break;
                    case "neutral":
                        moodDistribution.Neutral++;
                        break;
                    case "negative":
                        moodDistribution.Negative++;
                        break;
                }
            }

            // Calculate word count trend grouped by week
            var wordTrend = new WordCountTrendData();

            // Group journals by week start date (Sunday)
            var groupedJournals = journals
                .GroupBy(j => j.EntryDate.AddDays(-(int)j.EntryDate.DayOfWeek))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in groupedJournals)
            {
                wordTrend.Labels.Add(group.Key.ToString("MMM dd"));  // Month and day
                wordTrend.Data.Add(group.Sum(j => j.WordCount));
            }

            // Return comprehensive analytics data
            return ServiceResult<AnalyticsData>.SuccessResult(new AnalyticsData
            {
                TotalEntries = journals.Count,
                MoodDistribution = moodDistribution,
                WordTrend = wordTrend,
                Journals = journals
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<AnalyticsData>.FailureResult($"Error getting analytics data: {ex.Message}");
        }
    }

    // Gets the user's most recent mood information
    public async Task<ServiceResult<CurrentMoodInfo>> GetCurrentMoodAsync(int userId)
    {
        try
        {
            // Get the most recent journal entry for the user
            var latestJournal = await _context.Journals
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.EntryDate)
                .FirstOrDefaultAsync();

            // If no journals exist, return default mood info
            if (latestJournal == null)
            {
                return ServiceResult<CurrentMoodInfo>.SuccessResult(new CurrentMoodInfo
                {
                    Mood = "No entries yet",
                    Icon = "neutral",
                    IsRecent = false,
                    Date = null
                });
            }

            // Create mood info from latest journal
            var moodInfo = new CurrentMoodInfo
            {
                Mood = latestJournal.PrimaryMood,
                IsRecent = latestJournal.EntryDate.Date >= DateTime.Today.AddDays(-1), // Recent if within last day
                Date = latestJournal.EntryDate
            };

            // Set appropriate icon based on mood
            moodInfo.Icon = latestJournal.PrimaryMood.ToLower() switch
            {
                "positive" => "happy",
                "negative" => "sad",
                _ => "neutral"
            };

            return ServiceResult<CurrentMoodInfo>.SuccessResult(moodInfo);
        }
        catch (Exception ex)
        {
            return ServiceResult<CurrentMoodInfo>.FailureResult($"Error getting current mood: {ex.Message}");
        }
    }

    // Helper method to calculate streaks from journal dates
    private (int currentStreak, int longestStreak) CalculateStreaks(List<Journal> journals)
    {
        if (!journals.Any()) return (0, 0);

        // Extract and order journal dates
        var journalDates = journals.Select(j => j.EntryDate.Date).OrderBy(d => d).ToList();

        // Calculate streaks
        var longestStreak = CalculateLongestStreak(journalDates);
        var currentStreak = CalculateCurrentStreak(journalDates);

        return (currentStreak, longestStreak);
    }

    // Calculates the longest streak of consecutive journal entries
    private int CalculateLongestStreak(List<DateTime> journalDates)
    {
        if (!journalDates.Any()) return 0;

        int longestStreak = 1;
        int currentStreak = 1;

        // Iterate through dates to find consecutive days
        for (int i = 1; i < journalDates.Count; i++)
        {
            var diff = (journalDates[i] - journalDates[i - 1]).Days;

            if (diff == 1)
            {
                // Consecutive day found
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else if (diff > 1)
            {
                // Streak broken (gap of more than 1 day)
                currentStreak = 1;
            }
        }

        return longestStreak;
    }

    // Calculates the current streak (consecutive days up to most recent journal)
    private int CalculateCurrentStreak(List<DateTime> journalDates)
    {
        if (!journalDates.Any()) return 0;

        var today = DateTime.Today;
        var mostRecentJournal = journalDates.Last();

        // Check if streak is broken (most recent journal is more than 1 day ago)
        var daysSinceLastJournal = (today - mostRecentJournal).Days;

        // IMPORTANT FIX: Today is not counted as missed until the day is over
        // If today's journal doesn't exist and it's still today, streak should continue
        if (daysSinceLastJournal > 1)
        {
            // Streak broken - last journal was 2+ days ago
            return 0;
        }

        // Count consecutive days backwards starting from most recent journal
        return CountConsecutiveDaysBackwards(journalDates, mostRecentJournal);
    }

    // Counts consecutive days backwards from a start date
    private int CountConsecutiveDaysBackwards(List<DateTime> journalDates, DateTime startDate)
    {
        var streak = 1;
        var currentDate = startDate.AddDays(-1);

        // Check previous days for consecutive journals
        while (journalDates.Contains(currentDate))
        {
            streak++;
            currentDate = currentDate.AddDays(-1);
        }

        return streak;
    }

    // Calculates missed days (days without journals) in the current month
    private int CalculateMissedDaysThisMonth(List<Journal> journals)
    {
        var today = DateTime.Today;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        // IMPORTANT FIX: Only count up to yesterday
        // Today is not a "missed day" until tomorrow
        var yesterday = today.AddDays(-1);

        // If yesterday is before the start of month, no missed days yet
        if (yesterday < firstDayOfMonth)
        {
            return 0;
        }

        // Generate all dates from first day of month to yesterday
        var datesInMonth = new List<DateTime>();
        for (var date = firstDayOfMonth; date <= yesterday; date = date.AddDays(1))
        {
            datesInMonth.Add(date);
        }

        // Get set of dates that have journals
        var journalDates = journals.Select(j => j.EntryDate.Date).ToHashSet();

        // Count dates without journals
        var missedDays = datesInMonth.Count(date => !journalDates.Contains(date));

        return missedDays;
    }

    // Data transfer object for comprehensive analytics data
    public class AnalyticsData
    {
        // Total number of journal entries in the date range
        public int TotalEntries { get; set; }

        // Mood distribution data
        public MoodDistributionData MoodDistribution { get; set; } = new();

        // Word count trend data
        public WordCountTrendData WordTrend { get; set; } = new();

        // List of journal entries
        public List<Journal> Journals { get; set; } = new();
    }
}