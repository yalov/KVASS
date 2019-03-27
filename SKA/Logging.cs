using System;
using UnityEngine;

namespace SKA
{
    public static class Logging
    {
        private static readonly string PREFIX = "<color=green>[SKA]</color> ";

        public static void Log(string msg, params object[] arg)
        {
            Log(String.Format(msg, arg));
        }

        public static void Log<T>(T msg, bool time = false)
        {
            Debug.Log(PREFIX + (time?DateTime.Now.ToString("HH:mm:ss.f "):"") + msg.ToString());
        }
    }
}
