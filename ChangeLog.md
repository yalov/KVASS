## ChangeLog:

### Version 2.5.1
* fix NullReferenceException
* remove log spam

### Version 2.5.0
 * ksp 1.12.2
 * using stock Alarm Clock App by default
 * Supported SANDBOX
 * Planning time by cost and by mass is enabled in CAREER, SCIENCE and SANDBOX
 * take into account cost of a kerbal's inventory

### Version 2.0.10 (Beta)
 * ksp 1.12
 * using stock Alarm Clock App by default
 * [known bug]
   Adding new alarm when Alarm Clock App GUI is opened, makes the alarm be showed in the GUI as 0:00:00 until scene change.

### Version 2.0.2
 * fix doubling time of the appeding alarm

### Version 2.0.1
 * v1 was having some irritating features:
     * it was harder to roleplay some more complicated concept, like reusable rockets
     * writing "Test", and then removing autosaved "Test" copy of the vessels.
     * Switching append/prepend to queue require exit to KSC and go to the settings.
     * It was not possible to add several vessel with the same name to queue.
     
   So I reimagine the mod, and now it's even less restrictive:
     * restore stock Launch button action (disable any checking there)
     * add 3 new separate buttons in the Editor: [Simulation], [Prepend] and [Append] planning to the quere (if its enabled)
   So now launch button doesn't check anything itself and you can launch reusable rocket at ones,
   you don't need to write "Test" since there is spetial button for the Simulation
   Also you have separate buttons for Append/Prepend (disableable), and it is possible to add any amount of equally named vessels with a same name
 * enabled queue by default on all difficulty preset
 * option for autoremove finished planning timers on launch. 
 * added SpeedUp by passing Years (5 years is default)
 * disabled speedup by Rep by default. 
   It is very easy to get >700 at early career with default Rep settings without leaving homeworld SOI, 
   so it doesn't make much sence.
 * renamed bureaucracy to constTime

### Version 1.0.0
 * bump version

### Version 0.13.0
 * added option for appending to the queue of planning, instead of prepending
 * fixed queueing bug when created non planning alarm
 * updated localization 
 * splitted settings on 3 columns
 * ksp 1.9.1
 * option to show calculation when creating new planning alarm.
 
### Version 0.12.0
 * Recompile for ksp 1.8
 * .NET 4.7.2
 * add SpeedUp by Science (disabled by default)
 * add difficulty presets
 * option for showing the planning time on an alarm creation
 * support of Launchpad/Runway GUI
     * The green button launches only to Launchpad/Runway
     * the MH launchbuttons launch to the MH launchsites
     * [known-bug] radiobuttons doesn't work for now
 * Queue shifts works on any scene
 * [known-bug] Alarms are created with WarpKill, even if the option is disabled. 
   After a scene change alarms become as it suppose to be with/without WarpKill.
 
### Version 0.11.0
 * supports MH Launch buttons
 * options to ignore SPH. Planes and trucks are built without planning or simulation.
 * option to kill time-warp on the end of a simulation.
 * queue of planning (and option to disable):
   New planning is added to start of the queue, other timers delayed correspondingly in the KAC list. 
   Removing any planning from the queue (in the Editor) will shift following timers back

### Version 0.8.3
 * at least 1 decimal digit
   in the science warning message
 * lower default sci-cost in the science mode
 * clearing text and settings

### Version 0.8.2
 * renamed to the KVASS
 * updated settings min/max
 * significant digits in the warning message

### Version 0.8.0
 * clearing code

### Version 0.7.6
 * Beta
 * renamed to the KVAS

### Version 0.6.8
 * Alpha