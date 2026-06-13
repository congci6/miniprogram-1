using UnityEngine;

namespace PocketCity.Input
{
    /// <summary>
    /// 输入系统包装器 - 统一新旧输入系统
    /// </summary>
    public static class GetAxisRaw
    {
        public static float Horizontal()
        {
            return UnityEngine.Input.GetAxis("Horizontal");
        }

        public static float Vertical()
        {
            return UnityEngine.Input.GetAxis("Vertical");
        }
    }

    public static class GetMouseButtonDown
    {
        public static bool Left()
        {
            return UnityEngine.Input.GetMouseButtonDown(0);
        }

        public static bool Right()
        {
            return UnityEngine.Input.GetMouseButtonDown(1);
        }

        public static bool Middle()
        {
            return UnityEngine.Input.GetMouseButtonDown(2);
        }
    }

    public static class GetMouseButton
    {
        public static bool Left()
        {
            return UnityEngine.Input.GetMouseButton(0);
        }

        public static bool Right()
        {
            return UnityEngine.Input.GetMouseButton(1);
        }

        public static bool Middle()
        {
            return UnityEngine.Input.GetMouseButton(2);
        }
    }

    public static class GetMouseButtonUp
    {
        public static bool Left()
        {
            return UnityEngine.Input.GetMouseButtonUp(0);
        }

        public static bool Right()
        {
            return UnityEngine.Input.GetMouseButtonUp(1);
        }
    }

    public static class mousePosition
    {
        public static Vector3 Get()
        {
            return UnityEngine.Input.mousePosition;
        }
    }

    public static class GetKey
    {
        public static bool IsPressed(KeyCode key)
        {
            return UnityEngine.Input.GetKey(key);
        }
    }

    public static class GetKeyDown
    {
        public static bool IsPressed(KeyCode key)
        {
            return UnityEngine.Input.GetKeyDown(key);
        }
    }
}
