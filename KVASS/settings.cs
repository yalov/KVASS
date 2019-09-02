using KSP.Localization;
using System.Collections;
using System.Reflection;

namespace KVASSNS
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings


    public class KVASSSimSettings : GameParameters.CustomParameterNode
    {

        public override string Title { get { return Localizer.Format("#KVASS_sim_title"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE; } }
        public override string Section { get { return "KVASS"; } }
        public override string DisplaySection { get { return "KVASS"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#KVASS_sim_enable")]
        public bool Enable { get; private set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_sim_ignore_SPH")]
        public bool IgnoreSPH { get; set; } = false;


        [GameParameters.CustomFloatParameterUI("#KVASS_sim_career_vessel", gameMode = GameParameters.GameMode.CAREER,
            minValue = 0.0f, maxValue = 20.0f, displayFormat = "N1")]
        public double CareerVessel { get; set; } = 2.0f;

        [GameParameters.CustomFloatParameterUI("#KVASS_sim_science_vessel", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.0f, maxValue = 20.0f, displayFormat = "N1")]
        public float ScienceVessel { get; set; } = 1.0f;


        [GameParameters.CustomParameterUI("#KVASS_sim_career_bureaucracy", toolTip = "#KVASS_sim_career_bureaucracy_tooltip",
            gameMode = GameParameters.GameMode.CAREER)]
        public bool CareerBureaucracy { get; set; } = false;

        [GameParameters.CustomParameterUI("#KVASS_sim_science_bureaucracy", toolTip = "#KVASS_sim_science_bureaucracy_tooltip",
            gameMode = GameParameters.GameMode.SCIENCE)]
        public bool ScienceBureaucracy { get; set; } = false;


        [GameParameters.CustomIntParameterUI("#KVASS_sim_career_const", gameMode = GameParameters.GameMode.CAREER,
            minValue = 500, maxValue = 100000, stepSize = 500)]
        public int CareerConst { get; set; } = 1000;

        [GameParameters.CustomFloatParameterUI("#KVASS_sim_science_const", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.1f, maxValue = 20.0f, displayFormat = "N1")]
        public float ScienceConst { get; set; } = 0.5f;



        [GameParameters.CustomStringParameterUI("#KVASS_sim_re", lines = 7, title = "#KVASS_sim_re")]
        public string REString { get; set; } = "";

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {

            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member == null) return false;

            if (member.Name == "IgnoreSPH"
                || member.Name == "Science_Vessel" || member.Name == "Career_Vessel"
                || member.Name == "Science_Bureaucracy" || member.Name == "Career_Bureaucracy"
                || member.Name == "RE_String"
                )
                return Enable;

            if (member.Name == "Career_Const")
                return CareerBureaucracy && Enable;

            if (member.Name == "Science_Const")
                return ScienceBureaucracy && Enable;

            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }


    public class KVASSPlanSettings : GameParameters.CustomParameterNode
    {

        public override string Title { get { return Localizer.Format("#KVASS_plan_title"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE; } }
        public override string Section { get { return "KVASS"; } }
        public override string DisplaySection { get { return "KVASS"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#KVASS_plan_enable")]
        public bool Enable { get; set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_ignore_SPH")]
        public bool IgnoreSPH { get; set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_career_seconds", gameMode = GameParameters.GameMode.CAREER,
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int CareerSeconds { get; set; } = 10;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_science_seconds", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int ScienceSeconds { get; set; } = 4;

        [GameParameters.CustomParameterUI("#KVASS_plan_enable_rep", toolTip = "#KVASS_plan_enable_rep_tooltip",
            gameMode = GameParameters.GameMode.CAREER)]
        public bool RepSpeedUp { get; set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_rep", toolTip = "#KVASS_plan_rep_tooltip",
            gameMode = GameParameters.GameMode.CAREER, minValue = 10, maxValue = 300, stepSize = 5)]
        public int RepToNextLevel { get; set; } = 240;


        [GameParameters.CustomParameterUI("#KVASS_plan_enable_kerb", toolTip = "#KVASS_plan_enable_kerb_tooltip")]
        public bool KerbSpeedUp { get; set; } = false;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_kerb", toolTip = "#KVASS_plan_kerb_tooltip",
            minValue = 3, maxValue = 20, stepSize = 1)]
        public int KerbToNextLevel { get; set; } = 7;


        [GameParameters.CustomParameterUI("#KVASS_plan_enable_bureaucracy", toolTip = "#KVASS_plan_enable_bureaucracy_tooltip")]
        public bool Bureaucracy { get; set; } = true;

        [GameParameters.CustomIntParameterUI("#KVASS_plan_bureaucracy", minValue = 1, maxValue = 142, stepSize = 1)]
        public int BureaucracyTime { get; set; } = 1;

        [GameParameters.CustomParameterUI("#KVASS_plan_kill_timewarp", toolTip = "#KVASS_plan_kill_timewarp_tooltip")]
        public bool KillTimeWarp { get; set; } = true;

        [GameParameters.CustomParameterUI("#KVASS_plan_queue", toolTip = "#KVASS_plan_queue_tooltip")]
        public bool Queue { get; set; } = false;



        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member == null) return false;

            if (member.Name == "IgnoreSPH"
                || member.Name == "Career_Seconds" || member.Name == "Science_Seconds"
                || member.Name == "RepSpeedUp" || member.Name == "KerbSpeedUp"
                || member.Name == "Bureaucracy"
                || member.Name == "KillTimeWarp"
                || member.Name == "Queue"

                )
                return Enable;

            if (member.Name == "RepToNextLevel")
                return RepSpeedUp && Enable;

            if (member.Name == "KerbToNextLevel")
                return KerbSpeedUp && Enable;

            if (member.Name == "BureaucracyTime")
                return Bureaucracy && Enable;

            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
