            
This interface is used by the WBIVTOLManager to control the hover state of an engine.
        
## Methods


### IsEngineActive
Determines whether or not the engine is active.
> #### Return value
> True if active, false if not.

### StartEngine
Tells the engine to start.

### StopEngine
Tells the engine to shut down.

### UpdateHoverState(System.Single)
Updates the hover state with the current throttle value.
> #### Parameters
> **throttleValue:** A float containing the throttle value.


### SetHoverMode(System.Boolean)
Tells the controller to set the hover mode.
> #### Parameters
> **isActive:** True if hover mode is active, false if not.


### SetVerticalSpeed(System.Single)
Sets the desired vertical speed of the craft.
> #### Parameters
> **verticalSpeed:** A float containing the desired vertical speed in meters/sec.


### KillVerticalSpeed
Tells the hover controller to that the craft should be at 0 vertical speed.

