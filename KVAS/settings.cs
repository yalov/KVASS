using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KSP.Localization;

namespace KVAS
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings


    public class KVAS_SimSettings : GameParameters.CustomParameterNode
    {

        public override string Title { get { return Localizer.Format("#KVAS_sim_title") ; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER| GameParameters.GameMode.SCIENCE; } }
        public override string Section { get { return "KVAS"; } }
        public override string DisplaySection { get { return "KVAS"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#KVAS_sim_enable")]
        public bool Enable = true;
        

        [GameParameters.CustomFloatParameterUI("#KVAS_sim_career_vessel", gameMode = GameParameters.GameMode.CAREER,
            minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1")]
        public double Career_Vessel = 2.0f;

        [GameParameters.CustomFloatParameterUI("#KVAS_sim_science_vessel", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1")]
        public float Science_Vessel = 2.0f;
        

        [GameParameters.CustomParameterUI("#KVAS_sim_career_bureaucracy", toolTip = "#KVAS_sim_career_bureaucracy_tooltip", 
            gameMode = GameParameters.GameMode.CAREER)]
        public bool Career_Bureaucracy = false;

        [GameParameters.CustomParameterUI("#KVAS_sim_science_bureaucracy", toolTip = "#KVAS_sim_science_bureaucracy_tooltip", 
            gameMode = GameParameters.GameMode.SCIENCE)]
        public bool Science_Bureaucracy = false;


        [GameParameters.CustomIntParameterUI("#KVAS_sim_career_const", gameMode = GameParameters.GameMode.CAREER,
            minValue = 500, maxValue = 100000, stepSize = 500)]
        public int Career_Const = 1000;

        [GameParameters.CustomFloatParameterUI("#KVAS_sim_science_const", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1")]
        public float Science_Const = 0.5f;



        [GameParameters.CustomStringParameterUI("#KVAS_sim_re", lines = 7, title = "#KVAS_sim_re")]
        public string RE_String = "";

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {

            if (member.Name == "RE_String"
                
                || member.Name == "Science_Vessel"|| member.Name == "Career_Vessel"
                || member.Name == "Science_Bureaucracy" || member.Name == "Career_Bureaucracy"
                )
                return Enable;

            if (member.Name == "Career_Const")
                return Career_Bureaucracy && Enable;

            if (member.Name == "Science_Const")
                return Science_Bureaucracy && Enable;

            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }


    public class KVAS_PlanSettings : GameParameters.CustomParameterNode
    {

        public override string Title { get { return Localizer.Format("#KVAS_plan_title"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE; } }
        public override string Section { get { return "KVAS"; } }
        public override string DisplaySection { get { return "KVAS"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#KVAS_plan_enable")]
        public bool Enable = true;
        
        [GameParameters.CustomIntParameterUI("#KVAS_plan_career_seconds", gameMode = GameParameters.GameMode.CAREER,
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int Career_Seconds = 10;

        [GameParameters.CustomIntParameterUI("#KVAS_plan_science_seconds", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int Science_Seconds = 10;
        
        [GameParameters.CustomParameterUI("#KVAS_plan_enable_rep", toolTip = "#KVAS_plan_enable_rep_tooltip", 
            gameMode = GameParameters.GameMode.CAREER)]
        public bool RepSpeedUp = true;

        [GameParameters.CustomIntParameterUI("#KVAS_plan_rep", toolTip = "#KVAS_plan_rep_tooltip", 
            gameMode = GameParameters.GameMode.CAREER, minValue = 10, maxValue = 300, stepSize = 5)]
        public int RepToNextLevel = 240;


        [GameParameters.CustomParameterUI("#KVAS_plan_enable_kerb", toolTip = "#KVAS_plan_enable_kerb_tooltip")]
        public bool KerbSpeedUp = false;

        [GameParameters.CustomIntParameterUI("#KVAS_plan_kerb", toolTip = "#KVAS_plan_kerb_tooltip",
            minValue = 3, maxValue = 20, stepSize = 1)]
        public int KerbToNextLevel = 7;


        [GameParameters.CustomParameterUI("#KVAS_plan_enable_bureaucracy", toolTip = "#KVAS_plan_enable_bureaucracy_tooltip")]
        public bool Bureaucracy = true;

        [GameParameters.CustomIntParameterUI("#KVAS_plan_bureaucracy", minValue = 1, maxValue = 142, stepSize = 1)]
        public int BureaucracyTime = 1;



        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "Career_Seconds" || member.Name == "Science_Seconds"
                || member.Name == "RepSpeedUp" || member.Name == "KerbSpeedUp"
                || member.Name == "Bureaucracy"
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
