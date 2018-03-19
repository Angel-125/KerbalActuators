            
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

