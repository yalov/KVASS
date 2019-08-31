using System;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEngine;
using KSP.Localization;

using KVASS_KACWrapper;
using static KVASS.Logging;
using KSP.UI;
using System.Collections;
using System.Collections.Generic;

using static KVASS_KACWrapper.KACWrapper.KACAPI;

namespace KVASS
{
    // https://github.com/linuxgurugamer/KCT was used there
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASS_SimSettings settingsSim;
        static KVASS_PlanSettings settingsPlan;
        static GameParameters.DifficultyParams settingsDiff;
        static Regex regex;
        
        private static string Orange(string message) => "<color=orange>" + message + "</color>"; 

        public void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                Log("Game mode not supported!");
                Destroy(this);
                return;
            }
        }

        public void Start()
        {
            Log("Start");

            settingsSim = HighLogic.CurrentGame.Parameters.CustomParams<KVASS_SimSettings>();
            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASS_PlanSettings>();
            settingsDiff = HighLogic.CurrentGame.Parameters.Difficulty;

            regex = new Regex(LoadRegExpPattern());

            KACWrapper.InitKACWrapper();

            if (KACWrapper.APIReady && settingsPlan.Queue)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;

            // straight calling has problem on the first going into editor
            // GameEvents has problem later.
            // hopefully they will work together
            // also possible to uncomment Coroutine
            ResetEditorLaunchButtons();
            GameEvents.onEditorStarted.Add(ResetEditorLaunchButtons);
            

            /*
            try 
            {
                StartCoroutine(HandleEditorButton_Coroutine());
            }
            catch
            {
                Log("Cannot start coroutine");
            }
            */

        }



        IEnumerable<KACAlarm> GetPlanningAlarms()
        {
            if (!KACWrapper.APIReady) return new List<KACAlarm>();

            var alarms = KACWrapper.KAC.Alarms.Where(
                z => z.Name.StartsWith(Localizer.Format("#KVASS_plan_alarm_title_prefix")));

            return alarms;
        }

        List<KACAlarm> GetPlanningAlarmsSorted()
        {
            var alarms = GetPlanningAlarms();
            return alarms.OrderBy(z => z.AlarmTime).ToList();
        }


        void KAC_onAlarmStateChanged(KACWrapper.KACAPI.AlarmStateChangedEventArgs e)
        {
            if (e.eventType == KACAlarm.AlarmStateEventsEnum.Deleted)
            {
                // e.alarm is still in the list
                var deleting_alarm = e.alarm;
                if (deleting_alarm == null || Remaining(deleting_alarm) <= 0) return;

                var alarms = GetPlanningAlarmsSorted();

                int del_index = alarms.FindIndex(z => z.ID == deleting_alarm.ID);

                double planning_UT_start;
                
                if (del_index == 0)
                    planning_UT_start = HighLogic.CurrentGame.flightState.universalTime;
                else
                    planning_UT_start = alarms[del_index-1].AlarmTime;

                double planning_UT_end = deleting_alarm.AlarmTime;

                double planningTime = planning_UT_end - planning_UT_start;

                for (var i = del_index + 1; i < alarms.Count; i++)
                {
                    alarms[i].AlarmTime = Math.Round(alarms[i].AlarmTime - planningTime);
                }

            }




        }

        public void OnDisable()
        {
            GameEvents.onEditorStarted.Remove(ResetEditorLaunchButtons);
        }

        /// <summary>
        /// Coroutine to reset the launch button handlers every 1/2 second
        /// Needed (not confirmed) because KSP seems to change them behind the scene sometimes
        /// </summary>
        /// <returns></returns>
        IEnumerator HandleEditorButton_Coroutine()
        {
            while (true)
            {
                ResetEditorLaunchButtons();
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator HandleEditorButton_Wait()
        {
            yield return new WaitForSeconds(1f);
            ResetEditorLaunchButtons();
        }


        void ResetEditorLaunchButtons()
        {
            //Log("ResetEditorLaunchButtons");
            //UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent();
            //c.AddListener(OnLoadClick);
            //EditorLogic.fetch.launchBtn.onClick = c;

            // possible revert to previous way?
            UnityEngine.UI.Button greenButton = EditorLogic.fetch.launchBtn;
            greenButton.onClick.RemoveAllListeners();
            greenButton.onClick.AddListener(() => { LaunchListener(null); });

            //yield return new WaitForSeconds(0.25f);


            if (settingsDiff.AllowOtherLaunchSites)
            {
                UILaunchsiteController controller = UnityEngine.Object.FindObjectOfType<UILaunchsiteController>();
                if (controller == null)
                {
                    Log("ResetEditorLaunchButtons: Controller is null");
                    return;
                }

                Log("ResetEditorLaunchButtons: Controller is OK");

                //try
                //{
                //IEnumerable list = controller.GetType()?.GetPrivateMemberValue("launchPadItems", controller, 4) as IEnumerable;

                object items = controller.GetType()?.GetPrivateMemberValue("launchPadItems", controller, 4);

                // the private member "launchPadItems" is a list, and if it is null, then it is
                // not castable to a IEnumerable
                if (items == null) return;

                IEnumerable list = items as IEnumerable;

                Log("ResetEditorLaunchButtons: list is OK");
                foreach (object site in list)
                {
                    //find and disable the button
                    //why isn't EditorLaunchPadItem public despite all of its members being public?


                    UnityEngine.UI.Button button = site.GetType().GetPublicValue<UnityEngine.UI.Button>("buttonLaunch", site);
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                        string siteName = site.GetType().GetPublicValue<string>("siteName", site);
                        button.onClick.AddListener(() => { LaunchListener(siteName); });
                    }
                }

                //}
                //catch (Exception ex)
                //{
                //   Log("ResetEditorLaunchButtons: Exception: " + ex.Message);
                //}

            }

        }


        
        
        



        //Replace the default action to LaunchListener.
        public void LaunchListener(string launchSite) {

            if (string.IsNullOrEmpty(launchSite))
            {
                launchSite = EditorLogic.fetch.launchSiteName;
            }

            if (settingsSim.Enable 
                && !(settingsSim.IgnoreSPH && EditorDriver.editorFacility == EditorFacility.SPH) 
                && regex.IsMatch(EditorLogic.fetch.ship.shipName))
            {
                // Log("Simulation Launch");
                bool success = SimulationPurchase();

                if (success)
                    EditorLogic.fetch.launchVessel(launchSite);
            }
            else if (settingsPlan.Enable
                 && !(settingsPlan.IgnoreSPH && EditorDriver.editorFacility == EditorFacility.SPH) 
                 && KACWrapper.APIReady)
            {
                // Log("Building");

                //string ID;
                string shipName = EditorLogic.fetch.ship.shipName;
                string alarmTitle = Localizer.Format("#KVASS_plan_alarm_title_prefix") + " " + shipName;

                if (IsAlarmFound(alarmTitle, out string ID))
                {
                    if (IsAlarmFinished(ID))
                    {
                        if (IsPossibleToLaunch())
                        {
                            RemoveAlarm(ID);
                            EditorLogic.fetch.launchVessel(launchSite);
                        }
                    }
                    else
                    {
                        PostScreenMessage(Orange(shipName + ": the planning is not finished"));
                    }
                }
                else
                {
                    // Alarm Is Not Found, Creating
                    CreateNewAlarm(alarmTitle);
                    PostScreenMessage(shipName + ": the planning is started\nNew alarm is created", 7);
                }

            }
            else
            {
                Log("SafeLaunch");
                EditorLogic.fetch.launchVessel();
            }
            
        }


        private string LoadRegExpPattern()
        {
            string[] RegExs = { "^.?[Tt]est" };

            ConfigNode[] configs = GameDatabase.Instance.GetConfigNodes("KVASS");

            if (configs != null)
            {
                foreach (var conf in configs)
                    RegExs = RegExs.Concat(conf.GetValues("Regex")).ToArray();
            }

            for (int i = 0; i < RegExs.Length; i++)
                RegExs[i] = "(" + Localizer.Format(RegExs[i]).Trim('"') + ")";

            return String.Join("|", RegExs);
        }

        private static void PostScreenMessage(string message, float duration =5.0f, 
            ScreenMessageStyle style = ScreenMessageStyle.UPPER_CENTER )
        {
            var msg = new ScreenMessage(message, duration, style);
            ScreenMessages.PostScreenMessage(msg);
        }


        //private static void ShowDialog(string message = "You can't afford to launch this vessel.",
        //    string close_title = "Unable to Launch", float height = 100f)
        //{
        //    PopupDialog.SpawnPopupDialog(
        //        new MultiOptionDialog(
        //            "NotEnoughFunds", message, "Not Enough Funds!",
        //            HighLogic.UISkin,
        //            new Rect(0.5f, 0.5f, 300f, height),
        //            new DialogGUIButton(close_title, () => { }, true)
        //        ), false, HighLogic.UISkin, true);
        //}


        private static bool IsPossibleToLaunch()
        {
            // TODO: Check on Damaged LaunchSite
            // TODO: Check on Levels of LaunchSite
            // TODO: Check on Levels of Editor
            // MethodBase CheckFunction = typeof(EditorLogic).GetMethod("GetStockPreFlightCheck", 
            // BindingFlags.Instance | BindingFlags.NonPublic);

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                double shipCost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                if (Funding.Instance.Funds >= shipCost)
                {
                    return true;
                }
                else
                {
                    double diff = Math.Abs(shipCost - Funding.Instance.Funds);
                    double diffLog = Math.Log10(diff);
                    string format = (diffLog > 0) ? "F0" : "F" + Math.Ceiling(-diffLog);

                    string message = "Not Enough Funds!\n"
                    + Funding.Instance.Funds.ToString(format) + " < " + shipCost.ToString(format);

                    PostScreenMessage(Orange(message));

                    return false;
                }
            }
            else
                return true;
        }


        //public static bool LaunchFacilityIntact(bool isVAB)
        //{
        //    bool intact = true;
        //    if (isVAB)
        //    {
        //        //intact = new PreFlightTests.FacilityOperational("LaunchPad", "building").Test();
        //        intact = new PreFlightTests.FacilityOperational("LaunchPad", "LaunchPad").Test();
        //    }
        //    else // SPH
        //    {
        //        if (!new PreFlightTests.FacilityOperational("Runway", "Runway").Test())
        //            intact = false;
        //    }
        //    return intact;
        //}


        /// <summary>
        /// return Boolean success
        /// </summary>
        private static bool SimulationPurchase()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                double shipCost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                double simulCost = 0.01 * settingsSim.Career_Vessel * shipCost;

                if (settingsSim.Career_Bureaucracy)
                    simulCost += settingsSim.Career_Const;

                if (simulCost == 0) return true;

                if (Funding.Instance.Funds >= shipCost + simulCost)
                {
                    Funding.Instance.AddFunds(-simulCost, TransactionReasons.VesselRollout);
                    return true;
                }
                else
                {
                    double diff = Math.Abs(shipCost + simulCost - Funding.Instance.Funds);
                    double diffLog = Math.Log10(diff);
                    string format = (diffLog > 0) ? "F0" : "F" + Math.Ceiling(-diffLog);

                    string message = "Not Enough Funds To Simulate!\n"
                    + Funding.Instance.Funds.ToString(format) + " < " +
                    shipCost.ToString(format) + " + " + simulCost.ToString(format);

                    PostScreenMessage(Orange(message));
                    
                    return false;
                }
            }

            else // if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                float science_points = settingsSim.Science_Vessel * EditorLogic.fetch.ship.GetTotalMass() / 100;

                if (settingsSim.Science_Bureaucracy)
                    science_points += settingsSim.Science_Const;

                if (science_points == 0) return true;

                if (ResearchAndDevelopment .Instance.Science >= science_points )
                {
                    ResearchAndDevelopment.Instance.AddScience(-science_points, TransactionReasons.VesselRollout);
                    return true;
                }
                else
                {
                    double diff = Math.Abs(science_points - ResearchAndDevelopment.Instance.Science);
                    double diffLog = Math.Log10(diff);
                    string format = (diffLog > 0)? "F1" : "F" + Math.Ceiling(-diffLog);

                    string message = "Not Enough Sci-points To Simulate!\n" + 
                    ResearchAndDevelopment.Instance.Science.ToString(format) + " < " + science_points.ToString(format);

                    PostScreenMessage(Orange(message));
                    
                    return false;
                }
            }
        }
        

        private string CreateNewAlarm(string title)
        {
            double time = CalcAlarmTime();

            string aID = "";
            if (KACWrapper.APIReady)
            {
                aID = KACWrapper.KAC.CreateAlarm(
                    KACWrapper.KACAPI.AlarmTypeEnum.Raw,
                    title,
                    Planetarium.GetUniversalTime() + time);

                Log("New Alarm: {0}", title);

                if (aID != "")
                {
                    //if the alarm was made get the object so we can update it
                    KACAlarm alarm = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

                    // a.Remaining doesn't work in the VAB/SPH

                    alarm.Notes = String.Format("{0}", Localizer.Format("#KVASS_plan_message_alarm"));
                    alarm.AlarmMargin = 0;

                    if (settingsPlan.KillTimeWarp)
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
                    else
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.DoNothing;

                    if (settingsPlan.Queue)
                    {
                        var alarms = GetPlanningAlarms();

                        foreach (var a in alarms)
                        {
                            if (Remaining(a) > 0 && a.ID != aID)
                                a.AlarmTime = Math.Round(a.AlarmTime + time);
                        }
                    }
                }
            }
            return aID;
        }

        /*
        void QueueAllAlarms(string aID)
        {
            if (KACWrapper.APIReady && aID != "")
            {
                KACAlarm alarm = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.ID == aID);
                

                double time = Remaining(alarm);


                if (time > 0)
                {
                    var alarms = GetPlanningAlarmsSorted();

                    int index = alarms.FindIndex(z => z.ID == aID);

                    for (var i = index + 1; i < alarms.Count; i++)
                    {
                        alarms[i].AlarmTime += time;
                    }

                    
                    foreach (var a in alarms)
                    {
                        a.AlarmTime += time;
                    }
                    
                }
            }
        }
        */


        private static double CalcAlarmTime()
        {
            double time = 0;
            float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
            float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
            bool career = HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
            
            if (career)
                time = cost * settingsPlan.Career_Seconds;
            else
                time = mass * settingsPlan.Science_Seconds;

            string log_str = "";

            if (settingsPlan.RepSpeedUp && career)
            {
                int currRep = Math.Max((int)Reputation.CurrentRep, 0);
                double lines = currRep / settingsPlan.RepToNextLevel +1;
                time /= lines;
                log_str += String.Format(", RepSpeedUp: x{0}" , lines);
            }

            if (settingsPlan.KerbSpeedUp)
            {
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsPlan.KerbToNextLevel + 1;

                time /= teams ;
                log_str += String.Format(", CrewSpeedUp: x{0}", teams);
            }

            // The last one. The SpeedUps do not affect. 
            if (settingsPlan.Bureaucracy)
                time += settingsPlan.BureaucracyTime * KSPUtil.dateTimeFormatter.Day;

            log_str = String.Format("PlanTime: {0:F1} days", time / KSPUtil.dateTimeFormatter.Day) + log_str;
            Log(log_str);

            return time;
        }

        /// <summary>
        /// Get Alarm by vessel name. Return Alarm or null.
        /// </summary>
        /// <param name="vessel_name"></param>
        /// <returns></returns>
        KACAlarm GetAlarm(string vessel_name)
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
        /// Check is alarm found
        /// </summary>
        /// <param name="vessel_name"></param>
        /// <param name="id"> out ID of found alarm or empty string if alarm is not found</param>
        /// <returns></returns>
        private static bool IsAlarmFound(string vessel_name, out string id)
        {
            if (KACWrapper.APIReady)
            {
                KACAlarm a = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.Name == vessel_name);

                if (a != null)
                {
                    id = a.ID;
                    return true;
                }
            }
            id = "";
            return false;
        }


        /// <summary>
        /// How many seconds remains until alarm will be finished. 
        /// Returns negative values for already finished alarms
        /// </summary>
        /// <param name="a"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        private static double Remaining(KACAlarm a)
        {
            if (a == null) throw new ArgumentNullException("Alarm is null");

            double time_now = HighLogic.CurrentGame.flightState.universalTime;
            double alarmTime = a.AlarmTime;

            return alarmTime - time_now;
        }



        private static bool IsAlarmFinished(string id)
        {
            if (KACWrapper.APIReady)
            {
                KACAlarm a = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.ID == id);

                double rem = Remaining(a);

                return rem < 0.0;
                
            }
            Log("KAC is not found");
            return true; // SafeLoad
        }


        private static bool RemoveAlarm(string id)
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
    }
}
