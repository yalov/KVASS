using System;
using UnityEngine;

namespace KVAS
{
    public static class Logging
    {
        private static readonly string PREFIX = "<color=green>[KVAS]</color> ";

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
