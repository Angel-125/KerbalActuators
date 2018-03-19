            
The WBIHoverController is designed to help engines figure out what thrust is needed to maintain a desired vertical speed. The hover controller can support multiple engines.
        
## Fields

### verticalSpeedIncrements
Desired vertical speed. Increments in meters/sec.
### verticalSpeed
The current vertical speed.
### hoverActive
A field to indicate whether or not the hover mode is active.
### guiVisible
A flag to indicate whether or not the Part Action Window GUI is active.
### engine
The current engine to update during hover state updates.
## Methods


### ToggleHoverMode
This event toggles the hover mode.

### IsEngineActive
Determines whether or not the engine is active.
> #### Return value
> True if the engine is active, false if not.

### StartEngine
Tells the controller to start the engine.

### StopEngine
Tells the hover controller to stop the engine.

### UpdateHoverState(System.Single)
Tells the hover controller to update its hover state.
> #### Parameters
> **throttleValue:** A float containing the throttle value to account for during the hover state


### SetHoverMode(System.Boolean)
Sets the hover state in the controller.
> #### Parameters
> **isActive:** True if hover mode is active, false if not.


### IncreaseVerticalSpeed
This event increases the vertical speed by verticalSpeedIncrements (in meters/sec)/

### DecreaseVerticalSpeed
This event decreases the vertical speed by verticalSpeedIncrements (in meters/sec).

### toggleHoverAction(KSPActionParam)
This action toggles the hover mode.
> #### Parameters
> **param:** A KSPActionParam containing action state information.


### increaseVerticalSpeed(KSPActionParam)
This action increases the vertical speed by verticalSpeedIncrements (in meters/sec).
> #### Parameters
> **param:** A KSPActionParam containing action state information.


### decreaseVerticalSpeed(KSPActionParam)
This action decreases the vertical speed by verticalSpeedIncrements (in meters/sec).
> #### Parameters
> **param:** A KSPActionParam containing action state information.


### SetVerticalSpeed(System.Single)
Sets the desired vertical speed in meters/sec.
> #### Parameters
> **verticalSpeed:** A float containing the vertical speed in meters/sec.


### KillVerticalSpeed
Sets the desired vertical speed to 0.

### SetGUIVisible(System.Boolean)
Show or hides the GUI controls in the Part Action Window.
> #### Parameters
> **isVisible:** True if the controls are visible, false if not.


### printSpeed
Prints the vertical speed on the screen.

### ActivateHover
Activates hover mode.

### DeactivateHover
Deactivates hover mode.

