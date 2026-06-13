using UnityEngine;
using System.Collections.Generic;
using PocketCity.Simulation;
using PocketCity.Core;
using PocketCity.UI;

namespace PocketCity.Services
{
    /// <summary>
    /// 服务覆盖可视化系统
    /// </summary>
    public class ServiceCoverageVisualization : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CitySimulationCore simulation;

        [Header("Visualization")]
        [SerializeField] private Material coverageCircleMaterial;
        [SerializeField] private Color fireProtectionColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color policeColor = new Color(0f, 0f, 1f, 0.3f);
        [SerializeField] private Color healthColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color educationColor = new Color(1f, 1f, 0f, 0.3f);

        private Dictionary<string, GameObject> coverageVisuals = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> warningIcons = new Dictionary<string, GameObject>();

        /// <summary>
        /// 显示服务覆盖范围
        /// </summary>
        public void ShowCoverageForBuilding(string buildingId)
        {
            var building = simulation?.FindPlacedBuilding(buildingId);
            if (building == null) return;

            var definition = simulation.Config.GetBuilding(building.ConfigId);
            if (definition == null) return;

            // 确定服务类型和范围
            ServiceType serviceType = GetServiceType(definition);
            if (serviceType == ServiceType.None) return;

            int range = GetServiceRange(definition);
            Color color = GetServiceColor(serviceType);

            // 创建可视化圆圈
            CreateCoverageCircle(buildingId, building.FootprintOrigin.ToVector3(), range, color);

            // 标记不覆盖的建筑
            MarkUncoveredBuildings(building.FootprintOrigin, range, serviceType);
        }

        /// <summary>
        /// 隐藏服务覆盖范围
        /// </summary>
        public void HideCoverage(string buildingId)
        {
            if (coverageVisuals.TryGetValue(buildingId, out var visual))
            {
                Destroy(visual);
                coverageVisuals.Remove(buildingId);
            }
        }

        /// <summary>
        /// 隐藏所有覆盖显示
        /// </summary>
        public void HideAllCoverage()
        {
            foreach (var visual in coverageVisuals.Values)
            {
                if (visual != null) Destroy(visual);
            }
            coverageVisuals.Clear();

            foreach (var icon in warningIcons.Values)
            {
                if (icon != null) Destroy(icon);
            }
            warningIcons.Clear();
        }

        /// <summary>
        /// 更新所有建筑的服务警告图标
        /// </summary>
        public void UpdateAllServiceWarnings()
        {
            if (simulation == null) return;

            // 清除旧图标
            foreach (var icon in warningIcons.Values)
            {
                if (icon != null) Destroy(icon);
            }
            warningIcons.Clear();

            // 检查每栋建筑
            foreach (var building in simulation.Buildings)
            {
                CheckBuildingServiceCoverage(building);
            }
        }

        private void CheckBuildingServiceCoverage(PlacedBuilding building)
        {
            var pos = building.FootprintOrigin;
            List<ServiceType> missingServices = new List<ServiceType>();

            // 检查各项服务
            if (!IsPositionCoveredByService(pos, ServiceType.Fire))
                missingServices.Add(ServiceType.Fire);

            if (!IsPositionCoveredByService(pos, ServiceType.Police))
                missingServices.Add(ServiceType.Police);

            if (!IsPositionCoveredByService(pos, ServiceType.Health))
                missingServices.Add(ServiceType.Health);

            // 创建警告图标
            if (missingServices.Count > 0)
            {
                CreateWarningIcon(building, missingServices);
            }
        }

        private void CreateWarningIcon(PlacedBuilding building, List<ServiceType> missingServices)
        {
            Vector3 worldPos = building.FootprintOrigin.ToVector3() + Vector3.up * 3f;

            GameObject icon = new GameObject($"Warning_{building.Id}");
            icon.transform.position = worldPos;

            // 创建文本显示
            var canvas = icon.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var text = TMPUIHelper.CreateText(icon.transform, "WarningText", GetWarningText(missingServices), 24);
            text.color = Color.red;

            warningIcons[building.Id] = icon;
        }

        private string GetWarningText(List<ServiceType> missing)
        {
            string text = "";
            foreach (var service in missing)
            {
                text += service switch
                {
                    ServiceType.Fire => "🔥",
                    ServiceType.Police => "🚔",
                    ServiceType.Health => "🏥",
                    ServiceType.Education => "🎓",
                    _ => "⚠️"
                };
            }
            return text;
        }

        private void CreateCoverageCircle(string buildingId, Vector3 center, int radius, Color color)
        {
            GameObject circle = new GameObject($"Coverage_{buildingId}");
            circle.transform.position = center + Vector3.up * 0.1f;

            // 创建圆形网格
            MeshFilter meshFilter = circle.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = circle.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateCircleMesh(radius);

            if (coverageCircleMaterial != null)
            {
                meshRenderer.material = coverageCircleMaterial;
                meshRenderer.material.color = color;
            }
            else
            {
                meshRenderer.material = new Material(Shader.Find("Standard"));
                meshRenderer.material.color = color;
            }

            coverageVisuals[buildingId] = circle;
        }

        private Mesh CreateCircleMesh(float radius)
        {
            Mesh mesh = new Mesh();
            int segments = 64;

            Vector3[] vertices = new Vector3[segments + 1];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 2 > segments) ? 1 : i + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        private void MarkUncoveredBuildings(GridPos servicePos, int range, ServiceType serviceType)
        {
            if (simulation == null) return;

            foreach (var building in simulation.Buildings)
            {
                int distance = GridPos.ManhattanDistance(servicePos, building.FootprintOrigin);

                if (distance > range)
                {
                    // 不覆盖，显示警告
                    if (!warningIcons.ContainsKey(building.Id))
                    {
                        CreateWarningIcon(building, new List<ServiceType> { serviceType });
                    }
                }
            }
        }

        private bool IsPositionCoveredByService(GridPos pos, ServiceType serviceType)
        {
            if (simulation == null) return false;

            foreach (var building in simulation.Buildings)
            {
                var def = simulation.Config.GetBuilding(building.ConfigId);
                if (def == null) continue;

                if (GetServiceType(def) == serviceType)
                {
                    int range = GetServiceRange(def);
                    int distance = GridPos.ManhattanDistance(pos, building.FootprintOrigin);

                    if (distance <= range)
                        return true;
                }
            }

            return false;
        }

        private ServiceType GetServiceType(BuildingDefinition definition)
        {
            if (definition.Id.Contains("fire")) return ServiceType.Fire;
            if (definition.Id.Contains("police")) return ServiceType.Police;
            if (definition.Id.Contains("hospital") || definition.Id.Contains("clinic")) return ServiceType.Health;
            if (definition.Id.Contains("school") || definition.Id.Contains("university")) return ServiceType.Education;
            return ServiceType.None;
        }

        private int GetServiceRange(BuildingDefinition definition)
        {
            // 默认范围，可以从配置中读取
            return 15;
        }

        private Color GetServiceColor(ServiceType type)
        {
            return type switch
            {
                ServiceType.Fire => fireProtectionColor,
                ServiceType.Police => policeColor,
                ServiceType.Health => healthColor,
                ServiceType.Education => educationColor,
                _ => Color.white
            };
        }
    }

    public enum ServiceType
    {
        None,
        Fire,
        Police,
        Health,
        Education
    }
}
