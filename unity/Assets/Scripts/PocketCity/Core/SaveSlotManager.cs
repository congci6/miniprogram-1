using System;
using UnityEngine;

namespace PocketCity.Core
{
    public static class SaveSlotManager
    {
        private const string SaveKeyPrefix = "pocket_city_save_";
        private const int MaxSlots = 3;

        public static string GetSaveKey(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index must be between 0 and {MaxSlots - 1}");
            }
            return SaveKeyPrefix + slotIndex;
        }

        public static bool HasSave(int slotIndex)
        {
            return PlayerPrefs.HasKey(GetSaveKey(slotIndex));
        }

        public static void DeleteSave(int slotIndex)
        {
            PlayerPrefs.DeleteKey(GetSaveKey(slotIndex));
            PlayerPrefs.Save();
        }

        public static int GetMaxSlots() => MaxSlots;

        public static string GetLegacySaveKey() => "pocket_city_save_v1";

        public static bool HasLegacySave() => PlayerPrefs.HasKey(GetLegacySaveKey());

        public static void MigrateLegacySave()
        {
            if (HasLegacySave() && !HasSave(0))
            {
                var legacyData = PlayerPrefs.GetString(GetLegacySaveKey());
                PlayerPrefs.SetString(GetSaveKey(0), legacyData);
                PlayerPrefs.DeleteKey(GetLegacySaveKey());
                PlayerPrefs.Save();
            }
        }
    }
}
