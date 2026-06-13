using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PocketCity.Disaster
{
    [Serializable]
    public class DisasterCard
    {
        public DisasterType type;
        public int level;
        public string cardId;
        public Sprite cardIcon;

        public DisasterCard(DisasterType type, int level)
        {
            this.type = type;
            this.level = level;
            this.cardId = $"{type}_{level}";
        }
    }

    public class DisasterCardSystem : MonoBehaviour
    {
        [SerializeField] private int maxInventorySize = 50;
        private List<DisasterCard> inventory = new List<DisasterCard>();
        private DisasterSystem disasterSystem;

        public event Action<DisasterCard> OnCardCollected;
        public event Action<DisasterCard> OnCardUsed;
        public event Action OnInventoryChanged;

        private void Awake()
        {
            disasterSystem = GetComponent<DisasterSystem>();
        }

        public bool CollectCard(DisasterType type, int level)
        {
            if (inventory.Count >= maxInventorySize)
                return false;

            DisasterCard card = new DisasterCard(type, level);
            inventory.Add(card);

            OnCardCollected?.Invoke(card);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool UseCard(string cardId, Vector3 targetPosition)
        {
            DisasterCard card = inventory.Find(c => c.cardId == cardId);
            if (card == null)
                return false;

            inventory.Remove(card);

            disasterSystem?.TriggerDisaster(card.type, card.level, targetPosition);
            OnCardUsed?.Invoke(card);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool UseCardForClubWar(string cardId, Vector3 enemyBasePosition)
        {
            return UseCard(cardId, enemyBasePosition);
        }

        public int GetCardCount(string cardId)
        {
            return inventory.Count(c => c.cardId == cardId);
        }

        public List<DisasterCard> GetInventory()
        {
            return new List<DisasterCard>(inventory);
        }

        public Dictionary<string, int> GetCardCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var card in inventory)
            {
                if (!counts.ContainsKey(card.cardId))
                    counts[card.cardId] = 0;
                counts[card.cardId]++;
            }
            return counts;
        }

        public void ClearInventory()
        {
            inventory.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}
