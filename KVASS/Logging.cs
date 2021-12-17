using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


namespace KVASSNS
{
    public static class Logging
    {
        private const string PREFIX = "<color=green>[KVASS]</color> ";
        private const bool time = false;

        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogDebug(params object[] args)
        {
            Log(args);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void LogFormatDebug(string msg, params object[] args)
        {
            LogFormat(msg, args);
        }

        public static void Log(params object[] args)
        {
            Debug.Log(PREFIX + (time ? DateTime.Now.ToString("HH:mm:ss.f ") : "") +
                String.Join(", ", args)
                );
        }

        public static void LogFormat(string msg, params object[] args)
        {
            Debug.LogFormat(PREFIX + (time ? DateTime.Now.ToString("HH:mm:ss.f ") : "") +
                msg, args);
        }
    }
}
