using UnityEngine;
using System.Collections;
using PocketCity.Simulation;

namespace PocketCity.VFX
{
    /// <summary>
    /// 建造和拆除动画系统
    /// </summary>
    public class ConstructionAnimation : MonoBehaviour
    {
        public static ConstructionAnimation Instance { get; private set; }

        [SerializeField] private Material scaffoldingMaterial;
        [SerializeField] private float constructionDuration = 3f;
        [SerializeField] private float demolishDuration = 1f;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 播放建造动画
        /// </summary>
        public void PlayConstructionAnimation(Vector3 position, System.Action onComplete)
        {
            StartCoroutine(ConstructionSequence(position, onComplete));
        }

        private IEnumerator ConstructionSequence(Vector3 position, System.Action onComplete)
        {
            // 创建脚手架
            GameObject scaffolding = CreateScaffolding(position);

            // 等待3秒
            yield return new WaitForSeconds(constructionDuration);

            // 移除脚手架
            if (scaffolding != null)
                Destroy(scaffolding);

            // 完成回调
            onComplete?.Invoke();

            // 播放完成特效
            if (ParticleEffectSystem.Instance != null)
            {
                ParticleEffectSystem.Instance.PlayEffect(EffectType.BuildingPlaced, position);
            }
        }

        private GameObject CreateScaffolding(Vector3 position)
        {
            GameObject scaffolding = new GameObject("Scaffolding");
            scaffolding.transform.position = position;

            // 创建围栏（4根柱子）
            for (int i = 0; i < 4; i++)
            {
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pole.transform.SetParent(scaffolding.transform);

                float angle = i * 90f * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * 2f;
                float z = Mathf.Sin(angle) * 2f;

                pole.transform.localPosition = new Vector3(x, 1.5f, z);
                pole.transform.localScale = new Vector3(0.2f, 3f, 0.2f);

                var renderer = pole.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(1f, 0.8f, 0f, 0.5f); // 半透明黄色
                }
            }

            // 添加旋转动画
            var rotator = scaffolding.AddComponent<Rotator>();
            rotator.rotationSpeed = 30f;

            return scaffolding;
        }

        /// <summary>
        /// 播放拆除动画
        /// </summary>
        public void PlayDemolishAnimation(GameObject building, Vector3 position, System.Action onComplete)
        {
            StartCoroutine(DemolishSequence(building, position, onComplete));
        }

        private IEnumerator DemolishSequence(GameObject building, Vector3 position, System.Action onComplete)
        {
            // 闪烁红色
            var renderers = building.GetComponentsInChildren<Renderer>();
            Color originalColor = Color.white;

            if (renderers.Length > 0 && renderers[0].material != null)
            {
                originalColor = renderers[0].material.color;
            }

            float flashTime = 0.5f;
            float elapsed = 0f;

            while (elapsed < flashTime)
            {
                float t = (Mathf.Sin(elapsed * 20f) + 1f) * 0.5f;
                Color flashColor = Color.Lerp(originalColor, Color.red, t);

                foreach (var r in renderers)
                {
                    if (r != null && r.material != null)
                        r.material.color = flashColor;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 播放烟尘粒子
            if (ParticleEffectSystem.Instance != null)
            {
                ParticleEffectSystem.Instance.PlayEffect(EffectType.BuildingDestroyed, position);
            }

            // 播放音效
            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlaySound(Audio.SoundType.BuildingDemolished);
            }

            // 等待烟尘
            yield return new WaitForSeconds(0.5f);

            // 掉落材料包（视觉效果）
            CreateMaterialDrop(position);

            // 完成回调
            onComplete?.Invoke();
        }

        private void CreateMaterialDrop(Vector3 position)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.transform.position = position + Vector3.up * 2f;
            drop.transform.localScale = Vector3.one * 0.5f;

            var renderer = drop.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.8f, 0.6f, 0.2f); // 金色
            }

            // 下落动画
            StartCoroutine(DropAnimation(drop, position));
        }

        private IEnumerator DropAnimation(GameObject drop, Vector3 targetPos)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 startPos = drop.transform.position;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                drop.transform.position = Vector3.Lerp(startPos, targetPos, t);
                drop.transform.Rotate(Vector3.up, Time.deltaTime * 360f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 销毁
            Destroy(drop, 1f);
        }

        /// <summary>
        /// 点击地面反馈
        /// </summary>
        public void PlayClickFeedback(Vector3 position)
        {
            StartCoroutine(ClickRipple(position));
        }

        private IEnumerator ClickRipple(Vector3 position)
        {
            GameObject ripple = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ripple.transform.position = position + Vector3.up * 0.01f;
            ripple.transform.localScale = new Vector3(0.1f, 0.01f, 0.1f);

            var renderer = ripple.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 1f, 1f, 0.5f);
            }

            // 移除碰撞体
            var collider = ripple.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0.1f, 2f, t);
                ripple.transform.localScale = new Vector3(scale, 0.01f, scale);

                if (renderer != null && renderer.material != null)
                {
                    Color c = renderer.material.color;
                    c.a = 1f - t;
                    renderer.material.color = c;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(ripple);
        }
    }

    /// <summary>
    /// 简单旋转组件
    /// </summary>
    public class Rotator : MonoBehaviour
    {
        public float rotationSpeed = 30f;

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
