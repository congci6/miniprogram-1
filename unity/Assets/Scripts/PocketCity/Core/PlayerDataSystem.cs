using UnityEngine;

namespace PocketCity.Core
{
    public class PlayerDataSystem : MonoBehaviour
    {
        public static PlayerDataSystem Instance { get; private set; }

        [SerializeField] private int level = 1;
        [SerializeField] private int population = 0;
        [SerializeField] private int gold = 1000;
        [SerializeField] private int materials = 100;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public int GetLevel() => level;
        public int GetPopulation() => population;
        public int GetGold() => gold;
        public int GetMaterials() => materials;

        public void SetLevel(int value) => level = value;
        public void SetPopulation(int value) => population = value;

        public void AddGold(int amount) { if (amount > 0) gold += amount; }
        public void SpendGold(int amount) { if (amount > 0 && gold >= amount) gold -= amount; }

        public void AddMaterials(int amount) { if (amount > 0) materials += amount; }
        public void SpendMaterials(int amount) { if (amount > 0 && materials >= amount) materials -= amount; }
    }
}
