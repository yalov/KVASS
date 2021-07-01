using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.title == alarmTitle)
                    return enumerator.Current;
            }
            return null;
        }

        public static IEnumerable<AlarmTypeBase> GetPlanningActiveAlarms()
        {

            var alarms = AlarmClockScenario.Instance.alarms;

            var enumerator = alarms.GetListEnumerator();

            List<AlarmTypeBase> list = new List<AlarmTypeBase>();

            while (enumerator.MoveNext())
            {
                if (!enumerator.Current.Actioned &&
                    enumerator.Current.title.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal)
                    )
                {
                    list.Add(enumerator.Current);
                }

            }

            return list;
        }

        public static IEnumerable<AlarmTypeBase> GetSortedPlanningActiveAlarms()
        {
            return GetPlanningActiveAlarms().OrderBy(z => z.ut);
        }
    }
}
