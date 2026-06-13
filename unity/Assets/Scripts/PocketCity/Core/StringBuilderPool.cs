using System.Text;
using UnityEngine;

namespace PocketCity.Core
{
    /// <summary>
    /// StringBuilder池 - 避免每次分配
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly System.Collections.Generic.Stack<StringBuilder> pool =
            new System.Collections.Generic.Stack<StringBuilder>();

        private const int DefaultCapacity = 256;
        private const int MaxCapacity = 4096;
        private const int MaxPoolSize = 10;

        public static StringBuilder Get()
        {
            if (pool.Count > 0)
            {
                var sb = pool.Pop();
                sb.Clear();
                return sb;
            }
            return new StringBuilder(DefaultCapacity);
        }

        public static void Return(StringBuilder sb)
        {
            if (sb == null) return;

            // 如果容量太大，不回收（避免内存膨胀）
            if (sb.Capacity > MaxCapacity)
            {
                return;
            }

            // 限制池大小
            if (pool.Count < MaxPoolSize)
            {
                sb.Clear();
                pool.Push(sb);
            }
        }

        public static string GetStringAndReturn(StringBuilder sb)
        {
            var result = sb.ToString();
            Return(sb);
            return result;
        }
    }

    /// <summary>
    /// 字符串构建辅助类
    /// </summary>
    public static class StringBuilderHelper
    {
        public static string BuildWithSeparator(string separator, params string[] parts)
        {
            var sb = StringBuilderPool.Get();
            bool first = true;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (!first) sb.Append(separator);
                sb.Append(part);
                first = false;
            }
            return StringBuilderPool.GetStringAndReturn(sb);
        }

        public static string AppendWithLabel(string label, int value, string suffix = "")
        {
            var sb = StringBuilderPool.Get();
            sb.Append(label);
            sb.Append(value);
            if (!string.IsNullOrEmpty(suffix))
                sb.Append(suffix);
            return StringBuilderPool.GetStringAndReturn(sb);
        }

        public static string AppendWithLabel(string label, string value)
        {
            var sb = StringBuilderPool.Get();
            sb.Append(label);
            sb.Append(value);
            return StringBuilderPool.GetStringAndReturn(sb);
        }
    }
}
