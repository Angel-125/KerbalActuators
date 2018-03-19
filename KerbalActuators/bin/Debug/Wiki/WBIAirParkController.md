            
This class is designed to let you "park" a vessel in mid-air and treat it as if landed on the ground.
        
## Fields

### currentSituation
Displays the current vessel situation. This is used in debug mode.
### previousSituation
Displays the previous vessel situation. This is used in debug mode.
### isParked
This flag indicates whether or not the vessel is parked.
### parkedAltitude
The altitude at which the vessel is parked.
### isOnRails
A flag to indicate whether or not the vessel is on rails.
## Methods


### SetLanded
This event tells the controller to set the vessel state as landed. It's not perfect, and you have to F5/F9 for it to take effect, but it basically works.

### SetFlying
This event tells the controller to set the vessel state as flying.

### TogglePark
This event toggles the vessel flying/landed state.

### ToggleParkAction(KSPActionParam)
This action sets the parking state on/off.
> #### Parameters
> **param:** A KSPActionParam containing state information for the action.


### KillVelocity
Attemps to kill the vessel velocity. Best used when under 100m/sec.

