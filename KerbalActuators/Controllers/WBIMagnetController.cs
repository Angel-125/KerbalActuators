using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2017, by Sirkut & Michael Billard (Angel-125)
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
    public enum WBIMagnetStates
    {
        None,
        Initialized,
        Activated,
        Deactivated
    }

    [KSPModule("Magnet")]
    public class WBIMagnetController : PartModule, IServoController
    {
        const int kPanelHeight = 70;
        const string kRequiredResource = "ElectricCharge";

        [KSPField]
        public bool debugMode;

        [KSPField]
        public float ecPerSec = 5.0f;

        [KSPField]
        public string magnetTransformName = "magnetTransform";

        [KSPField]
        public float attachRange = 0.5f;

        [KSPField(guiName = "Ray", guiActive = true)]
        public string rayInfo;

        [KSPField]
        public bool drawAttachRay = true;

        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };
        GameObject magnetObject;
        GameObject targetObject;
        Part targetPart;
        Rigidbody magnetRigidBody;
        Rigidbody targetRigidBody;
        ConfigurableJoint attachmentJoint;
        bool magnetIsReady = false;
        int layerMask = 0;
        bool isConnected;
        Transform magnetTransform = null;

        protected void DebugLog(string message)
        {
            if (!debugMode)
                return;

            Debug.Log("[WBIMagnetController] - " + message);
        }

        [KSPEvent(guiActive = true, guiName = "Magnet: Off", active = true, guiActiveUnfocused = true, unfocusedRange = 40f)]
        public void MagnetToggle()
        {
            if (targetObject == null)
                DebugLog("targetObject is null");
            else
                DebugLog("targetObject is not null");
            if (targetObject != null && isConnected == false)
            {
                magnetRigidBody = magnetObject.AddComponent<Rigidbody>();
                magnetRigidBody.isKinematic = true;

                attachmentJoint = magnetObject.AddComponent<ConfigurableJoint>();

                attachmentJoint.xMotion = ConfigurableJointMotion.Locked;
                attachmentJoint.yMotion = ConfigurableJointMotion.Locked;
                attachmentJoint.zMotion = ConfigurableJointMotion.Locked;

                attachmentJoint.angularXMotion = ConfigurableJointMotion.Locked;
                attachmentJoint.angularYMotion = ConfigurableJointMotion.Locked;
                attachmentJoint.angularZMotion = ConfigurableJointMotion.Locked;

                attachmentJoint.connectedBody = targetRigidBody;
                isConnected = true;
                Events["MagnetToggle"].guiName = "Magnet: On";
                DebugLog("Created attachment joint");
            }
            else if (isConnected == true)
            {
                UnityEngine.Object.Destroy(attachmentJoint.GetComponent<ConfigurableJoint>());
                UnityEngine.Object.Destroy(magnetRigidBody.GetComponent<Rigidbody>());
                isConnected = false;
                Events["MagnetToggle"].guiName = "Magnet: Off";
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight)
            {
                magnetTransform = part.FindModelTransform(magnetTransformName);
                if (magnetTransform != null)
                {
                    DebugLog("Found magnet transform");
                    magnetObject = magnetTransform.gameObject;
                    magnetIsReady = true;
                }
                else
                {
                    DebugLog("Magnet transform not found");
                }
            }
        }

        /*
        public void OnCollisionEnter(Collision collision)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (targetPart != null)
                return;

            targetObject = collision.collider.gameObject;
            targetPart = targetObject.transform.GetComponentInParent<Part>();
            if (targetPart != null)
                rayInfo = targetPart.partInfo.title;
            else
                rayInfo = "";
        }

        public void OnCollisionStay(Collision collision)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (targetPart != null)
                return;

            targetObject = collision.collider.gameObject;
            targetPart = targetObject.transform.GetComponentInParent<Part>();
            if (targetPart != null)
                rayInfo = targetPart.partInfo.title;
            else
                rayInfo = "";
        }

        public void OnCollisionExit(Collision collision)
        {
            targetPart = null;
            targetObject = null;
        }
        */

        void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (magnetTransform == null)
                return;
            if (isConnected)
                return;

            if (drawAttachRay)
            {
                //DebugDrawer.DebugLine(magnetTransform.position, magnetTransform.position + magnetTransform.forward * attachRange, Color.red);
                //DrawLine
            }

            RaycastHit rayCastHit;
            int tempLayerMask = ~layerMask;
            if (Physics.Raycast(magnetTransform.position, magnetTransform.forward, out rayCastHit, attachRange, tempLayerMask))
            {
                rayInfo = rayCastHit.collider.gameObject.name.ToString();
                try
                {
                    targetObject = rayCastHit.collider.gameObject;
                    targetRigidBody = rayCastHit.rigidbody;
                }
                catch (Exception ex) 
                { 
                    DebugLog("Exception encountered while trying to get targetObject: " + ex.ToString());
                }
            }
            else
            {
                rayInfo = "";
                targetObject = null;
            }
        }

        #region IServoController
        public void DrawControls()
        {
            GUILayout.BeginScrollView(scrollVector, panelOptions);

            GUILayout.BeginVertical();


            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        public void HideGUI()
        {
        }

        public int GetPanelHeight()
        {
            return kPanelHeight;
        }

        public ConfigNode TakeSnapshot()
        {
            ConfigNode node = new ConfigNode(WBIServoManager.SERVODATA_NODE);


            return node;
        }

        public void SetFromSnapshot(ConfigNode node)
        {
        }

        public bool IsMoving()
        {
            return true;
        }

        public string GetGroupID()
        {
            return string.Empty;
        }
        #endregion
    }
}
