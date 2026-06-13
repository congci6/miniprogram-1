using UnityEngine;
using System;
using PocketCity.Core;

namespace PocketCity.Specialized
{
    public enum KeySource
    {
        CargoShipment,
        Achievement,
        Event
    }

    public class GoldenKeySystem : MonoBehaviour
    {
        public static GoldenKeySystem Instance { get; private set; }

        [SerializeField] private int goldenKeys = 0;
        [SerializeField] private int maxKeysFromCargo = 100;
        [SerializeField] private float cargoKeyDropRate = 0.15f; // 15% chance per cargo

        public event Action<int> OnKeysChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public int GetKeyCount() => goldenKeys;

        public bool HasKeys(int amount) => goldenKeys >= amount;

        public void AddKeys(int amount, KeySource source)
        {
            if (amount <= 0) return;

            if (source == KeySource.CargoShipment)
            {
                var remainingCargoKeys = maxKeysFromCargo - goldenKeys;
                if (remainingCargoKeys <= 0)
                {
                    return;
                }

                amount = Mathf.Min(amount, remainingCargoKeys);
            }

            goldenKeys += amount;
            OnKeysChanged?.Invoke(goldenKeys);
            Debug.Log($"Golden Keys +{amount} from {source}. Total: {goldenKeys}");
        }

        public bool SpendKeys(int amount)
        {
            if (amount <= 0 || !HasKeys(amount)) return false;

            goldenKeys -= amount;
            OnKeysChanged?.Invoke(goldenKeys);
            Debug.Log($"Golden Keys -{amount}. Remaining: {goldenKeys}");
            return true;
        }

        public void OnCargoShipmentCompleted()
        {
            if (UnityEngine.Random.value <= cargoKeyDropRate)
            {
                int keysEarned = UnityEngine.Random.Range(1, 3); // 1-2 keys per cargo
                AddKeys(keysEarned, KeySource.CargoShipment);
            }
        }

        public float GetCargoDropRate() => cargoKeyDropRate;

        public int GetMaxKeysFromCargo() => maxKeysFromCargo;
    }
}
