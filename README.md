[![Version](https://img.shields.io/github/release/yalov/KVASS.svg?label=Version&colorB=4CC61E)](https://github.com/yalov/KVASS/releases) 
[![Last Release](https://img.shields.io/github/release-date/yalov/KVASS.svg?label=Last%20Release&colorB=99C611)](https://github.com/yalov/KVASS/releases) 
<!--[![CKAN-Indexed](https://img.shields.io/badge/CKAN-Indexed-yellowgreen.svg)](https://github.com/KSP-CKAN/CKAN)-->
[![Forum thread](https://img.shields.io/badge/Link-Forum%20thread-blue.svg)](https://forum.kerbalspaceprogram.com/index.php?/topic/183393-*) 
<!--[![Spacedock](https://img.shields.io/badge/Link-Spacedock-blue.svg)](https://spacedock.info/mod/1989)-->


![scheme](https://github.com/yalov/KVASS/blob/master/Screenshots/scheme.jpg?raw=true)


### Simulation.

If your vessel name starts with "Test" or "Simulation" that mean you in the simulation mode.  
Use usual Launch button, and beside a cost of the vessel, you will need to pay **additional funds** for a Simulation.  
After you finish you testing, use usual `Revert to Editor` option - you will get back a vessel's cost but an additional payment will not be reverted.  
Default additional payment is 2% of a vessel's cost.

### Planning Time.

if you vessel name isn't started with the Simulation pattern, then you need "to plan" your vessel before a launch.
For the planning time management **Kerbal Alarm Clock** is used.
You make your vessel in the Editor, click Launch, and instead of launching, new Alarm in the KAC is created.
if planning time satisfies you, you exit to the KSC and wait until your 'planning' KAC Alarm is finished.

Do not delete the alarm, it needs to stay in the alarm list when you going back to the Editor and finally
launch your vessel with the Launch button. Alarm is deleted automatically.
Also there is queue of planning: when you add new planning, all existing planning are delayed (KAC timers change accordingly)

By default at start of your game, 1 fund of a vessel's cost will be planned 10 seconds, and additional day for a bureaucracy after. Simple as that!

So, for example: 
 * GDLV3 Vehicle (24,557 funds) will be planned in 24557*10/3600/6 +1 = 12.4 days
 * Dynawing Shuttle (132,899 funds) will be planned in 132899*10/3600/6 +1 = 62.5 days

Later you will have speedUp by Reputation effect, and if you want to get even more, you could enable speedUp by Available Kerbals:

 * speedUp by Reputation: Every time you pass 240X reputation (240, 480, 720, 960) planning time is reduced, 
so after 240 Rep time will be halved, after 480 Rep time will be thirded, etc.
 * speedUp by Available Kerbals: Every 7 available kerbonauts in the Kerbonaut Complex creates new team, which reduces planning time the same way as Reputation.

The Dynawing will be planned in 31 days if you have 240 Rep.
If you enable second SpeedUp and have 7 available kerbonauts The Dynawing will be planned in 15 days

Do not disable the reverting in the stock difficulty section, you need it for reverting any Simulation,
and you are not suppose to revert the "real" launch after the planning, because they are "real".

Consider to add some mod like KER, that can show name of the vessel on flight screen —
this way you will not forget do you in a simulation or in a "real" flight.

### FAQ:
 * Q: Can I change the simulation text pattern?
 * A: Yes, check SettingRegex.cfg for instruction, you need just a MM-patch, that add new RegEx.
 * Q: Why is it "planning" and not "building"?
 * A: The funds will be spent not when you start "process", but when the process is finished and vessel is launching. 
   So, in-universe, when planning is started, the best minds of KSC-staff meet each other in the meeting room,
   and start planning: how to build the vessel, where get the fuel, and so on. After all planning is done, 
   the building proceeds in the usual kerbal way: exchange funds for a ready-to-go vessel from the Green Pocket Dimension


### Notes/TODO
 * The mod supports only Launching from the VAB/SPH.
 * The launch button on the launchpad GUI isn't disabled and is ignored by the mod.
