using System.Collections;
using UnityEngine;

namespace PocketCity.Disaster
{
    public class DisasterEffects : MonoBehaviour
    {
        [SerializeField] private GameObject earthquakeEffect;
        [SerializeField] private GameObject tornadoEffect;
        [SerializeField] private GameObject meteorEffect;
        [SerializeField] private GameObject fireEffect;
        [SerializeField] private GameObject alienEffect;
        [SerializeField] private GameObject robotEffect;
        [SerializeField] private GameObject monsterEffect;

        private DamageSystem damageSystem;

        private void Awake()
        {
            damageSystem = GetComponent<DamageSystem>();
        }

        public void ExecuteDisaster(DisasterConfig config, Vector3 position)
        {
            switch (config.type)
            {
                case DisasterType.Earthquake:
                    StartCoroutine(EarthquakeEffect(config, position));
                    break;
                case DisasterType.Tornado:
                    StartCoroutine(TornadoEffect(config, position));
                    break;
                case DisasterType.Meteor:
                    StartCoroutine(MeteorEffect(config, position));
                    break;
                case DisasterType.Fire:
                    StartCoroutine(FireEffect(config, position));
                    break;
                case DisasterType.Alien:
                    StartCoroutine(AlienEffect(config, position));
                    break;
                case DisasterType.Robot:
                    StartCoroutine(RobotEffect(config, position));
                    break;
                case DisasterType.Monster:
                    StartCoroutine(MonsterEffect(config, position));
                    break;
            }
        }

        private IEnumerator EarthquakeEffect(DisasterConfig config, Vector3 center)
        {
            SpawnEffect(earthquakeEffect, center, config.duration);
            float elapsed = 0f;
            int waves = 3;

            for (int i = 0; i < waves; i++)
            {
                float waveRadius = config.radius * (i + 1) / waves;
                damageSystem?.DamageArea(center, waveRadius, config.damage / waves);
                yield return new WaitForSeconds(config.duration / waves);
                elapsed += config.duration / waves;
            }
        }

        private IEnumerator TornadoEffect(DisasterConfig config, Vector3 startPos)
        {
            GameObject tornado = SpawnEffect(tornadoEffect, startPos, config.duration);
            Vector3 direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            float pathLength = config.radius * 3f;
            float speed = pathLength / config.duration;
            float elapsed = 0f;

            while (elapsed < config.duration)
            {
                Vector3 currentPos = startPos + direction * speed * elapsed;
                if (tornado != null)
                    tornado.transform.position = currentPos;

                damageSystem?.DamageArea(currentPos, config.radius * 0.3f, Mathf.RoundToInt(config.damage * Time.deltaTime / config.duration));

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (tornado != null)
                Destroy(tornado);
        }

        private IEnumerator MeteorEffect(DisasterConfig config, Vector3 position)
        {
            Vector3 spawnPos = position + Vector3.up * 50f;
            GameObject meteor = SpawnEffect(meteorEffect, spawnPos, 2f);

            float fallTime = 2f;
            float elapsed = 0f;

            while (elapsed < fallTime && meteor != null)
            {
                meteor.transform.position = Vector3.Lerp(spawnPos, position, elapsed / fallTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            damageSystem?.DamageArea(position, config.radius, config.damage);
            SpawnEffect(fireEffect, position, 3f);
        }

        private IEnumerator FireEffect(DisasterConfig config, Vector3 center)
        {
            SpawnEffect(fireEffect, center, config.duration);
            float elapsed = 0f;
            float spreadRadius = config.radius * 0.2f;

            while (elapsed < config.duration)
            {
                Vector3 spreadPos = center + new Vector3(
                    Random.Range(-spreadRadius, spreadRadius),
                    0f,
                    Random.Range(-spreadRadius, spreadRadius)
                );
                damageSystem?.DamageArea(spreadPos, config.radius * 0.5f, Mathf.RoundToInt(config.damage * Time.deltaTime / config.duration));

                elapsed += Time.deltaTime;
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator AlienEffect(DisasterConfig config, Vector3 center)
        {
            GameObject alien = SpawnEffect(alienEffect, center, config.duration);
            float elapsed = 0f;
            int attackCount = config.level * 2;
            float attackInterval = config.duration / attackCount;

            for (int i = 0; i < attackCount; i++)
            {
                Vector3 attackPos = center + new Vector3(
                    Random.Range(-config.radius, config.radius),
                    0f,
                    Random.Range(-config.radius, config.radius)
                );
                damageSystem?.DamageArea(attackPos, config.radius * 0.3f, config.damage / attackCount);
                yield return new WaitForSeconds(attackInterval);
            }

            if (alien != null)
                Destroy(alien);
        }

        private IEnumerator RobotEffect(DisasterConfig config, Vector3 center)
        {
            GameObject robot = SpawnEffect(robotEffect, center, config.duration);
            float elapsed = 0f;

            while (elapsed < config.duration)
            {
                damageSystem?.DamageArea(center, config.radius * 0.7f, Mathf.RoundToInt(config.damage * Time.deltaTime / config.duration));
                center += new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (robot != null)
                Destroy(robot);
        }

        private IEnumerator MonsterEffect(DisasterConfig config, Vector3 center)
        {
            GameObject monster = SpawnEffect(monsterEffect, center, config.duration);
            float elapsed = 0f;

            while (elapsed < config.duration)
            {
                damageSystem?.DamageArea(center, config.radius, Mathf.RoundToInt(config.damage * Time.deltaTime / config.duration));
                elapsed += Time.deltaTime;
                yield return new WaitForSeconds(0.2f);
            }

            if (monster != null)
                Destroy(monster);
        }

        private GameObject SpawnEffect(GameObject effectPrefab, Vector3 position, float duration)
        {
            if (effectPrefab == null)
                return null;

            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, duration);
            return effect;
        }
    }
}
