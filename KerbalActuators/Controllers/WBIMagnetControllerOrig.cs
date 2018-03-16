using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
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
    public class WBIMagnetControllerOrig : PartModule, IRotationController
    {
        const int kPanelHeight = 70;
        const string kRequiredResource = "ElectricCharge";

        [KSPField]
        public float ecPerSec = 5.0f;

        [KSPField]
        public string magnetTransformName = "magnetTransform";

        [KSPField(guiName = "Deploy Limit", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        public float magnetPercent = 100f;

        [KSPField]
        public float magnetForce = 10.0f;

        [KSPField(isPersistant = true, guiName = "Magnet")]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool magnetIsActive;

        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };
        Transform[] magnetTransforms = null;
        Part targetPart = null;
        ResourceBroker resourceBroker;
        float unitsPerUpdate;
//        FixedJoint fixedJoint;
        float forcePerTransform;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            magnetTransforms = this.part.FindModelTransforms(magnetTransformName);
            if (magnetTransforms != null && magnetTransforms.Length > 0)
                forcePerTransform = magnetForce / magnetTransforms.Length;

            resourceBroker = new ResourceBroker();
            unitsPerUpdate = ecPerSec * TimeWarp.fixedDeltaTime;
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (targetPart != null)
                return;

            GameObject colliderGO = collision.collider.gameObject;

            targetPart = colliderGO.transform.GetComponentInParent<Part>();
        }

        public void OnCollisionStay(Collision collision)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (targetPart != null)
                return;

            GameObject colliderGO = collision.collider.gameObject;

            targetPart = colliderGO.transform.GetComponentInParent<Part>();
        }

        public void OnCollisionExit(Collision collision)
        {
            targetPart = null;
        }

        public void FixedUpdate()
        {
            if (magnetTransforms == null || magnetTransforms.Length == 0)
                return;
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!magnetIsActive)
                return;
            if (targetPart == null)
                return;

            /*
            //Check power requirements
            if (resourceBroker.AmountAvailable(this.part, kRequiredResource, TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) >= unitsPerUpdate)
            {
                resourceBroker.RequestResource(this.part, kRequiredResource, unitsPerUpdate, TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
            }

            else
            {
                magnetIsActive = false;
                return;
            }
             */

            //Apply magnetic forces
            float magneticForce = forcePerTransform * (magnetPercent / 100.0f);
            for (int index = 0; index < magnetTransforms.Length; index++)
            {
                targetPart.AddForceAtPosition(magnetTransforms[index].forward.normalized * -magneticForce, magnetTransforms[index].transform.position);
                this.part.AddForceAtPosition(magnetTransforms[index].forward.normalized * magneticForce, magnetTransforms[index].transform.position);
            }
        }

        /*
        protected void createAttachJoint()
        {
            if (fixedJoint != null)
                return;
            this.part.collider.isTrigger = true;

            fixedJoint = this.part.gameObject.AddComponent<FixedJoint>();
            fixedJoint.connectedBody = targetPart.Rigidbody;
            fixedJoint.breakForce = magnetForceKN;
            fixedJoint.breakTorque = magnetForceKN;
            Debug.Log("attach joint created");
        }

        protected void removeAttachJoint()
        {
            if (fixedJoint == null)
                return;
            this.part.collider.isTrigger = false;

            Destroy(fixedJoint);
            fixedJoint = null;
            Debug.Log("attach joint removed");
        }
        */
        #region IRotationController
        public void DrawControls()
        {
            GUILayout.BeginScrollView(scrollVector, panelOptions);

            GUILayout.BeginVertical();

            magnetIsActive = GUILayout.Toggle(magnetIsActive, "Enable magnet");

            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        public void HideGUI()
        {
            Fields["magnetIsActive"].guiActive = true;
            Fields["magnetPercent"].guiActive = true;
        }

        public int GetPanelHeight()
        {
            return kPanelHeight;
        }

        public ConfigNode TakeSnapshot()
        {
            ConfigNode node = new ConfigNode(WBIServoManager.SERVODATA_NODE);

            node.AddValue("magnetIsActive", magnetIsActive);

            return node;
        }

        public void SetFromSnapshot(ConfigNode node)
        {
            bool.TryParse(node.GetValue("magnetIsActive"), out magnetIsActive);
        }

        public bool IsMoving()
        {
            return magnetIsActive;
        }

        public string GetGroupID()
        {
            throw new NotImplementedException();
        }

        public bool CanRotateMax()
        {
            throw new NotImplementedException();
        }

        public bool CanRotateMin()
        {
            throw new NotImplementedException();
        }

        public void RotateDown(float rotationDelta)
        {
            throw new NotImplementedException();
        }

        public void RotateUp(float rotationDelta)
        {
            throw new NotImplementedException();
        }

        public void RotateNeutral(bool applyToCounterparts = true)
        {
            throw new NotImplementedException();
        }

        public void RotateMin(bool applyToCounterparts = true)
        {
            throw new NotImplementedException();
        }

        public void RotateMax(bool applyToCounterparts = true)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
