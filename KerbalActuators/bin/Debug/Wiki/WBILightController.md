            
Derived from the stock ModuleLight, this controller lets you control a light through the Servo Manager. Unlike a stock light, you can change the color tint in flight.
        
## Fields

### groupID
Servo group ID. Default is "Light"
### servoName
Name of the servo. Used to identify it in the servo manager and the sequence file.
## Methods


### GetGroupID
Returns the group ID of the servo. Used by the servo manager to know what servos it controlls.
> #### Return value
> A string containing the group ID

### DrawControls
Tells the servo to draw its GUI controls. It's used by the servo manager.

### HideGUI
Hides the GUI controls in the Part Action Window.

### GetPanelHeight
Returns the panel height for the servo manager's GUI.
> #### Return value
> An Int containing the height of the panel.

### TakeSnapshot
Takes a snapshot of the current state of the servo.
> #### Return value
> A SERVODATA_NODE ConfigNode containing the servo's state

### SetFromSnapshot(ConfigNode)
Sets the servo's state based upon the supplied config node.
> #### Parameters
> **node:** A SERVODAT_NODE ConfigNode containing servo state data.


