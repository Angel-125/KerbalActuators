            
Generic servo controller interface
        
## Methods


### GetGroupID
Specifies the group identifier string for the servo controller. Enables you to have servos in distinct groups like an engine and an arm, on the same part.
> #### Return value
> A string containing the identifier

### DrawControls
Tells the servo to draw its GUI controls

### HideGUI
Tells the servo to hide its part action window controls

### GetPanelHeight
Asks for the height of the GUI panel
> #### Return value
> An int containing the height of the panel

### TakeSnapshot
Tells the servo to take a snapshot of its current state. This is used to produce sequences for the servo.
> #### Return value
> A SERVODATA_NODE ConfigNode containing the current state of the servo

### SetFromSnapshot(ConfigNode)
Instructs the servo to update its current state by parsing the supplied ConfigNode.
> #### Parameters
> **node:** A SERVODATA_NODE ConfigNode containing the desired servo state.


### IsMoving
Indicates whether or not the servo controller is moving in some way.
> #### Return value
> True if moving, false if not.

