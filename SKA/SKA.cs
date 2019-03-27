using System;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEngine;
using KSP.Localization;

using SKA_KACWrapper;
using static SKA.Logging;

namespace SKA
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class SKA : MonoBehaviour
    {
        static SKASettings settingsSKA;
        static STASettings settingsSTA;
        static Regex regex;

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
            settingsSKA = HighLogic.CurrentGame.Parameters.CustomParams<SKASettings>();
            settingsSTA = HighLogic.CurrentGame.Parameters.CustomParams<STASettings>();

            regex = new Regex(LoadRegExpPattern());

            KACWrapper.InitKACWrapper();

            if (KACWrapper.APIReady)
            {
                // Log("KACWrapper: API Ready");
            }
            else
            {
                Log("KACWrapper: API is not ready");
            }

            try
            {
                UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent();
                c.AddListener(OnLoadClick);
                EditorLogic.fetch.launchBtn.onClick = c;
            }
            catch
            {
                Log("Cannot reset launch button");
            }

        }

        /*
        public void OnDisable()
        {
        }
        */

        //Replace the default action
        public UnityEngine.Events.UnityAction OnLoadClick = new UnityEngine.Events.UnityAction(() => {

            if (settingsSKA.Enable && regex.IsMatch(EditorLogic.fetch.ship.shipName))
            {
                Log("Simulation Launch");
                SimulationPurchase();
                EditorLogic.fetch.launchVessel();
            }
            else if (settingsSTA.Enable && KACWrapper.APIReady)
            {
                Log("Building");

                //string ID;
                string AlarmTitle = Localizer.Format("#STA_AlarmTitle", EditorLogic.fetch.ship.shipName);

                if (IsAlarmFound(AlarmTitle, out string ID))
                {
                    if (IsAlarmFinished(ID))
                    {
                        RemoveAlarm(ID);
                        Log("Alarm Is Finished, Launching");
                        EditorLogic.fetch.launchVessel();
                    }
                    else
                    {
                        Log("Alarm Is Not Finished, Exit to KSC");
                        HighLogic.LoadScene(GameScenes.SPACECENTER);
                    }
                }
                else
                {
                    Log("Alarm Is Not Found, Creating");
                    CreateNewAlarm(AlarmTitle);
                    //HighLogic.LoadScene(GameScenes.SPACECENTER);
                }

            }
            else
            {
                Log("Safe Launch");
                EditorLogic.fetch.launchVessel();
            }
            
        });


        private string LoadRegExpPattern()
        {
            string[] RegExs = { "^.?[Tt]est" };

            ConfigNode[] configs = GameDatabase.Instance.GetConfigNodes("SKA");

            if (configs != null)
            {
                foreach (var conf in configs)
                    RegExs = RegExs.Concat(conf.GetValues("Regex")).ToArray();
                
                //ConfigNode config = configs[0];
                //RegExs = config.GetValues("Regex");
            }

            for (int i = 0; i < RegExs.Length; i++)
                RegExs[i] = "(" + Localizer.Format(RegExs[i]).Trim('"') + ")";

            return String.Join("|", RegExs);
        }

        private static void SimulationPurchase()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                double cost = 0;
                if (settingsSKA.CalcMode == Localizer.Format("#SKA_VESSEL"))
                    cost = 0.01 * settingsSKA.Career_Vessel * EditorLogic.fetch.ship.GetShipCosts(out _, out _);

                else if (settingsSKA.CalcMode == Localizer.Format("#SKA_TOTAL")) 
                    cost = 0.01 * settingsSKA.Career_Total * Funding.Instance.Funds;

                else // if (settingsSKA.CalcMode == Localizer.Format("#SKA_CONST"))
                    cost = settingsSKA.Career_Const;
                
                

                Funding.Instance.AddFunds(-cost, TransactionReasons.VesselRollout);
            }

            else if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                float science_points = 0.0f;

                if (settingsSKA.CalcMode == Localizer.Format("#SKA_VESSEL"))
                    science_points = settingsSKA.Science_Vessel * EditorLogic.fetch.ship.GetTotalMass() / 100;

                else if (settingsSKA.CalcMode == Localizer.Format("#SKA_TOTAL"))
                    science_points = 0.01f * settingsSKA.Science_Total * ResearchAndDevelopment.Instance.Science;

                else //if (settingsSKA.CalcMode == Localizer.Format("#SKA_CONST"))
                    science_points = settingsSKA.Science_Const;

                ResearchAndDevelopment.Instance.AddScience(-science_points, TransactionReasons.VesselRollout);
            }
        }


        private static string CreateNewAlarm(string title)
        {
            double time = CalcAlarmTime();

            string aID = "";
            if (KACWrapper.APIReady)
            {
                //Create a raw alarm 15 mins from now game time and get the id back
                aID = KACWrapper.KAC.CreateAlarm(
                    KACWrapper.KACAPI.AlarmTypeEnum.Raw,
                    title,
                    Planetarium.GetUniversalTime() + time);

                Log("New Alarm: {0}, {1:F1} days", title, time / 3600 / 6);

                if (aID != "")
                {
                    //if the alarm was made get the object so we can update it
                    KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

                    //Now update some of the other properties
                    //a.Notes = Localizer.Format("#STA_Message_Alarm");
                    a.Notes = HighLogic.CurrentGame.flightState.universalTime.ToString("F0") +" "+ time.ToString("F0");
                    a.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.DoNothing;
                    
                }
            }
            return aID;
        }


        private static double CalcAlarmTime()
        {
            double time = 0;
            float cost = EditorLogic.fetch.ship.GetShipCosts(out _, out _);
            float mass = EditorLogic.fetch.ship.GetTotalMass() * 1000;
            bool career = HighLogic.CurrentGame.Mode == Game.Modes.CAREER;

            if (settingsSTA.TimeCalcMode == Localizer.Format("#STA_VESSEL"))
                if (career)
                    time = cost * settingsSTA.Career_Seconds;
                else
                    time = mass * settingsSTA.Science_Seconds;

            else // Localizer.Format("#STA_CONST")
                time = settingsSTA.Days * 60 * 60 * (GameSettings.KERBIN_TIME ? 6 : 24);

            if (settingsSTA.RepSpeedUp)
            {
                int currRep = Math.Max((int)Reputation.CurrentRep, 0);
                double lines = currRep / settingsSTA.RepToNextLevel +1;
                time /= lines;
                Log("Reputation: {0}, ProdLines: {1}" , Reputation.CurrentRep, lines);
            }

            if (settingsSTA.KerbSpeedUp)
            {
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsSTA.KerbToNextLevel + 1;

                time /= teams ;
                Log("Available Crew: {0}, Teams: {1}", availableKerbs, teams);
            }

            return time;
        }

        private static bool IsAlarmFound(string vessel_name, out string id)
        {
            if (KACWrapper.APIReady)
            {
                KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.Name == vessel_name);

                if (a != null)
                {
                    id = a.ID;
                    return true;
                }
            }
            id = "";
            return false;
        }


        private static double Remaining(KACWrapper.KACAPI.KACAlarm a)
        {
            // looks like a.Remaining doesn't work in the Editor :(

            var list = a.Notes.Split(' ');

            if (list.Count() != 2)
            {
                Log("Alarm Note has not correct values. Safe Launch");
                return -1; // negative
            }

            double ut = HighLogic.CurrentGame.flightState.universalTime;
            double ut_start = Convert.ToDouble(list[0]);
            double passed = ut - ut_start;
            double rem = Convert.ToDouble(list[1]) - passed;

            return rem;

        }

        private static bool IsAlarmFinished(string id)
        {
            if (KACWrapper.APIReady)
            {
                KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.ID == id);

                double rem = Remaining(a);
                if (rem < 0.0)
                {
                    Log("Loading vessel");
                    return true;
                }
                else
                {
                    Log("LaunchRequest: Denied");

                    Log("Back to KSC");
                    return false;
                }


                //foreach (var alarm in KACWrapper.KAC.Alarms)
                //{
                //    if (alarm.ID == id)
                //    {
                //        double rem = Remaining(alarm);
                //        if (rem < 0.0)
                //        {
                //            Log("Loading vessel");
                //            return true;
                //        }
                //        else
                //        {
                //            Log("LaunchRequest: Denied");

                //            Log("Back to KSC");
                //            return false;
                //        }
                //    }
                //}
            }
            Log("KAC is not found");
            return true; // something wrong, need to load the vessel
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
