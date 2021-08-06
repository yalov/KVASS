using KSP.Localization;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace KVASSNS
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings


    public class KVASSSimSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return Localizer.Format("#KVASS_sim_title"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.NOTMISSION; } }
        public override string Section { get { return "KVASS"; } }
        public override string DisplaySection { get { return "KVASS"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#KVASS_sim_enable")]
        public bool Enable { get; private set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_sim_ignore_SPH")]
        public bool IgnoreSPH { get; private set; } = false;

        [GameParameters.CustomStringParameterUI("SSDM", gameMode = GameParameters.GameMode.SANDBOX,
            autoPersistance = true, lines = 4, title = "#KVASS_sim_sand_disabled")]
        public string StringSSDM = "";

        [GameParameters.CustomIntParameterUI("#KVASS_sim_rel_cost_enabled", gameMode = GameParameters.GameMode.CAREER)]
        public bool RelCostEnabled { get; private set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_sim_rel_science_enabled", gameMode = GameParameters.GameMode.SCIENCE)]
        public bool RelScienceEnabled { get; private set; } = true;

        [GameParameters.CustomFloatParameterUI("#KVASS_sim_rel_cost", gameMode = GameParameters.GameMode.CAREER,
            minValue = 0.0f, maxValue = 20.0f, displayFormat = "N1")]
        public double RelCost { get; private set; } = 2.0f;

        [GameParameters.CustomFloatParameterUI("#KVASS_sim_rel_science", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.0f, maxValue = 20.0f, displayFormat = "N1")]
        public float RelScience { get; private set; } = 1.0f;


        [GameParameters.CustomParameterUI("#KVASS_sim_const_cost_enabled",
            gameMode = GameParameters.GameMode.CAREER)]
        public bool ConstCostEnabled { get; private set; } = false;

        [GameParameters.CustomParameterUI("#KVASS_sim_const_science_enabled",
            gameMode = GameParameters.GameMode.SCIENCE)]
        public bool ConstScienceEnabled { get; private set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_sim_const_cost", gameMode = GameParameters.GameMode.CAREER,
            minValue = 500, maxValue = 100000, stepSize = 500)]
        public int ConstCost { get; private set; } = 1000;

        [GameParameters.CustomFloatParameterUI("#KVASS_sim_const_science", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.1f, maxValue = 20.0f, displayFormat = "N1")]
        public float ConstScience { get; private set; } = 0.5f;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {

            switch (preset)
            {
                case GameParameters.Preset.Easy:   RelCost = 1; RelScience = 0.5f; break;
                case GameParameters.Preset.Normal: RelCost = 2; RelScience = 1; break;
                case GameParameters.Preset.Hard:   RelCost = 5; RelScience = 2;  break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member == null) return false;

            if (member.Name == "ConstCost")
                return ConstCostEnabled;

            if (member.Name == "ConstScience")
                return ConstScienceEnabled;

            if (member.Name == "RelCost")
                return RelCostEnabled;

            if (member.Name == "RelScience")
                return RelScienceEnabled;

            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member == null) 
                return false;

            if (member.Name == "Enable")
                return true;
            else
                return Enable;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }


    public class KVASSPlanSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return Localizer.Format("#KVASS_plan_title"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.NOTMISSION; } }
        public override string Section { get { return "KVASS"; } }
        public override string DisplaySection { get { return "KVASS"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#KVASS_plan_enable")]
        public bool Enable { get; private set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_KAC_enable")]
        public bool KACEnable { get; private set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_ignore_SPH")]
        public bool IgnoreSPH { get; private set; } = false;

        [GameParameters.CustomParameterUI("#KVASS_plan_queue", toolTip = "#KVASS_plan_queue_tooltip")]
        public bool Queue { get; private set; } = true;

        [GameParameters.CustomParameterUI("#KVASS_plan_queue_append", toolTip = "#KVASS_plan_queue_append_tooltip")]
        public bool QueueAppend { get; private set; } = true;

        [GameParameters.CustomParameterUI("#KVASS_plan_queue_prepend", toolTip = "#KVASS_plan_queue_prepend_tooltip")]
        public bool QueuePrepend { get; private set; } = true;

        [GameParameters.CustomParameterUI("#KVASS_plan_kill_timewarp", toolTip = "#KVASS_plan_kill_timewarp_tooltip")]
        public bool KillTimeWarp { get; private set; } = false;

        [GameParameters.CustomParameterUI("#KVASS_plan_autoremove", toolTip = "#KVASS_plan_autoremove_tooltip")]
        public bool AutoRemoveFinishedTimers { get; private set; } = true;

        [GameParameters.CustomParameterUI("#KVASS_plan_message_speedUps", toolTip = "#KVASS_plan_message_speedUps_tooltip")]
        public string ShowMessageSpeedUps { get; private set; } = Localizer.Format("#KVASS_plan_message_Shorter");

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            KVASSPlanSettings2.interactible = Enable;

            if (member == null) return false;

            if (member.Name == "Enable")
                return true;

            return Enable;
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member == null) return false;

            if (member.Name == "QueueAppend" || member.Name == "QueuePrepend")
                return Queue;
            else
                return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            if (member == null) return null;

            if (member.Name == "ShowMessageSpeedUps")
            {
                List<string> myList = new List<string>
                {
                    Localizer.Format("#KVASS_plan_message_No"),
                    Localizer.Format("#KVASS_plan_message_Shorter"),
                    Localizer.Format("#KVASS_plan_message_Short"),
                    Localizer.Format("#KVASS_plan_message_Expanded")
                };

                return myList;
            }

            else
            {
                return null;
            }
        }
    }



    public class KVASSPlanSettings2 : GameParameters.CustomParameterNode
    {
        public override string Title { get { return Localizer.Format("#KVASS_plan_title"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.NOTMISSION; } }
        public override string Section { get { return "KVASS"; } }
        public override string DisplaySection { get { return "KVASS"; } }
        public override int SectionOrder { get { return 3; } }
        public override bool HasPresets { get { return false; } }

        internal static bool interactible = true;

        [GameParameters.CustomParameterUI("#KVASS_plan_cost_seconds_enable", toolTip = "#KVASS_plan_cost_seconds_enable_tooltip")]
        public bool SecondsPerFundEnable { get; private set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_cost_seconds", 
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int SecondsPerFund { get; private set; } = 10;

        [GameParameters.CustomParameterUI("#KVASS_plan_mass_seconds_enable", toolTip = "#KVASS_plan_mass_seconds_enable_tooltip")]
        public bool SecondsPerKgEnable { get; private set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_mass_seconds", 
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int SecondsPerKg { get; private set; } = 4;

        [GameParameters.CustomParameterUI("#KVASS_plan_enable_calendar", toolTip = "#KVASS_plan_enable_calendar_tooltip")]
        public bool CalendarSpeedUp { get; private set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_calendar", toolTip = "#KVASS_plan_calendar_tooltip",
            minValue = 2, maxValue = 50, stepSize = 1)]
        public int CalendarYearsToNextLevel { get; private set; } = 5;


        [GameParameters.CustomIntParameterUI("#KVASS_plan_calendar_max", toolTip = "#KVASS_plan_calendar_max_tooltip",
            minValue = 1, maxValue = 50, stepSize = 1)]
        public int CalendarYearsSpeedUpsMaxCount { get; private set; } = 5;



        [GameParameters.CustomParameterUI("#KVASS_plan_enable_rep", toolTip = "#KVASS_plan_enable_rep_tooltip",
            gameMode = GameParameters.GameMode.CAREER)]
        public bool RepSpeedUp { get; private set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_rep", toolTip = "#KVASS_plan_rep_tooltip",
            gameMode = GameParameters.GameMode.CAREER, minValue = 10, maxValue = 300, stepSize = 5)]
        public int RepToNextLevel { get; private set; } = 240;


        [GameParameters.CustomParameterUI("#KVASS_plan_enable_kerb", toolTip = "#KVASS_plan_enable_kerb_tooltip")]
        public bool KerbSpeedUp { get; private set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_kerb", toolTip = "#KVASS_plan_kerb_tooltip",
            minValue = 3, maxValue = 20, stepSize = 1)]
        public int KerbToNextLevel { get; private set; } = 7;

        [GameParameters.CustomParameterUI("#KVASS_plan_enable_sci", toolTip = "#KVASS_plan_enable_sci_tooltip",
            gameMode = GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE)]
        public bool SciSpeedUp { get; private set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_sci", toolTip = "#KVASS_plan_sci_tooltip",
            gameMode = GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE,
            minValue = 100, maxValue = 10000, stepSize = 100)]
        public int SciToNextLevel { get; private set; } = 2500;


        [GameParameters.CustomParameterUI("#KVASS_plan_enable_constTime", toolTip = "#KVASS_plan_enable_constTime_tooltip")]
        public bool ConstTime { get; private set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_constTime", minValue = 1, maxValue = 142, stepSize = 1)]
        public int ConstTimeDays { get; private set; } = 1;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:   
                    SecondsPerFund = 10; SecondsPerKg = 4; ConstTime = false; break;
                case GameParameters.Preset.Normal: 
                    SecondsPerFund = 10; SecondsPerKg = 4; break;
                case GameParameters.Preset.Hard:   
                    SecondsPerFund = 20; SecondsPerKg = 8; break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member == null) return false;

            if (member.Name == "SecondsPerFund")
                return SecondsPerFundEnable;

            if (member.Name == "SecondsPerKg")
                return SecondsPerKgEnable;

            if (member.Name == "CalendarYearsToNextLevel")
                return CalendarSpeedUp;

            if (member.Name == "CalendarYearsSpeedUpsMaxCount")
                return CalendarSpeedUp;

            if (member.Name == "RepToNextLevel")
                return RepSpeedUp;

            if (member.Name == "KerbToNextLevel")
                return KerbSpeedUp;

            if (member.Name == "SciToNextLevel")
                return SciSpeedUp;

            if (member.Name == "ConstTimeDays")
                return ConstTime;

            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return interactible;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
