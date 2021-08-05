using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KVASSNS.Logging;

namespace KVASSNS
{
    public static class ACAUtils
    {

        /// <summary>
        /// Get Alarm by title. Return Alarm or null.
        /// </summary>
        /// <param name="alarmTitle"></param>
        /// <returns></returns>
        static public AlarmTypeBase GetAlarm(string alarmTitle)
        {            
            var alarms = AlarmClockScenario.Instance.alarms;

            var enumerator = alarms.GetListEnumerator();
            Log("GetAlarm " + alarmTitle);
            while (enumerator.MoveNext())
            {
                Log("GetAlarm " + enumerator.Current.title + " " +  enumerator.Current.Id);
                if (enumerator.Current.title == alarmTitle)
                    return enumerator.Current;
            }
            return null;
        }

        public static IEnumerable<AlarmTypeRaw> GetPlanningActiveAlarms()
        {

            var alarms = AlarmClockScenario.Instance.alarms;

            var enumerator = alarms.GetListEnumerator();

            List<AlarmTypeRaw> list = new List<AlarmTypeRaw>();

            

            while (enumerator.MoveNext())
            {
                if (!enumerator.Current.Actioned &&
                    enumerator.Current.title.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal)
                    )
                {
                    if (enumerator.Current is AlarmTypeRaw)
                        list.Add(enumerator.Current as AlarmTypeRaw);
                }

            }

            return list;
        }

        public static IEnumerable<AlarmTypeRaw> GetSortedPlanningActiveAlarms()
        {
            return GetPlanningActiveAlarms().OrderBy(z => z.ut);
        }
    }
}
