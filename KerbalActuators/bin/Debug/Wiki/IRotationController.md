            
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


