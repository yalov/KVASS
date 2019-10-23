using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;
using static KVASSNS.KACWrapper.KACAPI;
using static KVASSNS.Logging;


namespace KVASSNS
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class KACListener : MonoBehaviour
    {
        public void Start()
        {
            //Log("KACListener: Start");
            KACWrapper.InitKACWrapper();

            var settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();

            if (settingsPlan.Enable && settingsPlan.Queue && KACWrapper.APIReady)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;

        }

        void KAC_onAlarmStateChanged(AlarmStateChangedEventArgs e)
        {
            if (e.eventType == KACAlarm.AlarmStateEventsEnum.Created)
            {
                var creating_alarm = e.alarm;
                var alarms = KACUtils.GetPlanningActiveAlarms();
                int alarmsMoved = 0;
                string firstName = "";

                var planning_UT_start = Utils.UT();
                double planning_UT_end = creating_alarm.AlarmTime;
                double planningTime = Math.Round(planning_UT_end - planning_UT_start);

                foreach (var a in alarms)
                {
                    if (a.ID != creating_alarm.ID)
                    {
                        a.AlarmTime += planningTime;
                        alarmsMoved++;
                        if (alarmsMoved == 1) firstName = a.Name;
                    }
                }

                Messages.Add(Localizer.Format("#KVASS_alarm_created", creating_alarm.Name), 0);

                if (alarmsMoved == 1)
                {
                    string shipname = KACUtils.ShipName(firstName);
                    Messages.Add(Localizer.Format("#KVASS_alarm_created_another", shipname), 2);
                }
                else if (alarmsMoved > 1)
                    Messages.Add(Localizer.Format("#KVASS_alarm_created_others", alarmsMoved), 2);

                Messages.ShowAndClear(7, false);


            }
            if (e.eventType == KACAlarm.AlarmStateEventsEnum.Deleted)
            {

                // e.alarm is still in the list
                var deleting_alarm = e.alarm;
                if (deleting_alarm == null || deleting_alarm.Finished()) return;

                var alarms = KACUtils.GetPlanningActiveAlarms().OrderBy(z => z.AlarmTime).ToList();

                int del_index = alarms.FindIndex(z => z.ID == deleting_alarm.ID);

                double planning_UT_start;

                if (del_index == 0)
                    planning_UT_start = Utils.UT(); // HighLogic.CurrentGame.UniversalTime; //HighLogic.CurrentGame.flightState.universalTime; //Planetarium.GetUniversalTime();
                else
                    planning_UT_start = alarms[del_index - 1].AlarmTime;

                double planning_UT_end = deleting_alarm.AlarmTime;

                double planningTime = Math.Round(planning_UT_end - planning_UT_start);

                //Log("planning_UT_end: " + planning_UT_end);
                //Log("planning_UT_start: " + planning_UT_start);
                //Log("planningTime: " + planningTime);

                for (var i = del_index + 1; i < alarms.Count; i++)
                {
                    alarms[i].AlarmTime -= planningTime;
                }

                int alarmsMoved = alarms.Count - (del_index + 1);

                Messages.Add(Localizer.Format("#KVASS_alarm_deleted", deleting_alarm.Name), 0);

                if (alarmsMoved == 1)
                {
                    string ShipName = KACUtils.ShipName(alarms[del_index + 1].Name);
                    Messages.Add(Localizer.Format("#KVASS_alarm_deleted_another", ShipName), 1);
                }
                else if (alarmsMoved > 1)
                    Messages.Add(Localizer.Format("#KVASS_alarm_deleted_others", alarmsMoved), 1);

                Messages.ShowAndClear(5, false);

            }
        }

    }
}
