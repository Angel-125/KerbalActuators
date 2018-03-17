using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Text.RegularExpressions;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
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
        RotatingDown,
        Spinning,
        SlowingDown
    }

    public delegate void RotatorMirroredEvent(bool isMirrored);

    public interface IRotationController : IServoController
    {
        bool CanRotateMax();
        bool CanRotateMin();
        void RotateDown(float rotationDelta);
        void RotateUp(float rotationDelta);
        void RotateNeutral(bool applyToCounterparts = true);
        void RotateMin(bool applyToCounterparts = true);
        void RotateMax(bool applyToCounterparts = true);
    }
    
    [KSPModule("Rotator")]
    public class WBIRotationController : PartModule, IRotationController
    {
        const int kPanelHeight = 130;
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

        [KSPField]
        public bool canMirrorRotation = true;

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

        /// <summary>
        /// Name of the effect to play while a servo controller is running
        /// </summary>
        [KSPField]
        public string runningEffectName = string.Empty;

        public event RotatorMirroredEvent onRotatorMirrored;
        
        protected Transform rotationTarget = null;
        protected Vector3 rotationVector;
        protected float degPerUpdate;
        protected string targetAngleText = "";

        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };
        string degPerSecText = "15";

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
            if (currentRotationAngle == minRotateAngle)
                return;

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
            if (currentRotationAngle == maxRotateAngle)
                return;

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

        public string GetGroupID()
        {
            return groupID;
        }

        public bool CanRotateMin()
        {
            return canRotateMin;
        }

        public bool CanRotateMax()
        {
            return canRotateMax;
        }

        public void RotateNeutral(bool applyToCounterparts = true)
        {
            if (currentRotationAngle == 0f)
                return;

            SetRotation(0f);
            if (applyToCounterparts)
                updateCounterparts();
        }

        public void SetRotation(float rotationAngle)
        {
            if (rotationAngle == currentRotationAngle)
                return;

            //Angles go from 0 to 360
            rotationAngle = rotationAngle % 360.0f;
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
            {
                rotationState = ERotationStates.RotatingUp;
            }

            //We're at 0, we going to 270: rotate down
            else if (targetAngle > currentRotationAngle && targetAngle <= minRotateAngle)
            {
                rotationState = ERotationStates.RotatingDown;
            }

            else
            {
                if ((targetAngle - currentRotationAngle + 360f) % 360f <= 180f)
                    rotationState = ERotationStates.RotatingUp;
                else
                    rotationState = ERotationStates.RotatingDown;
            }

            //Update state
            rotationStateInt = (int)rotationState;
            state = kRotating;
        }

        public void RotateUp(float rotationDelta)
        {
            targetAngle += rotationDelta;
            targetAngle = targetAngle % 360.0f;

            //Make sure the new target angle is in bounds: between minRotateAngle and maxRotateAngle.
            if (targetAngle > maxRotateAngle && targetAngle < minRotateAngle)
                targetAngle = maxRotateAngle;
            else if (targetAngle > maxRotateAngle && minRotateAngle == 0f)
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
                targetAngle = 360f - Mathf.Abs(targetAngle);

            //Make sure the new target angle is in bounds: between minRotateAngle and maxRotateAngle.
            if (targetAngle > maxRotateAngle && targetAngle < minRotateAngle)
                targetAngle = minRotateAngle;

            if (currentRotationAngle == targetAngle)
                return;

            SetRotation(targetAngle);
        }

        public void SetDegreesPerSec(float degPerSec)
        {
            rotationDegPerSec = degPerSec;

            //Calculate degrees per update
            degPerUpdate = rotationDegPerSec * TimeWarp.fixedDeltaTime;
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
            Events["MirrorRotation"].guiActiveEditor = canMirrorRotation;
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
                Events["MirrorRotation"].guiActiveEditor = canMirrorRotation;
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

        public virtual void HideGUI()
        {
            Fields["state"].guiActive = false;
            Fields["currentAngleDisplay"].guiActive = false;
            Fields["state"].guiActiveEditor = false;
            Fields["currentAngleDisplay"].guiActiveEditor = false;
            Events["RotateToMin"].guiActive = false;
            Events["RotateToMin"].guiActiveEditor = false;
            Events["RotateToMax"].guiActive = false;
            Events["RotateToMax"].guiActiveEditor = false;
            Events["RotateToNeutral"].guiActive = false;
            Events["RotateToNeutral"].guiActiveEditor = false;
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
            {
                this.part.Effect("runningServo", -1.0f);
                return;
            }

            //calculate the new rotation angle
            switch (rotationState)
            {
                case ERotationStates.RotatingUp:
                    currentRotationAngle += degPerUpdate;
                    currentRotationAngle = currentRotationAngle % 360.0f;
//                    if (onServoMoving != null)
//                        onServoMoving(this);
                    break;

                case ERotationStates.RotatingDown:
                    currentRotationAngle -= degPerUpdate;
                    if (currentRotationAngle < 0f)
                        currentRotationAngle = 360f - currentRotationAngle;
//                    if (onServoMoving != null)
//                        onServoMoving(this);
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

        public int GetPanelHeight()
        {
            return kPanelHeight;
        }

        public ConfigNode TakeSnapshot()
        {
            ConfigNode node = new ConfigNode(WBIServoManager.SERVODATA_NODE);

            node.AddValue("currentRotationAngle", currentRotationAngle);
            node.AddValue("rotationDegPerSec", rotationDegPerSec);

            return node;
        }

        public void SetFromSnapshot(ConfigNode node)
        {
            float setAngle;

            if (float.TryParse(node.GetValue("currentRotationAngle"), out setAngle))
            {
                SetRotation(setAngle);
            }

            float.TryParse(node.GetValue("rotationDegPerSec"), out rotationDegPerSec);
        }

        public bool IsMoving()
        {
            if (rotationState == ERotationStates.Locked)
                return false;
            else
                return true;
        }

        public void DrawControls()
        {
            float setAngle;

            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(scrollVector, panelOptions);

            //Rotator name
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=white>" + rotatorName + "</color></b>");
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format("<color=white><b>Angle: </b>{0:f2}</color>", currentRotationAngle));
            GUILayout.EndHorizontal();

            //Rotation speed
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white>Rotate:</color>");

            int degPerSecInt = (int)rotationDegPerSec;
            degPerSecText = degPerSecInt.ToString();
            degPerSecText = GUILayout.TextField(degPerSecText);
            degPerSecText = Regex.Replace(degPerSecText, @"[^0-9]", "");
            float.TryParse(degPerSecText, out rotationDegPerSec);
            degPerUpdate = rotationDegPerSec * TimeWarp.fixedDeltaTime;

            GUILayout.Label("<color=white>deg/s</color>");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            //Rotation controls
            if (minRotateAngle != -1.0f)
            {
                if (GUILayout.Button("Min"))
                    RotateToMin();
            }

            if (GUILayout.RepeatButton("<"))
            {
                RotateDown(rotationDegPerSec * TimeWarp.fixedDeltaTime);
                if (!string.IsNullOrEmpty(runningEffectName))
                    this.part.Effect(runningEffectName, 1.0f);
            }

            if (GUILayout.Button("0"))
                RotateToNeutral();

            if (GUILayout.RepeatButton(">"))
            {
                RotateUp(rotationDegPerSec * TimeWarp.fixedDeltaTime);
                if (!string.IsNullOrEmpty(runningEffectName))
                    this.part.Effect(runningEffectName, 1.0f);
            }

            if (maxRotateAngle != -1.0f)
            {
                if (GUILayout.Button("Max"))
                    RotateToMax();
            }

            GUILayout.EndHorizontal();

            //Specific target angle
            GUILayout.BeginHorizontal();

            GUILayout.Label("<color=white>Angle:</color>");
            targetAngleText = GUILayout.TextField(targetAngleText);
            targetAngleText = Regex.Replace(targetAngleText, @"[^0-9]", "");
            
            //Make sure we're in bounds
            if (float.TryParse(targetAngleText, out setAngle))
            {
                if (minRotateAngle != -1 && maxRotateAngle != -1)
                {
                    if (setAngle > maxRotateAngle && setAngle < minRotateAngle)
                    {
                        if (setAngle <= 180.0f)
                            setAngle = maxRotateAngle;
                        else
                            setAngle = minRotateAngle;

                        int angleInt = (int)setAngle;
                        targetAngleText = angleInt.ToString();
                    }
                }

                else
                {
                    setAngle = setAngle % 360.0f;
                    int angleInt = (int)setAngle;
                    targetAngleText = angleInt.ToString();
                }
            }

            if (GUILayout.Button("Set"))
                SetRotation(setAngle);

            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }
    }
}
