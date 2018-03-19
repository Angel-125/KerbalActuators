using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125) & Sirkut
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace KerbalActuators
{
    /// <summary>
    /// This class implements a magnet that's used for moving other parts around.
    /// It does so by creating an attachment joint on the detected target.
    /// Whenever the magnet moves around, so to does the target.
    /// Special thanks to Sirkut for show how this is done!
    /// NOTE: The part must have a trigger collider to detect when the magnet
    /// touches a target part.
    /// </summary>
    [KSPModule("Magnet")]
    public class WBIMagnetController : PartModule, IServoController
    {
        #region Constants and user strings
        const int kPanelHeight = 120;
        const string kRequiredResource = "ElectricCharge";

        public static string kMagnetActivated = "On";
        public static string kTargetFound = "<color=white><b>Target: </b>{0}</color>";
        public static string kTargetNotFound = "<color=white><b>Target: </b>None</color>";
        public static string kStatus = "<color=white><b>Status: </b>{0}</color>";
        public static string kStatusOn = "On";
        public static string kStatusOff = "Off";
        public static string kStatusAttached = "Attached";
        public static string kStatusInsufficientEC = "Insufficient E.C.";
        #endregion

        #region Fields
        /// <summary>
        /// Flag to indicate if we should operate in debug mode
        /// </summary>
        [KSPField]
        public bool debugMode;

        /// <summary>
        /// How much ElectricCharge per second is required to operate the magnet.
        /// </summary>
        [KSPField]
        public float ecPerSec = 0f;

        /// <summary>
        /// Name of the magnet transform in the 3D mesh.
        /// </summary>
        [KSPField]
        public string magnetTransformName = "magnetTransform";

        /// <summary>
        /// Servo group ID. Default is "Magnet"
        /// </summary>
        [KSPField]
        public string groupID = "Magnet";

        /// <summary>
        /// Name of the servo. Used to identify it in the servo manager and the sequence file.
        /// </summary>
        [KSPField]
        public string servoName = "Magnet";

        /// <summary>
        /// Name of the target detected via the trigger
        /// </summary>
        [KSPField(guiName = "Target", guiActive = true)]
        public string targetName;

        /// <summary>
        /// Name of the effect to play when the magnet attaches to a part.
        /// </summary>
        [KSPField]
        public string attachEffectName = string.Empty;

        /// <summary>
        /// Name of the effect to play when the magnet detaches from a part.
        /// </summary>
        [KSPField]
        public string detachEffectName = string.Empty;

        /// <summary>
        /// Name of the effect to play while the magnet is activated.
        /// </summary>
        [KSPField]
        public string runningEffectName = string.Empty;

        /// <summary>
        /// Field to indicate whether or not the magnet is on.
        /// You won't pick up parts with the magnet turned off...
        /// </summary>
        [KSPField(guiName = "Magnet", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool magnetActivated;

        [KSPField(guiActive = true, guiName = "Magnet Status")]
        public string status = string.Empty;
        #endregion

        #region Housekeeping
        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };

        protected Part targetPart;
        protected Rigidbody magnetRigidBody;
        protected ConfigurableJoint attachmentJoint;
        protected Transform magnetTransform = null;
        #endregion

        #region API
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            magnetTransform = part.FindModelTransform(magnetTransformName);

            //Setup effects
            if (!string.IsNullOrEmpty(detachEffectName))
                this.part.Effect(detachEffectName, 1.0f);
            if (!string.IsNullOrEmpty(runningEffectName))
                this.part.Effect(runningEffectName, -1.0f);
            if (!string.IsNullOrEmpty(detachEffectName))
                this.part.Effect(detachEffectName, -1.0f);
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            UpdateTargetData(collision);
        }

        public void OnCollisionStay(Collision collision)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            UpdateTargetData(collision);
        }

        /// <summary>
        /// Determines the target part based upon the supplied Collision object.
        /// If the magnet is on and the attachment joint hasn't been created, then
        /// it creates the attachment joint. Othwerwise, if the magnet is off
        /// and the attachment joint is created, then it removes the joint.
        /// </summary>
        /// <param name="collision">A Collision object containing collision data. Usually comes from an OnCollision event.</param>
        public virtual void UpdateTargetData(Collision collision)
        {
            //Get the target part
            GameObject targetObject = collision.collider.gameObject;
            targetPart = targetObject.transform.GetComponentInParent<Part>();
            if (targetPart == null)
            {
                targetName = "";
                return;
            }

            //Make sure we're not colliding with the vessel
            if (this.part.vessel.ContainsCollider(collision.collider))
            {
                targetName = "";
                return;
            }

            //Update target name
            targetName = targetPart.partInfo.title;

            //If the magnet is on and we need to create the attachment joint then do so.
            if (magnetActivated && attachmentJoint == null)
                CreateAttachmentJoint();
            else if (!magnetActivated && attachmentJoint != null)
                RemoveAttachmentJoint();
        }

        /// <summary>
        /// Creates the attachment joint if there is a target part and we have a magnetTransform.
        /// </summary>
        public virtual void CreateAttachmentJoint()
        {
            if (targetPart == null || magnetTransform == null)
            {
                if (targetPart == null)
                    DebugLog("No targetPart found");
                if (magnetTransform == null)
                    DebugLog("No magnetTransform found");
                return;
            }

            magnetRigidBody = magnetTransform.gameObject.AddComponent<Rigidbody>();
            magnetRigidBody.isKinematic = true;

            attachmentJoint = magnetTransform.gameObject.AddComponent<ConfigurableJoint>();

            attachmentJoint.xMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.yMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.zMotion = ConfigurableJointMotion.Locked;

            attachmentJoint.angularXMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.angularYMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.angularZMotion = ConfigurableJointMotion.Locked;

            attachmentJoint.connectedBody = targetPart.Rigidbody;
            DebugLog("Added attachment joint");

            //Play the attachment effect
            if (!string.IsNullOrEmpty(attachEffectName))
                this.part.Effect(attachEffectName, 1.0f);
        }

        /// <summary>
        /// Removes a previously created attachment joint.
        /// </summary>
        public virtual void RemoveAttachmentJoint()
        {
            if (attachmentJoint != null)
            {
                UnityEngine.Object.Destroy(attachmentJoint.GetComponent<ConfigurableJoint>());
                DebugLog("Removed attachment joint");
            }
            if (magnetRigidBody != null)
            {
                UnityEngine.Object.Destroy(magnetRigidBody.GetComponent<Rigidbody>());
                DebugLog("Removed magnet rigid body");
            }

            //Play the detach effect
            if (!string.IsNullOrEmpty(detachEffectName))
                this.part.Effect(detachEffectName, 1.0f);
            if (!string.IsNullOrEmpty(runningEffectName))
                this.part.Effect(runningEffectName, -1.0f);
        }
        #endregion

        #region Helpers
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Pay the EC cost if needed
            if (ecPerSec > 0.0f && magnetActivated)
            {
                double ecPerFrame = ecPerSec * TimeWarp.fixedDeltaTime;
                double amountObtained = this.part.RequestResource(kRequiredResource, ecPerFrame, ResourceFlowMode.ALL_VESSEL);
                if ((amountObtained / ecPerFrame) < 0.999f)
                {
                    magnetActivated = false;
                    status = kStatusInsufficientEC;
                    return;
                }
            }

            //Magnet status
            if (magnetActivated && attachmentJoint != null)
                status = kStatusAttached;
            else if (magnetActivated && attachmentJoint == null)
                status = kStatusOn;
            else if (magnetActivated == false)
                status = kStatusOff;

            //Play the running effect
            if (!string.IsNullOrEmpty(runningEffectName) && magnetActivated)
                this.part.Effect(runningEffectName, 1.0f);
            else if (!string.IsNullOrEmpty(runningEffectName))
                this.part.Effect(runningEffectName, -1.0f);
        }

        protected void DebugLog(string message)
        {
            if (!debugMode)
                return;

            Debug.Log("[WBIMagnetController] - " + message);
        }
        #endregion

        #region IServoController
        /// <summary>
        /// Tells the servo to draw its GUI controls. It's used by the servo manager.
        /// </summary>
        public void DrawControls()
        {
            GUILayout.BeginScrollView(scrollVector, panelOptions);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            //Name of the magnet
            GUILayout.Label("<color=white><b>" + servoName + "</b></color>");

            //Magnet state
            magnetActivated = GUILayout.Toggle(magnetActivated, kMagnetActivated);
            GUILayout.EndHorizontal();

            //Target
            if (string.IsNullOrEmpty(targetName))
                GUILayout.Label(kTargetNotFound);
            else
                GUILayout.Label(string.Format(kTargetFound, targetName));

            //Status
            GUILayout.Label(string.Format(kStatus, status));

            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Hides the GUI controls in the Part Action Window.
        /// </summary>
        public void HideGUI()
        {
            Fields["targetName"].guiActive = false;
            Fields["status"].guiActive = false;
            Fields["magnetActivated"].guiActive = false;
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
            node.AddValue("magnetActivated", magnetActivated);

            return node;
        }

        /// <summary>
        /// Sets the servo's state based upon the supplied config node.
        /// </summary>
        /// <param name="node">A SERVODAT_NODE ConfigNode containing servo state data.</param>
        public void SetFromSnapshot(ConfigNode node)
        {
            if (node.HasValue("magnetActivated"))
                bool.TryParse(node.GetValue("magnetActivated"), out magnetActivated);
        }

        /// <summary>
        /// Determines whether or not the servo is moving
        /// </summary>
        /// <returns>True if the servo is moving, false if not.</returns>
        public bool IsMoving()
        {
            //No special sauce here...
            return false;
        }

        /// <summary>
        /// Returns the group ID of the servo. Used by the servo manager to know what servos it controlls.
        /// </summary>
        /// <returns>A string containing the group ID</returns>
        public string GetGroupID()
        {
            return groupID;
        }
        #endregion
    }
}
