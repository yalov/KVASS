using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KVASSNS.Logging;

namespace KVASSNS
{
    public enum AlarmType
    {
        KerbalAlarmClock,
        AlarmClockApp
    }

    public class Alarm
    {
        public enum WarpType
        {
            DoNothing,
            KillWarp,
            PauseGame
        }

        public Alarm(String title, String desc, double ut, double time, 
            string id = "", WarpType warp = WarpType.DoNothing, AlarmType alarmType = AlarmType.AlarmClockApp)
        {
            Title = title;
            Description = desc;
            UT = ut;
            Time = time;
            ID = id;
            Warp = warp;
            AlarmType = alarmType;
        }

        public AlarmType AlarmType { get; }
        public WarpType Warp { get; set; }
        public String Title { get; }
        public String Description { get; }
        public double UT { get; set; }
        public double Time { get; set; }
        public string ID { get; set; }
        

     
        /// <summary>
        /// How many seconds remains until alarm will be finished. 
        /// Returns negative values for already finished alarms
        /// </summary>
        public double Remaining() => UT - Utils.UT();
        public bool Finished() => Remaining() < 0.0;


        /// <summary>
        /// Create Alarms on GUI and write ID into alarm
        /// </summary>
        public void CreateonGUI()
        {
            KVASSPlanSettings settings = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();

            if (settings.KACEnable)
            {
                if (KACWrapper.APIReady)
                {

                    ID = KACWrapper.KAC.CreateAlarm(
                        KACWrapper.KACAPI.AlarmTypeEnum.Raw,
                        Title,
                        UT);


                    if (!String.IsNullOrEmpty(ID))
                    {
                        // a.Remaining doesn't work in the VAB/SPH

                        var alarm = KACWrapper.KAC.Alarms.First(z => z.ID == ID);
                        alarm.Notes = Description;
                        alarm.AlarmMargin = 0;

                        if (Warp == WarpType.KillWarp)
                            alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
                        else
                            alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.DoNothing;
                        
                    }
                }
            }
            else
            {
                
                AlarmTypeRaw alarm = new AlarmTypeRaw();
                alarm.title = Title;
                alarm.ut = UT;
                alarm.description = Description;
                alarm.timeEntry = Time;

                //Log(String.Format("CreateonGUI ACA title:{0}, UT:{1:F0}, timeEntry:{2:F0}", alarm.title, alarm.ut, alarm.timeEntry));

                if (settings.KillTimeWarp)
                    alarm.actions.warp = AlarmActions.WarpEnum.KillWarp;
                else
                    alarm.actions.warp = AlarmActions.WarpEnum.DoNothing;

                alarm.actions.message = AlarmActions.MessageEnum.No;

                if (alarm.CanSetAlarm(KSP.UI.AlarmUIDisplayMode.Add))
                {
                    AlarmClockScenario.AddAlarm(alarm);
                    alarm.OnScenarioUpdate();
                    alarm.UIInputPanelUpdate();                    

                    var enumerator = AlarmClockScenario.Instance.alarms.GetListEnumerator();
                    while (enumerator.MoveNext())
                    {
                        //Log("id: {0}, {1}, ut: {2:F0}, TimeToAlarm: {3:F0}, timeEntry: {4:F0}",
                        //    enumerator.Current.Id, enumerator.Current.title, enumerator.Current.ut, enumerator.Current.TimeToAlarm,
                        //(enumerator.Current as AlarmTypeRaw).timeEntry);

                        if (enumerator.Current.ut == UT && enumerator.Current.title == Title)
                        {
                            ID = enumerator.Current.Id.ToString();
                        }
                    }
                }
            }
        }
    }

    public class AlarmUtils
    {
        public AlarmType AlarmType { get; }

        public AlarmUtils(bool kac)
        {
            
            AlarmType = (kac ? AlarmType.KerbalAlarmClock : AlarmType.AlarmClockApp);
        }

        public Alarm GetAlarm(string alarmTitle)
        {
            Alarm alarm = null;
            switch (AlarmType)
            {
                case AlarmType.KerbalAlarmClock:
                    var kac_alarm = KACUtils.GetAlarm(alarmTitle);
                    if (kac_alarm != null)
                        alarm = new Alarm(kac_alarm.Name, kac_alarm.Notes, kac_alarm.AlarmTime, kac_alarm.Remaining, id: kac_alarm.ID);
                    break;

                case AlarmType.AlarmClockApp:
                    var aca_alarm = ACAUtils.GetAlarm(alarmTitle);
                    //Log("Alarm.GetAlarm " + aca_alarm.Id);
                    if (aca_alarm != null && aca_alarm is AlarmTypeRaw)
                    {
                        alarm = new Alarm(aca_alarm.title, aca_alarm.description, aca_alarm.ut, (aca_alarm as AlarmTypeRaw).timeEntry, id: aca_alarm.Id.ToString());
                        //Log("Alarm.GetAlarm " + alarm.ID);
                    }
                        break;
            }
            return alarm;
        }

        public bool RemoveAlarm(string ID)
        {
            switch (AlarmType)
            {
                case AlarmType.KerbalAlarmClock:
                    return KACUtils.RemoveAlarm(ID);
                   
                case AlarmType.AlarmClockApp:
                    return AlarmClockScenario.DeleteAlarm(uint.Parse(ID));
                    
                default:
                    return false;
            }
        }

        public bool RemoveIfFinished(string alarmTitle)
        {
            bool success = false;

            Alarm a = GetAlarm(alarmTitle);
            if (a.Finished())
                success = RemoveAlarm(a.ID);

            return success;
        }

        public void AlarmPrependedToQueue(Alarm alarm)
        {
            if (alarm == null) return;

            int alarmsMoved = 0;

            switch (AlarmType)
            {
                case AlarmType.KerbalAlarmClock:
                    {
                        var alarms = KACUtils.GetPlanningActiveAlarms();

                        foreach (var a in alarms)
                        {
                            a.AlarmTime += alarm.Time;
                            alarmsMoved++;
                        }
                        break;
                    }
                case AlarmType.AlarmClockApp:
                    {
                        

                        var alarms = ACAUtils.GetPlanningActiveAlarms();

                        foreach (var a in alarms)
                        {
                            //Log(String.Format("Bef - title:{0}, UT:{1:F0}, timeEntry:{2:F0}", a.title, a.ut, a.timeEntry));
                            a.ut += alarm.Time;

                            a.timeEntry = a.ut - Utils.UT(); // += alarm.Times

                            a.OnScenarioUpdate();
                            a.UIInputPanelUpdate();
                            alarmsMoved++;
                            //Log(String.Format("Aft - title:{0}, UT:{1:F0}, timeEntry:{2:F0}", a.title, a.ut, a.timeEntry));
                        }
                        break;
                    }
                default:
                    break;
            }



            Messages.Add(Localizer.Format("#KVASS_alarm_created", alarm.Title), 0);

            if (alarmsMoved == 1)
            {
                Messages.Add(Localizer.Format("#KVASS_alarm_created_other", alarmsMoved), 1);
            }
            else if (alarmsMoved > 1)
            {
                Messages.Add(Localizer.Format("#KVASS_alarm_created_others", alarmsMoved), 1);
            }
        }

        public void AlarmAppendedToQueue(ref Alarm alarm)
        {
            if (alarm == null) return;

            switch (AlarmType)
            {
                case AlarmType.KerbalAlarmClock:
                    {
                        
                        var alarms = KACUtils.GetSortedPlanningActiveAlarms();

                        if (alarms.Any())
                        {
                            double busy_UT_end = alarms.Last().AlarmTime;
                            double busyTime = Math.Round(busy_UT_end - Utils.UT());

                            alarm.UT += busyTime;
                            

                            Messages.Add(Localizer.Format("#KVASS_alarm_appended", alarm.Title), 0);
                        }
                        else
                        {
                            Messages.Add(Localizer.Format("#KVASS_alarm_created", alarm.Title), 0);
                        }
                        break;
                    }
                case AlarmType.AlarmClockApp:
                    {
                        var alarms = ACAUtils.GetSortedPlanningActiveAlarms();

                        if (alarms.Any())
                        {
                            double busy_UT_end = alarms.Last().ut;
                            double busyTime = Math.Round(busy_UT_end - Utils.UT());

                            
                            //Log(String.Format("Bef - title:{0}, UT:{1}, Time:{2}", alarm.Title, alarm.UT, alarm.Time));
                            alarm.UT += busyTime;
                            alarm.Time += busyTime;
                            //Log(String.Format("Aft - title:{0}, UT:{1}, Time:{2}", alarm.Title, alarm.UT, alarm.Time));

                            Messages.Add(Localizer.Format("#KVASS_alarm_appended", alarm.Title), 0);
                        }
                        else
                        {
                            Messages.Add(Localizer.Format("#KVASS_alarm_created", alarm.Title), 0);
                        }
                        break;
                    }
                default:
                    break;
            }

            
        }
    }
}
