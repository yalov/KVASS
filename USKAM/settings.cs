using System.Collections;
using System.Reflection;
using KSP.Localization;

namespace USKAM
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    public class USKAMSettings : GameParameters.CustomParameterNode
    {

        public override string Title { get { return Localizer.Format("#USKAM_settings_title") ; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER; } }
        public override string Section { get { return "USKAM"; } }
        public override string DisplaySection { get { return "USKAM"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }



        [GameParameters.CustomIntParameterUI("#USKAM_ConstBool", toolTip = "#USKAM_ConstBoolToolTip")]
        public bool ConstBool = false;

        [GameParameters.CustomIntParameterUI("#USKAM_ConstCost", minValue = 1000, maxValue = 100000, stepSize = 500,
                      toolTip = "#USKAM_ConstCostTooltip")]
        public int ConstCost = 5000;

        [GameParameters.CustomIntParameterUI("#USKAM_RelativeToVesselBool", toolTip = "#USKAM_RelativeToVesselBoolToolTip")]
        public bool RelativeToVesselBool = true;

        [GameParameters.CustomFloatParameterUI("#USKAM_RelativeToVesselCost", minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1",
    toolTip = "#USKAM_RelativeToVesselCostTooltip")]
        public double RelativeToVesselCost = 2.0f;

        [GameParameters.CustomFloatParameterUI("#USKAM_RelativeToBankCost", minValue = 0.2f, maxValue = 20.0f, displayFormat = "N1",
    toolTip = "#USKAM_RelativeToBankCostTooltip")]
        public double RelativeToBankCost = 2.0f;

        [GameParameters.CustomStringParameterUI("#USKAM_RE_String", lines = 6, title = "#USKAM_RE_String")]
        public string RE_String = "";

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "ConstCost")
                return ConstBool;

            if (member.Name == "RelativeToVesselBool")
                return !ConstBool;

            if (member.Name == "RelativeToVesselCost")
                return RelativeToVesselBool && !ConstBool;

            if (member.Name == "RelativeToBankCost")
                return !RelativeToVesselBool && !ConstBool;

            return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
