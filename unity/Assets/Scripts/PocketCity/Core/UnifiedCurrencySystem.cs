using UnityEngine;

namespace PocketCity.Core
{
    /// <summary>
    /// 统一货币系统 - 简化为两种货币
    /// Cash: 主货币（税收/交易获得，日常消耗）
    /// Premium: 高级货币（成就/付费获得，加速/稀有用途）
    /// </summary>
    public class UnifiedCurrencySystem : MonoBehaviour
    {
        public static UnifiedCurrencySystem Instance { get; private set; }

        private int cash = 0;           // 主货币（对应原CityMetrics.Cash）
        private int premium = 0;        // 高级货币（对应原simcash）
        private int goldenKeys = 0;     // 金钥匙（稀有货币）

        public event System.Action<int> OnCashChanged;
        public event System.Action<int> OnPremiumChanged;
        public event System.Action<int> OnGoldenKeysChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // === 主货币（Cash） ===
        public int Cash => cash;

        public void AddCash(int amount)
        {
            if (amount <= 0) return;
            cash += amount;
            OnCashChanged?.Invoke(cash);
        }

        public bool SpendCash(int amount)
        {
            if (amount <= 0 || cash < amount) return false;
            cash -= amount;
            OnCashChanged?.Invoke(cash);
            return true;
        }

        // === 高级货币（Premium） ===
        public int Premium => premium;

        public void AddPremium(int amount)
        {
            if (amount <= 0) return;
            premium += amount;
            OnPremiumChanged?.Invoke(premium);
        }

        public bool SpendPremium(int amount)
        {
            if (amount <= 0 || premium < amount) return false;
            premium -= amount;
            OnPremiumChanged?.Invoke(premium);
            return true;
        }

        // 高级货币用途：加速生产
        public int GetSpeedupCost(float remainingTime)
        {
            return Mathf.CeilToInt(remainingTime / 60f); // 1高级货币 = 1分钟
        }

        // === 金钥匙 ===
        public int GoldenKeys => goldenKeys;

        public void AddGoldenKeys(int amount)
        {
            if (amount <= 0) return;
            goldenKeys += amount;
            OnGoldenKeysChanged?.Invoke(goldenKeys);
        }

        public bool SpendGoldenKeys(int amount)
        {
            if (amount <= 0 || goldenKeys < amount) return false;
            goldenKeys -= amount;
            OnGoldenKeysChanged?.Invoke(goldenKeys);
            return true;
        }

        // === 持久化 ===
        public void Save()
        {
            PlayerPrefs.SetInt("Cash", cash);
            PlayerPrefs.SetInt("Premium", premium);
            PlayerPrefs.SetInt("GoldenKeys", goldenKeys);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            cash = PlayerPrefs.GetInt("Cash", 15000);
            premium = PlayerPrefs.GetInt("Premium", 50);
            goldenKeys = PlayerPrefs.GetInt("GoldenKeys", 0);

            OnCashChanged?.Invoke(cash);
            OnPremiumChanged?.Invoke(premium);
            OnGoldenKeysChanged?.Invoke(goldenKeys);
        }
    }
}
