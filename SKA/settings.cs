using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KSP.Localization;

namespace SKA
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings


    public class SKASettings : GameParameters.CustomParameterNode
    {

        public override string Title { get { return Localizer.Format("#SKA_settings_title") ; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER| GameParameters.GameMode.SCIENCE; } }
        public override string Section { get { return "SKA"; } }
        public override string DisplaySection { get { return "SKA"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#SKA_Enable")]
        public bool Enable = true;
        

        [GameParameters.CustomFloatParameterUI("#SKA_Career_Vessel", gameMode = GameParameters.GameMode.CAREER,
            minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1")]
        public double Career_Vessel = 2.0f;

        [GameParameters.CustomFloatParameterUI("#SKA_Science_Vessel", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1")]
        public float Science_Vessel = 2.0f;
        

        [GameParameters.CustomParameterUI("#SKA_Career_Bureaucracy", toolTip = "#SKA_Career_Bureaucracy_Tooltip", 
            gameMode = GameParameters.GameMode.CAREER)]
        public bool Career_Bureaucracy = false;

        [GameParameters.CustomParameterUI("#SKA_Science_Bureaucracy", toolTip = "#SKA_Science_Bureaucracy_Tooltip", 
            gameMode = GameParameters.GameMode.SCIENCE)]
        public bool Science_Bureaucracy = false;


        [GameParameters.CustomIntParameterUI("#SKA_Career_Const", gameMode = GameParameters.GameMode.CAREER,
            minValue = 500, maxValue = 100000, stepSize = 500)]
        public int Career_Const = 1000;

        [GameParameters.CustomFloatParameterUI("#SKA_Science_Const", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1")]
        public float Science_Const = 0.5f;



        [GameParameters.CustomStringParameterUI("#SKA_RE_String", lines = 7, title = "#SKA_RE_String")]
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


    public class STASettings : GameParameters.CustomParameterNode
    {

        public override string Title { get { return Localizer.Format("#STA_settings_title"); } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE; } }
        public override string Section { get { return "SKA"; } }
        public override string DisplaySection { get { return "SKA"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("#STA_Enable")]
        public bool Enable = true;
        
        [GameParameters.CustomIntParameterUI("#STA_Career_Seconds", gameMode = GameParameters.GameMode.CAREER,
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int Career_Seconds = 10;

        [GameParameters.CustomIntParameterUI("#STA_Science_Seconds", gameMode = GameParameters.GameMode.SCIENCE,
            minValue = 0, maxValue = 180, stepSize = 1)]
        public int Science_Seconds = 10;
        
        [GameParameters.CustomParameterUI("#STA_Enable_RepSpeedUp", toolTip = "#STA_Enable_RepSpeedUp_Tooltip", 
            gameMode = GameParameters.GameMode.CAREER)]
        public bool RepSpeedUp = true;

        [GameParameters.CustomIntParameterUI("#STA_RepToNextLevel", toolTip = "#STA_RepToNextLevel_Tooltip", 
            gameMode = GameParameters.GameMode.CAREER, minValue = 10, maxValue = 300, stepSize = 5)]
        public int RepToNextLevel = 240;


        [GameParameters.CustomParameterUI("#STA_Enable_KerbSpeedUp", toolTip = "#STA_Enable_KerbSpeedUp_Tooltip")]
        public bool KerbSpeedUp = true;

        [GameParameters.CustomIntParameterUI("#STA_KerbToNextLevel", toolTip = "#STA_KerbToNextLevel_Tooltip",
            minValue = 3, maxValue = 20, stepSize = 1)]
        public int KerbToNextLevel = 7;


        [GameParameters.CustomParameterUI("#STA_Enable_Bureaucracy", toolTip = "#STA_Enable_Bureaucracy_Tooltip")]
        public bool Bureaucracy = true;

        [GameParameters.CustomIntParameterUI("#STA_BureaucracyTime", minValue = 1, maxValue = 142, stepSize = 1)]
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
