using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static KVASSNS.Logging;

using KSP.UI.TooltipTypes;

namespace KVASSNS
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASSSimSettings settingsSim;
        static KVASSPlanSettings settingsPlan;
        static KVASSPlanSettings2 settingsPlan2;
        //static GameParameters.DifficultyParams settingsGame;

        class ButtonData
        {
            public GameObject Object { get; set; }
            public string TexturePath { get; set; }
            public Button Button { get; set; }
            public string Text { get; set; }
            public UnityEngine.Events.UnityAction Action { get; set; }

            private bool _enabled;
            public bool Enabled
            {
                get { return _enabled; }
                set
                {
                    _enabled = value;
                    Button?.gameObject?.SetActive(Enabled);
                }
            }

            public ButtonData(string texturePath, string text, UnityEngine.Events.UnityAction action, bool enabled = true)
            {
                TexturePath = texturePath;

                Text = text;
                Action = action;
                Enabled = enabled;
            }

            public void CreateTopBarButton(GameObject originalbutton, GameObject parent)
            {
                Object = UnityEngine.Object.Instantiate(originalbutton);
                Object.transform.SetParent(parent.transform);

                UnityEngine.Object.DestroyImmediate(Object.GetComponent<Button>());
                Button = Object.AddOrGetComponent<Button>();
                Button.image = Object.GetComponent<Image>();

                Object.GetComponent<TooltipController_Text>().textString = Text;

                Texture2D texture = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(t => t.name == TexturePath);
                Button.transition = Selectable.Transition.SpriteSwap;
                Button.spriteState = new SpriteState
                {
                    highlightedSprite = Sprite.Create(texture, new Rect(128, 128, 128, 128), Vector2.zero),
                    pressedSprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), Vector2.zero),
                    disabledSprite = Sprite.Create(texture, new Rect(128, 0, 128, 128), Vector2.zero)
                };
                Button.image.sprite = Sprite.Create(texture, new Rect(0, 128, 128, 128), Vector2.zero);

                Button.onClick.AddListener(Action);
                Button.gameObject.SetActive(Enabled);
            }
        }

        GameObject buttonLaunch;
        List<GameObject> StockButtons;
        List<ButtonData> buttons;

        Vector3 diff;
        Vector3 diff_space;

        public void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                Log(HighLogic.CurrentGame.Mode + " mode is not supported.");
                Destroy(gameObject); return;
            }


            settingsSim = HighLogic.CurrentGame.Parameters.CustomParams<KVASSSimSettings>();
            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings>();
            settingsPlan2 = HighLogic.CurrentGame.Parameters.CustomParams<KVASSPlanSettings2>();
            //  settingsGame = HighLogic.CurrentGame.Parameters.Difficulty;

        }

        public void Start()
        {
            KACWrapper.InitKACWrapper();

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                CreateTopBarButtons();

                GameEvents.onEditorStarted.Add(OnEditorStarted);
            }

            // remove alarm
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                string vesselName = Utils.GetVesselName();

                if (KACWrapper.APIReady && settingsPlan.Enable && settingsPlan.AutoRemoveFinishedTimers)
                {
                    var alarm = KACUtils.GetAlarm(KACUtils.AlarmTitle(vesselName));

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
            Log("OnDisable");
            G﻿ameEvents.onEditorStarted.Remove(OnEditorStarted);
        }



        public void CreateTopBarButtons()
        {
            // TopBar Buttons. Based on
            // https://github.com/Sigma88/Sigma-EditorView

            buttons = new List<ButtonData>();
            
            bool isVAB = EditorDriver.editorFacility == EditorFacility.VAB;

            bool simEnabled = settingsSim.Enable && !(settingsSim.IgnoreSPH && !isVAB);
            buttons.Add(new ButtonData("KVASS/Textures/Simulation", "Simulation", OnSimulationButtonClick, simEnabled));


            bool planEnabled = settingsPlan.Enable && !(settingsPlan.IgnoreSPH && !isVAB) && KACWrapper.APIReady;
            if (settingsPlan.Queue)
            {
                if (settingsPlan.QueueAppend)
                    buttons.Add(new ButtonData("KVASS/Textures/PlanningAppend", "Planning (Append)", OnAppendButtonClick, planEnabled));

                if (settingsPlan.QueuePrepend)
                    buttons.Add(new ButtonData("KVASS/Textures/PlanningPrepend", "Planning (Prepend)", OnPrependButtonClick, planEnabled));
            }
            else
                buttons.Add(new ButtonData("KVASS/Textures/Planning", "Planning", OnPlanningButtonClick, planEnabled));



            GameObject topBar = GameObject.Find("Top Bar");

            buttonLaunch = EditorLogic.fetch.launchBtn.gameObject;
            GameObject buttonNew = EditorLogic.fetch.newBtn.gameObject;
            GameObject buttonLoad = EditorLogic.fetch.loadBtn.gameObject;
            GameObject buttonSave = EditorLogic.fetch.saveBtn.gameObject;
            

            StockButtons = new List<GameObject> { buttonSave, buttonLoad, buttonNew };

            bool steamPresent = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "KSPSteamCtrlr");
            if (steamPresent)
                try
                {
                    GameObject buttonSteam = EditorLogic.fetch.steamBtn.gameObject;
                    StockButtons.Add(buttonSteam);
                }
                catch (NullReferenceException e)
                {
                    Log("Failed to find the Steam Button");
                }


            foreach (var b in buttons)
                b.CreateTopBarButton(buttonLaunch, topBar);

            diff = buttonSave.transform.position - buttonLoad.transform.position;
            diff_space = buttonLaunch.transform.position - buttonSave.transform.position - diff;

            MoveAllButtons();
        }

        void OnSimulationButtonClick()
        {
            bool success = SimulationPurchase();

            if (success)
                Launch(EditorLogic.fetch.launchSiteName);
        }


        static void OnPlanningButtonClick() => AnyButtonClick(queueAppend: null);
        static void OnPrependButtonClick() => AnyButtonClick(queueAppend: false);
        static void OnAppendButtonClick() => AnyButtonClick(queueAppend: true);
  

        static void AnyButtonClick(bool? queueAppend)
        {
            string VesselName = Utils.GetVesselName();

            if (KACWrapper.APIReady)
            {
                string alarmTitle = KACUtils.AlarmTitle(VesselName);

                float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
                float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
                CreateNewAlarm(alarmTitle, cost, mass, queueAppend);
            }
        }


        /// <summary>
        /// GameEvent for Toggling Editor
        /// </summary>
        private void OnEditorStarted()
        {
            // aka onEditorSwitch

            bool isVAB = EditorDriver.editorFacility == EditorFacility.VAB;
            bool isSimToggling = settingsSim.Enable && settingsSim.IgnoreSPH;
            bool isPlanToggling = settingsPlan.Enable && settingsPlan.IgnoreSPH && KACWrapper.APIReady;


            if (isSimToggling || isPlanToggling)
            {
                if (isSimToggling)
                {
                    if (isVAB)
                    {
                        buttons[0].Enabled = true;
                    }
                    else
                    {
                        buttons[0].Enabled = false;
                    }
                }

                if (isPlanToggling)
                {
                    if (isVAB)
                    {
                        for (int i = 1; i < buttons.Count; i++)
                            buttons[i].Enabled = true;
                    }
                    else
                    {
                        for (int i = 1; i < buttons.Count; i++)
                            buttons[i].Enabled = false;
                    }
                }

                MoveAllButtons();
            }
        }

        void MoveAllButtons()
        {
            int index = 0;
            foreach (var b in buttons)
            {
                if (b.Enabled)
                {
                    b.Object.transform.position = buttonLaunch.transform.position - ++index * diff;
                }
            }

            
            //bool mechjebPresent = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "MechJeb2");

            //Vector3 mj_space = new Vector3(146, 0, 0);

            Log(diff_space);
            Log(diff);
            foreach (var sb in StockButtons)
            {
                sb.transform.position = buttonLaunch.transform.position - diff_space - ++index * diff;
                //if (mechjebPresent) sb.transform.position -= mj_space;
            }

        }

        void Launch(String launchSite, string craftSubfolder = "")
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
                EditorLogic.fetch.launchVessel(launchSite);
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

                    string format = Utils.GetComparingFormat(Funding.Instance.Funds, shipCost, simulCost);

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

                    string format = Utils.GetComparingFormat(ResearchAndDevelopment.Instance.Science, science_points);

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

        static string CreateNewAlarm(string title, float cost, float mass, bool? queueAppend = null)
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

                    alarm.Notes = Localizer.Format("#KVASS_alarm_note",
                        cost.ToString("F0"),
                        (time / KSPUtil.dateTimeFormatter.Day).ToString("F1"));
                       
                    alarm.AlarmMargin = 0;

                    if (settingsPlan.KillTimeWarp)
                    {
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
                    }
                    else
                    {
                        alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.DoNothing;
                    }

                    KACListener.AlarmCreatedQueueChange(alarm, queueAppend);

                }
            }
            return aID;
        }

        static double CalcAlarmTime(float cost, float mass)
        {
            //Log("cost: " + cost + "mass: " + mass);
            bool career = HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
            double time;

            if (career)
                time = cost * settingsPlan2.CareerSeconds;
            else
                time = mass * settingsPlan2.ScienceSeconds;

            List<string> LogStrList = new List<string>();
            LogStrList.Add(Localizer.Format("#KVASS_time_short1", (time / KSPUtil.dateTimeFormatter.Day).ToString("F1")));
            
            if (settingsPlan2.RepSpeedUp && career)
            {
                int currRep = Math.Max((int)Reputation.CurrentRep, 0);
                int lines = currRep / settingsPlan2.RepToNextLevel + 1;
                time /= lines;
                LogStrList[0] += " / " + lines;
                LogStrList.Add(Localizer.Format("#KVASS_time_Reputation", lines, currRep, settingsPlan2.RepToNextLevel));
            }

            if (settingsPlan2.KerbSpeedUp)
            {
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsPlan2.KerbToNextLevel + 1;
                time /= teams;
                LogStrList[0] += " / " + teams;
                LogStrList.Add(Localizer.Format("#KVASS_time_Kerbals", teams, availableKerbs, settingsPlan2.KerbToNextLevel));
            }

            if (settingsPlan2.SciSpeedUp)
            {
                int currScience = Math.Max((int)ResearchAndDevelopment.Instance.Science, 0);

                int scilevel = currScience / settingsPlan2.SciToNextLevel + 1;
                time /= scilevel;
                LogStrList[0] += " / " + scilevel;
                LogStrList.Add(Localizer.Format("#KVASS_time_Science", scilevel, currScience, settingsPlan2.SciToNextLevel));
            }

            // The last one. The SpeedUps do not affect. 
            if (settingsPlan2.Bureaucracy)
            {
                double bureaucracy_increment = settingsPlan2.BureaucracyTime * KSPUtil.dateTimeFormatter.Day;
                time += bureaucracy_increment;
                LogStrList[0] += " + " + settingsPlan2.BureaucracyTime;
                LogStrList.Add(Localizer.Format("#KVASS_time_Bureaucracy", settingsPlan2.BureaucracyTime));
            }

            LogStrList[0] += Localizer.Format("#KVASS_time_short2", (time / KSPUtil.dateTimeFormatter.Day).ToString("F1"));

            LogStrList.Add(LogStrList[0]);
            LogStrList.RemoveAt(0);

            if (settingsPlan.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Shorter"))
            {
                Messages.Add(Localizer.Format("#KVASS_time_short", (time / KSPUtil.dateTimeFormatter.Day).ToString("F1")), 1);
            }
            else if (settingsPlan.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Short"))
            {
                Messages.Add(LogStrList[0], 1);
            }
            else if (settingsPlan.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Expanded"))
            {
                Messages.Add(string.Join("\n", LogStrList), 1);
            }

            Log(LogStrList);

            return time;
        }
    }
}
