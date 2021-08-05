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

            Log("Start1======");

            var enumerator = AlarmClockScenario.Instance.alarms.GetListEnumerator();
            while (enumerator.MoveNext())
            {
                Log("id: {0}, {1}, ut: {2:F0}, tta: {3:F0}, te: {4:F0}",
                    enumerator.Current.Id, enumerator.Current.title, enumerator.Current.ut, enumerator.Current.TimeToAlarm,
                (enumerator.Current as AlarmTypeRaw).timeEntry);
            }

            Log("Start2=======");

            var als = ACAUtils.GetSortedPlanningActiveAlarms();
            foreach (var a in als)
            {
                Log("id: {0}, {1}, ut: {2:F0}, tta: {3:F0}, te: {4:F0}",
                    a.Id, a.title, a.ut, a.TimeToAlarm,
                a.timeEntry);
            }
            Log("Start3=======");

            KVASSPlanSettings settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();

            if (settingsPlan.Enable && settingsPlan.Queue)
            {
                if (settingsPlan.KACEnable)
                {
                    KACWrapper.InitKACWrapper();

                    if (KACWrapper.APIReady)
                        KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;

                }
                else
                {
                    GameEvents.onAlarmAdded.Add(OnAlarmAdded);
                    GameEvents.onAlarmRemoving.Add(OnAlarmRemoving);
                }
            }
            else
            {
                Destroy(gameObject);
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

        private void OnAlarmRemoving(AlarmTypeBase deleting_alarm)
        {
            if (deleting_alarm == null || deleting_alarm.Actioned) return;

            if (deleting_alarm.title.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal))
            {
                var alarms = ACAUtils.GetSortedPlanningActiveAlarms().ToList();
                Log("Rem alarms.Count " + alarms.Count);
                int del_index = alarms.FindIndex(z => z.Id == deleting_alarm.Id);
                Log("Rem del_index" + del_index);

                double planning_UT_start;

                if (del_index == 0)
                    planning_UT_start = Utils.UT();
                else
                    planning_UT_start = alarms[del_index - 1].ut;

                double planning_UT_end = deleting_alarm.ut;

                double planningTime = Math.Round(planning_UT_end - planning_UT_start);

                Log("Rem planning_UT_start " + planning_UT_start);
                Log("Rem planning_UT_end " + planning_UT_end);
                

                //Log("planning_UT_end: " + planning_UT_end);
                //Log("planning_UT_start: " + planning_UT_start);
                //Log("planningTime: " + planningTime);

                for (var i = del_index + 1; i < alarms.Count; i++)
                {
                    Log(String.Format("Rem Bef title:{0}, UT:{1:F0}, Time:{2:F0}", alarms[i].title, alarms[i].ut, alarms[i].timeEntry));
                    alarms[i].ut -= planningTime;
                    //alarms[i].timeEntry -= planningTime;
                    Log(String.Format("Rem Aft title:{0}, UT:{1:F0}, Time:{2:F0}", alarms[i].title, alarms[i].ut, alarms[i].timeEntry));
                    alarms[i].OnScenarioUpdate();
                    alarms[i].UIInputPanelUpdate();
                }

                

                String message = Localizer.Format("#KVASS_alarm_deleted", deleting_alarm.title);

                int alarmsMoved = alarms.Count - (del_index + 1);
                if (alarmsMoved == 1)
                {
                    string AlarmName = alarms[del_index + 1].title;
                    message += "\n" + Localizer.Format("#KVASS_alarm_deleted_another", AlarmName);
                }
                else if (alarmsMoved > 1)
                    message += "\n" + Localizer.Format("#KVASS_alarm_deleted_others", alarmsMoved);

                Messages.QuickPost(message);
                Log(message);
            }
        }

        private void OnAlarmAdded(AlarmTypeBase data)
        {
            Log(String.Format("OnAlarmAdded added title:{0}, UT:{1:F0}, timeEntry:{2:F0}", data.title, data.ut, (data as AlarmTypeRaw).timeEntry));

            var alarms = ACAUtils.GetSortedPlanningActiveAlarms();
            
            foreach (var a in alarms)
            {
                Log(String.Format("OnAlarmAdded title:{0}, UT:{1:F0}, timeEntry:{2:F0}", a.title, a.ut, a.timeEntry));
            }
        }

    }
}
