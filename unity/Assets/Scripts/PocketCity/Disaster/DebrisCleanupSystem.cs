using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PocketCity.Core;

namespace PocketCity.Disaster
{
    /// <summary>
    /// 废墟（占格），30分钟后自动清理或花材料立即清理
    /// </summary>
    [System.Serializable]
    public class Debris
    {
        public string id;
        public GridPos position;
        public float createTime;
        public float autoCleanupTime; // 30分钟后
        public string originalBuildingId;
    }

    public class DebrisCleanupSystem : MonoBehaviour
    {
        public static DebrisCleanupSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float autoCleanupSeconds = 1800f; // 30分钟
        [SerializeField] private int instantCleanupCost = 100; // 金币成本

        [Header("References")]
        [SerializeField] private Simulation.CitySimulationCore simulation;

        private List<Debris> activeDebris = new List<Debris>();
        private Dictionary<string, GameObject> debrisVisuals = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            CheckAutoCleanup();
        }

        /// <summary>
        /// 建筑被摧毁时创建废墟
        /// </summary>
        public void CreateDebris(GridPos position, string originalBuildingId)
        {
            string debrisId = System.Guid.NewGuid().ToString();

            var debris = new Debris
            {
                id = debrisId,
                position = position,
                createTime = Time.time,
                autoCleanupTime = Time.time + autoCleanupSeconds,
                originalBuildingId = originalBuildingId
            };

            activeDebris.Add(debris);

            // 创建废墟视觉
            CreateDebrisVisual(debris);

            // 标记该格为占用
            MarkGridAsDebris(position, debrisId);

            Debug.Log($"创建废墟 {debrisId} 在 {position}，{autoCleanupSeconds / 60f} 分钟后自动清理");
        }

        private void CreateDebrisVisual(Debris debris)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = $"Debris_{debris.id}";
            visual.transform.position = debris.position.ToVector3();
            visual.transform.localScale = Vector3.one * 0.8f;

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // 灰色半透明
            }

            // 添加烟雾粒子效果
            if (VFX.ParticleEffectSystem.Instance != null)
            {
                VFX.ParticleEffectSystem.Instance.PlayEffect(
                    VFX.EffectType.BuildingDestroyed,
                    debris.position.ToVector3()
                );
            }

            debrisVisuals[debris.id] = visual;
        }

        private void CheckAutoCleanup()
        {
            float currentTime = Time.time;

            for (int i = activeDebris.Count - 1; i >= 0; i--)
            {
                var debris = activeDebris[i];
                if (currentTime >= debris.autoCleanupTime)
                {
                    CleanupDebris(debris.id, true);
                }
            }
        }

        /// <summary>
        /// 立即清理废墟（消耗金币或材料）
        /// </summary>
        public bool TryInstantCleanup(string debrisId)
        {
            var debris = activeDebris.Find(d => d.id == debrisId);
            if (debris == null) return false;

            // 检查金币
            if (UnifiedCurrencySystem.Instance == null) return false;

            if (!UnifiedCurrencySystem.Instance.SpendCash(instantCleanupCost))
            {
                Debug.Log($"金币不足，需要 {instantCleanupCost} 金币");
                return false;
            }

            CleanupDebris(debrisId, false);
            return true;
        }

        /// <summary>
        /// 清理废墟
        /// </summary>
        private void CleanupDebris(string debrisId, bool isAuto)
        {
            var debris = activeDebris.Find(d => d.id == debrisId);
            if (debris == null) return;

            // 移除视觉
            if (debrisVisuals.TryGetValue(debrisId, out var visual))
            {
                Destroy(visual);
                debrisVisuals.Remove(debrisId);
            }

            // 释放格子
            UnmarkGrid(debris.position);

            // 移除废墟数据
            activeDebris.Remove(debris);

            string method = isAuto ? "自动清理" : "立即清理";
            Debug.Log($"{method}废墟 {debrisId}");

            // 播放清理特效
            if (VFX.ParticleEffectSystem.Instance != null)
            {
                VFX.ParticleEffectSystem.Instance.PlayEffect(
                    VFX.EffectType.BuildingPlaced,
                    debris.position.ToVector3()
                );
            }
        }

        /// <summary>
        /// 获取废墟信息（用于UI显示）
        /// </summary>
        public string GetDebrisInfo(string debrisId)
        {
            var debris = activeDebris.Find(d => d.id == debrisId);
            if (debris == null) return "";

            float remaining = debris.autoCleanupTime - Time.time;
            int minutes = Mathf.CeilToInt(remaining / 60f);

            return $"废墟\n" +
                   $"📍 位置: ({debris.position.X}, {debris.position.Y})\n" +
                   $"⏱️ 自动清理: {minutes} 分钟\n" +
                   $"💰 立即清理: {instantCleanupCost} 金币";
        }

        /// <summary>
        /// 获取位置的废墟（如果有）
        /// </summary>
        public Debris GetDebrisAt(GridPos pos)
        {
            return activeDebris.Find(d => d.position.Equals(pos));
        }

        /// <summary>
        /// 检查位置是否有废墟
        /// </summary>
        public bool HasDebrisAt(GridPos pos)
        {
            return GetDebrisAt(pos) != null;
        }

        /// <summary>
        /// 获取所有废墟
        /// </summary>
        public List<Debris> GetAllDebris()
        {
            return new List<Debris>(activeDebris);
        }

        /// <summary>
        /// 获取附近废墟影响的幸福度惩罚
        /// </summary>
        public int GetDebrisHappinessPenalty(GridPos pos)
        {
            int penalty = 0;
            int checkRadius = 5;

            foreach (var debris in activeDebris)
            {
                int distance = GridPos.ManhattanDistance(pos, debris.position);
                if (distance <= checkRadius)
                {
                    penalty += Mathf.Max(0, 5 - distance); // 越近惩罚越大
                }
            }

            return penalty;
        }

        private void MarkGridAsDebris(GridPos pos, string debrisId)
        {
            // TODO: 在Grid中标记该格为废墟占用
            if (simulation != null && simulation.Grid != null)
            {
                // simulation.Grid.MarkAsDebris(pos, debrisId);
            }
        }

        private void UnmarkGrid(GridPos pos)
        {
            // TODO: 在Grid中释放该格
            if (simulation != null && simulation.Grid != null)
            {
                // simulation.Grid.UnmarkDebris(pos);
            }
        }
    }
}
