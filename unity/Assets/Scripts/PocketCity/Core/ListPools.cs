using System.Collections.Generic;
using PocketCity.Simulation;

namespace PocketCity.Core
{
    /// <summary>
    /// 建筑列表池 - 避免RecomputeMetrics中的重复分配
    /// </summary>
    public class BuildingListPool
    {
        private static readonly Stack<List<PlacedBuilding>> pool = new Stack<List<PlacedBuilding>>();
        private const int MaxPoolSize = 30; // 对应25个Connected*方法
        private const int InitialCapacity = 100;

        public static List<PlacedBuilding> Get()
        {
            if (pool.Count > 0)
            {
                var list = pool.Pop();
                list.Clear();
                return list;
            }
            return new List<PlacedBuilding>(InitialCapacity);
        }

        public static void Return(List<PlacedBuilding> list)
        {
            if (list == null) return;
            if (pool.Count < MaxPoolSize)
            {
                list.Clear();
                pool.Push(list);
            }
        }

        public static void Clear()
        {
            pool.Clear();
        }
    }

    /// <summary>
    /// GridPos列表池 - 避免Zone拖拽时的逐格分配
    /// </summary>
    public class GridPosListPool
    {
        private static readonly Stack<List<GridPos>> pool = new Stack<List<GridPos>>();
        private const int MaxPoolSize = 20;
        private const int InitialCapacity = 100;

        public static List<GridPos> Get()
        {
            if (pool.Count > 0)
            {
                var list = pool.Pop();
                list.Clear();
                return list;
            }
            return new List<GridPos>(InitialCapacity);
        }

        public static void Return(List<GridPos> list)
        {
            if (list == null) return;
            if (pool.Count < MaxPoolSize)
            {
                list.Clear();
                pool.Push(list);
            }
        }
    }

    /// <summary>
    /// 通用列表池工厂
    /// </summary>
    public static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new Stack<List<T>>();
        private const int MaxPoolSize = 20;
        private const int InitialCapacity = 16;

        public static List<T> Get()
        {
            if (pool.Count > 0)
            {
                var list = pool.Pop();
                list.Clear();
                return list;
            }
            return new List<T>(InitialCapacity);
        }

        public static void Return(List<T> list)
        {
            if (list == null) return;
            if (pool.Count < MaxPoolSize)
            {
                list.Clear();
                pool.Push(list);
            }
        }
    }
}
