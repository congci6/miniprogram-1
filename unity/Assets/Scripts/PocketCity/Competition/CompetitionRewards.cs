using System;
using System.Collections.Generic;

namespace PocketCity.Competition
{
    public enum RewardType
    {
        Simcash,
        PlatinumKey,
        WarChest,
        SeasonalToken
    }

    public class Reward
    {
        public RewardType Type { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; }
    }

    public class CompetitionRewards
    {
        private static readonly Dictionary<LeagueLevel, Dictionary<int, List<Reward>>> contestRewards = new Dictionary<LeagueLevel, Dictionary<int, List<Reward>>>
        {
            {
                LeagueLevel.Neighborhood, new Dictionary<int, List<Reward>>
                {
                    { 1, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 500 }, new Reward { Type = RewardType.PlatinumKey, Amount = 2 } } },
                    { 2, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 350 }, new Reward { Type = RewardType.PlatinumKey, Amount = 1 } } },
                    { 3, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 200 } } }
                }
            },
            {
                LeagueLevel.Suburb, new Dictionary<int, List<Reward>>
                {
                    { 1, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 800 }, new Reward { Type = RewardType.PlatinumKey, Amount = 3 } } },
                    { 2, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 550 }, new Reward { Type = RewardType.PlatinumKey, Amount = 2 } } },
                    { 3, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 350 }, new Reward { Type = RewardType.PlatinumKey, Amount = 1 } } }
                }
            },
            {
                LeagueLevel.SmallTown, new Dictionary<int, List<Reward>>
                {
                    { 1, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 1200 }, new Reward { Type = RewardType.PlatinumKey, Amount = 4 } } },
                    { 2, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 850 }, new Reward { Type = RewardType.PlatinumKey, Amount = 3 } } },
                    { 3, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 550 }, new Reward { Type = RewardType.PlatinumKey, Amount = 2 } } }
                }
            },
            {
                LeagueLevel.Town, new Dictionary<int, List<Reward>>
                {
                    { 1, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 1800 }, new Reward { Type = RewardType.PlatinumKey, Amount = 6 } } },
                    { 2, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 1300 }, new Reward { Type = RewardType.PlatinumKey, Amount = 4 } } },
                    { 3, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 900 }, new Reward { Type = RewardType.PlatinumKey, Amount = 3 } } }
                }
            },
            {
                LeagueLevel.City, new Dictionary<int, List<Reward>>
                {
                    { 1, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 2500 }, new Reward { Type = RewardType.PlatinumKey, Amount = 8 } } },
                    { 2, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 1800 }, new Reward { Type = RewardType.PlatinumKey, Amount = 6 } } },
                    { 3, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 1300 }, new Reward { Type = RewardType.PlatinumKey, Amount = 4 } } }
                }
            },
            {
                LeagueLevel.Metropolis, new Dictionary<int, List<Reward>>
                {
                    { 1, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 3500 }, new Reward { Type = RewardType.PlatinumKey, Amount = 12 } } },
                    { 2, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 2500 }, new Reward { Type = RewardType.PlatinumKey, Amount = 8 } } },
                    { 3, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 1800 }, new Reward { Type = RewardType.PlatinumKey, Amount = 6 } } }
                }
            },
            {
                LeagueLevel.Megapolis, new Dictionary<int, List<Reward>>
                {
                    { 1, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 5000 }, new Reward { Type = RewardType.PlatinumKey, Amount = 15 } } },
                    { 2, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 3500 }, new Reward { Type = RewardType.PlatinumKey, Amount = 12 } } },
                    { 3, new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 2500 }, new Reward { Type = RewardType.PlatinumKey, Amount = 8 } } }
                }
            }
        };

        private static readonly Dictionary<int, List<Reward>> warRewards = new Dictionary<int, List<Reward>>
        {
            { 1, new List<Reward> { new Reward { Type = RewardType.WarChest, Amount = 3 }, new Reward { Type = RewardType.Simcash, Amount = 2000 } } },
            { 2, new List<Reward> { new Reward { Type = RewardType.WarChest, Amount = 2 }, new Reward { Type = RewardType.Simcash, Amount = 1500 } } },
            { 3, new List<Reward> { new Reward { Type = RewardType.WarChest, Amount = 1 }, new Reward { Type = RewardType.Simcash, Amount = 1000 } } }
        };

        public static List<Reward> GetContestRewards(LeagueLevel league, int rank)
        {
            if (!contestRewards.ContainsKey(league))
                return new List<Reward>();

            var leagueRewards = contestRewards[league];

            if (leagueRewards.ContainsKey(rank))
                return new List<Reward>(leagueRewards[rank]);

            if (rank <= 10)
                return new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 100 * (11 - rank) } };

            return new List<Reward>();
        }

        public static List<Reward> GetWarRewards(int rank)
        {
            if (warRewards.ContainsKey(rank))
                return new List<Reward>(warRewards[rank]);

            if (rank <= 10)
                return new List<Reward> { new Reward { Type = RewardType.Simcash, Amount = 500 } };

            return new List<Reward>();
        }

        public static List<Reward> GetWarParticipationRewards(int personalScore)
        {
            var rewards = new List<Reward>();

            if (personalScore >= 10000)
                rewards.Add(new Reward { Type = RewardType.WarChest, Amount = 2, Description = "High participation" });
            else if (personalScore >= 5000)
                rewards.Add(new Reward { Type = RewardType.WarChest, Amount = 1, Description = "Active participation" });

            if (personalScore >= 1000)
                rewards.Add(new Reward { Type = RewardType.Simcash, Amount = personalScore / 10 });

            return rewards;
        }

        public static Reward OpenWarChest()
        {
            var random = new System.Random();
            var value = random.Next(0, 100);

            if (value < 5)
                return new Reward { Type = RewardType.PlatinumKey, Amount = 1, Description = "Rare!" };
            else if (value < 30)
                return new Reward { Type = RewardType.Simcash, Amount = random.Next(200, 500), Description = "High amount" };
            else
                return new Reward { Type = RewardType.Simcash, Amount = random.Next(50, 200), Description = "Standard amount" };
        }
    }
}
