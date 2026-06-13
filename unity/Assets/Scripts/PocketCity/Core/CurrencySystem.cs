using System;
using UnityEngine;

namespace PocketCity.Core
{
    public class CurrencySystem : MonoBehaviour
    {
        public static CurrencySystem Instance { get; private set; }

        [SerializeField] private int coins;
        [SerializeField] private int simcash;
        [SerializeField] private int goldenKeys;
        [SerializeField] private int level = 1;
        [SerializeField] private int population;
        [SerializeField] private int materials;

        public int Coins => coins;
        public int Simcash => simcash;
        public int GoldenKeys => goldenKeys;
        public int Level => level;
        public int Population => population;
        public int Materials => materials;

        public event Action<string, int> OnCurrencyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool CanAfford(int amount) => coins >= amount;

        public bool SpendCoins(int amount)
        {
            if (coins < amount) return false;
            coins -= amount;
            OnCurrencyChanged?.Invoke("coins", coins);
            return true;
        }

        public void AddCoins(int amount)
        {
            coins += amount;
            OnCurrencyChanged?.Invoke("coins", coins);
        }

        public bool SpendSimcash(int amount)
        {
            if (simcash < amount) return false;
            simcash -= amount;
            OnCurrencyChanged?.Invoke("simcash", simcash);
            return true;
        }

        public void AddSimcash(int amount)
        {
            simcash += amount;
            OnCurrencyChanged?.Invoke("simcash", simcash);
        }

        public void AddGoldenKeys(int amount)
        {
            goldenKeys += amount;
            OnCurrencyChanged?.Invoke("goldenKeys", goldenKeys);
        }

        public void SetLevel(int value) { level = value; }
        public void SetPopulation(int value) { population = value; }

        public bool SpendGold(int amount)
        {
            if (coins < amount) return false;
            coins -= amount;
            OnCurrencyChanged?.Invoke("gold", coins);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount > 0) coins += amount;
            OnCurrencyChanged?.Invoke("gold", coins);
        }

        public bool SpendMaterials(int amount)
        {
            if (materials < amount) return false;
            materials -= amount;
            OnCurrencyChanged?.Invoke("materials", materials);
            return true;
        }

        public void AddMaterials(int amount)
        {
            if (amount > 0) materials += amount;
            OnCurrencyChanged?.Invoke("materials", materials);
        }
    }
}
