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
        static KVASSPlanSettings settingsPlan;
        public void Start()
        {
            //Log("KACListener: Start");
            KACWrapper.InitKACWrapper();

            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();

            if (settingsPlan.Enable && settingsPlan.Queue && KACWrapper.APIReady)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;

        }

        void KAC_onAlarmStateChanged(AlarmStateChangedEventArgs e)
        {
            if (e.alarm == null || e.alarm.Finished()) return;

            if (e.alarm.Name.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal))
            {
                if (e.eventType == KACAlarm.AlarmStateEventsEnum.Deleted)
                {

                    // e.alarm is still in the list
                    var deleting_alarm = e.alarm;

                    var alarms = KACUtils.GetSortedPlanningActiveAlarms();

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

                    Messages.ShowAndClear(5, Messages.DurationType.CONST);
                }
            }
        }




        static public void AlarmCreatedQueueChange(KACAlarm alarm , bool? append)
        {
            if (settingsPlan.Queue)
            {
                if (append == null || alarm == null)
                {
                    return;
                }
                else if (append == true)
                {
                    AlarmAppendedToQueue(alarm);
                }
                else // (append == false)
                {
                    AlarmPrependedToQueue(alarm);
                }
            }
        }


        static void AlarmPrependedToQueue(KACAlarm alarm)
        {
            var alarms = KACUtils.GetPlanningActiveAlarms();

            var planning_UT_start = Utils.UT();
            double planning_UT_end = alarm.AlarmTime;
            double planningTime = Math.Round(planning_UT_end - planning_UT_start);

            int alarmsMoved = 0;
            string firstName = "";
            foreach (var a in alarms)
            {
                if (a.ID != alarm.ID)
                {
                    a.AlarmTime += planningTime;
                    alarmsMoved++;
                    if (alarmsMoved == 1) firstName = a.Name;
                }
            }

            Messages.Add(Localizer.Format("#KVASS_alarm_created", alarm.Name), 0);

            if (alarmsMoved == 1)
            {
                string shipname = KACUtils.ShipName(firstName);
                Messages.Add(Localizer.Format("#KVASS_alarm_created_another", shipname), 2);
            }
            else if (alarmsMoved > 1)
                Messages.Add(Localizer.Format("#KVASS_alarm_created_others", alarmsMoved), 2);

            Messages.ShowAndClear(3, Messages.DurationType.CLEVERCONSTPERLINE);

        }

        static void AlarmAppendedToQueue(KACAlarm alarm)
        {
            var alarms = KACUtils.GetSortedPlanningActiveAlarms();

            if (alarms.Count != 0)
            {
                var busy_UT_start = Utils.UT();
                double busy_UT_end = alarms.Last().AlarmTime;
                double busyTime = Math.Round(busy_UT_end - busy_UT_start);
                alarm.AlarmTime += busyTime;
                Messages.Add(Localizer.Format("#KVASS_alarm_appended", alarm.Name), 0);
            }
            else
            {
                Messages.Add(Localizer.Format("#KVASS_alarm_created", alarm.Name), 0);
            }
            Messages.ShowAndClear(3, Messages.DurationType.CLEVERCONSTPERLINE);
        }


    }
}
