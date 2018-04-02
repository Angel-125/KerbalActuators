# KerbalActuators


# WBILightController
            
Derived from the stock ModuleLight, this controller lets you control a light through the Servo Manager. Unlike a stock light, you can change the color tint in flight.
        
## Fields

### guiVisible
Sets the visibility state of the Part Action Window controls.
### groupID
Servo group ID. Default is "Light"
### servoName
Name of the servo. Used to identify it in the servo manager and the sequence file.
## Methods


### ToggleLights
KSP event to toggle the lights on and off

### TurnOnLights
Turns on the lights

### TurnOffLights
Turns off the lights

### ToggleLights(System.Boolean)
Toggles the light animation On/Off
> #### Parameters
> **deployed:** Set to true to turn on lights of false to turn them off


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

### IsMoving
Returns whether or not the animation is moving
> #### Return value
> True if moving, false if not.

### StopMoving
Tells the servo to stop moving.

### TakeSnapshot
Takes a snapshot of the current state of the servo.
> #### Return value
> A SERVODATA_NODE ConfigNode containing the servo's state

### SetFromSnapshot(ConfigNode)
Sets the servo's state based upon the supplied config node.
> #### Parameters
> **node:** A SERVODAT_NODE ConfigNode containing servo state data.


# IServoController
            
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

### StopMoving
Tells the servo to stop moving.

# IHoverController
            
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

# HoverUpdateEvent
            
This event tells interested parties that the hover state has been updated.
            
> **hoverActive:** A flag to indicate whether or not the hover mode is active.

            
> **verticalSpeed:** A float value telling the interested party what the vertical speed is, in meters/second.

        

# WBIHoverController
            
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

# IAirParkController
            
This controller interface defines an air park controller. The controller lets you "park" a vessel in mid-air and treat it as if landed on the ground.
        
## Methods


### SetParking(System.Boolean)
Sets the parking mode.
> #### Parameters
> **parked:** True if parked, false if not.


### IsParked
Determines whether or not the vessel is parked.
> #### Return value
> True if parked, false if not.

### TogglePark
Toggles the parking state from parked to unparked.

### GetSituation
Returns the current situation of the vesel.
> #### Return value
> 

# WBIAirParkController
            
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

# WBIMagnetController
            
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

# WBIMovementState
            
This enum describes the current state of the translation controller.
        
## Fields

### Locked
Controller is locked and not moving.
### MovingForward
Controller is moving forward. "Forward" is relative to the axis of movement.
### MovingBackward
Controller is moving backward. "Backward" is relative to the axis of movement.

# WBITranslationController
            
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

# IPropSpinner
            
This interface is used to toggle the thrust transform of a prop spinner.
        
## Methods


### ToggleThrust
Toggles the thrust from forward to reverse and back again.

### SetReverseThrust(System.Boolean)
Sets the reverse thrust.
> #### Parameters
> **isReverseThrust:** True if reverse thrust, false if forward thrust.


# WBIPropSpinner
            
This class is designed to spin propeller meshes for propeller-driven engines. It supports both propeller blades and blurred propeller meshes.
        
## Fields

### forwardThrustActionName
Localized name of forward thrust action
### reverseThrustActionName
Localized name of reverse thrust action
### reverseThrust
Flag to indicate if the controller is operating in reverse-thrust mode.
### thrustTransform
Name of the thrust transform for forward thrust.
### reverseThrustTransform
Name of the thrust transform for reverse-thrust.
### canReverseThrust
Flag to indicate whether or not the controller can reverse thrust.
### reverseThrustAnimation
Name of animation for reversed thrust
### animationLayer
Layer of the animation
### rotorTransformName
Name of the non-blurred rotor. The whole thing spins including any child meshes.
### standardBladesName
(Optional) To properly mirror the engine, these parameters specify the standard and mirrored (symmetrical) rotor blade transforms. If included, they MUST be child meshes of the mesh specified by rotorTransformName.
### mirrorBladesName
Name of the mirrored rotor blades
### rotorRotationAxis
Rotor axis of rotation
### rotorRPM
How fast to spin the rotor
### rotorSpoolTime
How fast to spin up or slow down the rotors until they reach rotorRPM
### blurredRotorFactor
How fast to spin the rotor when blurred; multiply rotorRPM by blurredRotorFactor
### minThrustRotorBlur
At what percentage of thrust to switch to the blurred rotor/mesh rotor.
### blurredRotorName
Name of the blurred rotor
### blurredRotorRPM
How fast to spin the blurred rotor
### isBlurred
Is the rotor system currently blurred.
### mirrorRotation
Flag to indicate that the rotors are mirrored.
### isHovering
Flag to indicate that the controller should be in hover mode.
### guiVisible
Flag to indicate whether or not the Part Action Window gui controls are visible.
### actionsVisible
Flag to indicate whether or not part module actions are visible.
### neutralSpinRate
During the shutdown process, how fast, in degrees/sec, do the rotors rotate to neutral?
## Methods


### MirrorRotation(System.Boolean)
Sets mirrored rotation.
> #### Parameters
> **isMirrored:** True if rotation is mirrored, false if not.


### ToggleThrustTransformAction(KSPActionParam)
This action toggles the thrust transforms from forward to reverse and back.
> #### Parameters
> **param:** A KSPActionParam with action state information.


### ToggleThrustTransform
This event toggles the thrust transforms from forward to reverse and back. It also plays the thrust reverse animation, if any.

### SetReverseThrust(System.Boolean)
Sets the thrust mode and plays the associated reverse-thrust andimation if any.
> #### Parameters
> **isReverseThrust:** True if the thrust is reversed, false if not.


### ToggleThrust
Toggles the thrust from forward to back or back to forward and plays the animation, if any.

### SetGUIVisible(System.Boolean)
Shows or Hides the Part Action Window GUI controls associated with the controller.
> #### Parameters
> **isVisible:** True if the controls should be shown, false if not.


### SetupThrustTransform
Sets up the thrust transforms.

### SetupAnimation
Sets up the thrust animation.

### HandleReverseThrustAnimation
Plays the reverse thrust animation, if any.

# WBIServoManager
            
The Servo Manager is designed to manage the states of one or more servos located in the part. The part module should be placed after the last servo controller part module in the config file. The manager is responsible for presenting the individual servo GUI panels as well as the GUI needed to create, load, update, delete, and play various sequences. These sequences are a way to programmatically control the positioning of various servos without having to manually enter in their positions.
        
## Fields

### maxWindowHeight
Maximum height of the GUI
### managerState
Current state of the manager
### sequenceID
Current sequence that's being played.
### snapshotID
Current snapshot
### runningEffectName
Name of the effect to play while a servo controller is running
## Methods


### ToggleGUI
This event shows or hides the servo manager GUI.

### TakeSnapshot
Takes a snapshot of the current state of the servo controllers
> #### Return value
> A SNAPSHOT ConfigNode containing the current state of the servo controllers

### PlaySequence(System.Int32)
Plays the desired sequence.
> #### Parameters
> **sequenceIndex:** An integer containing the desired sequence index.


### PlayHomeSequence
Plays the home sequence. Home sequence is the "stored" state of the part's servos.

### PlaySnapshot(System.Collections.Generic.List{ConfigNode})
Plays a list of supplied snapshots
> #### Parameters
> **snapshotList:** A list containing SNAPSHOT ConfigNode objects to play.


### PlaySnapshot(ConfigNode)
Plays a single snapshot
> #### Parameters
> **snapshotNode:** A SNAPSHOT ConfigNode containing servo state information


### PlaySnapshot(System.Int32)
Plays the desired snapshot from the current sequence
> #### Parameters
> **snapshotIndex:** An integer containing the desired snampshot index.


### AddSequence(ConfigNode)
Adds a new sequence node to the sequence list.
> #### Parameters
> **node:** A SEQUENCE_NODE ConfigNode containing the sequence to add.


### CreateHomeSequence
Uses the current servo states to define the "Home" sequence. When the user presses the Home button, the part's servos will return the mesh transforms to this recorded state.

### CreateHomeSequence(ConfigNode)
Creates a home sequence from the supplied config node.
> #### Parameters
> **node:** A SEQUENCE_NOD ConfigNode containing the new home sequence


### StopAllServos
Immediately stops all servos from moving.

# ERotationStates
            
Rotation states for the WBIRotationController
        
## Fields

### Locked
Rotation is locked.
### RotatingUp
Rotating upward. "Up" is determined by the controller.
### RotatingDown
Rotating downward. "Down" is determined by the controller.
### Spinning
Spinning right round like a record baby...
### SlowingDown
Rotation is slowing down.

# RotatorMirroredEvent
            
Event delegate to indicate that the rotator should be mirrored.
            
> **isMirrored:** True if mirrored, false if not.

        

# IRotationController
            
Interface for a rotation controller. Derives from IServoController.
        
## Methods


### CanRotateMax
Indicates whether or not the rotator can rotate to the maximum value. Usually this will be true if the rotator has a maximum rotation angle.
> #### Return value
> True if the rotator can rotate to maximum, false if not.

### CanRotateMin
Indicates whether or not the rotator can rotate to the minimum value. Usually this will be true if the rotator has a minimum rotation.
> #### Return value
> True if the rotator can rotate to minimum, false if not.

### RotateDown(System.Single)
Tells the rotator to rotate down. "Down" can be whatever the rotator decides it is.
> #### Parameters
> **rotationDelta:** How many degrees to rotate.


### RotateUp(System.Single)
Tells the rotator to rotate up. "Up" can be whatever the rotator decides it is.
> #### Parameters
> **rotationDelta:** How many degrees to rotate.


### RotateNeutral(System.Boolean)
Rotates to the rotator's neutral position.
> #### Parameters
> **applyToCounterparts:** True if the rotator should also rotate its counterparts.


### RotateMin(System.Boolean)
Rotates the rotator to its minimum angle (if any).
> #### Parameters
> **applyToCounterparts:** True if the rotator should also rotate its counterparts.


### RotateMax(System.Boolean)
Rotates the rotator to its maximum angle (if any)
> #### Parameters
> **applyToCounterparts:** True if the rotator should also rotate its counterparts.


# WBIRotationController
            
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

### DrawControls
Tells the servo to draw its GUI controls. It's used by the servo manager.