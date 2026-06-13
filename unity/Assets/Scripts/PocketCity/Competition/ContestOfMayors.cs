using System;
using System.Collections.Generic;
using System.Linq;

namespace PocketCity.Competition
{
    public enum LeagueLevel
    {
        Neighborhood = 0,
        Suburb = 1,
        SmallTown = 2,
        Town = 3,
        City = 4,
        Metropolis = 5,
        Megapolis = 6
    }

    public enum TaskType
    {
        Production,
        CollectTax,
        Upgrade,
        Disaster
    }

    public class ContestTask
    {
        public TaskType Type { get; set; }
        public string Description { get; set; }
        public int Points { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class PlayerRanking
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int TotalPoints { get; set; }
        public int Rank { get; set; }
    }

    public class ContestOfMayors
    {
        private LeagueLevel currentLeague;
        private List<ContestTask> availableTasks;
        private int totalPoints;
        private DateTime contestStartTime;
        private DateTime contestEndTime;
        private List<PlayerRanking> leaderboard;
        private const int CONTEST_DURATION_HOURS = 168; // 7 days
        private const int PROMOTION_THRESHOLD = 3;
        private const int DEMOTION_THRESHOLD = 15;

        public LeagueLevel CurrentLeague => currentLeague;
        public int TotalPoints => totalPoints;
        public List<ContestTask> AvailableTasks => availableTasks;
        public List<PlayerRanking> Leaderboard => leaderboard;

        public ContestOfMayors(LeagueLevel startingLeague = LeagueLevel.Neighborhood)
        {
            currentLeague = startingLeague;
            availableTasks = new List<ContestTask>();
            leaderboard = new List<PlayerRanking>();
            totalPoints = 0;
        }

        public void StartContest()
        {
            contestStartTime = DateTime.UtcNow;
            contestEndTime = contestStartTime.AddHours(CONTEST_DURATION_HOURS);
            totalPoints = 0;
            GenerateTasks();
        }

        private void GenerateTasks()
        {
            availableTasks.Clear();
            int taskCount = 10 + (int)currentLeague * 2;
            int basePoints = 1000 + (int)currentLeague * 500;

            var random = new System.Random();
            for (int i = 0; i < taskCount; i++)
            {
                var taskType = (TaskType)random.Next(0, 4);
                availableTasks.Add(new ContestTask
                {
                    Type = taskType,
                    Description = GetTaskDescription(taskType),
                    Points = basePoints + random.Next(-200, 500),
                    IsCompleted = false
                });
            }
        }

        private string GetTaskDescription(TaskType type)
        {
            switch (type)
            {
                case TaskType.Production:
                    return "Produce items in factories";
                case TaskType.CollectTax:
                    return "Collect taxes from buildings";
                case TaskType.Upgrade:
                    return "Upgrade residential buildings";
                case TaskType.Disaster:
                    return "Launch disaster on city";
                default:
                    return "Complete task";
            }
        }

        public bool CompleteTask(int taskIndex)
        {
            if (taskIndex < 0 || taskIndex >= availableTasks.Count)
                return false;

            var task = availableTasks[taskIndex];
            if (task.IsCompleted)
                return false;

            task.IsCompleted = true;
            totalPoints += task.Points;
            return true;
        }

        public void UpdateLeaderboard(List<PlayerRanking> rankings)
        {
            leaderboard = rankings.OrderByDescending(r => r.TotalPoints).ToList();
            for (int i = 0; i < leaderboard.Count; i++)
            {
                leaderboard[i].Rank = i + 1;
            }
        }

        public LeagueLevel EvaluateLeagueChange(int finalRank)
        {
            var previousLeague = currentLeague;

            if (finalRank <= PROMOTION_THRESHOLD && currentLeague < LeagueLevel.Megapolis)
            {
                currentLeague++;
            }
            else if (finalRank >= DEMOTION_THRESHOLD && currentLeague > LeagueLevel.Neighborhood)
            {
                currentLeague--;
            }

            return previousLeague != currentLeague ? currentLeague : previousLeague;
        }

        public bool IsContestActive()
        {
            return DateTime.UtcNow >= contestStartTime && DateTime.UtcNow < contestEndTime;
        }

        public TimeSpan GetTimeRemaining()
        {
            return contestEndTime - DateTime.UtcNow;
        }
    }
}
