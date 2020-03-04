using KSP.Localization;
using KSP.UI;
using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using static KVASSNS.Logging;

using KSP.UI.TooltipTypes;

namespace KVASSNS
{
    // code from https://github.com/linuxgurugamer/KCT was used there
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASSSimSettings settingsSim;
        static KVASSPlanSettings settingsPlan;
        static KVASSPlanSettings2 settingsPlan2;
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
            settingsPlan2 = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings2>();
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
            //Log("OnGUILaunchScreenVesselSelected: dt.filename: " + dt.filename); // Empty
            //Log("OnGUILaunchScreenVesselSelected: dt.shipName: " + dt.shipName); // ok
        }


        

        static Button simButton;
        static Button prependButton;
        static Button appendButton;

        public void CreateButton()
        {
            // Button.
            // https://github.com/Sigma88/Sigma-EditorView

            // EditorDriver.editorFacility

            
            //GameObject building;

            //if (EditorDriver.editorFacility == EditorFacility.SPH)
            //    building = GameObject.Find("SPHlvl1") ?? GameObject.Find("SPHlvl2") ?? GameObject.Find("SPHmodern");
            //else
            //    building = GameObject.Find("VABlvl2") ?? GameObject.Find("VABlvl3") ?? GameObject.Find("VABmodern");

            //Switch lightSwitch = building.AddOrGetComponent<Switch>();
            

            GameObject topBar = GameObject.Find("Top Bar");
            GameObject buttonCrew = topBar.GetChild("ButtonPanelCrew");
            GameObject buttonEditor = topBar.GetChild("ButtonPanelEditor");

            //Button oldButton = buttonCrew.GetComponent<Button>();

            Button greenButton = EditorLogic.fetch.launchBtn;
            Button newButton = EditorLogic.fetch.newBtn;
            Button loadButton = EditorLogic.fetch.loadBtn;
            Button saveButton = EditorLogic.fetch.saveBtn;


            GameObject buttonLaunch = greenButton.gameObject;
            GameObject buttonNew = newButton.gameObject;
            GameObject buttonLoad = loadButton.gameObject;
            GameObject buttonSave = saveButton.gameObject;

            Vector3 diff = buttonEditor.transform.position - buttonCrew.transform.position;

            buttonNew.transform.position -= diff*3;
            buttonLoad.transform.position -= diff*3;
            buttonSave.transform.position -= diff*3;


            GameObject buttonSimulation = UnityEngine.Object.Instantiate(buttonLaunch);
            buttonSimulation.transform.SetParent(topBar.transform);
            buttonSimulation.transform.position = buttonLaunch.transform.position - diff;  // buttonEditor.transform.position * 2 - buttonCrew.transform.position;

            GameObject buttonPrependPlanning = UnityEngine.Object.Instantiate(buttonLaunch);
            buttonPrependPlanning.transform.SetParent(topBar.transform);
            buttonPrependPlanning.transform.position = buttonLaunch.transform.position - 3 * diff;

            GameObject buttonAppendPlanning = UnityEngine.Object.Instantiate(buttonLaunch);
            buttonAppendPlanning.transform.SetParent(topBar.transform);
            buttonAppendPlanning.transform.position = buttonLaunch.transform.position - 2 * diff;

            Texture2D textureSimulation = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(t => t.name == "KVASS/Textures/Simulation");
            Texture2D texturePrepend = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(t => t.name == "KVASS/Textures/PlanningPrepend");
            Texture2D textureAppend = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(t => t.name == "KVASS/Textures/PlanningAppend");

            UnityEngine.Object.DestroyImmediate(buttonSimulation.GetComponent<Button>());
            simButton = buttonSimulation.AddOrGetComponent<Button>();
            simButton.image = buttonSimulation.GetComponent<Image>();

            UnityEngine.Object.DestroyImmediate(buttonPrependPlanning.GetComponent<Button>());
            prependButton = buttonPrependPlanning.AddOrGetComponent<Button>();
            prependButton.image = buttonPrependPlanning.GetComponent<Image>();

            UnityEngine.Object.DestroyImmediate(buttonAppendPlanning.GetComponent<Button>());
            appendButton = buttonAppendPlanning.AddOrGetComponent<Button>();
            appendButton.image = buttonAppendPlanning.GetComponent<Image>();

            buttonSimulation.GetComponent<TooltipController_Text>().textString = "Simulation";
            buttonPrependPlanning.GetComponent<TooltipController_Text>().textString = "Planning (Prepend)";
            buttonAppendPlanning.GetComponent<TooltipController_Text>().textString = "Planning (Append)";

            simButton.transition = Selectable.Transition.SpriteSwap;
            simButton.spriteState = new SpriteState
            {
                highlightedSprite = Sprite.Create(textureSimulation, new Rect(128, 128, 128, 128), Vector2.zero),
                pressedSprite = Sprite.Create(textureSimulation, new Rect(0, 0, 128, 128), Vector2.zero),
                disabledSprite = Sprite.Create(textureSimulation, new Rect(128, 0, 128, 128), Vector2.zero)
            };
            simButton.image.sprite = Sprite.Create(textureSimulation, new Rect(0, 128, 128, 128), Vector2.zero);

            prependButton.transition = Selectable.Transition.SpriteSwap;
            prependButton.spriteState = new SpriteState
            {
                highlightedSprite = Sprite.Create(texturePrepend, new Rect(128, 128, 128, 128), Vector2.zero),
                pressedSprite = Sprite.Create(texturePrepend, new Rect(0, 0, 128, 128), Vector2.zero),
                disabledSprite = Sprite.Create(texturePrepend, new Rect(128, 0, 128, 128), Vector2.zero)
            };
            prependButton.image.sprite = Sprite.Create(texturePrepend, new Rect(0, 128, 128, 128), Vector2.zero);

            appendButton.transition = Selectable.Transition.SpriteSwap;
            appendButton.spriteState = new SpriteState
            {
                highlightedSprite = Sprite.Create(textureAppend, new Rect(128, 128, 128, 128), Vector2.zero),
                pressedSprite = Sprite.Create(textureAppend, new Rect(0, 0, 128, 128), Vector2.zero),
                disabledSprite = Sprite.Create(textureAppend, new Rect(128, 0, 128, 128), Vector2.zero)
            };
            appendButton.image.sprite = Sprite.Create(textureAppend, new Rect(0, 128, 128, 128), Vector2.zero);

            simButton.onClick.AddListener(OnSimulationButtonClick);
            prependButton.onClick.AddListener(OnPrependButtonClick);
            appendButton.onClick.AddListener(OnAppendButtonClick);

            simButton.gameObject.SetActive(true);
            prependButton.gameObject.SetActive(true);
            appendButton.gameObject.SetActive(true);
        }

        static void OnSimulationButtonClick()
        {

        }


        static void OnPrependButtonClick()
        {
            // Cteate planning without checking.

            bool isVAB = EditorDriver.editorFacility == EditorFacility.VAB; ;
            string VesselName = EditorLogic.fetch.ship.shipName; ;

            if (KACWrapper.APIReady)
            {
                string alarmTitle = KACUtils.AlarmTitle(VesselName);

                float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
                CreateNewAlarm(alarmTitle, cost, mass);
            }
        }


        static void OnAppendButtonClick()
        {
            // Cteate planning without checking.

            bool isVAB = EditorDriver.editorFacility == EditorFacility.VAB; ;
            string VesselName = EditorLogic.fetch.ship.shipName; ;

            if (KACWrapper.APIReady)
            {
                string alarmTitle = KACUtils.AlarmTitle(VesselName);

                float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
                CreateNewAlarm(alarmTitle, cost, mass);
            }
        }
        public void Start()
        {
            //Log("Start");
            KACWrapper.InitKACWrapper();

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                // already have GameEvents.onEditorStarted.Add(ResetEditorLaunchButtons)
                // but better safe than sorry
                StartCoroutine(ResetBothLaunchButtons_WaitedCoroutine(null));
                      
                CreateButton();
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
                Destroy(this); return;
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
                //Log("Coroutine()");
                ResetBothLaunchButton(null);
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnGUILaunchScreenSpawn(GameEvents.VesselSpawnInfo e)
        {
            
            // onGUILaunchScreenSpawn is called before VesselSpawnDialog's Start() happens, 
            // but we want to wait after it's done so that we can replace
            StartCoroutine(ResetBothLaunchButtons_WaitedCoroutine(e));
        }

        /// <summary>
        /// Coroutine to reset the launch button handlers second time in 0.3 sec. after start. 
        /// </summary>
        /// <returns></returns>

        IEnumerator ResetBothLaunchButtons_WaitedCoroutine(GameEvents.VesselSpawnInfo? vesselSpawnInfo)
        {
            yield return new WaitForSeconds(0.5f);

            ResetBothLaunchButton(vesselSpawnInfo);
        }

        public void ResetBothLaunchButton(GameEvents.VesselSpawnInfo? vesselSpawnInfo)
        {
            UnityEngine.UI.Button greenButton;

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                greenButton = EditorLogic.fetch.launchBtn;
            else
            {
                if (VesselSpawnDialog.Instance == null) return;
                FieldInfo buttonFieldInfo = typeof(VesselSpawnDialog).GetField("buttonLaunch", BindingFlags.NonPublic | BindingFlags.Instance);
                if (buttonFieldInfo == null) return;
                greenButton = buttonFieldInfo.GetValue(VesselSpawnDialog.Instance) as Button;
            }

            if (greenButton == null) return;
            greenButton.onClick.RemoveAllListeners();
            greenButton.onClick.AddListener(() => { LaunchClickListenerBoth(vesselSpawnInfo, vesselSpawnInfo?.callingFacility?.name); });


            if (settingsGame.AllowOtherLaunchSites)
            {
                UILaunchsiteController controller = UnityEngine.Object.FindObjectOfType<UILaunchsiteController>();
                if (controller == null) return;

                object items = controller.GetType()?.GetPrivateMemberValue("launchPadItems", controller, 4);
                if (items == null) return;

                // the private member "launchPadItems" is a list, and if it is null, then it is
                // not castable to a IEnumerable
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
                        button.onClick.AddListener(() => { LaunchClickListenerBoth(vesselSpawnInfo, siteName); });
                    }
                }
            }
        }

        // CANNOT be marked as static
        public void ResetEditorLaunchButtons()
        {
            ResetBothLaunchButton(null);
        }

        void Launch(String launchSite, string craftSubfolder)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                EditorLogic.fetch.launchVessel(launchSite);
            else 
            {
                String path = Path.Combine(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "Ships",
                craftSubfolder, Localizer.Format(data.shipName) + ".craft");
                //Log("path: " + path);
                if (!File.Exists(path)) 
                    path = Path.Combine(KSPUtil.ApplicationRootPath, "Ships",
                craftSubfolder, Localizer.Format(data.shipName) + ".craft");

                VesselCrewManifest manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();

                string flag = HighLogic.CurrentGame.flagURL; // ship.missionFlag; // ;
                FlightDriver.StartWithNewLaunch(path, flag, launchSite, manifest);

            }
        }

        void LaunchClickListenerBoth(GameEvents.VesselSpawnInfo? vsi, string launchSite)
        {

            bool isVAB;
            string VesselName;
            string craftSubfolder = "";

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                VesselName = EditorLogic.fetch.ship.shipName;
                isVAB = EditorDriver.editorFacility == EditorFacility.VAB;
                if (string.IsNullOrEmpty(launchSite))
                    launchSite = EditorLogic.fetch.launchSiteName;

            }
            else
            {
                if (vsi.HasValue)
                    craftSubfolder = vsi.Value.craftSubfolder;

                VesselName = data.shipName;
                isVAB = (vsi.Value.callingFacility.facilityType == EditorFacility.VAB);
            }


            if (settingsSim.Enable
                && !(settingsSim.IgnoreSPH && !isVAB)
                && SimulationRegEx.IsMatch(VesselName))
            {
                //Log("Simulation");
                bool success = SimulationPurchase();

                if (success)
                    Launch(launchSite, craftSubfolder);
                    
            }
            else if (settingsPlan.Enable
                && !(settingsPlan.IgnoreSPH && !isVAB)
                && KACWrapper.APIReady)
            {
                //Log("Planning");
                string alarmTitle = KACUtils.AlarmTitle(VesselName);
                var alarm = KACUtils.GetAlarm(alarmTitle);

                if (alarm == null)
                {
                    // Alarm Is Not Found, Creating.
                    float cost, mass;
                    if (HighLogic.LoadedScene == GameScenes.EDITOR)
                    {
                        cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                        mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
                    }
                    else
                    {
                        cost = data.totalCost;
                        mass = data.totalMass;
                    }

                    CreateNewAlarm(alarmTitle, cost, mass);
                }
                else if (alarm.Finished())
                {
                    //Log("Planning: launching");
                    Launch(launchSite, craftSubfolder);
                }
                else
                {
                    Messages.QuickPost(Localizer.Format("#KVASS_alarm_not_finished", alarmTitle));
                }

            }
            else
            {
                Launch(launchSite, craftSubfolder);
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

                    Messages.AddFail(
                        Localizer.Format("#KVASS_message_not_enough_funds_to_sim"),
                        String.Format("{0} [{1}: {2}+{3}]\n",
                            Localizer.Format("#autoLOC_419441", Funding.Instance.Funds),
                            Localizer.Format("#autoLOC_900528"), 
                            shipCost.ToString(format), 
                            simulCost.ToString(format)
                        )
                    );

                    Messages.ShowFailsAndClear();

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

                    Messages.AddFail(
                        Localizer.Format("#KVASS_message_not_enough_sci_to_sim"),
                        String.Format("{0} [{1}: {2}]\n",
                            Localizer.Format("#autoLOC_419420", ResearchAndDevelopment.Instance.Science),
                            Localizer.Format("#autoLOC_900528"),
                            science_points.ToString(format)
                        )
                    );

                    Messages.ShowFailsAndClear();

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

        static string CreateNewAlarm(string title, float cost, float mass)
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

                    if (settingsPlan2.KillTimeWarp)
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
                    else
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.DoNothing;

                }
            }
            return aID;
        }

        static double CalcAlarmTime(float cost, float mass)
        {
            //float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
            //float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;

            //Log("cost: " + cost + "mass: " + mass);
            bool career = HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
            double time;

            if (career)
                time = cost * settingsPlan.CareerSeconds;
            else
                time = mass * settingsPlan.ScienceSeconds;

            List<string> LogStrList = new List<string>();
            LogStrList.Add(String.Format("Time: {0:F1}", time / KSPUtil.dateTimeFormatter.Day));
            
            if (settingsPlan.RepSpeedUp && career)
            {
                int currRep = Math.Max((int)Reputation.CurrentRep, 0);
                int lines = currRep / settingsPlan.RepToNextLevel + 1;
                time /= lines;
                LogStrList[0] += " / " + lines;
                LogStrList.Add(String.Format("Reputation: {1} / {2} + 1 = {0}", lines, currRep, settingsPlan.RepToNextLevel));
            }

            if (settingsPlan.KerbSpeedUp)
            {
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsPlan.KerbToNextLevel + 1;
                time /= teams;
                LogStrList[0] += " / " + teams;
                LogStrList.Add(String.Format("Kerbals: {1} / {2} + 1 = {0}", teams, availableKerbs, settingsPlan.KerbToNextLevel));
            }


            if (settingsPlan.SciSpeedUp)
            {
                int currScience = Math.Max((int)ResearchAndDevelopment.Instance.Science, 0);

                int scilevel = currScience / settingsPlan.SciToNextLevel + 1;
                time /= scilevel;
                LogStrList[0] += " / " + scilevel;
                LogStrList.Add(String.Format("Science: {1} / {2} + 1 = {0}", scilevel, currScience, settingsPlan.SciToNextLevel));
            }

            // The last one. The SpeedUps do not affect. 
            if (settingsPlan.Bureaucracy)
            {
                double bureaucracy_increment = settingsPlan.BureaucracyTime * KSPUtil.dateTimeFormatter.Day;
                time += bureaucracy_increment;
                LogStrList[0] += " + " + settingsPlan.BureaucracyTime;
                LogStrList.Add(String.Format("Bureaucracy: {0}", settingsPlan.BureaucracyTime));
            }

            LogStrList[0] += String.Format(" = {0:F1} days", time / KSPUtil.dateTimeFormatter.Day);

            if (settingsPlan2.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Short"))
                Messages.Add(LogStrList[0], 1);
            else if (settingsPlan2.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Expanded"))
            {
                LogStrList.Add(LogStrList[0]);
                LogStrList.RemoveAt(0);
                Messages.Add(string.Join("\n", LogStrList), 1);
            }

            Log(LogStrList);

            return time;
        }
    }
}
