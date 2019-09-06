using KSP.Localization;
using PreFlightTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KVASSNS

{

    public static class Reflection
    {
        public static int GetKKCraftPartCountLimit(string launchsitename) 
            => int.MaxValue;
        public static double GetKKCraftMassLimit(string launchsitename) 
            => double.MaxValue;
        public static Vector3 GetKKCraftSizeLimit(string launchsitename) 
            => new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

    }

    public static class PreFlightTestExtensions
    {
        public static string GetFailedMessage(this CraftWithinPartCountLimit check)
        {
            return Localizer.Format("#autoLOC_250727");
        }

        public static string GetFailedNote(this CraftWithinPartCountLimit check, int partCount, int limit)
        {
            return String.Format("{0} {1} [{2} {3}]\n",
                Localizer.Format("#autoLOC_443352"), partCount,
                Localizer.Format("#autoLOC_6001000"), limit
            );
        }

    }
}
