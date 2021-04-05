# KVASS v2

[![Version](https://img.shields.io/github/release/yalov/KVASS.svg?label=Version&colorB=4CC61E)](https://github.com/yalov/KVASS/releases)
[![Last Release](https://img.shields.io/github/release-date/yalov/KVASS.svg?label=Last%20Release&colorB=99C611)](https://github.com/yalov/KVASS/releases)
[![CKAN-Indexed](https://img.shields.io/badge/CKAN-Indexed-yellowgreen.svg)](https://github.com/KSP-CKAN/CKAN)
[![Forum thread](https://img.shields.io/badge/Link-Forum%20thread-blue.svg)](https://forum.kerbalspaceprogram.com/index.php?/topic/183393-*)
[![Spacedock](https://img.shields.io/badge/Link-Spacedock-blue.svg)](https://spacedock.info/mod/2138)

Simulation and Planning Time without any restriction.

![scheme](https://i.imgur.com/q07E7IO.png)

## Simulation

Use the Simulation button for a launch, and beside a cost of the vessel, you will need to pay additional funds for a simulation.
After you finish your testing, use usual "Revert to Editor" option — you will get back a vessel's cost, but an additional payment will not be reverted (default - 2% of a vessel's cost).
Do not forget to enable possibility to Revert in the stock settings.

## Planning Time

For a "real" launch kerbals want "to plan" a vessel before the launch. Also you aren't suppose to revert the "real" launch after the planning, because they are "real".  
For the planning time management Kerbal Alarm Clock is used: you make your vessel in the editor, click a Clock Button, and KAC alarm is created; then you exit to the KSC and wait until the Planning KAC Alarm is finished; after that you finally Launch your vessel from the Editor or KSC Launchpad/Runway GUI with the usual Launch button. Alarm is deleted automatically.

If you have reusable stage — just remove temporally that stage from a rocket in the editor, create a timer, and then put stage back, and launch the vessel later.  
If for some roleplay reason you have the ability to launch a vessel without the planning — just launch without planning.  
If you want to make a small tweak to a vessel after the planning just before launch — go for it, the mod will not restrict you.  

> *Why is it "planning" and not "building"?
> The funds will not be spent when you start "process", but when the process is finished and vessel is launching.
> So, in-universe, when planning is started, the best minds of KSC-staff go to the meeting room,
> and start planning: how to build the vessel, where get the fuel, where to place Place-Anywhere 7 Linear RCS Port and so on.
> After all planning is done, the building proceeds in the usual kerbal way: exchange funds for a ready-to-go vessel from the Green  Pocket Dimension.*

By default, 1 fund of a vessel's cost will be planned 10 seconds, and additional day after. Simple as that!

So, for example with the 6-hours day:

* GDLV3 Vehicle (:funds:24,557) will be planned in 24557*10/3600/6 + 1 = 12.4 days
* Dynawing Shuttle (:funds:132,899) will be planned in 132899*10/3600/6 + 1 = 62.5 days

Also you could enable some speedUps for having some kind of planning time progression:

* speedUp by passing Years (enabled by default):
  Every 5 years planning time will be reduced:
  after 5 years time will be halved, after 10 years - time will be thirded, etc.
* speedUp by Reputation:
  Every time you pass 240X reputation planning time is reduced,
  so after 240 Rep time will be halved, after 480 Rep time will be thirded, etc.
* speedUp by Available Kerbals:
  Every 7 available kerbonauts in the Kerbonaut Complex creates new team,
  which reduces planning time the same way as Reputation.
* speedUp by Science:
  Every time you pass 2500·X science planning time is reduced (science will not be spended)
  This could make lategame science usefull.

In the setting page, you could change many numbers, mentioned above.

The mod works in the Career or Science mode. In the Science mode you pay science points, and mass of the vessel is used.

Advanced Alternatives: KCT + KRASH & Co.
