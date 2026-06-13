using UnityEngine;
using PocketCity.Simulation;

namespace PocketCity.Economy
{
    /// <summary>
    /// 统一货币管理器 - 简化版，直接使用Metrics.Cash
    /// </summary>
    public class UnifiedCurrencyManager : MonoBehaviour
    {
        public static UnifiedCurrencyManager Instance { get; private set; }

        [SerializeField] private CitySimulationCore simulation;

        // 内部货币追踪
        private int goldenKeys = 0;
        private int premium = 0;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (simulation == null)
            {
                var controller = FindObjectOfType<PocketCity.Runtime.CityGameController>();
                simulation = controller != null ? controller.Simulation : null;
            }
        }

        public void AddCash(int amount)
        {
            if (simulation != null)
            {
                simulation.Metrics.Cash += amount;
            }
        }

        public bool SpendCash(int amount)
        {
            if (simulation == null)
                return false;

            if (simulation.Metrics.Cash < amount)
                return false;

            simulation.Metrics.Cash -= amount;
            return true;
        }

        public int GetCash()
        {
            if (simulation != null)
                return simulation.Metrics.Cash;
            return 0;
        }

        public void AddGoldenKeys(int amount)
        {
            goldenKeys += amount;
        }

        public bool SpendGoldenKeys(int amount)
        {
            if (goldenKeys < amount)
                return false;
            goldenKeys -= amount;
            return true;
        }

        public int GetGoldenKeys()
        {
            return goldenKeys;
        }

        public void AddPremium(int amount)
        {
            premium += amount;
        }

        public bool SpendPremium(int amount)
        {
            if (premium < amount)
                return false;
            premium -= amount;
            return true;
        }

        public int GetPremium()
        {
            return premium;
        }
    }
}
