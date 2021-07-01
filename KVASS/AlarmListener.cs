using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static KVASSNS.KACWrapper.KACAPI;
using static KVASSNS.Logging;


namespace KVASSNS
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class AlarmListener : MonoBehaviour
    {
        public void Start()
        {
            Log("KACListener.Start");
            KVASSPlanSettings settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();
            
            if (settingsPlan.Enable)
            {

                if (settingsPlan.KACEnable)
                {
                    KACWrapper.InitKACWrapper();


                    if (settingsPlan.Queue && KACWrapper.APIReady)
                        KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;

                }
                else
                {
                    GameEvents.onAlarmAdded.Add(OnAlarmAdded);
                    GameEvents.onAlarmRemoving.Add(OnAlarmRemoving);
                }
            }
            

        }

        public void OnDisable()
        {
            G﻿ameEvents.onAlarmAdded.Remove(OnAlarmAdded);
            GameEvents.onAlarmRemoving.Remove(OnAlarmRemoving);
        }

        private void KAC_onAlarmStateChanged(AlarmStateChangedEventArgs e)
        {
            Log("KAC_onAlarmStateChanged");
            if (e.eventType == KACAlarm.AlarmStateEventsEnum.Deleted)
            {
                if (e.alarm == null || e.alarm.Finished()) return;

                if (e.alarm.Name.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal))
                {
                    // e.alarm is still in the list
                    var deleting_alarm = e.alarm;

                    var alarms = KACUtils.GetSortedPlanningActiveAlarms().ToList();

                    int del_index = alarms.FindIndex(z => z.ID == deleting_alarm.ID);

                    double planning_UT_start;

                    if (del_index == 0)
                        planning_UT_start = Utils.UT();
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
                        string ShipName = Utils.VesselName(alarms[del_index + 1].Name);
                        Messages.Add(Localizer.Format("#KVASS_alarm_deleted_another", ShipName), 1);
                    }
                    else if (alarmsMoved > 1)
                        Messages.Add(Localizer.Format("#KVASS_alarm_deleted_others", alarmsMoved), 1);

                    Messages.ShowAndClear(5, Messages.DurationType.CONST);

                }
            }
        }

        private void OnAlarmRemoving(AlarmTypeBase data)
        {
            Log("OnAlarmRemoving, {0}, ut:{1}", data.title, data.ut);

            if (data == null || data.Actioned) return;

            if (data.title.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal))
            {
                // e.alarm is still in the list
                var deleting_alarm = data;

                var alarms = ACAUtils.GetSortedPlanningActiveAlarms().ToList();

                int del_index = alarms.FindIndex(z => z.Id == deleting_alarm.Id);

                double planning_UT_start;

                if (del_index == 0)
                    planning_UT_start = Utils.UT();
                else
                    planning_UT_start = alarms[del_index - 1].ut;

                double planning_UT_end = deleting_alarm.ut;

                double planningTime = Math.Round(planning_UT_end - planning_UT_start);

                //Log("planning_UT_end: " + planning_UT_end);
                //Log("planning_UT_start: " + planning_UT_start);
                //Log("planningTime: " + planningTime);

                for (var i = del_index + 1; i < alarms.Count; i++)
                {
                    alarms[i].ut -= planningTime;
                }

                int alarmsMoved = alarms.Count - (del_index + 1);

                Messages.Add(Localizer.Format("#KVASS_alarm_deleted", deleting_alarm.title), 0);

                if (alarmsMoved == 1)
                {
                    string ShipName = Utils.VesselName(alarms[del_index + 1].title);
                    Messages.Add(Localizer.Format("#KVASS_alarm_deleted_another", ShipName), 1);
                }
                else if (alarmsMoved > 1)
                    Messages.Add(Localizer.Format("#KVASS_alarm_deleted_others", alarmsMoved), 1);

                Messages.ShowAndClear(5, Messages.DurationType.CONST);

            }

        }

        private void OnAlarmAdded(AlarmTypeBase data)
        {
            Log("OnAlarmAdded, {0}, ut:{1}", data.title, data.ut);

            // doesn't work in VAB
            data.OnScenarioUpdate();
            data.UIInputPanelUpdate();
        }

    }
}
