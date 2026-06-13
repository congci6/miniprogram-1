using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PocketCity.Simulation;
using PocketCity.Core;

namespace PocketCity.Disaster
{
    /// <summary>
    /// 7种灾难的独立机制实现
    /// </summary>
    public class DifferentiatedDisasterSystem : MonoBehaviour
    {
        [SerializeField] private CitySimulationCore simulation;
        [SerializeField] private Camera mainCamera;

        /// <summary>
        /// 地震：全城晃动，范围大伤害低，随机摧毁老旧建筑
        /// </summary>
        public void TriggerEarthquake(Vector3 epicenter, int level)
        {
            StartCoroutine(EarthquakeSequence(epicenter, level));
        }

        private IEnumerator EarthquakeSequence(Vector3 epicenter, int level)
        {
            // 屏幕震动
            if (mainCamera != null)
            {
                StartCoroutine(ScreenShake(1f + level * 0.5f, 0.3f + level * 0.1f));
            }

            // 播放音效
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.DisasterWarning);
            }

            // 影响范围
            int radius = 20 + level * 5;
            float damage = 10 + level * 5; // 低伤害

            // 随机摧毁老旧建筑
            var affectedBuildings = GetBuildingsInRadius(epicenter, radius);
            foreach (var building in affectedBuildings)
            {
                // 老旧建筑（AgeDays > 100）更容易被摧毁
                float destroyChance = building.AgeDays > 100 ? 0.3f : 0.1f;
                if (Random.value < destroyChance)
                {
                    simulation.DamageBuilding(building.Id, (int)damage);
                }
            }

            yield return new WaitForSeconds(3f);
        }

        /// <summary>
        /// 龙卷风：从边缘进入，S型路径移动，秒杀路径建筑
        /// </summary>
        public void TriggerTornado(int level)
        {
            StartCoroutine(TornadoSequence(level));
        }

        private IEnumerator TornadoSequence(int level)
        {
            // 从地图边缘随机点开始
            Vector3 startPos = GetMapEdgePosition();
            Vector3 currentPos = startPos;

            // S型路径
            float duration = 10f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // S型移动
                float t = elapsed / duration;
                float xOffset = Mathf.Sin(t * Mathf.PI * 4) * 5f;
                currentPos += Vector3.forward * Time.deltaTime * 5f;
                currentPos.x = startPos.x + xOffset;

                // 摧毁路径上的建筑
                var building = GetBuildingAt(currentPos);
                if (building != null)
                {
                    simulation.DamageBuilding(building.Id, 999); // 秒杀
                }

                // 视觉效果
                if (VFX.ParticleEffectSystem.Instance != null)
                {
                    VFX.ParticleEffectSystem.Instance.PlayEffect(VFX.EffectType.Disaster, currentPos);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// 陨石：单格高伤害，砸出弹坑（永久装饰）
        /// </summary>
        public void TriggerMeteor(Vector3 targetPos, int level)
        {
            StartCoroutine(MeteorSequence(targetPos, level));
        }

        private IEnumerator MeteorSequence(Vector3 targetPos, int level)
        {
            // 预警
            yield return new WaitForSeconds(1f);

            // 坠落特效
            if (VFX.ParticleEffectSystem.Instance != null)
            {
                VFX.ParticleEffectSystem.Instance.PlayEffect(VFX.EffectType.Disaster, targetPos + Vector3.up * 50f);
            }

            yield return new WaitForSeconds(0.5f);

            // 砸中
            var building = GetBuildingAt(targetPos);
            if (building != null)
            {
                simulation.DamageBuilding(building.Id, 100 + level * 20);
            }

            // 创建弹坑（永久装饰）
            CreateCrater(targetPos);
        }

        /// <summary>
        /// 火灾：单栋起火，每10秒扩散到邻格，消防局可阻止
        /// </summary>
        public void TriggerFire(Vector3 startPos)
        {
            StartCoroutine(FireSequence(startPos));
        }

        private IEnumerator FireSequence(Vector3 startPos)
        {
            List<Vector3> burningPositions = new List<Vector3> { startPos };
            HashSet<Vector3> burned = new HashSet<Vector3>();

            while (burningPositions.Count > 0)
            {
                var newBurning = new List<Vector3>();

                foreach (var pos in burningPositions)
                {
                    // 损坏建筑
                    var building = GetBuildingAt(pos);
                    if (building != null)
                    {
                        simulation.DamageBuilding(building.Id, 20);
                    }

                    burned.Add(pos);

                    // 检查是否被消防局覆盖
                    if (IsFireProtected(pos))
                    {
                        continue; // 消防局阻止扩散
                    }

                    // 扩散到邻格
                    var neighbors = GetNeighborPositions(pos);
                    foreach (var neighbor in neighbors)
                    {
                        if (!burned.Contains(neighbor) && Random.value < 0.5f)
                        {
                            newBurning.Add(neighbor);
                        }
                    }
                }

                burningPositions = newBurning;
                yield return new WaitForSeconds(10f);
            }
        }

        /// <summary>
        /// 外星人：随机抓走1栋建筑，60秒后返回
        /// </summary>
        public void TriggerAlien()
        {
            StartCoroutine(AlienSequence());
        }

        private IEnumerator AlienSequence()
        {
            // 随机选择建筑
            var building = GetRandomBuilding();
            if (building == null) yield break;

            // 抓走（隐藏建筑）
            var buildingGO = FindBuildingGameObject(building.Id);
            if (buildingGO != null)
            {
                buildingGO.SetActive(false);
            }

            // 60秒后返回
            yield return new WaitForSeconds(60f);

            if (buildingGO != null)
            {
                buildingGO.SetActive(true);
            }
        }

        /// <summary>
        /// 机器人：攻击工业区，摧毁后掉落材料
        /// </summary>
        public void TriggerRobot(int level)
        {
            StartCoroutine(RobotSequence(level));
        }

        private IEnumerator RobotSequence(int level)
        {
            // 寻找工业建筑
            var industrialBuildings = GetBuildingsByCategory(BuildingCategory.Industrial);

            int attackCount = Mathf.Min(3 + level, industrialBuildings.Count);

            for (int i = 0; i < attackCount; i++)
            {
                var building = industrialBuildings[Random.Range(0, industrialBuildings.Count)];
                simulation.DamageBuilding(building.Id, 50);

                // 掉落材料
                if (Production.StorageSystem.Instance != null)
                {
                    Production.StorageSystem.Instance.AddItem("metal", Random.Range(1, 3));
                }

                yield return new WaitForSeconds(2f);
            }
        }

        /// <summary>
        /// 怪兽：攻击地标建筑，需要多个警察局联合驱离
        /// </summary>
        public void TriggerMonster()
        {
            StartCoroutine(MonsterSequence());
        }

        private IEnumerator MonsterSequence()
        {
            // 寻找地标建筑
            var landmark = GetLandmarkBuilding();
            if (landmark == null) yield break;

            // 持续攻击
            int attacksCount = 0;
            int maxAttacks = 10;

            while (attacksCount < maxAttacks)
            {
                simulation.DamageBuilding(landmark.Id, 10);

                // 检查警察局数量
                int policeCount = GetPoliceStationCount();
                if (policeCount >= 3)
                {
                    // 驱离成功
                    break;
                }

                attacksCount++;
                yield return new WaitForSeconds(3f);
            }
        }

        // === 辅助方法 ===

        private IEnumerator ScreenShake(float duration, float magnitude)
        {
            Vector3 originalPos = mainCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                mainCamera.transform.position = originalPos + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCamera.transform.position = originalPos;
        }

        private List<PlacedBuilding> GetBuildingsInRadius(Vector3 center, int radius)
        {
            // TODO: 实现范围查询
            return new List<PlacedBuilding>();
        }

        private PlacedBuilding GetBuildingAt(Vector3 pos)
        {
            // TODO: 实现位置查询
            return null;
        }

        private Vector3 GetMapEdgePosition()
        {
            return Vector3.zero; // TODO: 实现边缘位置
        }

        private void CreateCrater(Vector3 pos)
        {
            // TODO: 创建弹坑装饰物
        }

        private bool IsFireProtected(Vector3 pos)
        {
            // TODO: 检查消防局覆盖
            return false;
        }

        private List<Vector3> GetNeighborPositions(Vector3 pos)
        {
            return new List<Vector3>
            {
                pos + Vector3.forward,
                pos + Vector3.back,
                pos + Vector3.left,
                pos + Vector3.right
            };
        }

        private PlacedBuilding GetRandomBuilding()
        {
            if (simulation == null || simulation.Buildings.Count == 0) return null;
            return simulation.Buildings[Random.Range(0, simulation.Buildings.Count)];
        }

        private GameObject FindBuildingGameObject(string buildingId)
        {
            // TODO: 查找建筑GameObject
            return null;
        }

        private List<PlacedBuilding> GetBuildingsByCategory(BuildingCategory category)
        {
            return simulation?.Buildings.Where(b =>
            {
                var def = simulation.Config.GetBuilding(b.ConfigId);
                return def != null && def.Category == category;
            }).ToList() ?? new List<PlacedBuilding>();
        }

        private PlacedBuilding GetLandmarkBuilding()
        {
            // TODO: 查找地标建筑
            return GetRandomBuilding();
        }

        private int GetPoliceStationCount()
        {
            return GetBuildingsByCategory(BuildingCategory.Service).Count;
        }
    }
}
