using System;
using System.Collections.Generic;
using System.Linq;

namespace PocketCity.Competition
{
    public enum WarPhase
    {
        Preparation,
        Battle,
        Ended
    }

    public class WarCard
    {
        public string CardId { get; set; }
        public int AttackPower { get; set; }
        public int DefensePower { get; set; }
        public int EnergyCost { get; set; }
    }

    public class ClubMember
    {
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public int AttackScore { get; set; }
        public int DefenseScore { get; set; }
        public bool HasShield { get; set; }
        public DateTime ShieldExpiry { get; set; }
    }

    public class Club
    {
        public string ClubId { get; set; }
        public string ClubName { get; set; }
        public List<ClubMember> Members { get; set; }
        public int TotalWarScore { get; set; }
    }

    public class ClubWars
    {
        private WarPhase currentPhase;
        private DateTime phaseStartTime;
        private DateTime phaseEndTime;
        private Club playerClub;
        private Club opponentClub;
        private const int PREPARATION_DURATION_HOURS = 24;
        private const int BATTLE_DURATION_HOURS = 48;
        private const int SHIELD_DURATION_HOURS = 12;
        private const int MAX_ENERGY = 60;
        private int currentEnergy;

        public WarPhase CurrentPhase => currentPhase;
        public Club PlayerClub => playerClub;
        public Club OpponentClub => opponentClub;
        public int CurrentEnergy => currentEnergy;

        public ClubWars()
        {
            playerClub = new Club { Members = new List<ClubMember>() };
            opponentClub = new Club { Members = new List<ClubMember>() };
            currentEnergy = MAX_ENERGY;
            currentPhase = WarPhase.Preparation;
            phaseStartTime = DateTime.UtcNow;
            phaseEndTime = phaseStartTime.AddHours(PREPARATION_DURATION_HOURS);
        }

        public void StartWar(Club player, Club opponent)
        {
            playerClub = player;
            opponentClub = opponent;
            currentPhase = WarPhase.Preparation;
            phaseStartTime = DateTime.UtcNow;
            phaseEndTime = phaseStartTime.AddHours(PREPARATION_DURATION_HOURS);
            currentEnergy = MAX_ENERGY;
            ResetScores();
        }

        private void ResetScores()
        {
            playerClub.TotalWarScore = 0;
            opponentClub.TotalWarScore = 0;
            foreach (var member in playerClub.Members)
            {
                member.AttackScore = 0;
                member.DefenseScore = 0;
            }
            foreach (var member in opponentClub.Members)
            {
                member.AttackScore = 0;
                member.DefenseScore = 0;
            }
        }

        public void UpdatePhase()
        {
            if (DateTime.UtcNow < phaseEndTime)
                return;

            if (currentPhase == WarPhase.Preparation)
            {
                currentPhase = WarPhase.Battle;
                phaseStartTime = DateTime.UtcNow;
                phaseEndTime = phaseStartTime.AddHours(BATTLE_DURATION_HOURS);
            }
            else if (currentPhase == WarPhase.Battle)
            {
                currentPhase = WarPhase.Ended;
            }
        }

        public bool LaunchAttack(string attackerId, string targetId, WarCard card)
        {
            if (currentPhase != WarPhase.Battle)
                return false;

            if (currentEnergy < card.EnergyCost)
                return false;

            var target = opponentClub.Members.FirstOrDefault(m => m.MemberId == targetId);
            if (target == null)
                return false;

            if (target.HasShield && DateTime.UtcNow < target.ShieldExpiry)
                return false;

            currentEnergy -= card.EnergyCost;
            int attackPoints = card.AttackPower;

            var attacker = playerClub.Members.FirstOrDefault(m => m.MemberId == attackerId);
            if (attacker != null)
            {
                attacker.AttackScore += attackPoints;
            }

            playerClub.TotalWarScore += attackPoints;

            // 对手也会反击并得分
            int counterPoints = attackPoints / 2;
            opponentClub.TotalWarScore += counterPoints;

            return true;
        }

        public bool DeployDefense(string defenderId, WarCard card)
        {
            if (currentPhase != WarPhase.Battle)
                return false;

            if (currentEnergy < card.EnergyCost)
                return false;

            var defender = playerClub.Members.FirstOrDefault(m => m.MemberId == defenderId);
            if (defender == null)
                return false;

            currentEnergy -= card.EnergyCost;
            defender.DefenseScore += card.DefensePower;
            playerClub.TotalWarScore += card.DefensePower;
            return true;
        }

        public bool ActivateShield(string memberId)
        {
            var member = playerClub.Members.FirstOrDefault(m => m.MemberId == memberId);
            if (member == null || member.HasShield)
                return false;

            member.HasShield = true;
            member.ShieldExpiry = DateTime.UtcNow.AddHours(SHIELD_DURATION_HOURS);
            return true;
        }

        public void RegenerateEnergy(int amount)
        {
            currentEnergy = Math.Min(currentEnergy + amount, MAX_ENERGY);
        }

        public Club GetWinner()
        {
            if (currentPhase != WarPhase.Ended)
                return null;

            return playerClub.TotalWarScore > opponentClub.TotalWarScore ? playerClub : opponentClub;
        }

        public TimeSpan GetPhaseTimeRemaining()
        {
            var remaining = phaseEndTime - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public bool IsShieldActive(string memberId)
        {
            var member = playerClub.Members.FirstOrDefault(m => m.MemberId == memberId);
            if (member == null)
                return false;

            return member.HasShield && DateTime.UtcNow < member.ShieldExpiry;
        }
    }
}
