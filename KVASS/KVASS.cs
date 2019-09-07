using KSP.Localization;
using KSP.UI;
using KVASS_KACWrapper;
using PreFlightTests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using static KVASSNS.Logging;
using static KVASS_KACWrapper.KACWrapper.KACAPI;

namespace KVASSNS
{
    // https://github.com/linuxgurugamer/KCT was used there
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASSSimSettings settingsSim;
        static KVASSPlanSettings settingsPlan;
        static GameParameters.DifficultyParams settingsGame;
        static Regex SimulationRegEx;

        private bool eventsRegistered = false;

        public void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                Log("Game mode is not supported!");
                Destroy(this);
                return;
            }

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                Log("KSC Scene, KVASS is destroyed");
                Destroy(this);
                return;
            }


            settingsSim = HighLogic.CurrentGame.Parameters.CustomParams<KVASSSimSettings>();
            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();
            settingsGame = HighLogic.CurrentGame.Parameters.Difficulty;

            SimulationRegEx = new Regex(LoadRegExpPattern(), RegexOptions.IgnoreCase);

            if (!eventsRegistered)
            {
                eventsRegistered = true;
                GameEvents.onEditorStarted.Add(ResetEditorLaunchButtons);

                // in the awake?
                //Logger.Log("Adding button hooks");
                //EditorLogic.fetch.saveBtn.onClick.AddListener(() => updateSavedVesselCache());
                //EditorLogic.fetch.launchBtn.onClick.AddListener(() => updateSavedVesselCache(SaveButtonSource.LAUNCH));
            }
        }

        public void Start()
        {
            
            KACWrapper.InitKACWrapper();

            if (KACWrapper.APIReady && settingsPlan.Queue)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;


            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                string name = FlightGlobals.ActiveVessel.GetDisplayName();

                if (settingsPlan.Enable
                    && !SimulationRegEx.IsMatch(name)
                //&& !(settingsPlan.IgnoreSPH && FlightGlobals.fetch.activeVessel. == EditorFacility.SPH)
                && KACWrapper.APIReady)
                {
                    string alarmTitle = KACUtils.AlarmTitle(name);

                    KACAlarm alarm = KACUtils.GetAlarm(alarmTitle);
                    
                    if (alarm.Finished())
                    {
                        Log("Removing Alarm");
                        KACUtils.RemoveAlarm(alarm.ID);
                        //settingsGame.
                    }

                    Destroy(this);
                    
                }
                return;
            }

            try 
            {
                //Log("try to start coroutine");
                //StartCoroutine(ResetEditorLaunchButtons_Coroutine());

                StartCoroutine(ResetEditorLaunchButtons_Wait());
            }
            catch (Exception)
            {
                Log("Cannot start coroutine");
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
        IEnumerator ResetEditorLaunchButtons_Coroutine()
        {
            Log("Coroutine()");
            while (true)
            {
                Log("Coroutine()");
                ResetEditorLaunchButtons();
                yield return new WaitForSeconds(0.5f);
            }
        }
        /// <summary>
        /// Coroutine to reset the launch button handlers second time in 0.3 sec. after start. 
        /// </summary>
        /// <returns></returns>
        IEnumerator ResetEditorLaunchButtons_Wait()
        {
            Log("Pouring...");
            yield return new WaitForSeconds(0.3f);

            ResetEditorLaunchButtons();
        }

        // CANNOT be marked as static
        public void ResetEditorLaunchButtons()

        {
            Log("Chug! Chug! Chug!");
            //Log("ResetEditorLaunchButtons");
            //UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent();
            //c.AddListener(OnLoadClick);
            //EditorLogic.fetch.launchBtn.onClick = c;

            // possible revert to previous way?
            UnityEngine.UI.Button greenButton = EditorLogic.fetch.launchBtn;
            greenButton.onClick.RemoveAllListeners();
            greenButton.onClick.AddListener(() => { LaunchClickListener(null); });

            //Log("Green is resetted");


            if (settingsGame.AllowOtherLaunchSites)
            {
                UILaunchsiteController controller = UnityEngine.Object.FindObjectOfType<UILaunchsiteController>();
                if (controller == null)
                {
                    //Log("ResetEditorLaunchButtons: Controller is null");
                    return;
                }

                //Log("ResetEditorLaunchButtons: Controller is OK");


                object items = controller.GetType()?.GetPrivateMemberValue("launchPadItems", controller, 4);


                //Log("ResetEditorLaunchButtons: items");

                //object items2 = controller.GetType()?.GetPrivateMemberValue("runWayItems", controller, 4);

                // the private member "launchPadItems" is a list, and if it is null, then it is
                // not castable to a IEnumerable
                if (items == null) return;
                IEnumerable list = items as IEnumerable;

                //Log("ResetEditorLaunchButtons: list is OK");

                //int i = 0; foreach (object site in list) { i++; }; Log("launchPadItems: " + i);



                //if (items2 == null) return;
                //IEnumerable list2 = items2 as IEnumerable;
                //i = 0; foreach (object site in list2) { i++; }; Log("runWayItems: " + i);

                foreach (object site in list)
                {
                    //find and disable the button
                    //why isn't EditorLaunchPadItem public despite all of its members being public?
                    UnityEngine.UI.Button button = site.GetType().GetPublicValue<UnityEngine.UI.Button>("buttonLaunch", site);
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                        string siteName = site.GetType().GetPublicValue<string>("siteName", site);
                        button.onClick.AddListener(() => { LaunchClickListener(siteName); });
                    }
                }
            }
        }

        //Replace the default action to LaunchListener.
        static void LaunchClickListener(string launchSite)
        {
            Log("KVASS Launch Sequence");
            Log("launchSite param: " + launchSite);

            if (string.IsNullOrEmpty(launchSite))
            {
                launchSite = EditorLogic.fetch.launchSiteName;
            }
            Log("launchSite reseted: " + launchSite);


            if (settingsSim.Enable
                && !(settingsSim.IgnoreSPH && EditorDriver.editorFacility == EditorFacility.SPH)
                && SimulationRegEx.IsMatch(EditorLogic.fetch.ship.shipName))
            {
                bool success = SimulationPurchase();

                if (success)
                    EditorLogic.fetch.launchVessel(launchSite);
            }
            else if (settingsPlan.Enable
                && !(settingsPlan.IgnoreSPH && EditorDriver.editorFacility == EditorFacility.SPH)
                && KACWrapper.APIReady)
            {
                string shipName = EditorLogic.fetch.ship.shipName;
                string alarmTitle = KACUtils.AlarmTitle(shipName);

                KACAlarm alarm = KACUtils.GetAlarm(alarmTitle);

                if (alarm == null)
                {
                    // Alarm Is Not Found, Creating
                    CreateNewAlarm(alarmTitle);
                }
                else if (KACUtils.IsAlarmFinished(alarm))
                {
                    EditorLogic.fetch.launchVessel(launchSite);
                }
                else
                {
                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_not_finished", alarmTitle));
                }

            }
            else
            {
                Log("SafeLaunch");
                EditorLogic.fetch.launchVessel();
            }
        }

        static string LoadRegExpPattern()
        {
            string[] RegExs = { "^.?test" };

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

        /// <summary>
        /// return Boolean success
        /// </summary>
        static bool SimulationPurchase()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                double shipCost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                double simulCost = 0.01 * settingsSim.CareerVessel * shipCost;

                if (settingsSim.CareerBureaucracy)
                    simulCost += settingsSim.CareerConst;

                if (simulCost == 0) return true;



                if (Funding.Instance.Funds >= shipCost + simulCost)
                {
                    Funding.Instance.AddFunds(-simulCost, TransactionReasons.VesselRollout);
                    return true;
                }
                else
                {
                    //#autoLOC_419441 = Funds: <<1>>
                    //#autoLOC_900528 = Cost

                    string format = GetComparingFormat(Funding.Instance.Funds, shipCost, simulCost);

                    Messages.Add(
                        Localizer.Format("#KVASS_message_not_enough_funds_to_sim"),
                        String.Format("{0} [{1}: {2}+{3}]\n",
                            Localizer.Format("#autoLOC_419441", Funding.Instance.Funds),
                            Localizer.Format("#autoLOC_900528"), 
                            shipCost.ToString(format), 
                            simulCost.ToString(format)
                        )
                    );

                    Messages.ShowAndClear();

                    return false;
                }
            }

            else // if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                float science_points = settingsSim.ScienceVessel * EditorLogic.fetch.ship.GetTotalMass() / 100;

                if (settingsSim.ScienceBureaucracy)
                    science_points += settingsSim.ScienceConst;

                if (science_points == 0) return true;

                if (ResearchAndDevelopment.Instance.Science >= science_points)
                {
                    ResearchAndDevelopment.Instance.AddScience(-science_points, TransactionReasons.VesselRollout);
                    return true;
                }
                else
                {
                    //#autoLOC_419420 = Science: <<1>>
                    //#autoLOC_900528 = Cost

                    string format = GetComparingFormat(ResearchAndDevelopment.Instance.Science, science_points);

                    Messages.Add(
                        Localizer.Format("#KVASS_message_not_enough_sci_to_sim"),
                        String.Format("{0} [{1}: {2}]\n",
                            Localizer.Format("#autoLOC_419420", ResearchAndDevelopment.Instance.Science),
                            Localizer.Format("#autoLOC_900528"),
                            science_points.ToString(format)
                        )
                    );

                    Messages.ShowAndClear();

                    return false;
                }
            }
        }

        /// <summary>
        /// Get a precision format for comparing string with the least digits.
        /// Example: value: 12.04, addends: 3.01, 9.02
        /// returns "F2";
        /// </summary>
        /// <param name="value"></param>
        /// <param name="addends"></param>
        /// <returns></returns>
        static string GetComparingFormat(double value, params double[] addends)
        {
            if (addends.Length == 0) return "";
            const double Eps = 1e-10;

            double sum = addends.Sum();
            double diff = sum - value;
            double diff_abs = Math.Abs(diff);

            if (diff_abs < Eps) return "";

            int i = 0;
            const int maxFracDigits = 8;

            for (double diff_rounded_abs = 0; i < maxFracDigits; i++)
            {
                double sum_rounded = addends.Select(z => Math.Round(z, i)).Sum();
                double value_rounded = Math.Round(value, i);
                double diff_rounded = sum_rounded - value_rounded;
                diff_rounded_abs = Math.Abs(diff_rounded);

                if (diff_rounded_abs > Eps
                        && Math.Sign(diff_rounded) == Math.Sign(diff))
                    return "F" + i;
            }

            return "";
        }

        static string CreateNewAlarm(string title)
        {
            double time = CalcAlarmTime();

            string aID = "";
            if (KACWrapper.APIReady)
            {
                double UT = Planetarium.GetUniversalTime();
                aID = KACWrapper.KAC.CreateAlarm(
                    KACWrapper.KACAPI.AlarmTypeEnum.Raw,
                    title,
                    UT + time);


                if (!String.IsNullOrEmpty(aID))
                {
                    //if the alarm was made get the object so we can update it
                    KACAlarm alarm = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

                    // a.Remaining doesn't work in the VAB/SPH

                    alarm.Notes = Localizer.Format("#KVASS_alarm_note");
                    alarm.AlarmMargin = 0;

                    if (settingsPlan.KillTimeWarp)
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
                    else
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.DoNothing;


                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_created", title), 7);

                    if (settingsPlan.Queue)
                    {
                        var alarms = KACUtils.GetPlanningActiveAlarms();
                        int alarmsMoved = 0;
                        string firstName = "";

                        foreach (var a in alarms)
                        {
                            if (a.ID != aID)
                            {
                                a.AlarmTime += time;
                                alarmsMoved++;
                                if (alarmsMoved == 1) firstName = a.Name;
                            }
                        }

                        if (alarmsMoved == 1)
                        {
                            string shipname = KACUtils.ShipName(firstName);
                            Messages.QuickPost(Localizer.Format("#KVASS_alarm_created_another", shipname));
                        }
                        else if (alarmsMoved > 1)
                            Messages.QuickPost(Localizer.Format("#KVASS_alarm_created_others", alarmsMoved));
                    }
                }
            }
            return aID;
        }

        static double CalcAlarmTime()
        {
            float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
            float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
            bool career = HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
            double time;

            if (career)
                time = cost * settingsPlan.CareerSeconds;
            else
                time = mass * settingsPlan.ScienceSeconds;

            string log_str = "";

            if (settingsPlan.RepSpeedUp && career)
            {
                int currRep = Math.Max((int)Reputation.CurrentRep, 0);
                double lines = currRep / settingsPlan.RepToNextLevel + 1;
                time /= lines;
                log_str += ", RepSpeedUp: x" + lines;
            }

            if (settingsPlan.KerbSpeedUp)
            {
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsPlan.KerbToNextLevel + 1;

                time /= teams;
                log_str += ", CrewSpeedUp: x" + teams;
            }

            // The last one. The SpeedUps do not affect. 
            if (settingsPlan.Bureaucracy)
                time += settingsPlan.BureaucracyTime * KSPUtil.dateTimeFormatter.Day;

            log_str = String.Format("PlanTime: {0:F1} days", time / KSPUtil.dateTimeFormatter.Day) + log_str;
            Log(log_str);

            return time;
        }

        void KAC_onAlarmStateChanged(KACWrapper.KACAPI.AlarmStateChangedEventArgs e)
        {
            if (e.eventType == KACAlarm.AlarmStateEventsEnum.Deleted)
            {

                // e.alarm is still in the list
                var deleting_alarm = e.alarm;
                if (deleting_alarm == null || KACUtils.Remaining(deleting_alarm) <= 0) return;

                //Log("    Deleting: " + deleting_alarm.Name);

                var alarms = KACUtils.GetPlanningActiveAlarms().OrderBy(z => z.AlarmTime).ToList();

                //alarms.ForEach(z => Log(z.Name+" " +z.AlarmTime));

                int del_index = alarms.FindIndex(z => z.ID == deleting_alarm.ID);

                double planning_UT_start;

                if (del_index == 0)
                    planning_UT_start = HighLogic.CurrentGame.flightState.universalTime;
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

                int alarmsMoved = alarms.Count - (del_index+1);

                Messages.QuickPost(Localizer.Format("#KVASS_alarm_deleted", deleting_alarm.Name));

                if (alarmsMoved == 1)
                {
                    string ShipName = KACUtils.ShipName(alarms[del_index + 1].Name);

                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_deleted_another", ShipName));
                }
                else if (alarmsMoved > 1)
                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_deleted_others", alarmsMoved));

            }
        }
    }
}
