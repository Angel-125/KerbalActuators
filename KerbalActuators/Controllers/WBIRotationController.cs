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
    #region ERotationStates
    /// <summary>
    /// Rotation states for the WBIRotationController
    /// </summary>
    public enum ERotationStates
    {
        /// <summary>
        /// Rotation is locked.
        /// </summary>
        Locked,

        /// <summary>
        /// Rotating upward. "Up" is determined by the controller.
        /// </summary>
        RotatingUp,

        /// <summary>
        /// Rotating downward. "Down" is determined by the controller.
        /// </summary>
        RotatingDown,

        /// <summary>
        /// Spinning right round like a record baby...
        /// </summary>
        Spinning,

        /// <summary>
        /// Rotation is slowing down.
        /// </summary>
        SlowingDown
    }
    #endregion

    /// <summary>
    /// Event delegate to indicate that the rotator should be mirrored.
    /// </summary>
    /// <param name="isMirrored">True if mirrored, false if not.</param>
    public delegate void RotatorMirroredEvent(bool isMirrored);

    #region IRotationController
    /// <summary>
    /// Interface for a rotation controller. Derives from IServoController.
    /// </summary>
    public interface IRotationController : IServoController
    {
        /// <summary>
        /// Indicates whether or not the rotator can rotate to the maximum value. Usually this will be true if the rotator has a maximum rotation angle.
        /// </summary>
        /// <returns>True if the rotator can rotate to maximum, false if not.</returns>
        bool CanRotateMax();

        /// <summary>
        /// Indicates whether or not the rotator can rotate to the minimum value. Usually this will be true if the rotator has a minimum rotation.
        /// </summary>
        /// <returns>True if the rotator can rotate to minimum, false if not.</returns>
        bool CanRotateMin();

        /// <summary>
        /// Tells the rotator to rotate down. "Down" can be whatever the rotator decides it is.
        /// </summary>
        /// <param name="rotationDelta">How many degrees to rotate.</param>
        void RotateDown(float rotationDelta);

        /// <summary>
        /// Tells the rotator to rotate up. "Up" can be whatever the rotator decides it is.
        /// </summary>
        /// <param name="rotationDelta">How many degrees to rotate.</param>
        void RotateUp(float rotationDelta);

        /// <summary>
        /// Rotates to the rotator's neutral position.
        /// </summary>
        /// <param name="applyToCounterparts">True if the rotator should also rotate its counterparts.</param>
        void RotateNeutral(bool applyToCounterparts = true);

        /// <summary>
        /// Rotates the rotator to its minimum angle (if any).
        /// </summary>
        /// <param name="applyToCounterparts">True if the rotator should also rotate its counterparts.</param>
        void RotateMin(bool applyToCounterparts = true);

        /// <summary>
        /// Rotates the rotator to its maximum angle (if any)
        /// </summary>
        /// <param name="applyToCounterparts">True if the rotator should also rotate its counterparts.</param>
        void RotateMax(bool applyToCounterparts = true);
    }
    #endregion

    /// <summary>
    /// The WBIRotationController handles the rotation of mesh transforms under its control. It is useful for
    /// things like rotating sections of a robot arm or engine nacelles.
    /// </summary>
    [KSPModule("Rotator")]
    public class WBIRotationController : PartModule, IRotationController
    {
        const int kPanelHeight = 130;
        const string kRotating = "Rotating";
        const string kLocked = "Locked";

        #region Fields
        /// <summary>
        /// Is the GUI visible in the Part Action Window (PAW).
        /// </summary>
        [KSPField]
        public bool guiVisible = true;

        /// <summary>
        /// User-friendly name of the servo. Default is "Actuator."
        /// </summary>
        [KSPField]
        public string servoName = "Actuator";

        /// <summary>
        /// GroupID is used to separate controllers by group. It enables you to have more than one servo manager on a part, and each servo manager
        /// controls a separate group.
        /// </summary>
        [KSPField]
        public string groupID = "Engine";

        /// <summary>
        /// Name of the mesh transform that the rotator will rotate.
        /// </summary>
        [KSPField]
        public string rotationMeshName;

        /// <summary>
        /// Axis of rotation for the mesh transform.
        /// </summary>
        [KSPField]
        public string rotationMeshAxis = "1,0,0";

        /// <summary>
        /// User-friendly text to rotate the rotation mesh to its neutral position.
        /// </summary>
        [KSPField]
        public string rotateNeutralName = "Rotate to Neutral";

        /// <summary>
        /// User-friendly text to rotate the rotation mesh to its minimum rotation angle.
        /// </summary>
        [KSPField]
        public string rotateMinName = "Rotate to Minimum";

        /// <summary>
        /// Indicates whether or not the rotator has a minimum rotation angle.
        /// </summary>
        [KSPField]
        public bool canRotateMin = true;

        /// <summary>
        /// If the rotator has a minimum rotation angle, then this field specifies what that minimum angle is.
        /// </summary>
        [KSPField()]
        public float minRotateAngle = -1f;

        /// <summary>
        /// User-friendly text to rotate the rotation mesh to its maximum rotation angle.
        /// </summary>
        [KSPField]
        public string rotateMaxName = "Rotate to Maximum";

        /// <summary>
        /// Indicates whether or not the rotator has a maximum rotation angle.
        /// </summary>
        [KSPField]
        public bool canRotateMax = true;

        /// <summary>
        /// If the rotator has a maximum rotation angle, then this field specifies what that maximum angle is.
        /// </summary>
        [KSPField()]
        public float maxRotateAngle = -1f;

        /// <summary>
        /// The rate, in degrees per second, that the rotation occurs.
        /// </summary>
        [KSPField]
        public float rotationDegPerSec = 15f;

        /// <summary>
        /// Indicates whether or not the rotator can mirror its rotation.
        /// </summary>
        [KSPField]
        public bool canMirrorRotation = true;

        /// <summary>
        /// User-friendly text for the mirror rotation event. This is for the normal rotation.
        /// </summary>
        [KSPField]
        public string normalRotationName = "Mirror: Is left engine";

        /// <summary>
        /// User-friendly text for the mirror rotation event. This is for the mirrored rotation.
        /// </summary>
        [KSPField]
        public string mirrorRotationName = "Mirror: Is right engine";

        /// <summary>
        /// Indicates whether or not the rotation is mirrored.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool mirrorRotation;

        /// <summary>
        /// Current state of the rotator
        /// </summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "State")]
        public string state;

        /// <summary>
        /// Current rotation angle in degrees.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float currentRotationAngle;

        /// <summary>
        /// A user-friendly version of the current rotation angle.
        /// </summary>
        [KSPField(guiActiveEditor = true, guiActive = true, guiName = "Angle", guiFormat = "f2", guiUnits = "deg.")]
        public float currentAngleDisplay;

        /// <summary>
        /// The angle that we want to rotate to.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float targetAngle;

        /// <summary>
        /// Current rotation state from ERotationStates.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int rotationStateInt = 0;
        public ERotationStates rotationState = ERotationStates.Locked;

        /// <summary>
        /// Name of the effect to play while a servo controller is running. Uses the standard EFFECTS node found in the part config.
        /// </summary>
        [KSPField]
        public string runningEffectName = string.Empty;

        /// <summary>
        /// Event to indicate that the rotator was mirrored.
        /// </summary>
        public event RotatorMirroredEvent onRotatorMirrored;
        #endregion

        #region Housekeeping
        protected Transform rotationTarget = null;
        protected Vector3 rotationVector;
        protected float degPerUpdate;
        protected string targetAngleText = "";

        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };
        string degPerSecText = "15";
        #endregion

        #region API
        /// <summary>
        /// Tells the rotator to mirror its rotation. This is helpful when making, say, a tilt-rotor engine, and making sure that each nacelle rotates in the proper direction.
        /// </summary>
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

        /// <summary>
        /// Action that rotates the mesh transform to its minimum angle.
        /// </summary>
        /// <param name="param">A KSPActionParam containing state information.</param>
        [KSPAction("Rotate To Minimum")]
        public void ActionRotateToMin(KSPActionParam param)
        {
            RotateMin();
        }

        /// <summary>
        /// This event tells the rotator to rotate to its minimum angle.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate To Minimum")]
        public void RotateToMin()
        {
            RotateMin();
        }

        /// <summary>
        /// Rotates the mesh transform to its minimum angle
        /// </summary>
        /// <param name="applyToCounterparts">True if it should tell its counterparts to rotate to minumum as well.</param>
        public void RotateMin(bool applyToCounterparts = true)
        {
            if (currentRotationAngle == minRotateAngle)
                return;

            SetRotation(minRotateAngle);
            if (applyToCounterparts)
                updateCounterparts();
        }

        /// <summary>
        /// Action that rotates the mesh transform to its maximum angle.
        /// </summary>
        /// <param name="param">A KSPActionParam containing state information.</param>
        [KSPAction("Rotate To Maximum")]
        public void ActionRotateToMax(KSPActionParam param)
        {
            RotateMax();
        }

        /// <summary>
        /// This event tells the rotator to rotate to its maximum angle.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate To Maximum")]
        public void RotateToMax()
        {
            RotateMax();
        }

        /// <summary>
        /// Rotates the mesh transform to its maximum angle
        /// </summary>
        /// <param name="applyToCounterparts">True if it should tell its counterparts to rotate to maximum as well.</param>
        public void RotateMax(bool applyToCounterparts = true)
        {
            if (currentRotationAngle == maxRotateAngle)
                return;

            SetRotation(maxRotateAngle);
            if (applyToCounterparts)
                updateCounterparts();
        }

        /// <summary>
        /// Tells the rotator to rotate the mesh transform to its neutral angle.
        /// </summary>
        /// <param name="param">A KSPActionParam containing state information.</param>
        [KSPAction("Rotate To Neutral")]
        public void ActionRotateToNeutral(KSPActionParam param)
        {
            RotateNeutral();
        }

        /// <summary>
        /// This event tells the rotator to rotate the mesh transform to its neutral angle.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate To Neutral")]
        public void RotateToNeutral()
        {
            RotateNeutral();
        }

        /// <summary>
        /// Determines whether or not the rotator can rotate to a minimum angle.
        /// </summary>
        /// <returns>True if the rotator can rotate to a minimum angle, false if not.</returns>
        public bool CanRotateMin()
        {
            return canRotateMin;
        }

        /// <summary>
        /// Determines whether or not the rotator can rotate to a maximum angle.
        /// </summary>
        /// <returns>True if the rotator can rotate to a maximum angle, false if not.</returns>
        public bool CanRotateMax()
        {
            return canRotateMax;
        }

        /// <summary>
        /// Tells the rotator to rotate to the neutral angle. Typically this angle is 0.
        /// </summary>
        /// <param name="applyToCounterparts">True if the rotator should tell its counterparts to rotate to the neutral angle as well, false if not.</param>
        public void RotateNeutral(bool applyToCounterparts = true)
        {
            if (currentRotationAngle == 0f)
                return;

            //Clear any current rotations
            if (rotationState != ERotationStates.Locked)
            {
                targetAngle = currentRotationAngle;
                rotationState = ERotationStates.Locked;
            }

            SetRotation(0f);
            if (applyToCounterparts)
                updateCounterparts();
        }

        /// <summary>
        /// Sets the desired rotation angle. The mesh transform will rotate at the rotator's rotation speed.
        /// </summary>
        /// <param name="rotationAngle">The desired rotation angle from 0 to 360 degrees.</param>
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

        /// <summary>
        /// Rotates up by the specified amount. "Up" is subjective; an engine nacelle might rotate vertical, while an arm might rotate left.
        /// </summary>
        /// <param name="rotationDelta">The amount to rotate, in degrees.</param>
        public void RotateUp(float rotationDelta)
        {
            //Clear any current rotations
            if (rotationState == ERotationStates.RotatingDown)
            {
                targetAngle = currentRotationAngle;
                rotationState = ERotationStates.Locked;
            }

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

        /// <summary>
        /// Rotates down by the specified amount. "Down" is subjective; an engine nacelle might rotate horizontal, while an arm might rotate right.
        /// </summary>
        /// <param name="rotationDelta">The amount to rotate, in degrees.</param>
        public void RotateDown(float rotationDelta)
        {
            if (targetAngle - rotationDelta < 0f && minRotateAngle == 0f)
                return;

            //Clear any current rotations
            if (rotationState == ERotationStates.RotatingUp)
            {
                targetAngle = currentRotationAngle;
                rotationState = ERotationStates.Locked;
            }

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

        /// <summary>
        /// Sets the desired rotation rate in degrees per second.
        /// </summary>
        /// <param name="degPerSec">The new rotation rate in degrees per second.</param>
        public void SetDegreesPerSec(float degPerSec)
        {
            rotationDegPerSec = degPerSec;

            //Calculate degrees per update
            degPerUpdate = rotationDegPerSec * TimeWarp.fixedDeltaTime;
        }

        /// <summary>
        /// Updates the counterparts with state information from the rotator.
        /// </summary>
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

        /// <summary>
        /// Hides or shows the GUI controls in the Part Action Window.
        /// </summary>
        /// <param name="isVisible">True if the GUI controls should be visible, false if not.</param>
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

        /// <summary>
        /// Sets the initial rotation without bothering to rotate at a specific rate. This method is used during startup.
        /// </summary>
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
        #endregion

        #region Overrides
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

        public virtual void FixedUpdate()
        {
            //calculate the new rotation angle
            switch (rotationState)
            {
                case ERotationStates.RotatingUp:
                    currentRotationAngle += degPerUpdate;
                    currentRotationAngle = currentRotationAngle % 360.0f;
                    if (!string.IsNullOrEmpty(runningEffectName))
                        this.part.Effect(runningEffectName, 1.0f);
                    break;

                case ERotationStates.RotatingDown:
                    currentRotationAngle -= degPerUpdate;
                    if (currentRotationAngle < 0f)
                        currentRotationAngle = 360f - currentRotationAngle;
                    if (!string.IsNullOrEmpty(runningEffectName))
                        this.part.Effect(runningEffectName, 1.0f);
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
        #endregion

        #region IServoController
        /// <summary>
        /// Hides the GUI controls in the Part Action Window.
        /// </summary>
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

        /// <summary>
        /// Returns the group ID of the servo. Used by the servo manager to know what servos it controlls.
        /// </summary>
        /// <returns>A string containing the group ID</returns>
        public string GetGroupID()
        {
            return groupID;
        }

        /// <summary>
        /// Returns the panel height for the servo manager's GUI.
        /// </summary>
        /// <returns>An Int containing the height of the panel.</returns>
        public int GetPanelHeight()
        {
            return kPanelHeight;
        }

        /// <summary>
        /// Takes a snapshot of the current state of the servo.
        /// </summary>
        /// <returns>A SERVODATA_NODE ConfigNode containing the servo's state</returns>
        public ConfigNode TakeSnapshot()
        {
            ConfigNode node = new ConfigNode(WBIServoManager.SERVODATA_NODE);

            node.AddValue("servoName", servoName);
            node.AddValue("currentRotationAngle", currentRotationAngle);
            node.AddValue("rotationDegPerSec", rotationDegPerSec);
            return node;
        }

        /// <summary>
        /// Sets the servo's state based upon the supplied config node.
        /// </summary>
        /// <param name="node">A SERVODAT_NODE ConfigNode containing servo state data.</param>
        public void SetFromSnapshot(ConfigNode node)
        {
            float setAngle;

            if (float.TryParse(node.GetValue("currentRotationAngle"), out setAngle))
            {
                SetRotation(setAngle);
            }

            float.TryParse(node.GetValue("rotationDegPerSec"), out rotationDegPerSec);
        }

        /// <summary>
        /// Determines whether or not the servo is moving
        /// </summary>
        /// <returns>True if the servo is moving, false if not.</returns>
        public bool IsMoving()
        {
            if (rotationState == ERotationStates.Locked)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Tells the servo to stop moving.
        /// </summary>
        public void StopMoving()
        {
            rotationState = ERotationStates.Locked;
            rotationStateInt = (int)rotationState;
            state = kLocked;

            //Update display
            currentAngleDisplay = currentRotationAngle;
        }

        /// <summary>
        /// Tells the servo to draw its GUI controls. It's used by the servo manager.
        /// </summary>
        public void DrawControls()
        {
            float setAngle;

            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(scrollVector, panelOptions);

            //Rotator name
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=white>" + servoName + "</color></b>");
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
                if (GUILayout.Button(ServoGUI.minIcon, ServoGUI.buttonOptions))
                    RotateToMin();
            }

            if (GUILayout.RepeatButton(ServoGUI.backIcon, ServoGUI.buttonOptions))
            {
                RotateDown(rotationDegPerSec * TimeWarp.fixedDeltaTime);
                if (!string.IsNullOrEmpty(runningEffectName))
                    this.part.Effect(runningEffectName, 1.0f);
            }

            if (GUILayout.Button(ServoGUI.homeIcon, ServoGUI.buttonOptions))
                RotateToNeutral();

            if (GUILayout.RepeatButton(ServoGUI.forwardIcon, ServoGUI.buttonOptions))
            {
                RotateUp(rotationDegPerSec * TimeWarp.fixedDeltaTime);
                if (!string.IsNullOrEmpty(runningEffectName))
                    this.part.Effect(runningEffectName, 1.0f);
            }

            if (maxRotateAngle != -1.0f)
            {
                if (GUILayout.Button(ServoGUI.maxIcon, ServoGUI.buttonOptions))
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
        #endregion
    }
}
