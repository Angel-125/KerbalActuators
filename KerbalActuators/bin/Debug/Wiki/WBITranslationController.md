            
Instead of rotating a mesh transform, the WBITranslationContorller can move the mesh around along its X, Y, and Z axis.
        
## Fields

### guiVisible
Is the GUI visible in the Part Action Window (PAW).
### servoName
User-friendly name of the servo. Default is "Actuator."
### groupID
GroupID is used to separate controllers by group. It enables you to have more than one servo manager on a part, and each servo manager controls a separate group.
### meshTransformName
Name of the transform to move around.
### movementAxis
Axis along which to move the mesh.
### hasMinDistance
Flag to indicate if the mesh can move "left" of its neutral position. "Neutral" is where the mesh is when first loaded into the game before any translation is applied. Default: true.
### minDistance
Minimum distance in meters that the mesh is allowed to traverse. minDistance-----neutral (0)-----maxDistance.
### hasMaxDistance
Flag to indicate if the mesh can move "right" of its neutral position. "Neutral" is where the mesh is when first loaded into the game before any translation is applied. Default: true
### maxDistance
Maximum distance in meters that the mesh is allowed to traverse. minDistance-----neutral (0)-----maxDistance.
### velocityMetersPerSec
The rate in meters per second that the mesh may move. Can be overriden by the user.
### currentPosition
Current relative position of the mesh.
### targetPosition
Target position of the mesh.
### movementState
Current movement state.
### runningEffectName
Name of the effect to play while a servo controller is running. Uses the standard EFFECTS node found in the part config.
### status
User-friendly status display.
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


### IsMoving
Determines whether or not the servo is moving
> #### Return value
> True if the servo is moving, false if not.

### StopMoving
Tells the servo to stop moving.

