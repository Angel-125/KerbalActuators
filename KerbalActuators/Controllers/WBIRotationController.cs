using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace KerbalActuators
{
    public enum ERotationStates
    {
        Locked,
        RotatingUp,
        RotatingDown
    }

    public delegate void RotatorMirroredEvent(bool isMirrored);

    [KSPModule("Rotator")]
    public class WBIRotationController : PartModule
    {
        const string kRotating = "Rotating";
        const string kLocked = "Locked";

        [KSPField]
        public bool guiVisible = true;

        [KSPField]
        public string rotatorName = "Actuator";

        [KSPField]
        public string groupID = "Engine";

        [KSPField]
        public string normalRotationName = "Mirror: Is left engine";

        [KSPField]
        public string mirrorRotationName = "Mirror: Is right engine";

        [KSPField]
        public string rotationMeshName;

        [KSPField]
        public string rotationMeshAxis = "1,0,0";

        [KSPField]
        public string rotateNeutralName = "Rotate to Neutral";

        [KSPField]
        public string rotateMinName = "Rotate to Minimum";

        [KSPField]
        public bool canRotateMin = true;

        [KSPField()]
        public float minRotateAngle = -1f;

        [KSPField]
        public string rotateMaxName = "Rotate to Maximum";

        [KSPField]
        public bool canRotateMax = true;

        [KSPField()]
        public float maxRotateAngle = -1f;

        [KSPField]
        public float rotationDegPerSec = 15f;

        [KSPField(isPersistant = true)]
        public bool mirrorRotation;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "State")]
        public string state;

        [KSPField(isPersistant = true)]
        public float currentRotationAngle;

        [KSPField(isPersistant = true)]
        public float targetAngle;

        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "Angle", guiFormat = "f2", guiUnits = "deg.")]
        public float currentAngleDisplay;

        [KSPField(isPersistant = true)]
        public int rotationStateInt = 0;
        public ERotationStates rotationState = ERotationStates.Locked;

        public event RotatorMirroredEvent onRotatorMirrored;
        
        protected Transform rotationTarget = null;
        protected Vector3 rotationVector;
        protected float degPerUpdate;

        [KSPEvent(guiActiveEditor = true)]
        public virtual void MirrorRotation()
        {
            mirrorRotation = !mirrorRotation;

            if (mirrorRotation)
            {
                Events["MirrorRotation"].guiName = mirrorRotationName;
            }
            else
            {
                Events["MirrorRotation"].guiName = normalRotationName;
            }

            if (onRotatorMirrored != null)
                onRotatorMirrored(mirrorRotation);
        }

        [KSPAction("Rotate To Minimum")]
        public void ActionRotateToMin(KSPActionParam param)
        {
            RotateMin();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate To Minimum")]
        public void RotateToMin()
        {
            RotateMin();
        }

        public void RotateMin(bool applyToCounterparts = true)
        {
            SetRotation(minRotateAngle);
            if (applyToCounterparts)
                updateCounterparts();
        }

        [KSPAction("Rotate To Maximum")]
        public void ActionRotateToMax(KSPActionParam param)
        {
            RotateMax();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate To Maximum")]
        public void RotateToMax()
        {
            RotateMax();
        }

        public void RotateMax(bool applyToCounterparts = true)
        {
            SetRotation(maxRotateAngle);
            if (applyToCounterparts)
                updateCounterparts();
        }

        [KSPAction("Rotate To Neutral")]
        public void ActionRotateToNeutral(KSPActionParam param)
        {
            RotateNeutral();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate To Neutral")]
        public void RotateToNeutral()
        {
            RotateNeutral();
        }

        public void RotateNeutral(bool applyToCounterparts = true)
        {
            SetRotation(0f);
            if (applyToCounterparts)
                updateCounterparts();
        }

        public void SetRotation(float rotationAngle)
        {
            //Angles go from 0 to 360
            rotationAngle = Mathf.Clamp(rotationAngle, 0, 360);
            targetAngle = rotationAngle;

            //If we have no min/max limits, then just find the shortest path to the target angle.
            if (minRotateAngle == -1f && maxRotateAngle == -1f)
            {
                if ((targetAngle - currentRotationAngle + 360f) % 360f <= 180f)
                    rotationState = ERotationStates.RotatingUp;
                else
                    rotationState = ERotationStates.RotatingDown;

                //Update state
                rotationStateInt = (int)rotationState;
                state = kRotating;

                return;
            }

            //We need to figure out the shortest direction to rotate in.
            //If we have limits to our rotation, then that affects which direction we can rotate.
            //EX
            //min --------- max
            //270 --- 0 --- 90
            //If we're at 90 degrees and we want to go to 270, we have to rotate down instead of up
            //because going up would move us past our max rotation limit.

            //We're at 0, we going to 90: rotate up
            if (targetAngle > currentRotationAngle && targetAngle <= maxRotateAngle)
                rotationState = ERotationStates.RotatingUp;

            //We're at 0, we going to 270: rotate down
            else if (targetAngle > currentRotationAngle && targetAngle <= minRotateAngle)
                rotationState = ERotationStates.RotatingDown;

            //We're at 270, we going to 90: rotate up
            //Or we're at 90 and going to 0: find shortest route
            else if (targetAngle < currentRotationAngle && targetAngle <= maxRotateAngle)
            {
                //Find shortest route
                if ((targetAngle - currentRotationAngle + 360f) % 360f <= 180f)
                    rotationState = ERotationStates.RotatingUp;
                else
                    rotationState = ERotationStates.RotatingDown;
            }

            //Rotate down
            else
                rotationState = ERotationStates.RotatingDown;

            //Update state
            rotationStateInt = (int)rotationState;
            state = kRotating;
        }

        public void RotateUp(float rotationDelta)
        {
            targetAngle += rotationDelta % 360f;

            if (targetAngle >= maxRotateAngle)
                targetAngle = maxRotateAngle;

            if (currentRotationAngle == targetAngle)
                return;

            SetRotation(targetAngle);
        }

        public void RotateDown(float rotationDelta)
        {
            if (targetAngle - rotationDelta < 0f && minRotateAngle == 0f)
                return;

            targetAngle -= rotationDelta;

            if (targetAngle < 0f)
                targetAngle = 360f - targetAngle;
            if (targetAngle <= minRotateAngle)
                targetAngle = minRotateAngle;

            if (currentRotationAngle == targetAngle)
                return;

            SetRotation(targetAngle);
        }

        protected void updateCounterparts()
        {
            foreach (Part symmetryPart in this.part.symmetryCounterparts)
            {
                WBIRotationController rotator = symmetryPart.GetComponent<WBIRotationController>();
                if (rotator != null)
                {
                    rotator.targetAngle = targetAngle;
                    rotator.rotationState = rotationState;
                    rotationStateInt = (int)rotationState;
                    rotator.state = kRotating;
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Symmetry controls
            if (mirrorRotation)
                Events["MirrorRotation"].guiName = mirrorRotationName;
            else
                Events["MirrorRotation"].guiName = normalRotationName;

            if (string.IsNullOrEmpty(rotationMeshName))
            {
                Debug.Log("No rotation transform!");
                return;
            }

            //Get the rotation target
            rotationTarget = part.FindModelTransform(rotationMeshName);

            //Get the rotation axis
            if (string.IsNullOrEmpty(rotationMeshAxis) == false)
            {
                string[] axisValues = rotationMeshAxis.Split(',');
                float value;
                if (axisValues.Length == 3)
                {
                    if (float.TryParse(axisValues[0], out value))
                        rotationVector.x = value;
                    if (float.TryParse(axisValues[1], out value))
                        rotationVector.y = value;
                    if (float.TryParse(axisValues[2], out value))
                        rotationVector.z = value;
                }
            }

            //Get the rotation state
            rotationState = (ERotationStates)rotationStateInt;

            //Calculate degrees per update
            degPerUpdate = rotationDegPerSec * TimeWarp.fixedDeltaTime;

            //Set initial rotation
            setInitialRotation();

            //Set gui controls
            SetGUIVisible(guiVisible);
        }

        public virtual void SetGUIVisible(bool isVisible)
        {
            guiVisible = isVisible;

            if (isVisible)
            {
                Events["RotateToMin"].active = canRotateMin;
                Events["RotateToMin"].guiName = rotateMinName;
                Events["RotateToMax"].active = canRotateMax;
                Events["RotateToMax"].guiName = rotateMaxName;
                Events["RotateToNeutral"].guiName = rotateNeutralName;
                Actions["ActionRotateToMin"].active = canRotateMin;
                Actions["ActionRotateToMax"].active = canRotateMax;
            }

            else //Always allow editor controls
            {
                Events["RotateToMin"].guiActive = false;
                Events["RotateToMin"].guiActiveEditor = canRotateMin;
                Events["RotateToMax"].guiActive = false;
                Events["RotateToMax"].guiActiveEditor = canRotateMax;
                Events["RotateToNeutral"].guiActive = false;
            }
        }

        protected virtual void setInitialRotation()
        {
            if (rotationState == ERotationStates.Locked)
                state = kLocked;
            else
                state = kRotating;

            currentAngleDisplay = Mathf.Abs(currentRotationAngle);
            if (!mirrorRotation)
                rotationTarget.transform.localEulerAngles = (rotationVector * currentRotationAngle);
            else
                rotationTarget.transform.localEulerAngles = (rotationVector * -currentRotationAngle);

            targetAngle = currentRotationAngle;
        }

        public virtual void FixedUpdate()
        {
            if (rotationState == ERotationStates.Locked)
                return;

            //calculate the new rotation angle
            switch (rotationState)
            {
                case ERotationStates.RotatingUp:
                    currentRotationAngle += degPerUpdate;
                    currentRotationAngle = currentRotationAngle % 360.0f;
                    break;

                case ERotationStates.RotatingDown:
                    currentRotationAngle -= degPerUpdate;
                    if (currentRotationAngle < 0f)
                        currentRotationAngle = 360f - currentRotationAngle;

                    break;
            }

            //See if we've met our target
            if (currentRotationAngle > targetAngle - degPerUpdate && currentRotationAngle < targetAngle + degPerUpdate)
            {
                currentRotationAngle = targetAngle;
                rotationState = ERotationStates.Locked;
                rotationStateInt = (int)rotationState;
                state = kLocked;
            }

            //Update display
            currentAngleDisplay = currentRotationAngle;

            //Rotate the mesh
            if (!mirrorRotation)
                rotationTarget.transform.localEulerAngles = (rotationVector * currentRotationAngle);
            else
                rotationTarget.transform.localEulerAngles = (rotationVector * -currentRotationAngle);
        }
    }
}
