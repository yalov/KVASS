using System;
using System.Text.RegularExpressions;
using UnityEngine;
using static USKAM.Logging;


namespace USKAM
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class USKAM : MonoBehaviour
    {
        static USKAMSettings settings;
        static Regex regex;

        public void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
                Log("Game mode not supported!");
                Destroy(this);
                return;
            }
        }

        public void Start()
        {
            settings = HighLogic.CurrentGame.Parameters.CustomParams<USKAMSettings>();
            
            regex = new Regex(LoadRegExpPattern());

            try
            {
                UnityEngine.UI.Button.ButtonClickedEvent c = new UnityEngine.UI.Button.ButtonClickedEvent();
                c.AddListener(OnLoadClick);
                EditorLogic.fetch.launchBtn.onClick = c;
            }
            catch
            {
                Log("Cannot reset launchBtn");
            }

        }

        /*
        public void OnDisable()
        {
            GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
            G﻿ameEvents.OnVesselRollout.Remove(onVesselRollout);
            GameEvents.onLaunch.Remove(onLaunch);
            onEditorVesselNamingChanged
        }
        */
        
        //Replace the default action
        public UnityEngine.Events.UnityAction OnLoadClick = new UnityEngine.Events.UnityAction(() => {

            if (regex.IsMatch(EditorLogic.fetch.ship.shipName))
            {
                Log("OnLoadClick {0} {1}-{2}", 
                    EditorLogic.fetch.ship.shipName, Funding.Instance.Funds, getSimulationCost());
                Funding.Instance.AddFunds(-getSimulationCost(), TransactionReasons.VesselRollout);
            }
            else
            {
                Log("OnLoadClick NotMatch");
            }

            EditorLogic.fetch.launchVessel();
        });


        private string LoadRegExpPattern()
        {
            string[] RegExs = { "^.?[Tt]est" };

            ConfigNode[] configs = GameDatabase.Instance.GetConfigNodes("USKAM");

            
            //Log("configs: {0}", configs.Length);

            if (configs != null && configs.Length != 0)
            {
                ConfigNode config = configs[0];
                RegExs = config.GetValues("Regex");
            }

            for (int i = 0; i < RegExs.Length; i++)
                RegExs[i] = "(" + RegExs[i].Trim('"') + ")";

            return String.Join("|", RegExs);
        }


        private static double getSimulationCost()
        {
            if (settings.ConstBool)
                return settings.ConstCost;
            else if (settings.RelativeToVesselBool)
                return 0.01 * settings.RelativeToVesselCost * EditorLogic.fetch.ship.GetShipCosts(out _, out _);
            else
                return 0.01 * settings.RelativeToBankCost * Funding.Instance.Funds;
        }
    }
}
