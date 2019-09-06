
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static KVASS_KACWrapper.KACWrapper.KACAPI;
using KVASS_KACWrapper;


using KSP.Localization;
using static KVASSNS.Logging;

namespace KVASSNS
{
    public static class KACUtils
    {

        public static string AlarmTitle(string shipName) 
            => Localizer.Format("#KVASS_alarm_title_prefix") + " " + shipName;
        public static string ShipName(string AlarmTitle)
        {
            if (String.IsNullOrEmpty(AlarmTitle)) return "";

            return AlarmTitle?.Replace(Localizer.Format("#KVASS_alarm_title_prefix"), "").Trim();
        }

        /// <summary>
        /// How many seconds remains until alarm will be finished. 
        /// Returns negative values for already finished alarms
        /// </summary>
        /// <param name="a"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        static public double Remaining(KACAlarm a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));

            double time_now = HighLogic.CurrentGame.flightState.universalTime;
            double alarmTime = a.AlarmTime;

            return alarmTime - time_now;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        static public bool IsAlarmFinished(KACAlarm a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));

            return Remaining(a) < 0.0;
        }

        static public bool Finished(this KACAlarm a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));

            return Remaining(a) < 0.0;
        }


        /// <summary>
        /// Get Alarm by vessel name. Return Alarm or null.
        /// </summary>
        /// <param name="vessel_name"></param>
        /// <returns></returns>
        static public KACAlarm GetAlarm(string vessel_name)
        {
            if (KACWrapper.APIReady)
            {
                KACAlarm a = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.Name == vessel_name);

                if (a != null)
                {
                    return a;
                }
            }

            return null;
        }


        /// <summary>
        /// Remove alarm by ID. Return bool success.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static public bool RemoveAlarm(string id)
        {
            if (KACWrapper.APIReady)
            {
                bool result = KACWrapper.KAC.DeleteAlarm(id);
                Log("Removing Alarm, Success:{0}", result);
                return result;
            }

            Log("KAC is not found");
            return false;
        }

        static public IEnumerable<KACAlarm> GetPlanningActiveAlarms()
        {
            if (!KACWrapper.APIReady) return new List<KACAlarm>();

            var alarms = KACWrapper.KAC.Alarms.Where(
                    a => Remaining(a) > 0 &&
                    a.Name.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal)
                    );

            return alarms;
        }

    }
}
