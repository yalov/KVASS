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
    enum NewAlarmQueueType
    {
        Append, 
        Prepend,
        No_Queue
    }

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

        static AlarmUtils alarmutils;

        public void Awake()
        {
            if (!(HighLogic.CurrentGame.Mode == Game.Modes.CAREER
                || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX
                || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX))
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
            alarmutils = new AlarmUtils(settingsPlan.KACEnable);

            if (settingsPlan.KACEnable)
            {
                KACWrapper.InitKACWrapper();
            }

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                CreateTopBarButtons();

                GameEvents.onEditorStarted.Add(OnEditorStarted);
            }

            // remove alarm
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                string vesselName = Utils.GetVesselName();

                if (settingsPlan.Enable && settingsPlan.AutoRemoveFinishedTimers)
                {
                    var alarm = alarmutils.GetAlarm(Utils.AlarmTitle(vesselName));

                    if (alarm.Finished())
                    {
                        bool success = alarmutils.RemoveAlarm(alarm.ID);
                    }
                }
                Destroy(this); return;
            }
        }

        public void OnDisable()
        {
            //Log("OnDisable");
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

            bool planEnabled = settingsPlan.Enable && !(settingsPlan.IgnoreSPH && !isVAB);
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
            {
                try
                {
                    GameObject buttonSteam = EditorLogic.fetch.steamBtn.gameObject;
                    StockButtons.Add(buttonSteam);
                }
                catch (NullReferenceException)
                {
                    Log("Failed to find the Steam Button");
                }
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


        static void OnPlanningButtonClick() => AnyButtonClick(queueButton: NewAlarmQueueType.No_Queue);
        static void OnPrependButtonClick() => AnyButtonClick(queueButton: NewAlarmQueueType.Prepend);
        static void OnAppendButtonClick() => AnyButtonClick(queueButton: NewAlarmQueueType.Append);
  

        static void AnyButtonClick(NewAlarmQueueType queueButton)
        {
            string alarmTitle = Utils.AlarmTitle(Utils.GetVesselName());

            float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _, ShipConstruction.ShipManifest);
            float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
            double time = CalcAlarmTime(cost, mass, out string desc);
            double alarm_ut = Utils.UT() + time;

            



            Alarm alarm = new Alarm(alarmTitle, desc, alarm_ut, time);

            if (queueButton == NewAlarmQueueType.Append)
                alarmutils.AlarmAppendedToQueue(ref alarm);
            else if (queueButton == NewAlarmQueueType.Prepend)
                alarmutils.AlarmPrependedToQueue(alarm);

            alarm.CreateonGUI();

            Messages.ShowAndClear(3, Messages.DurationType.CLEVERCONSTPERLINE);
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
            int index = StockButtons.Count;

            foreach (var b in buttons)
                if (b.Enabled)
                    b.Object.transform.position = buttonLaunch.transform.position - 2*diff_space - ++index * diff;

        }

        void Launch(String launchSite)
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
                double shipCost = EditorLogic.fetch.ship.GetShipCosts(out _, out _, ShipConstruction.ShipManifest);
                double simulCost = 0;

                if (settingsSim.RelCostEnabled)
                    simulCost += 0.01 * settingsSim.RelCost * shipCost;

                if (settingsSim.ConstCostEnabled)
                    simulCost += settingsSim.ConstCost;

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

                    Messages.QuickPostFail(
                        Localizer.Format("#KVASS_message_not_enough_funds_to_sim"),
                        String.Format("{0} [{1}: {2}+{3}]\n",
                            Localizer.Format("#autoLOC_419441", Funding.Instance.Funds),
                            Localizer.Format("#autoLOC_900528"),
                            shipCost.ToString(format),
                            simulCost.ToString(format)
                        )
                    );

                    return false;
                }
            }

            else if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                float science_points = 0;

                if (settingsSim.RelScienceEnabled)
                    science_points += settingsSim.RelScience * EditorLogic.fetch.ship.GetTotalMass() / 100;

                if (settingsSim.ConstScienceEnabled)
                    science_points += settingsSim.ConstScience;

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


                    Messages.QuickPostFail(
                        Localizer.Format("#KVASS_message_not_enough_sci_to_sim"),
                        String.Format("{0} [{1}: {2}]\n",
                            Localizer.Format("#autoLOC_419420", ResearchAndDevelopment.Instance.Science),
                            Localizer.Format("#autoLOC_900528"),
                            science_points.ToString(format)
                        )
                    );

                    return false;
                }
            }
            else // SANDBOX
            {
                return true;
            }
        }

        static double CalcAlarmTime(float cost, float mass, out string desc)
        {
            bool career = HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
            bool career_or_science = (
                HighLogic.CurrentGame.Mode == Game.Modes.CAREER ||
                HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX);

            bool cost_mass = settingsPlan2.SecondsPerFundEnable && settingsPlan2.SecondsPerKgEnable;
            double time = 0;
            desc = "";
            bool SpeedUps = false;

            List<string> LogStrList = new List<string>();
            String LogShort = Localizer.Format("#KVASS_time");


            if (settingsPlan2.SecondsPerFundEnable)
            {
                double time_cost = cost * settingsPlan2.SecondsPerFund;
                time += time_cost;
                LogShort += (cost_mass ? "(" : "") + Utils.Days(time_cost);
                desc += Localizer.Format("#KVASS_alarm_note_cost", cost.ToString("F0"));
            }
            if (settingsPlan2.SecondsPerKgEnable)
            {
                double time_mass = mass * settingsPlan2.SecondsPerKg;
                time += time_mass;
                LogShort += (cost_mass ? " + " : "") + Utils.Days(time_mass) + (cost_mass ? ")" : "");
                desc += Localizer.Format("#KVASS_alarm_note_mass", mass.ToString("F0"));
            }


            if (settingsPlan2.CalendarSpeedUp)
            {
                SpeedUps = true;
                int fullYears = (int)(Planetarium.GetUniversalTime() / KSPUtil.dateTimeFormatter.Year);
                int fulldays = (int)((Planetarium.GetUniversalTime() - fullYears * KSPUtil.dateTimeFormatter.Year) / KSPUtil.dateTimeFormatter.Day);
                string date = "Y" + (fullYears + 1) + "D" + (fulldays + 1);

                int speedups = fullYears / settingsPlan2.CalendarYearsToNextLevel + 1;
                speedups = Math.Min(speedups, settingsPlan2.CalendarYearsSpeedUpsMaxCount);

                string dateNextSpeedUp;
                if (speedups == settingsPlan2.CalendarYearsSpeedUpsMaxCount)
                    dateNextSpeedUp = "No";
                else
                    dateNextSpeedUp = "Y" + (speedups * settingsPlan2.CalendarYearsToNextLevel + 1) + "D1";


                time /= speedups;
                LogShort += " / " + speedups;
                LogStrList.Add(Localizer.Format("#KVASS_time_Calendar", date, dateNextSpeedUp, speedups));
            }

            if (settingsPlan2.RepSpeedUp && career)
            {
                SpeedUps = true;
                int currRep = Math.Max((int)Reputation.CurrentRep, 0);
                int lines = currRep / settingsPlan2.RepToNextLevel + 1;
                time /= lines;
                LogShort += " / " + lines;
                LogStrList.Add(Localizer.Format("#KVASS_time_Reputation", lines, currRep, settingsPlan2.RepToNextLevel));
            }

            if (settingsPlan2.KerbSpeedUp)
            {
                SpeedUps = true;
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsPlan2.KerbToNextLevel + 1;
                time /= teams;
                LogShort += " / " + teams;
                LogStrList.Add(Localizer.Format("#KVASS_time_Kerbals", teams, availableKerbs, settingsPlan2.KerbToNextLevel));
            }

            if (settingsPlan2.SciSpeedUp && career_or_science)
            {
                SpeedUps = true;
                int currScience = Math.Max((int)ResearchAndDevelopment.Instance.Science, 0);

                int scilevel = currScience / settingsPlan2.SciToNextLevel + 1;
                time /= scilevel;
                LogShort += " / " + scilevel;
                LogStrList.Add(Localizer.Format("#KVASS_time_Science", scilevel, currScience, settingsPlan2.SciToNextLevel));
            }

            // The last one. The SpeedUps do not affect. 
            if (settingsPlan2.ConstTime)
            {
                SpeedUps = true;
                double constTime_increment = settingsPlan2.ConstTimeDays * KSPUtil.dateTimeFormatter.Day;
                time += constTime_increment;
                LogShort += " + " + settingsPlan2.ConstTimeDays;
                LogStrList.Add(Localizer.Format("#KVASS_time_ConstTime", settingsPlan2.ConstTimeDays));
            }


            

            LogShort += Localizer.Format("#KVASS_time_end", Utils.Days(time));
            desc += LogShort;

            LogStrList.Insert(0, LogShort);

            if (SpeedUps)
                LogStrList.Insert(1, Localizer.Format("#KVASS_time_where"));


            if (settingsPlan.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Shorter"))
            {
                Messages.Add(Localizer.Format("#KVASS_time_short", Utils.Days(time)), 2);
                Log(LogStrList);
            }
            else if (settingsPlan.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Short"))
            {
                Messages.Add(LogShort, 2);
                Log(LogStrList);
            }
            else if (settingsPlan.ShowMessageSpeedUps == Localizer.Format("#KVASS_plan_message_Expanded"))
            {
                Messages.Add(string.Join("\n", LogStrList), 2);
            }

            return time;
        }
    }
}
