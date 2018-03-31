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
    public enum WBIMovementState
    {
        Locked,
        MovingToMin,
        MovingToMax,
        MovingToNeutral,
        MovingMinward,
        MovingMaxward
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
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
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

            //Setup status
            status = kLocked;
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
                return;
            if (movementState == WBIMovementState.Locked)
                return;

            //Play the moving sound
            if (!string.IsNullOrEmpty(runningEffectName))
                this.part.Effect(runningEffectName, 1.0f);

            //Calculate movement delta and update state
            float moveDelta = 0.0f;
            float curPosMagnitude = meshTransform.localPosition.magnitude;
            switch (movementState)
            {
                default:
                case WBIMovementState.Locked:
                    break;

                case WBIMovementState.MovingMaxward:
                    moveDelta = velocityPerUpdate;

                    //Check for target distance
                    if (curPosMagnitude >= vecTargetPos.magnitude)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = targetPosition;
                        meshTransform.localPosition = vecTargetPos;
                        return;
                    }

                    //Check for max distance
                    else if (curPosMagnitude >= vecMaxPosition.magnitude)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = maxDistance;

                        //For good measure, make sure our position is at the max
                        meshTransform.localPosition = vecMaxPosition;
                        return;
                    }
                    break;

                case WBIMovementState.MovingMinward:
                    moveDelta = -velocityPerUpdate;

                    //Check for target distance
                    if (curPosMagnitude <= vecTargetPos.magnitude)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = targetPosition;
                        meshTransform.localPosition = vecTargetPos;
                        return;
                    }

                    //Check for min distance
                    else if (meshTransform.localPosition.magnitude <= vecMinPosition.magnitude)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = minDistance;

                        //For good measure, make sure our position is at the min
                        meshTransform.localPosition = vecMinPosition;
                        return;
                    }
                    break;

                case WBIMovementState.MovingToMax:
                    moveDelta = velocityPerUpdate;

                    //Check for target
                    if (meshTransform.localPosition.magnitude >= vecMaxPosition.magnitude)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = maxDistance;

                        //For good measure, make sure our position is at the max
                        meshTransform.localPosition = vecMaxPosition;
                        return;
                    }
                    break;

                case WBIMovementState.MovingToMin:
                    moveDelta = -velocityPerUpdate;

                    //Check for min distance
                    Vector3 direction = vecMinPosition - meshTransform.localPosition;
                    float angle = Vector3.SignedAngle(direction, meshTransform.forward, Vector3.up);
                    if (angle != 0.0f)
                    {
                        movementState = WBIMovementState.Locked;
                        status = kLocked;
                        currentPosition = minDistance;

                        //For good measure, make sure our position is at the min
                        meshTransform.localPosition = vecMinPosition;
                        return;
                    }
                    break;

                case WBIMovementState.MovingToNeutral:
                    if (currentPosition > 0.0f)
                        moveDelta = -velocityPerUpdate;
                    else
                        moveDelta = velocityPerUpdate;

                    //Check for neutral
                    float origPosMagnitude = vecOriginalPos.magnitude;
                    if (curPosMagnitude <= origPosMagnitude && moveDelta < 0.0f ||
                    curPosMagnitude >= origPosMagnitude && moveDelta > 0.0f)
                    {
                        meshTransform.localPosition = vecOriginalPos;
                        movementState = WBIMovementState.Locked;
                        currentPosition = 0.0f;
                        status = kLocked;
                        return;
                    }
                    break;
            }

            //Update status
            status = kMoving;
            currentPosition += moveDelta;

            //Move the mesh
            meshTransform.Translate(translateAxis * moveDelta);
        }

        #endregion

        #region IServoController
        public string GetGroupID()
        {
            return groupID;
        }

        public void DrawControls()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginScrollView(scrollVector, panelOptions);

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
            //metersPerSecText = Regex.Replace(metersPerSecText, @"[^0-9]", "");
            float moveRate = 0.0f;
            if (float.TryParse(metersPerSecText, out moveRate))
            {
                velocityMetersPerSec = moveRate;
                velocityPerUpdate = velocityMetersPerSec * TimeWarp.fixedDeltaTime;
            }

            GUILayout.Label("<color=white>m/s</color>");
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();

            //Movement controls
            drawMovementControls();

            //Move a specified distance
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white>Pos:</color>");
            metersPerSecText = GUILayout.TextField(metersPerSecText);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        public void HideGUI()
        {
            Fields["status"].guiActive = false;
            Fields["status"].guiActiveEditor = false;
        }

        public int GetPanelHeight()
        {
            return kPanelHeight;
        }

        public ConfigNode TakeSnapshot()
        {
            ConfigNode node = new ConfigNode(WBIServoManager.SERVODATA_NODE);

            node.AddValue("servoName", servoName);

            return node;
        }

        public void SetFromSnapshot(ConfigNode node)
        {
        }

        public bool IsMoving()
        {
            return movementState != WBIMovementState.Locked ? true : false;
        }

        public void StopMoving()
        {
            movementState = WBIMovementState.Locked;
        }
        #endregion

        #region Helpers
        protected void drawMovementControls()
        {
            GUILayout.BeginHorizontal();

            //If we don't have a minimum distance, then the Min button and the 0 button do the same thing,
            //so consolidate the GUI.
            if (hasMinDistance)
            {
                //Min
                if (GUILayout.Button("Min") && meshTransform.localPosition != vecMinPosition)
                {
                    movementState = WBIMovementState.MovingToMin;
                    targetPosition = minDistance;
                }
            }

            //Towards min
            if (GUILayout.RepeatButton("<") && meshTransform.localPosition != vecMinPosition)
            {
                //Moveit
                meshTransform.Translate(translateAxis * -velocityPerUpdate);
                status = kMoving;
                currentPosition -= velocityPerUpdate;

                //Check for min distance
                Vector3 direction = vecMinPosition - meshTransform.localPosition;
                float angle = Vector3.SignedAngle(direction, meshTransform.forward, Vector3.up);
                if (angle != 0.0f)
                {
                    movementState = WBIMovementState.Locked;
                    status = kLocked;
                    currentPosition = minDistance;

                    //For good measure, make sure our position is at the max
                    meshTransform.localPosition = vecMinPosition;
                }

                if (!string.IsNullOrEmpty(runningEffectName))
                    this.part.Effect(runningEffectName, 1.0f);
            }

            //0
            if (GUILayout.Button("0") && currentPosition != 0.0f)
            {
                movementState = WBIMovementState.MovingToNeutral;
                targetPosition = 0f;
            }

            //Towards max
            if (GUILayout.RepeatButton(">") && meshTransform.localPosition != vecMaxPosition)
            {
                //Moveit
                meshTransform.Translate(translateAxis * velocityPerUpdate);
                status = kMoving;
                currentPosition += velocityPerUpdate;

                //Check for max distance
                if (meshTransform.localPosition.magnitude >= vecMaxPosition.magnitude)
                {
                    movementState = WBIMovementState.Locked;
                    status = kLocked;
                    currentPosition = maxDistance;

                    //For good measure, make sure our position is at the max
                    meshTransform.localPosition = vecMaxPosition;
                }

                if (!string.IsNullOrEmpty(runningEffectName))
                    this.part.Effect(runningEffectName, 1.0f);
            }

            //If we don't have a max distance, then the Max button and the 0 button do the same thing,
            //so consolidate the GUI.
            if (hasMaxDistance)
            {
                //Max
                if (GUILayout.Button("Max") && meshTransform.localPosition != vecMaxPosition)
                {
                    movementState = WBIMovementState.MovingToMax;
                    targetPosition = maxDistance;
                }
            }
            GUILayout.EndHorizontal();
        }
        #endregion
    }
}
