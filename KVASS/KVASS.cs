using System;
using System.Text.RegularExpressions;
using System.Linq;

using UnityEngine;
using KSP.Localization;

using KVASS_KACWrapper;
using static KVASS.Logging;


namespace KVASS
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KVASS : MonoBehaviour
    {
        static KVASS_SimSettings settingsSim;
        static KVASS_PlanSettings settingsPlan;
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
            settingsSim = HighLogic.CurrentGame.Parameters.CustomParams<KVASS_SimSettings>();
            settingsPlan = HighLogic.CurrentGame.Parameters.CustomParams<KVASS_PlanSettings>();

            regex = new Regex(LoadRegExpPattern());

            KACWrapper.InitKACWrapper();

            if (KACWrapper.APIReady)
            {
                // Log("KACWrapper: API Ready");
            }
            else
            {
                // Log("KACWrapper: API is not ready");
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

            if (settingsSim.Enable && regex.IsMatch(EditorLogic.fetch.ship.shipName))
            {
                // Log("Simulation Launch");
                if (SimulationPurchase())
                    EditorLogic.fetch.launchVessel();
            }
            else if (settingsPlan.Enable && KACWrapper.APIReady)
            {
                // Log("Building");

                //string ID;
                string shipName = EditorLogic.fetch.ship.shipName;
                string alarmTitle = Localizer.Format("#KVASS_plan_alarm_title", shipName);

                if (IsAlarmFound(alarmTitle, out string ID))
                {
                    if (IsAlarmFinished(ID))
                    {
                        if (PossibleToLaunch())
                        {
                            RemoveAlarm(ID);
                            EditorLogic.fetch.launchVessel();
                        }
                    }
                    else
                    {
                        PostScreenMessage(Orange(shipName + ": the planning is not finished"));
                    }
                }
                else
                {
                    // Log("Alarm Is Not Found, Creating");
                    CreateNewAlarm(alarmTitle);
                    PostScreenMessage(shipName + ": the planning is started\nNew alarm is created", 7);
                }

            }
            else
            {
                if (settingsPlan.Enable)
                {
                    Log("KAC API is not ready. Install KAC or disable Planning");
                }
                    
                EditorLogic.fetch.launchVessel();
            }
            
        });

        //public static List<string> GetLaunchSites(bool isVAB)
        //{
        //    EditorDriver.editorFacility = isVAB ? EditorFacility.VAB : EditorFacility.SPH;
        //    typeof(EditorDriver).GetMethod("setupValidLaunchSites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
        //    return EditorDriver.ValidLaunchSites;
        //}


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


        private static bool PossibleToLaunch()
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
                    + Funding.Instance.Funds.ToString(format) + " < " +
                    shipCost.ToString(format);

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


        // return is there enough funds
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
        

        private static string CreateNewAlarm(string title)
        {
            double time = CalcAlarmTime();

            string aID = "";
            if (KACWrapper.APIReady)
            {
                aID = KACWrapper.KAC.CreateAlarm(
                    KACWrapper.KACAPI.AlarmTypeEnum.Raw,
                    title,
                    Planetarium.GetUniversalTime() + time);

                Log("New Alarm: {0}, {1:F1} days", title, time / KSPUtil.dateTimeFormatter.Day);

                if (aID != "")
                {
                    //if the alarm was made get the object so we can update it
                    KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

                    a.Notes = String.Format("{2}\n{0:F0} {1:F0}",
                        HighLogic.CurrentGame.flightState.universalTime, time,
                        Localizer.Format("#KVASS_plan_message_alarm"));
                    a.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarpOnly;
                    a.AlarmMargin = 0;
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
                log_str += String.Format("RepSpeedUp: x{0}, " , lines);
            }

            if (settingsPlan.KerbSpeedUp)
            {
                int availableKerbs = HighLogic.CurrentGame.CrewRoster.GetAvailableCrewCount();

                int teams = availableKerbs / settingsPlan.KerbToNextLevel + 1;

                time /= teams ;
                log_str += String.Format("CrewSpeedUp: x{0}, ", teams);
            }

            // the last. The SpeedUps no affect. 
            if (settingsPlan.Bureaucracy)
                time += settingsPlan.BureaucracyTime * KSPUtil.dateTimeFormatter.Day;

            log_str += String.Format("PlanTime: {0}d", time / KSPUtil.dateTimeFormatter.Day);
            Log(log_str);

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
            // looks like a.Remaining doesn't work in the VAB/SPH :(

            string line = a.Notes.Split('\n').Last();
            string[] values = line.Split(' ');

            if (values.Count() != 2)
            {
                Log("The note in the alarm hasn't correct values. Safe Launch");
                return -1; // negative
            }

            double timer_created = Convert.ToDouble(values[0]);
            double timer_interval = Convert.ToDouble(values[1]);
            double time_now = HighLogic.CurrentGame.flightState.universalTime;
            double passed = time_now - timer_created;
            double remains = timer_interval - passed;

            return remains;

        }

        private static bool IsAlarmFinished(string id)
        {
            if (KACWrapper.APIReady)
            {
                KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.FirstOrDefault(z => z.ID == id);

                double rem = Remaining(a);
                if (rem < 0.0)
                {
                    // Log("Loading vessel");
                    return true;
                }
                else
                {
                    // Log("LaunchRequest: Denied, Back to KSC");
                    
                    return false;
                }
                
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
