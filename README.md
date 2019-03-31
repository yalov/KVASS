<!--## KSP mod: KVAS - Kerbal Very Simplified Planning and Simulation-->
<!--[![Version](https://img.shields.io/github/release/yalov/KVAS.svg?label=Version&colorB=4CC61E)](https://github.com/yalov/KVAS/releases) -->
<!--[![Last Release](https://img.shields.io/github/release-date/yalov/KVAS.svg?label=Last%20Release&colorB=99C611)](https://github.com/yalov/KVAS/releases) -->
<!--[![CKAN-Indexed](https://img.shields.io/badge/CKAN-Indexed-yellowgreen.svg)](https://github.com/KSP-CKAN/CKAN)-->
<!--[![Forum thread](https://img.shields.io/badge/Link-Forum%20thread-blue.svg)](https://forum.kerbalspaceprogram.com/index.php?/topic/179385-*) -->
<!--[![Spacedock](https://img.shields.io/badge/Link-Spacedock-blue.svg)](https://spacedock.info/mod/1989)-->


Simple. No new GUI. No any Flight-Scene Calculation*. Simulation and Planning Time are together.

![](https://github.com/yalov/KVAS/blob/master/Screenshots/scheme.jpg?raw=true)


### Simulation.

If your vessel name starts with "Test" or "Simulation" that mean you in the simulation mode.  
Use usual Launch button, and beside a cost of the vessel, you will need to pay **additional funds** for a Simulation.  
After you finish you testing, use usual `Revert to Editor` option - you will get back a vessel's cost but an additional payment will not be reverted.  
Default additional payment is 2% of a vessel's cost.

### Planning Time.

if you vessel name isn't started with the Simulation pattern, then you need "to plan" your vessel before a launch.
For the planning time management **Kerbal Alarm Clock** is used.  
You make your vessel in the Editor, click Launch, and instead of launching, new Alarm in the KAC is created. 
if planning time satisfies you, you exit to the KSC and wait until your 'planning' KAC Alarm is finished. After that you going back to the Editor and finally launch your vessel with the Launch button. The funds will be spended when you launching your vessel.  Alarm is deleted automatically.

The mod doesn't limit how many vessel you can plan simultaneously.  

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

Amount of simultaneous planning isn't limited. 

You could change all numbers in the setting page or enable/disable the "Simulation" and the "Planning Time" separately, KAC is necessary only for the "Planning Time".

The mod works in the Career or Science mode. In the Science mode you pay science points, and mass of the vessel is used.


### Notes/TODO/Bugs:

 * The mod supports only Main Green Launch Button in the VAB/SPH.
 * The launch buttons on the MH VAB/SPH launchsites list are ignored by the mod.
 * The launch button on the launchpad GUI also is ignored by the mod.
 * Make sure you could launch your vessel and launchpad is not destroyed, otherwise you will need to plan the launching again.
 * Do not change the planning alarm notes (values used by the mod)