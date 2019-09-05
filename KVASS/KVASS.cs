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
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASSSimSettings settingsSim;
        static KVASSPlanSettings settingsPlan;
        static GameParameters.DifficultyParams settingsGame;
        static Regex regex;

        private bool eventsRegistered = false;

        public void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                Log("Game mode is not supported!");
                Destroy(this);
                return;
            }


            settingsSim = HighLogic.CurrentGame.Parameters.CustomParams<KVASSSimSettings>();
            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();
            settingsGame = HighLogic.CurrentGame.Parameters.Difficulty;

            regex = new Regex(LoadRegExpPattern(), RegexOptions.IgnoreCase);

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

        IEnumerator ResetEditorLaunchButtons_Wait()
        {
            Log("Pouring...");
            yield return new WaitForSeconds(0.3f);
            Log("Pouring finished");

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
                string alarmTitle = Localizer.Format("#KVASS_alarm_title_prefix") + " " + shipName;

                KACAlarm alarm = GetAlarm(alarmTitle);

                if (alarm == null)
                {
                    // Alarm Is Not Found, Creating
                    CreateNewAlarm(alarmTitle);
                }
                else if (IsAlarmFinished(alarm))
                {
                    bool checks_succeed = CheckLaunchingPossibility(launchSite);
                    if (checks_succeed)
                    {
                        RemoveAlarm(alarm.ID);
                        EditorLogic.fetch.launchVessel(launchSite);
                    }
                }
                else
                {
                    Messages.Add(Localizer.Format("#KVASS_alarm_not_finished", alarmTitle));
                    Messages.ShowAndClear();
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
        /// Check Possibility to Launch, and show necessary message on screen. 
        /// </summary>
        /// <returns></returns>
        static bool CheckLaunchingPossibility(string launchsite)
        {
            Log("Launch Site: " + launchsite);

            // TODO: launchsite_display
            string launchsite_display = launchsite.Replace("_", " ");

            // TODO: Science
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return true;

            //ShipConstruct ship = ShipConstruction.LoadShip();
            ShipConstruct ship = EditorLogic.fetch.ship;


            // !!!!!!!!!!!!!!!!
            // Detector isVAB ?
            // Detector isLaunchPad ? 
            // launchsite -> SpaceCenterFacility
            // launchsite -> launchsite_display

            bool isVABAndLaunchPad = EditorDriver.editorFacility.Equals(EditorFacility.VAB);

            //ship.shipFacility == EditorFacility.VAB;

            Log("isVABAndLaunchPad: " + isVABAndLaunchPad);


            SpaceCenterFacility EditorBuilding;
            SpaceCenterFacility LaunchBuilding;

            if (isVABAndLaunchPad)
            {
                EditorBuilding = SpaceCenterFacility.VehicleAssemblyBuilding;
                LaunchBuilding = SpaceCenterFacility.LaunchPad;
            }
            else
            {
                EditorBuilding = SpaceCenterFacility.SpaceplaneHangar;
                LaunchBuilding = SpaceCenterFacility.Runway;
            }

            float eb_level = ScenarioUpgradeableFacilities.GetFacilityLevel(EditorBuilding);
            float lb_level = ScenarioUpgradeableFacilities.GetFacilityLevel(LaunchBuilding);

            int PartsCountLimit = GameVariables.Instance.GetPartCountLimit(eb_level, isVABAndLaunchPad);
            Vector3 SizeLimit = GameVariables.Instance.GetCraftSizeLimit(lb_level, isVABAndLaunchPad);
            double MassLimit = (double)GameVariables.Instance.GetCraftMassLimit(lb_level, isVABAndLaunchPad);

            Log("PartCountLimit: " + PartsCountLimit);
            Log("CraftSizeLimit: " + SizeLimit);
            Log("CraftMassLimit: " + MassLimit);

            int KKPartsCountLimit = Reflection.GetKKCraftPartCountLimit(launchsite);
            Vector3 KKSizeLimit   = Reflection.GetKKCraftSizeLimit(launchsite);
            double KKMassLimit    = Reflection.GetKKCraftMassLimit(launchsite);

            Log("KKPartCountLimit: " + KKPartsCountLimit);
            Log("KKCraftSizeLimit: " + KKSizeLimit);
            Log("KKCraftMassLimit: " + KKMassLimit);

            PartsCountLimit = Math.Min(PartsCountLimit, PartsCountLimit);
            SizeLimit      = Utils.Min(SizeLimit,       SizeLimit      );
            MassLimit       = Math.Min(MassLimit,       MassLimit);
            

            CraftWithinPartCountLimit partCountTest = new CraftWithinPartCountLimit(ship, EditorBuilding, PartsCountLimit);
            CraftWithinSizeLimits sizeTest      = new CraftWithinSizeLimits(ship, LaunchBuilding, SizeLimit);
            CraftWithinMassLimits massTest      = new CraftWithinMassLimits(ship, LaunchBuilding, MassLimit);
            CanAffordLaunchTest fundsCheck = new CanAffordLaunchTest(ship, Funding.Instance);
            FacilityOperational operationalCheck = new FacilityOperational(launchsite, launchsite);
            LaunchSiteClear clearCheck = new LaunchSiteClear(launchsite, launchsite, HighLogic.CurrentGame);

            //bool partCountKKTest = Reflection.PartCountKKTest(ship, launchsite);
            //bool sizeKKTest = Reflection.SizeKKTest(ship, launchsite);
            //bool massKKTest = Reflection.MassKKTest(ship, launchsite);

            //#autoLOC_6001000 = Max.


            if (!partCountTest.Test())
            {
                //#autoLOC_250727 = Craft has too many parts!
                //#autoLOC_250732 = The <<1>> can't support vessels over <<2>> parts.
                //#autoLOC_443352 = Parts:

                Messages.Add(
                    partCountTest.GetFailedMessage(), 
                    partCountTest.GetFailedNote(ship.Parts.Count, PartsCountLimit));

                //Messages.Add(
                //    Localizer.Format("#autoLOC_250727"),
                //    String.Format("{0} {1} [{2} {3}]\n",
                //        Localizer.Format("#autoLOC_443352"), ship.Parts.Count,
                //        Localizer.Format("#autoLOC_6001000"), PartsCountLimit
                //    )
                //);
            }

            if (!sizeTest.Test())
            {
                //#autoLOC_250793 = Craft is too large for <<1>>!
                //#autoLOC_443418 = Height:
                //#autoLOC_443419 = Width:
                //#autoLOC_443420 = Length:
                //#autoLOC_482486 = <<1>>m

                float width = ship.shipSize.x;
                float height = ship.shipSize.y;
                float length = ship.shipSize.z;

                float max_width = SizeLimit.x;
                float max_height = SizeLimit.y;
                float max_length = SizeLimit.z;

                string message1 = Localizer.Format("#autoLOC_250793", launchsite_display);
                string message2 = "";

                if (height > max_height)
                    message2 += String.Format("{0} {1} [{2} {3}]\n",
                        Localizer.Format("#autoLOC_443418"), Localizer.Format("#autoLOC_482486", height.ToString("F1")),
                        Localizer.Format("#autoLOC_6001000"), Localizer.Format("#autoLOC_482486", max_height.ToString("F1")));

                if (width > max_width)
                    message2 += String.Format("{0} {1} [{2} {3}]\n", 
                        Localizer.Format("#autoLOC_443419"), Localizer.Format("#autoLOC_482486", width.ToString("F1")),
                        Localizer.Format("#autoLOC_6001000"), Localizer.Format("#autoLOC_482486", max_width.ToString("F1")));

                if (length > max_length)
                    message2 += String.Format("{0} {1} [{2} {3}]\n",
                        Localizer.Format("#autoLOC_443420"), Localizer.Format("#autoLOC_482486", length.ToString("F1")),
                        Localizer.Format("#autoLOC_6001000"), Localizer.Format("#autoLOC_482486", max_length.ToString("F1")));

                Messages.Add( message1, message2);
            }

            if (!massTest.Test())
            {
                //#autoLOC_250677 = Craft is too heavy!
                //#autoLOC_250682 = The <<1>> can't support vessels heavier than <<2>>t.\n<<3>>'s total mass is <<4>>t.
                //#autoLOC_482576 = Mass: <<1>>t
                //#autoLOC_5050023 = <<1>>t

                string format = GetComparingFormat(ship.GetTotalMass(), MassLimit);

                string mass = ship.GetTotalMass().ToString(format);
                string massMax = MassLimit.ToString(format);

                Messages.Add(
                    Localizer.Format("#autoLOC_250677") ,
                    String.Format("{0} [{1} {2}]\n",
                        Localizer.Format("#autoLOC_482576", mass),
                        Localizer.Format("#autoLOC_6001000"), 
                        Localizer.Format("#autoLOC_5050023", massMax)
                    )
                );
            }
  
            if (!operationalCheck.Test())
            {
                //#autoLOC_253284 = <<1>> Out of Service
                //#autoLOC_253289 = The <<1>> is not in serviceable conditions. Cannot proceed.
                //#autoLOC_238803 = Cleanup cost: <<1>>
                //#autoLOC_475433 = Repair for <color=<<1>>><<2>></b></color>\n
                //#autoLOC_439829 = <<1>> Funds
                //#autoLOC_475433 = Ремонтировать: <color=<<1>>><<2>></b></color>\n
                //#autoLOC_439829 = Кредиты: <<1>>

                List<DestructibleBuilding> destructibles = new List<DestructibleBuilding>();
                foreach (KeyValuePair<string, ScenarioDestructibles.ProtoDestructible> kvp in ScenarioDestructibles.protoDestructibles)
                    if (kvp.Key.Contains(launchsite)) // "LaunchPad"
                        destructibles.AddRange(kvp.Value.dBuildingRefs);

                //foreach (DestructibleBuilding facility in destructibles)
                //    Log(facility.name+ ", destroyed: "+ facility.IsDestroyed + ", cost: " + facility.RepairCost);

                float RepairCost = destructibles.Where(facility => facility.IsDestroyed)
                    .Sum(facility => facility.RepairCost);

                Log("RepairCost: " + RepairCost);

                Messages.Add(
                    Localizer.Format("#autoLOC_253284", launchsite_display),
                    //Localizer.Format("#autoLOC_253289", launchsite_display).Split('.').FirstOrDefault()
                    Localizer.Format("#autoLOC_475433", Messages.NoteColor, 
                        Localizer.Format("#autoLOC_439829", RepairCost.ToString("F0")))
                ) ;
            }

            if (!clearCheck.Test())
            {
                //#autoLOC_253369 = <<1>> not clear
                //#autoLOC_253374 = <<1>> is already on the <<2>>

                Messages.Add(
                    Localizer.Format("#autoLOC_253369", launchsite_display)
                );
                //Localizer.Format("#autoLOC_253374", "Some vessel", launchsite_display)
            }

            if (!fundsCheck.Test())
            {
                //#autoLOC_250625 = Not Enough Funds!
                //#autoLOC_250630 = You can't afford to launch this vessel.
                //#autoLOC_419420 = Science: <<1>>
                //#autoLOC_419441 = Funds: <<1>>
                //#autoLOC_223622 = Cost
                //#autoLOC_900528 = Cost
                //#autoLOC_6003099 = <b>Cost:</b> <<1>>
                // Funds: 566644 [Cost: 120500]

                float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                double funds = Funding.Instance.Funds;

                Messages.Add(
                    Localizer.Format("#autoLOC_250625"),
                    String.Format("{0} [{1}]\n",
                        Localizer.Format("#autoLOC_419441", funds),
                        Localizer.Format("#autoLOC_6003099", cost)
                    )
                );
            }            

            int count = Messages.Count();

            Messages.ShowAndClear();

            return (count == 0); // success
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
                        var alarms = GetPlanningActiveAlarms();
                        int alarmsMoved = 0;
                        string firstName = "";

                        //foreach (var a in alarms)
                        //    Log(a.Name + " " + a.AlarmTime);


                        //Log("time: " + time);

                        foreach (var a in alarms)
                        {
                            if (a.ID != aID)
                            {
                                a.AlarmTime += time;
                                alarmsMoved++;
                                if (alarmsMoved == 1) firstName = a.Name;
                            }
                        }

                        //foreach (var a in alarms)
                        //    Log(a.Name + " " + a.AlarmTime);

                        if (alarmsMoved == 1)
                        {
                            firstName = firstName.Replace(Localizer.Format("#KVASS_alarm_title_prefix"), "").Trim();
                            Messages.QuickPost(Localizer.Format("#KVASS_alarm_created_another", firstName));
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

        static IEnumerable<KACAlarm> GetPlanningActiveAlarms()
        {
            if (!KACWrapper.APIReady) return new List<KACAlarm>();

            var alarms = KACWrapper.KAC.Alarms.Where(
                    a => Remaining(a) > 0 &&
                    a.Name.StartsWith(Localizer.Format("#KVASS_alarm_title_prefix"), StringComparison.Ordinal)
                    );
                
            return alarms;
        }


        void KAC_onAlarmStateChanged(KACWrapper.KACAPI.AlarmStateChangedEventArgs e)
        {
            if (e.eventType == KACAlarm.AlarmStateEventsEnum.Deleted)
            {

                // e.alarm is still in the list
                var deleting_alarm = e.alarm;
                if (deleting_alarm == null || Remaining(deleting_alarm) <= 0) return;

                //Log("    Deleting: " + deleting_alarm.Name);

                var alarms = GetPlanningActiveAlarms().OrderBy(z => z.AlarmTime).ToList();

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

                //alarms.ForEach(z => Log(z.Name + " " + z.AlarmTime));

                int alarmsMoved = alarms.Count - (del_index+1);

                Messages.QuickPost(Localizer.Format("#KVASS_alarm_deleted", deleting_alarm.Name));

                if (alarmsMoved == 1)
                {
                    string firstName = alarms[del_index + 1].Name;
                    firstName = firstName.Replace(Localizer.Format("#KVASS_alarm_title_prefix"), "").Trim();

                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_deleted_another", firstName));
                }
                else if (alarmsMoved > 1)
                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_deleted_others", alarmsMoved));

            }
        }
    }
}
