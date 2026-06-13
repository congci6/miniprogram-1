using System.Collections.Generic;
using UnityEngine;

namespace PocketCity.Core
{
    /// <summary>
    /// 通用GameObject对象池
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private readonly int initialSize;
        private int totalCreated;

        public GameObjectPool(GameObject prefab, Transform parent = null, int initialSize = 10)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.initialSize = initialSize;

            // 预热池
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private GameObject CreateNewObject()
        {
            var obj = Object.Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Push(obj);
            totalCreated++;
            return obj;
        }

        public GameObject Get()
        {
            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Pop();
            }
            else
            {
                obj = CreateNewObject();
                obj = pool.Pop();
            }
            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            pool.Push(obj);
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                var obj = pool.Pop();
                if (obj != null)
                    Object.Destroy(obj);
            }
            totalCreated = 0;
        }

        public int PoolSize => pool.Count;
        public int TotalCreated => totalCreated;
    }

    /// <summary>
    /// Mesh对象池
    /// </summary>
    public class MeshPool
    {
        private readonly Stack<Mesh> pool = new Stack<Mesh>();
        private int totalCreated;

        public Mesh Get()
        {
            if (pool.Count > 0)
            {
                var mesh = pool.Pop();
                mesh.Clear();
                return mesh;
            }
            totalCreated++;
            return new Mesh();
        }

        public void Return(Mesh mesh)
        {
            if (mesh == null) return;
            mesh.Clear();
            pool.Push(mesh);
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                var mesh = pool.Pop();
                if (mesh != null)
                    Object.Destroy(mesh);
            }
            totalCreated = 0;
        }
    }

    /// <summary>
    /// 对象池管理器 - 集中管理所有池
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        private Dictionary<string, GameObjectPool> gameObjectPools = new Dictionary<string, GameObjectPool>();
        private MeshPool meshPool = new MeshPool();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public GameObjectPool GetOrCreatePool(string poolName, GameObject prefab, int initialSize = 10)
        {
            if (!gameObjectPools.TryGetValue(poolName, out var pool))
            {
                pool = new GameObjectPool(prefab, transform, initialSize);
                gameObjectPools[poolName] = pool;
            }
            return pool;
        }

        public GameObject GetGameObject(string poolName)
        {
            if (gameObjectPools.TryGetValue(poolName, out var pool))
            {
                return pool.Get();
            }
            Debug.LogWarning($"Pool '{poolName}' not found");
            return null;
        }

        public void ReturnGameObject(string poolName, GameObject obj)
        {
            if (gameObjectPools.TryGetValue(poolName, out var pool))
            {
                pool.Return(obj);
            }
        }

        public Mesh GetMesh() => meshPool.Get();
        public void ReturnMesh(Mesh mesh) => meshPool.Return(mesh);

        private void OnDestroy()
        {
            foreach (var pool in gameObjectPools.Values)
            {
                pool.Clear();
            }
            meshPool.Clear();
        }
    }
}
