using KSP.Localization;
using KSP.UI;
using KSP.UI.Screens;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using static KVASSNS.KACWrapper.KACAPI;
using static KVASSNS.Logging;


namespace KVASSNS
{
    // https://github.com/linuxgurugamer/KCT was used there
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASSSimSettings settingsSim;
        static KVASSPlanSettings settingsPlan;
        static GameParameters.DifficultyParams settingsGame;
        static Regex SimulationRegEx;

        private bool eventsRegistered = false;

        ShipTemplate data;

        public void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                Log(HighLogic.CurrentGame.Mode + " mode is not supported.");
                Destroy(this); return;
            }
            

            settingsSim = HighLogic.CurrentGame.Parameters.CustomParams<KVASSSimSettings>();
            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();
            settingsGame = HighLogic.CurrentGame.Parameters.Difficulty;

            SimulationRegEx = new Regex(LoadRegExpPattern(), RegexOptions.IgnoreCase);

            if (!eventsRegistered)
            {
                eventsRegistered = true;
                GameEvents.onEditorStarted.Add(ResetEditorLaunchButtons);
                GameEvents.onGUILaunchScreenSpawn.Add(OnGUILaunchScreenSpawn);
                GameEvents.onGUILaunchScreenVesselSelected.Add(OnGUILaunchScreenVesselSelected);

            }
        }

        private void OnGUILaunchScreenVesselSelected(ShipTemplate dt)
        {
            this.data = dt;
            Log("OnGUILaunchScreenVesselSelected: dt.filename: " + dt.filename); // Empty
            Log("OnGUILaunchScreenVesselSelected: dt.shipName: " + dt.shipName); // ok
        }

        public void Start()
        {
            Log("Start");
            KACWrapper.InitKACWrapper();

            if (KACWrapper.APIReady && settingsPlan.Queue)
                KACWrapper.KAC.onAlarmStateChanged += KAC_onAlarmStateChanged;

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {

                // already have 
                // GameEvents.onEditorStarted.Add(ResetEditorLaunchButtons)
                // but better safe than sorry
                StartCoroutine(ResetEditorLaunchButtons_WaitedCoroutine());
            }

            // remove alarm
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                string name = FlightGlobals.ActiveVessel.GetDisplayName();

                if (KACWrapper.APIReady && settingsPlan.Enable
                    && !SimulationRegEx.IsMatch(name)
                //&& !(settingsPlan.IgnoreSPH && FlightGlobals.fetch.activeVessel. == EditorFacility.SPH)
                )
                {
                    var alarm = KACUtils.GetAlarm(KACUtils.AlarmTitle(name));

                    if (alarm.Finished())
                    {
                        bool success = KACUtils.RemoveAlarm(alarm.ID);
                        Log("Removing alarm, success:{0}", success);
                    }
                }
            }
        }

        public void OnDisable()
        {
            GameEvents.onEditorStarted.Remove(ResetEditorLaunchButtons);
            GameEvents.onGUILaunchScreenSpawn.Remove(OnGUILaunchScreenSpawn);
            GameEvents.onGUILaunchScreenVesselSelected.Remove(OnGUILaunchScreenVesselSelected);
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
                Log("Coroutine()");
                ResetEditorLaunchButtons();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnGUILaunchScreenSpawn(GameEvents.VesselSpawnInfo e)
        {
            
            // onGUILaunchScreenSpawn is called before VesselSpawnDialog's Start() happens, 
            // but we want to wait after it's done so that we can replace
            StartCoroutine(ResetSpawnDialogLaunchButton_WaitedCoroutine(e));
        }

        /// <summary>
        /// Coroutine to reset the launch button handlers second time in 0.3 sec. after start. 
        /// </summary>
        /// <returns></returns>
        IEnumerator ResetEditorLaunchButtons_WaitedCoroutine()
        {
            yield return new WaitForSeconds(0.3f);

            ResetEditorLaunchButtons();
        }


        IEnumerator ResetSpawnDialogLaunchButton_WaitedCoroutine(GameEvents.VesselSpawnInfo vesselSpawnInfo)
        {
            yield return new WaitForSeconds(0.5f);

            ResetSpawnDialogLaunchButton(vesselSpawnInfo);
        }


        public void ResetSpawnDialogLaunchButton(GameEvents.VesselSpawnInfo vesselSpawnInfo)
        {
            Log("ResetSpawnDialogLaunchButton");

            //UIListSorter sorter = VesselSpawnDialog.Instance.vesselListSorter;

            if (VesselSpawnDialog.Instance != null)
            {
                //FieldInfo craftListInfo = typeof(VesselSpawnDialog).GetField("vesselDataItemList", BindingFlags.NonPublic | BindingFlags.Instance);
                //object obj = craftListInfo.GetValue(VesselSpawnDialog.Instance);
                //List<CraftEntry> craftList = obj as List<CraftEntry>;

                //UnityEngine.UI.Button button = VesselSpawnDialog.Instance.GetType()
                //    .GetPublicValue<UnityEngine.UI.Button>("buttonLaunch", VesselSpawnDialog.Instance);

                //A bit of reflection because buttonLaunch in VesselSpawnDialog is private :/
                FieldInfo buttonFieldInfo = typeof(VesselSpawnDialog).GetField("buttonLaunch", BindingFlags.NonPublic | BindingFlags.Instance);
                if (buttonFieldInfo == null) return;

                Button Greenbutton = buttonFieldInfo.GetValue(VesselSpawnDialog.Instance) as Button;
                if (Greenbutton == null) return;

                Greenbutton.onClick.RemoveAllListeners(); ////originalAction: VesselSpawnDialog.Instance.ButtonLaunch
                Greenbutton.onClick.AddListener(() => { LaunchClickListenerVesselSpawnDialog(vesselSpawnInfo, vesselSpawnInfo.callingFacility.name); }); 

                if (settingsGame.AllowOtherLaunchSites)
                {
                    UILaunchsiteController controller = UnityEngine.Object.FindObjectOfType<UILaunchsiteController>();
                    if (controller == null) return;

                    object items = controller.GetType()?.GetPrivateMemberValue("launchPadItems", controller, 4);
                    if (items == null) return;

                    IEnumerable list = items as IEnumerable;

                    foreach (object site in list)
                    {
                        Button b = site.GetType().GetPublicValue<Button>("buttonLaunch", site);
                        if (b != null)
                        {
                            b.onClick.RemoveAllListeners();
                            string siteName = site.GetType().GetPublicValue<string>("siteName", site);
                            b.onClick.AddListener(() => { LaunchClickListenerVesselSpawnDialog(vesselSpawnInfo, siteName); });
                        }
                    }
                }
            }
        }

        // CANNOT be marked as static
        public void ResetEditorLaunchButtons()
        {
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

        void LaunchClickListenerVesselSpawnDialog(GameEvents.VesselSpawnInfo vsi, string launchSite)
        {
            
            Log("LaunchClickListenerVesselSpawnDialog");

            // data
            //ShipConstruct ship = new ShipConstruct(;
            //string ksp_root = Directory.GetParent(KSPUtil.ApplicationRootPath).FullName;
            //string[] files = Directory.GetFiles(Utils.PathJoin(ksp_root, "saves", vsi.craftSubfolder), "*.craft", SearchOption.AllDirectories);

            //String path = Utils.PathJoin(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", vsi.craftSubfolder, data.shipName+ ".craft" );
            String path = Path.Combine(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships", vsi.craftSubfolder, data.shipName + ".craft");
            

            Log("path" + path);
            

            bool isVAB = (vsi.callingFacility.facilityType == EditorFacility.VAB);

            // TODO: 
            string flag = HighLogic.CurrentGame.flagURL; // ship.missionFlag; // ;

            string VesselName = data.shipName;
            Log("data.shipName: " + data.shipName);

            VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();

            if (manifest != null)
                Log("manifest.CrewCount: " + manifest.CrewCount);
            
            //manifest = HighLogic.CurrentGame.CrewRoster.DefaultCrewForVessel(shipNode, null, false);
            
            Log("launchSite: " + launchSite);

            
            if (settingsSim.Enable
                && !(settingsSim.IgnoreSPH && !isVAB)
                && SimulationRegEx.IsMatch(VesselName))
            {
                Log("Simulation");
                bool success = SimulationPurchase();

                if (success)
                    FlightDriver.StartWithNewLaunch(path, flag, launchSite, manifest);
            }
            else if (settingsPlan.Enable
                && !(settingsPlan.IgnoreSPH && !isVAB)
                && KACWrapper.APIReady)
            {
                Log("Planning");
                string alarmTitle = KACUtils.AlarmTitle(VesselName);
                var alarm = KACUtils.GetAlarm(alarmTitle);

                if (alarm == null)
                {
                    // Alarm Is Not Found, Creating
                    float cost = data.totalCost;
                    float mass = data.totalMass;
                    CreateNewAlarm(alarmTitle, cost, mass);
                }
                else if (alarm.Finished())
                {
                    Log("Planning launching");
                    FlightDriver.StartWithNewLaunch(path, flag, launchSite, manifest);
                }
                else
                {
                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_not_finished", alarmTitle));
                }

            }
            else
            {
                Log("launch");
                EditorLogic.fetch.launchVessel();
            }
        }

        //Replace the default action to LaunchListener.
        void LaunchClickListener(string launchSite)
        {
            Log("LaunchClickListener");
            Log("launchSite param: " + launchSite);

            if (string.IsNullOrEmpty(launchSite))
            {
                launchSite = EditorLogic.fetch.launchSiteName;
            }
            Log("launchSite reseted: " + launchSite);


            if (
                settingsSim.Enable
                && 
                 !(settingsSim.IgnoreSPH && EditorDriver.editorFacility == EditorFacility.SPH)
                
                && SimulationRegEx.IsMatch(EditorLogic.fetch.ship.shipName))
            {
                Log("Simulation");
                bool success = SimulationPurchase();

                if (success)
                    EditorLogic.fetch.launchVessel(launchSite);
            }
            else if (settingsPlan.Enable
                && 
                 !(settingsPlan.IgnoreSPH && EditorDriver.editorFacility == EditorFacility.SPH)
                
                && KACWrapper.APIReady)
            {
                Log("Planning");
                string shipName = EditorLogic.fetch.ship.shipName;
                string alarmTitle = KACUtils.AlarmTitle(shipName);

                KACAlarm alarm = KACUtils.GetAlarm(alarmTitle);

                if (alarm == null)
                {
                    // Alarm Is Not Found, Creating
                    float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                    float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
                    CreateNewAlarm(alarmTitle, cost, mass);
                }
                else if (alarm.Finished())
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
                EditorLogic.fetch.launchVessel();
            }
        }

        string LoadRegExpPattern()
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
        bool SimulationPurchase()
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
        string GetComparingFormat(double value, params double[] addends)
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

        string CreateNewAlarm(string title, float cost, float mass)
        {

            double time = CalcAlarmTime(cost, mass);

            string aID = "";
            if (KACWrapper.APIReady)
            {
                double ut = Utils.UT(); // HighLogic.CurrentGame.UniversalTime; // HighLogic.CurrentGame.flightState.universalTime;
                aID = KACWrapper.KAC.CreateAlarm(
                    KACWrapper.KACAPI.AlarmTypeEnum.Raw,
                    title,
                    ut + time);


                if (!String.IsNullOrEmpty(aID))
                {
                    //if the alarm was made get the object so we can update it
                    var alarm = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

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

        double CalcAlarmTime(float cost, float mass)
        {
            //float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
            //float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;

            Log("cost: " + cost + "mass: " + mass);
            bool career = HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
            double time;

            if (career)
                time = cost * settingsPlan.CareerSeconds;
            else
                time = mass * settingsPlan.ScienceSeconds;

            string log_str = String.Format("PlanTime: {0:F1}", time / KSPUtil.dateTimeFormatter.Day);

            if (settingsPlan.RepSpeedUp && career)
            {
                int currRep = Math.Max((int)Reputation.CurrentRep, 0);
                int lines = currRep / settingsPlan.RepToNextLevel + 1;
                time /= lines;
                log_str += " / " + lines;
            }

            if (settingsPlan.KerbSpeedUp)
            {
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsPlan.KerbToNextLevel + 1;
                time /= teams;
                log_str += " / " + teams;
            }


            if (settingsPlan.SciSpeedUp)
            {
                int currScience = Math.Max((int)ResearchAndDevelopment.Instance.Science, 0);

                int scilevel = currScience / settingsPlan.SciToNextLevel + 1;
                time /= scilevel;
                log_str += " / " + scilevel;
            }

            // The last one. The SpeedUps do not affect. 
            if (settingsPlan.Bureaucracy)
            {
                double bureaucracy_increment = settingsPlan.BureaucracyTime * KSPUtil.dateTimeFormatter.Day;
                time += bureaucracy_increment;
                log_str += " + " + settingsPlan.BureaucracyTime;
            }

            log_str += String.Format(" = {0:F1} days", time / KSPUtil.dateTimeFormatter.Day);
            
            if (settingsPlan.ShowMessageSpeedUps)
                Messages.QuickPost(log_str);

            Log(log_str);

            return time;
        }

        void KAC_onAlarmStateChanged(AlarmStateChangedEventArgs e)
        {
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
