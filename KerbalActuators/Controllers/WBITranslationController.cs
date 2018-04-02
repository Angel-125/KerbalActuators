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
    #region WBIMovementState
    /// <summary>
    /// This enum describes the current state of the translation controller.
    /// </summary>
    public enum WBIMovementState
    {
        /// <summary>
        /// Controller is locked and not moving.
        /// </summary>
        Locked,

        /// <summary>
        /// Controller is moving forward. "Forward" is relative to the axis of movement.
        /// </summary>
        MovingForward,

        /// <summary>
        /// Controller is moving backward. "Backward" is relative to the axis of movement.
        /// </summary>
        MovingBackward
    }
    #endregion

    /// <summary>
    /// Instead of rotating a mesh transform, the WBITranslationContorller can move the mesh around along its X, Y, and Z axis.
    /// </summary>
    public class WBITranslationController: PartModule, IServoController
    {
        const int kPanelHeight = 130;
        const string kMoving = "Moving";
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
        public string groupID = "Arm";

        /// <summary>
        /// Name of the transform to move around.
        /// </summary>
        [KSPField]
        public string meshTransformName = string.Empty;

        /// <summary>
        /// Axis along which to move the mesh.
        /// </summary>
        [KSPField]
        public string movementAxis = "0,1,0";

        /// <summary>
        /// Flag to indicate if the mesh can move "left" of its neutral position. "Neutral" is where the mesh is when first loaded into the game before any translation is applied. Default: true.
        /// </summary>
        [KSPField]
        public bool hasMinDistance = true;

        /// <summary>
        /// Minimum distance in meters that the mesh is allowed to traverse. minDistance-----neutral (0)-----maxDistance.
        /// </summary>
        [KSPField]
        public float minDistance = float.MinValue;

        /// <summary>
        /// Flag to indicate if the mesh can move "right" of its neutral position. "Neutral" is where the mesh is when first loaded into the game before any translation is applied. Default: true
        /// </summary>
        [KSPField]
        public bool hasMaxDistance = true;

        /// <summary>
        /// Maximum distance in meters that the mesh is allowed to traverse. minDistance-----neutral (0)-----maxDistance.
        /// </summary>
        [KSPField]
        public float maxDistance = float.MaxValue;

        /// <summary>
        /// The rate in meters per second that the mesh may move. Can be overriden by the user.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float velocityMetersPerSec = 1.0f;

        /// <summary>
        /// Current relative position of the mesh.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float currentPosition;

        /// <summary>
        /// Target position of the mesh.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float targetPosition;

        /// <summary>
        /// Current movement state.
        /// </summary>
        [KSPField(isPersistant = true)]
        WBIMovementState movementState;

        /// <summary>
        /// Name of the effect to play while a servo controller is running. Uses the standard EFFECTS node found in the part config.
        /// </summary>
        [KSPField]
        public string runningEffectName = string.Empty;

        /// <summary>
        /// User-friendly status display.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Status")]
        public string status;
        #endregion

        #region Housekeeping
        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };
        Transform meshTransform;
        float velocityPerUpdate = 0.0f;
        Vector3 translateAxis = Vector3.zero;
        Vector3 vecMaxPosition = Vector3d.zero;
        Vector3 vecMinPosition = Vector3.zero;
        Vector3 vecOriginalPos = Vector3.zero;
        Vector3 vecTargetPos = Vector3.zero;
        float newTargetPosition = 0.0f;
        string targetPositionText = "";
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            //Find the mesh transform
            meshTransform = this.part.FindModelTransform(meshTransformName);
            if (meshTransform == null)
                return;

            //Setup the translation axis
            if (string.IsNullOrEmpty(movementAxis) == false)
            {
                string[] axisValues = movementAxis.Split(',');
                float value;
                if (axisValues.Length == 3)
                {
                    if (float.TryParse(axisValues[0], out value))
                        translateAxis.x = value;
                    if (float.TryParse(axisValues[1], out value))
                        translateAxis.y = value;
                    if (float.TryParse(axisValues[2], out value))
                        translateAxis.z = value;
                }
            }

            //Setup min/max distance based on whether or not we have min/max distance.
            if (!hasMinDistance)
                minDistance = 0.0f;
            if (!hasMaxDistance)
                maxDistance = 0.0f;

            //Setup positions
            vecOriginalPos = meshTransform.localPosition;
            meshTransform.Translate(translateAxis * maxDistance);
            vecMaxPosition = meshTransform.localPosition;
            meshTransform.localPosition = vecOriginalPos;
            meshTransform.Translate(translateAxis * minDistance);
            vecMinPosition = meshTransform.localPosition;
            meshTransform.localPosition = vecOriginalPos;

            //Move to current position
            if (currentPosition != 0)
                meshTransform.Translate(translateAxis * currentPosition);

            //Setup status
            status = kLocked;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
                return;
            if (movementState == WBIMovementState.Locked)
                return;

            //Calculate movement delta and update state
            //Vector3.MoveTowards can also move meshTransform.localPosition but I've yet to figure out how to map the distance between meshTransform.localPosition and vecTargetPosition with minDistance and MaxDistance.
            float moveDelta = 0.0f;
            float distance = Vector3.Distance(vecMinPosition, meshTransform.localPosition);
            switch (movementState)
            {
                default:
                case WBIMovementState.Locked:
                    break;

                case WBIMovementState.MovingBackward:
                    moveDelta = -velocityPerUpdate;

                    //Now check for target
                    distance = Vector3.Distance(vecTargetPos, meshTransform.localPosition);
                    if (distance <= 0.01f || currentPosition <= targetPosition)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = targetPosition;

                        //For good measure, make sure our position is at the target
                        meshTransform.localPosition = vecTargetPos;
                        return;
                    }
                    break;

                case WBIMovementState.MovingForward:
                    moveDelta = velocityPerUpdate;

                    //Now check for target
                    distance = Vector3.Distance(vecTargetPos, meshTransform.localPosition);
                    if (distance <= 0.01f || currentPosition >= targetPosition)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = targetPosition;

                        //For good measure, make sure our position is at the target
                        meshTransform.localPosition = vecTargetPos;
                        return;
                    }

                    break;
            }

            //Play the moving sound
            if (!string.IsNullOrEmpty(runningEffectName))
                this.part.Effect(runningEffectName, 1.0f);

            //Update status
            status = kMoving;
            currentPosition += moveDelta;

            //Move the mesh
            meshTransform.Translate(translateAxis * moveDelta);
        }
        #endregion

        #region IServoController
        /// <summary>
        /// Returns the group ID of the servo. Used by the servo manager to know what servos it controlls.
        /// </summary>
        /// <returns>A string containing the group ID</returns>
        public string GetGroupID()
        {
            return groupID;
        }

        /// <summary>
        /// Tells the servo to draw its GUI controls. It's used by the servo manager.
        /// </summary>
        public void DrawControls()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginScrollView(scrollVector, panelOptions);

            //Info panel
            drawinfoPanel();

            //Movement controls
            GUILayout.BeginVertical();
            drawMovementControls();
            drawSetPosition();
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Hides the GUI controls in the Part Action Window.
        /// </summary>
        public void HideGUI()
        {
            Fields["status"].guiActive = false;
            Fields["status"].guiActiveEditor = false;
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
            node.AddValue("velocityMetersPerSec", velocityMetersPerSec);
            node.AddValue("targetPosition", targetPosition);

            return node;
        }

        /// <summary>
        /// Sets the servo's state based upon the supplied config node.
        /// </summary>
        /// <param name="node">A SERVODAT_NODE ConfigNode containing servo state data.</param>
        public void SetFromSnapshot(ConfigNode node)
        {
            float.TryParse(node.GetValue("velocityMetersPerSec"), out velocityMetersPerSec);
            float.TryParse(node.GetValue("targetPosition"), out targetPosition);

            moveToTarget();
        }

        /// <summary>
        /// Determines whether or not the servo is moving
        /// </summary>
        /// <returns>True if the servo is moving, false if not.</returns>
        public bool IsMoving()
        {
            return movementState != WBIMovementState.Locked ? true : false;
        }

        /// <summary>
        /// Tells the servo to stop moving.
        /// </summary>
        public void StopMoving()
        {
            movementState = WBIMovementState.Locked;
        }
        #endregion

        #region Helpers
        protected void moveToTarget()
        {
            //Set movement state
            if (currentPosition < targetPosition)
                movementState = WBIMovementState.MovingForward;
            else
                movementState = WBIMovementState.MovingBackward;

            //Set up the target position vector
            Vector3 currentPos = meshTransform.localPosition;
            meshTransform.localPosition = vecOriginalPos;
            meshTransform.Translate(translateAxis * targetPosition);
            vecTargetPos = meshTransform.localPosition;
            meshTransform.localPosition = currentPos;
        }

        protected void drawSetPosition()
        {
            //Move a specified distance
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white>Position:</color>");
            targetPositionText = GUILayout.TextField(targetPositionText);

            if (GUILayout.Button("Set"))
            {
                if (float.TryParse(targetPositionText, out newTargetPosition))
                {
                    //Make sure we're in bounds
                    if (newTargetPosition < minDistance)
                        newTargetPosition = minDistance;
                    else if (newTargetPosition > maxDistance)
                        newTargetPosition = maxDistance;

                    //Set target position
                    targetPosition = newTargetPosition;
                    targetPositionText = string.Format("{0:f2}", newTargetPosition);

                    //Move to target
                    moveToTarget();
                }
            }

            GUILayout.EndHorizontal();
        }

        protected void drawinfoPanel()
        {
            //Servo name
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=white>" + servoName + "</color></b>");
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format("<color=white><b>Dist: </b>{0:f2}m</color>", currentPosition));
            GUILayout.EndHorizontal();

            //Move speed
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white>Speed:</color>");

            string metersPerSecText = string.Format("{0:f2}", velocityMetersPerSec);
            metersPerSecText = GUILayout.TextField(metersPerSecText);
            float moveRate = 0.0f;
            if (float.TryParse(metersPerSecText, out moveRate))
            {
                velocityMetersPerSec = moveRate;
                velocityPerUpdate = velocityMetersPerSec * TimeWarp.fixedDeltaTime;
            }

            GUILayout.Label("<color=white>m/s</color>");
            GUILayout.EndHorizontal();
        }

        protected void drawMovementControls()
        {
            GUILayout.BeginHorizontal();

            //If we don't have a minimum distance, then the Min button and the 0 button do the same thing,
            //so consolidate the GUI.
            if (hasMinDistance)
            {
                //Min
                if (GUILayout.Button(ServoGUI.minIcon, ServoGUI.buttonOptions) && meshTransform.localPosition != vecMinPosition)
                {
                    targetPosition = minDistance;
                    moveToTarget();
                }
            }

            //Towards min
            if (GUILayout.RepeatButton(ServoGUI.backIcon, ServoGUI.buttonOptions) && meshTransform.localPosition != vecMinPosition)
            {
                targetPosition = currentPosition - velocityPerUpdate;
                if (targetPosition > minDistance)
                {
                    moveToTarget();

                    if (HighLogic.LoadedSceneIsFlight)
                        this.part.Effect(runningEffectName, 1.0f);
                }
                else
                {
                    targetPosition = minDistance;
                    currentPosition = minDistance;
                    meshTransform.localPosition = vecMinPosition;
                    movementState = WBIMovementState.Locked;
                    status = kLocked;

                    if (HighLogic.LoadedSceneIsFlight)
                        this.part.Effect(runningEffectName, 1.0f);
                }
            }

            //0
            if (GUILayout.Button(ServoGUI.homeIcon, ServoGUI.buttonOptions) && currentPosition != 0.0f)
            {
                targetPosition = 0f;
                moveToTarget();
            }

            //Towards max
            if (GUILayout.RepeatButton(ServoGUI.forwardIcon, ServoGUI.buttonOptions) && meshTransform.localPosition != vecMaxPosition)
            {
                targetPosition = currentPosition + velocityPerUpdate;
                if (targetPosition < maxDistance)
                {
                    moveToTarget();

                    if (HighLogic.LoadedSceneIsFlight)
                        this.part.Effect(runningEffectName, 1.0f);
                }
                else
                {
                    targetPosition = maxDistance;
                    currentPosition = maxDistance;
                    meshTransform.localPosition = vecMaxPosition;
                    movementState = WBIMovementState.Locked;
                    status = kLocked;

                    if (HighLogic.LoadedSceneIsFlight)
                        this.part.Effect(runningEffectName, 1.0f);
                }
            }

            //If we don't have a max distance, then the Max button and the 0 button do the same thing,
            //so consolidate the GUI.
            if (hasMaxDistance)
            {
                //Max
                if (GUILayout.Button(ServoGUI.maxIcon, ServoGUI.buttonOptions) && meshTransform.localPosition != vecMaxPosition)
                {
                    targetPosition = maxDistance;
                    moveToTarget();
                }
            }
            GUILayout.EndHorizontal();
        }
        #endregion
    }
}
