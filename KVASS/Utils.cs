using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KVASSNS
{
    public static class Utils
    {
        /// <summary>
        /// Get a precision format for comparing string with the least digits.
        /// Example: value: 12.04, addends: 3.01, 9.02
        /// returns "F2";
        /// </summary>
        /// <param name="value"></param>
        /// <param name="addends"></param>
        /// <returns></returns>
        public static string GetComparingFormat(double value, params double[] addends)
        {
            if (addends.Length == 0) return "";
            const double Eps = 1e-10;

            double sum = addends.Sum();
            double diff = sum - value;
            double diff_abs = Math.Abs(diff);

            if (diff_abs < Eps) return "";

            int i = 0;
            const int maxFracDigits = 8;

            for (double diff_rounded_abs = 0; i < maxFracDigits; i++)
            {
                double sum_rounded = addends.Select(z => Math.Round(z, i)).Sum();
                double value_rounded = Math.Round(value, i);
                double diff_rounded = sum_rounded - value_rounded;
                diff_rounded_abs = Math.Abs(diff_rounded);

                if (diff_rounded_abs > Eps
                        && Math.Sign(diff_rounded) == Math.Sign(diff))
                    return "F" + i;
            }

            return "";
        }


        /// <summary>
        /// Get Universal Time on any scene
        /// </summary>
        /// <returns></returns>
        static public double UT()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return HighLogic.CurrentGame.flightState.universalTime;
            else
                return Planetarium.GetUniversalTime();
        }

        static public string GetVesselName()
        {
            if (HighLogic.LoadedSceneIsEditor) 
                return EditorLogic.fetch.ship.shipName.Trim();
            else 
                return FlightGlobals.ActiveVessel.GetDisplayName().Trim();
        }


        public static string AlarmTitle(string vesselName)
        {
            return Localizer.Format("#KVASS_alarm_title_prefix") + " " + Localizer.Format(vesselName);
        }
        public static string VesselName(string alarmTitle)
        {
            if (String.IsNullOrEmpty(alarmTitle)) return "";

            return alarmTitle.Replace(Localizer.Format("#KVASS_alarm_title_prefix"), "").Trim();
        }

        public static string Days(double seconds, string format = "F1") => (seconds / KSPUtil.dateTimeFormatter.Day).ToString(format);
        
    }

}
