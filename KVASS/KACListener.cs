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
        KVASSPlanSettings settingsPlan;
        KVASSPlanSettings2 settingsPlan2;
        public void Start()
        {
            //Log("KACListener: Start");
            KACWrapper.InitKACWrapper();

            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();
            settingsPlan2 = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings2>();

            if (settingsPlan.Enable && settingsPlan2.Queue && KACWrapper.APIReady)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;

        }

        void KAC_onAlarmStateChanged(AlarmStateChangedEventArgs e)
        {
            if (e.alarm == null || e.alarm.Finished()) return;

            if (e.alarm.Name.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal))
            {
                if (e.eventType == KACAlarm.AlarmStateEventsEnum.Created)
                {
                    var creating_alarm = e.alarm;

                    if (!settingsPlan2.QueueAppend)
                    {
                        var alarms = KACUtils.GetPlanningActiveAlarms();

                        var planning_UT_start = Utils.UT();
                        double planning_UT_end = creating_alarm.AlarmTime;
                        double planningTime = Math.Round(planning_UT_end - planning_UT_start);

                        int alarmsMoved = 0;
                        string firstName = "";
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

                        Messages.ShowAndClear(3, Messages.DurationType.CLEVERCONSTPERLINE);
                    }
                    else
                    {
                        var alarms = KACUtils.GetSortedPlanningActiveAlarms();

                        if (alarms.Count != 0)
                        {
                            var busy_UT_start = Utils.UT();
                            double busy_UT_end = alarms.Last().AlarmTime;
                            double busyTime = Math.Round(busy_UT_end - busy_UT_start);
                            creating_alarm.AlarmTime += busyTime;
                            Messages.Add(Localizer.Format("#KVASS_alarm_appended", creating_alarm.Name), 0);
                        }
                        else
                        {
                            Messages.Add(Localizer.Format("#KVASS_alarm_created", creating_alarm.Name), 0);
                        }
                        Messages.ShowAndClear(3, Messages.DurationType.CLEVERCONSTPERLINE);
                    }
                }

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

    }
}
