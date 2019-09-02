using System;
using UnityEngine;
using System.Globalization;

namespace KVASSNS
{
    public static class Logging
    {
        private const string PREFIX = "<color=green>[KVASS]</color> ";
        private const bool time = false;

        public static void Log<T>(T msg, params object[] args)
        {
            Debug.Log(PREFIX +
                (time ? DateTime.Now.ToString("HH:mm:ss.f ", CultureInfo.InvariantCulture) : "") +
                String.Format(CultureInfo.InvariantCulture, msg.ToString(), args)
                );
        }
    }
}
