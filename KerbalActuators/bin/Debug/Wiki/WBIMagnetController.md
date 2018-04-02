            
This class implements a magnet that's used for moving other parts around. It does so by creating an attachment joint on the detected target. Whenever the magnet moves around, so to does the target. Special thanks to Sirkut for show how this is done! NOTE: The part must have a trigger collider to detect when the magnet touches a target part.
        
## Fields

### guiVisible
Sets the visibility state of the Part Action Window controls.
### debugMode
Flag to indicate if we should operate in debug mode
### ecPerSec
How much ElectricCharge per second is required to operate the magnet.
### magnetTransformName
Name of the magnet transform in the 3D mesh.
### groupID
Servo group ID. Default is "Magnet"
### servoName
Name of the servo. Used to identify it in the servo manager and the sequence file.
### targetName
Name of the target detected via the trigger
### attachEffectName
Name of the effect to play when the magnet attaches to a part.
### detachEffectName
Name of the effect to play when the magnet detaches from a part.
### runningEffectName
Name of the effect to play while the magnet is activated.
### magnetActivated
Field to indicate whether or not the magnet is on. You won't pick up parts with the magnet turned off...
## Methods


### UpdateTargetData(UnityEngine.Collision)
Determines the target part based upon the supplied Collision object. If the magnet is on and the attachment joint hasn't been created, then it creates the attachment joint. Othwerwise, if the magnet is off and the attachment joint is created, then it removes the joint.
> #### Parameters
> **collision:** A Collision object containing collision data. Usually comes from an OnCollision event.


### CreateAttachmentJoint
Creates the attachment joint if there is a target part and we have a magnetTransform.

### RemoveAttachmentJoint
Removes a previously created attachment joint.

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


### IsMoving
Determines whether or not the servo is moving
> #### Return value
> True if the servo is moving, false if not.

### StopMoving
Tells the servo to stop moving.

### GetGroupID
Returns the group ID of the servo. Used by the servo manager to know what servos it controlls.
> #### Return value
> A string containing the group ID

