            
The WBIRotationController handles the rotation of mesh transforms under its control. It is useful for things like rotating sections of a robot arm or engine nacelles.
        
## Fields

### guiVisible
Is the GUI visible in the Part Action Window (PAW).
### servoName
User-friendly name of the servo. Default is "Actuator."
### groupID
GroupID is used to separate controllers by group. It enables you to have more than one servo manager on a part, and each servo manager controls a separate group.
### rotationMeshName
Name of the mesh transform that the rotator will rotate.
### rotationMeshAxis
Axis of rotation for the mesh transform.
### rotateNeutralName
User-friendly text to rotate the rotation mesh to its neutral position.
### rotateMinName
User-friendly text to rotate the rotation mesh to its minimum rotation angle.
### canRotateMin
Indicates whether or not the rotator has a minimum rotation angle.
### minRotateAngle
If the rotator has a minimum rotation angle, then this field specifies what that minimum angle is.
### rotateMaxName
User-friendly text to rotate the rotation mesh to its maximum rotation angle.
### canRotateMax
Indicates whether or not the rotator has a maximum rotation angle.
### maxRotateAngle
If the rotator has a maximum rotation angle, then this field specifies what that maximum angle is.
### rotationDegPerSec
The rate, in degrees per second, that the rotation occurs.
### canMirrorRotation
Indicates whether or not the rotator can mirror its rotation.
### normalRotationName
User-friendly text for the mirror rotation event. This is for the normal rotation.
### mirrorRotationName
User-friendly text for the mirror rotation event. This is for the mirrored rotation.
### mirrorRotation
Indicates whether or not the rotation is mirrored.
### state
Current state of the rotator
### currentRotationAngle
Current rotation angle in degrees.
### currentAngleDisplay
A user-friendly version of the current rotation angle.
### targetAngle
The angle that we want to rotate to.
### rotationStateInt
Current rotation state from ERotationStates.
### runningEffectName
Name of the effect to play while a servo controller is running. Uses the standard EFFECTS node found in the part config.
## Methods


### MirrorRotation
Tells the rotator to mirror its rotation. This is helpful when making, say, a tilt-rotor engine, and making sure that each nacelle rotates in the proper direction.

### ActionRotateToMin(KSPActionParam)
Action that rotates the mesh transform to its minimum angle.
> #### Parameters
> **param:** A KSPActionParam containing state information.


### RotateToMin
This event tells the rotator to rotate to its minimum angle.

### RotateMin(System.Boolean)
Rotates the mesh transform to its minimum angle
> #### Parameters
> **applyToCounterparts:** True if it should tell its counterparts to rotate to minumum as well.


### ActionRotateToMax(KSPActionParam)
Action that rotates the mesh transform to its maximum angle.
> #### Parameters
> **param:** A KSPActionParam containing state information.


### RotateToMax
This event tells the rotator to rotate to its maximum angle.

### RotateMax(System.Boolean)
Rotates the mesh transform to its maximum angle
> #### Parameters
> **applyToCounterparts:** True if it should tell its counterparts to rotate to maximum as well.


### ActionRotateToNeutral(KSPActionParam)
Tells the rotator to rotate the mesh transform to its neutral angle.
> #### Parameters
> **param:** A KSPActionParam containing state information.


### RotateToNeutral
This event tells the rotator to rotate the mesh transform to its neutral angle.

### CanRotateMin
Determines whether or not the rotator can rotate to a minimum angle.
> #### Return value
> True if the rotator can rotate to a minimum angle, false if not.

### CanRotateMax
Determines whether or not the rotator can rotate to a maximum angle.
> #### Return value
> True if the rotator can rotate to a maximum angle, false if not.

### RotateNeutral(System.Boolean)
Tells the rotator to rotate to the neutral angle. Typically this angle is 0.
> #### Parameters
> **applyToCounterparts:** True if the rotator should tell its counterparts to rotate to the neutral angle as well, false if not.


### SetRotation(System.Single)
Sets the desired rotation angle. The mesh transform will rotate at the rotator's rotation speed.
> #### Parameters
> **rotationAngle:** The desired rotation angle from 0 to 360 degrees.


### RotateUp(System.Single)
Rotates up by the specified amount. "Up" is subjective; an engine nacelle might rotate vertical, while an arm might rotate left.
> #### Parameters
> **rotationDelta:** The amount to rotate, in degrees.


### RotateDown(System.Single)
Rotates down by the specified amount. "Down" is subjective; an engine nacelle might rotate horizontal, while an arm might rotate right.
> #### Parameters
> **rotationDelta:** The amount to rotate, in degrees.


### SetDegreesPerSec(System.Single)
Sets the desired rotation rate in degrees per second.
> #### Parameters
> **degPerSec:** The new rotation rate in degrees per second.


### updateCounterparts
Updates the counterparts with state information from the rotator.

### SetGUIVisible(System.Boolean)
Hides or shows the GUI controls in the Part Action Window.
> #### Parameters
> **isVisible:** True if the GUI controls should be visible, false if not.


### setInitialRotation
Sets the initial rotation without bothering to rotate at a specific rate. This method is used during startup.

### HideGUI
Hides the GUI controls in the Part Action Window.

### GetGroupID
Returns the group ID of the servo. Used by the servo manager to know what servos it controlls.
> #### Return value
> A string containing the group ID

### GetPanelHeight
Returns the panel height for the servo manager's GUI.
> #### Return value
> An Int containing the height of the panel.

### TakeSnapshot
Takes a snapshot of the current state of the servo.
> #### Return value
> A SERVODATA_N