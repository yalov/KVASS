using KSP.Localization;
using KSP.UI;
using KVASS_KACWrapper;
using PreFlightTests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static KVASSNS.Logging;

using static KVASS_KACWrapper.KACWrapper.KACAPI;
using Smooth.Algebraics;

namespace KVASSNS
{
    // https://github.com/linuxgurugamer/KCT was used there
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASSSimSettings settingsSim;
        static KVASSPlanSettings settingsPlan;
        static GameParameters.DifficultyParams settingsGame;
        static Regex regex;

        private static string Orange(string message) => "<color=orange>" + message + "</color>"; // #ffa500ff
        private static string OrangeAlpha(string message) => "<color=#ffa500af>" + message + "</color>";
        private static string Red(string message) => "<color=red>" + message + "</color>";

        private static string Asterix(string message, bool enclose) 
            => (enclose?"<color=red>*</color> ":"") + message + (enclose ? " <color=red>*</color> " : "");

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

            settingsSim = HighLogic.CurrentGame.Parameters.CustomParams<KVASSSimSettings>();
            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();
            settingsGame = HighLogic.CurrentGame.Parameters.Difficulty;

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



        static IEnumerable<KACAlarm> GetPlanningAlarms()
        {
            if (!KACWrapper.APIReady) return new List<KACAlarm>();

            var alarms = KACWrapper.KAC.Alarms.Where(
                z => z.Name.StartsWith(Localizer.Format("#KVASS_plan_alarm_title_prefix"), StringComparison.Ordinal));

            return alarms;
        }

        static List<KACAlarm> GetPlanningAlarmsSorted()
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
                    planning_UT_start = alarms[del_index - 1].AlarmTime;

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
        IEnumerator ResetEditorLaunchButtons_Coroutine()
        {
            while (true)
            {
                ResetEditorLaunchButtons();
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator ResetEditorLaunchButtons_Wait()
        {
            yield return new WaitForSeconds(1f);
            ResetEditorLaunchButtons();
        }


        static void ResetEditorLaunchButtons()
        {
            //Log("ResetEditorLaunchButtons");
            //UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent();
            //c.AddListener(OnLoadClick);
            //EditorLogic.fetch.launchBtn.onClick = c;

            // possible revert to previous way?
            UnityEngine.UI.Button greenButton = EditorLogic.fetch.launchBtn;
            greenButton.onClick.RemoveAllListeners();
            greenButton.onClick.AddListener(() => { LaunchListener(null); });

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

                // the private member "launchPadItems" is a list, and if it is null, then it is
                // not castable to a IEnumerable
                if (items == null) return;

                IEnumerable list = items as IEnumerable;

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
            }
        }



        //Replace the default action to LaunchListener.
        static void LaunchListener(string launchSite)
        {
            Log("KVASS Launch Sequence");

            if (string.IsNullOrEmpty(launchSite))
            {
                launchSite = EditorLogic.fetch.launchSiteName;
            }

            if (settingsSim.Enable
                && !(settingsSim.IgnoreSPH && EditorDriver.editorFacility == EditorFacility.SPH)
                && regex.IsMatch(EditorLogic.fetch.ship.shipName))
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
                string alarmTitle = Localizer.Format("#KVASS_plan_alarm_title_prefix") + " " + shipName;

                KACAlarm alarm = GetAlarm(alarmTitle);

                if (alarm == null)
                {
                    // Alarm Is Not Found, Creating
                    CreateNewAlarm(alarmTitle);
                    PostScreenMessage(shipName + ": the planning is started\nNew alarm is created", 7);
                }
                else if (IsAlarmFinished(alarm))
                {
                    bool checks_succeed = CheckLaunchingPossibility(launchSite);
                    if (checks_succeed)
                        RemoveAlarm(alarm.ID);

                    // even if checks_succeed is false, launching will be interrupted by KSP.
                    // CheckLaunchingPossibility needed for not removing alarm if launch will be failed
                    Log("KVASS launches now!");
                    EditorLogic.fetch.launchVessel(launchSite);
                }
                else
                {
                    PostScreenMessage(Orange(shipName + ": the planning is not finished"));
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

        static void PostScreenMessage(string message, float duration = 5.0f,
            ScreenMessageStyle style = ScreenMessageStyle.UPPER_CENTER)
        {
            //var msg = new ScreenMessage(message, duration, style);
            ScreenMessages.PostScreenMessage(message, duration, style);
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


        /// <summary>
        /// Check Possibility to Launch, and show nessesary message on screen. 
        /// Return bool possibility. 
        /// </summary>
        /// <returns></returns>
        static bool CheckLaunchingPossibility(string launchsite)
        {
            Log("launchsite: " + launchsite);
            string launchsite_display = launchsite.Replace("_", " ");
            

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return true;

            List<Tuple<string, string>> fail_messages = new List<Tuple<string, string>>();

            

            //ShipConstruct ship = ShipConstruction.LoadShip();
            ShipConstruct ship = EditorLogic.fetch.ship;
            string Editor_Desc = ship.shipFacility.displayDescription();

            // HighLogic.CurrentGame.editorFacility
 
            if (false)
            {

                SpaceCenterFacility sph = SpaceCenterFacility.SpaceplaneHangar;
                SpaceCenterFacility rw = SpaceCenterFacility.Runway;
                float sph_level = ScenarioUpgradeableFacilities.GetFacilityLevel(sph);
                float rw_level = ScenarioUpgradeableFacilities.GetFacilityLevel(rw);

                int pcl = GameVariables.Instance.GetPartCountLimit(sph_level, false);
                Vector3 psl = GameVariables.Instance.GetCraftSizeLimit(rw_level, false);
                double mass = (double)GameVariables.Instance.GetCraftMassLimit(rw_level, false);

                Log("SPH PartCountLimit: " + pcl);
                Log("SPH CraftSizeLimit: " + psl);
                Log("SPH CraftMassLimit: " + mass);

                CraftWithinPartCountLimit partcount = new CraftWithinPartCountLimit(ship, sph, pcl);
                CraftWithinSizeLimits sizetest      =     new CraftWithinSizeLimits(ship, rw,  psl);
                CraftWithinMassLimits masstest      =     new CraftWithinMassLimits(ship, rw,  mass);

                if (!partcount.Test()) {
              //      fail_messages.Add("SPH Part Count Test");
                }
                if (!sizetest.Test())
                {
             //       fail_messages.Add("SPH Size Test");
                }
                if (!masstest.Test())
                {
             //       fail_messages.Add("SPH Mass Test");
                }
            }
            else if (true)
            {
                SpaceCenterFacility vab = SpaceCenterFacility.VehicleAssemblyBuilding;
                SpaceCenterFacility lp = SpaceCenterFacility.LaunchPad;
                float vab_level = ScenarioUpgradeableFacilities.GetFacilityLevel(vab);
                float lp_level = ScenarioUpgradeableFacilities.GetFacilityLevel(lp);

                int PartsCountLimit = GameVariables.Instance.GetPartCountLimit(vab_level, true);
                Vector3 SizeLimit = GameVariables.Instance.GetCraftSizeLimit(lp_level, true);
                double MassLimit = (double)GameVariables.Instance.GetCraftMassLimit(lp_level, true);

                Log("VAB PartCountLimit: " + PartsCountLimit);
                Log("VAB CraftSizeLimit: " + SizeLimit);
                Log("VAB CraftMassLimit: " + MassLimit);

                CraftWithinPartCountLimit partcount = new CraftWithinPartCountLimit(ship, vab, PartsCountLimit);
                CraftWithinSizeLimits sizetest      = new CraftWithinSizeLimits(ship, lp, SizeLimit);
                CraftWithinMassLimits masstest      = new CraftWithinMassLimits(ship, lp, MassLimit);

                if (!partcount.Test())
                {
                    //#autoLOC_250727 = Craft has too many parts!
                    //#autoLOC_250732 = The <<1>> can't support vessels over <<2>> parts.
                    //#autoLOC_443352 = Parts:
                    //#autoLOC_6001000 = Max.


                    fail_messages.Add(new Tuple<string, string>(
                        Localizer.Format("#autoLOC_250727"),
                        Localizer.Format("#autoLOC_250732", Editor_Desc, PartsCountLimit)
                    ));


                }
                if (!sizetest.Test())
                {
                    //#autoLOC_250793 = Craft is too large for <<1>>!
                    //#autoLOC_443418 = Height:
                    //#autoLOC_443419 = Width:
                    //#autoLOC_443420 = Length:
                    //#autoLOC_6001000 = Max.


                    float width = ship.shipSize.x;
                    float height = ship.shipSize.y;
                    float length = ship.shipSize.z;
                    float diameter = Math.Max(ship.shipSize.x, ship.shipSize.z);

                    float max_width = SizeLimit.x;
                    float max_height = SizeLimit.y;
                    float max_length = SizeLimit.z;
                    float max_diameter = Math.Max(SizeLimit.x, SizeLimit.z);

                    string message1 = Localizer.Format("#autoLOC_250793", launchsite_display);
                    string message = "";
                    //  Height: 9.9 [Max. 5.5]

                    if (height > max_height)
                        message += String.Format("{0} {1:F1} [{2} {3:F1}]\n",
                            Localizer.Format("#autoLOC_443418"), height,
                            Localizer.Format("#autoLOC_6001000"), max_height);

                    if (width > max_width)
                        message += String.Format("{0} {1:F1} [{2} {3:F1}]\n", 
                            Localizer.Format("#autoLOC_443419"), width,
                            Localizer.Format("#autoLOC_6001000"), max_width);

                    if (length > max_length)
                        message += String.Format("{0} {1:F1} [{2} {3:F1}]",
                            Localizer.Format("#autoLOC_443420"), length,
                            Localizer.Format("#autoLOC_6001000"), max_length);

                    fail_messages.Add(new Tuple<string, string>( message1, message));

                }
                if (!masstest.Test())
                {
                    //#autoLOC_250677 = Craft is too heavy!
                    //#autoLOC_250682 = The <<1>> can't support vessels heavier than <<2>>t.\n<<3>>'s total mass is <<4>>t.
                    //# autoLOC_443357 = Mass:

        

                    fail_messages.Add(new Tuple<string, string>(
                        Localizer.Format("#autoLOC_250677") ,
                        Localizer.Format("#autoLOC_250682", launchsite_display, MassLimit).Split('\n').FirstOrDefault()
                    ));
                }
            }
            else
            {
               // fail_messages.Add("Failed to lauch vessel, unrecognized lauch site. Disable KVASS in the settings.");
            }


            CanAffordLaunchTest fundsCheck = new CanAffordLaunchTest(ship, Funding.Instance);
            FacilityOperational opCheck = new FacilityOperational(launchsite, launchsite);
            LaunchSiteClear clearCheck = new LaunchSiteClear(launchsite, launchsite, HighLogic.CurrentGame);

            if (!opCheck.Test())
            {

                //fail_messages.Add(Localizer.Format("#KVASS_message_out_of_service", launchsite.Replace("_", " ")));
                //#autoLOC_253284 = <<1>> Out of Service
                //#autoLOC_253289 = The <<1>> is not in serviceable conditions. Cannot proceed.



                fail_messages.Add(new Tuple<string, string>(
                    Localizer.Format("#autoLOC_253284", launchsite_display),
                    Localizer.Format("#autoLOC_253289", launchsite_display).Split('.').FirstOrDefault()
                ));
            }

            if (!clearCheck.Test())
            {
                //#autoLOC_253369 = <<1>> not clear
                //#autoLOC_253374 = <<1>> is already on the <<2>>

    

                fail_messages.Add(new Tuple<string, string>(
                    Localizer.Format("#autoLOC_253369", launchsite_display) ,
                    Localizer.Format("#autoLOC_253374", "Some vessel", launchsite_display)
                ));
            }

            if (!fundsCheck.Test())
            {
                //#autoLOC_250625 = Not Enough Funds!
                //#autoLOC_250630 = You can't afford to launch this vessel.

                float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                double funds = Funding.Instance.Funds;

    

                fail_messages.Add(new Tuple<string, string>(
                    Localizer.Format("#autoLOC_250625") + " (" + GetComparingString(funds, cost) + ")",
                    Localizer.Format("#autoLOC_250630")
                ));
            }            

            string paragraph_prefix = Localizer.Format("#KVASS_message_paragraph_prefix");  // "* ";

            //for (int i = 0; i < fail_messages.Count; i++)
            //    PostScreenMessage(
            //            (fail_messages.Count > 1 ? Red(paragraph_prefix): "") + Orange(fail_messages[i]), 
            //            5 * (i+1));

            //if (fail_messages.Count > 1)
            //    PostScreenMessage(Localizer.Format("#KVASS_message_total", fail_messages.Count),
            //        fail_messages.Count+1 * 5.0f);


            for (int i = 0; i < fail_messages.Count; i++)
                PostScreenMessage(
                    Asterix(Orange(fail_messages[i].Item1), fail_messages.Count > 1)
                        +"\n" + fail_messages[i].Item2,
                    5 * (i + 1)
                );

            if (fail_messages.Count > 1)
                PostScreenMessage(Orange(Localizer.Format("#KVASS_message_total", fail_messages.Count)),
                    (fail_messages.Count + 1) * 5);

            return (fail_messages.Count == 0); // success

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
                    double diff = Math.Abs(shipCost + simulCost - Funding.Instance.Funds);
                    double diffLog = Math.Log10(diff);
                    string format = (diffLog > 0) ? "F0" : "F" + Math.Ceiling(-diffLog);

                    string message = Localizer.Format("#KVASS_message_not_enough_funds_to_sim",
                    GetComparingString(Funding.Instance.Funds, shipCost, simulCost));

                    //+ Funding.Instance.Funds.ToString(format) + " < " +
                    //shipCost.ToString(format) + " + " + simulCost.ToString(format);

                    PostScreenMessage(Orange(message));

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
                    

                    double diff = Math.Abs(science_points - ResearchAndDevelopment.Instance.Science);
                    double diffLog = Math.Log10(diff);
                    string format = (diffLog > 0) ? "F1" : "F" + Math.Ceiling(-diffLog);

                    string message = Localizer.Format("#KVASS_message_not_enough_sci_to_sim",
                        GetComparingString(ResearchAndDevelopment.Instance.Science, science_points));

                    //ResearchAndDevelopment.Instance.Science.ToString(format) + " < " + science_points.ToString(format);

                    PostScreenMessage(Orange(message));

                    return false;
                }
            }
        }
        /// <summary>
        /// Get a comparing string with nessesary digits:
        /// value (<|>|=) summator1 + ... + summatorN
        /// </summary>
        /// <param name="value"></param>
        /// <param name="summators"></param>
        /// <returns></returns>
        static string GetComparingString(double value,params double[] summators)
        {
            if (summators.Length == 0) return "";

            double sum = summators.Sum();

            double diff = Math.Abs(sum - value);
            double diffLog = Math.Log10(diff);
            string format = (diffLog > 0) ? "F0" : "F" + Math.Ceiling(-diffLog);

            string equality = (value < sum ? " < " : (value > sum ? " > " : " = "));

            return value.ToString(format, CultureInfo.InvariantCulture) + equality + 
                String.Join(" + ", summators.Select(d => d.ToString(format, CultureInfo.InvariantCulture)).ToArray());
        }

        static string CreateNewAlarm(string title)
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


                if (!String.IsNullOrEmpty(aID))
                {
                    //if the alarm was made get the object so we can update it
                    KACAlarm alarm = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

                    // a.Remaining doesn't work in the VAB/SPH

                    alarm.Notes = Localizer.Format("#KVASS_plan_message_alarm");
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

        /// <summary>
        /// Get Alarm by vessel name. Return Alarm or null.
        /// </summary>
        /// <param name="vessel_name"></param>
        /// <returns></returns>
        static KACAlarm GetAlarm(string vessel_name)
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
        /// How many seconds remains until alarm will be finished. 
        /// Returns negative values for already finished alarms
        /// </summary>
        /// <param name="a"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        static private double Remaining(KACAlarm a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));

            double time_now = HighLogic.CurrentGame.flightState.universalTime;
            double alarmTime = a.AlarmTime;

            return alarmTime - time_now;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        static bool IsAlarmFinished(KACAlarm a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));

            return Remaining(a) < 0.0;
        }

        /// <summary>
        /// Remove alarm by ID. Return bool success.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        static bool RemoveAlarm(string id)
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
