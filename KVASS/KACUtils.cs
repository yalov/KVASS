using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using static KVASSNS.KACWrapper.KACAPI;


namespace KVASSNS
{
    public static class KACUtils
    {

        public static string AlarmTitle(string shipName)
        {
            return Localizer.Format("#KVASS_alarm_title_prefix") + " " + Localizer.Format(shipName);
        }
        public static string ShipName(string AlarmTitle)
        {
            if (String.IsNullOrEmpty(AlarmTitle)) return "";

            return AlarmTitle.Replace(Localizer.Format("#KVASS_alarm_title_prefix"), "").Trim();
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

            double time_now = Utils.UT(); // HighLogic.CurrentGame.UniversalTime;  //HighLogic.CurrentGame.flightState.universalTime;
            double alarmTime = a.AlarmTime;
            //Logging.Log("time_now: " + time_now + " alarmTime: " + alarmTime);

            return alarmTime - time_now;
        }
        
        /// <param name="a">alarm</param>
        /// <returns>boolean finished. false if alarm is null</returns>
        static public bool Finished(this KACAlarm a)
        {
            if (a == null) return false; //throw new ArgumentNullException(nameof(a));

            return Remaining(a) < 0.0;
        }

        /// <summary>
        /// Get Alarm by title. Return Alarm or null.
        /// </summary>
        /// <param name="alarmTitle"></param>
        /// <returns></returns>
        static public KACAlarm GetAlarm(string alarmTitle)
        {
            if (KACWrapper.APIReady)
            {
                KACAlarm a = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.Name == alarmTitle);

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
                return result;
            }
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
