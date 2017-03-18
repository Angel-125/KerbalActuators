using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2017, by Michael Billard (Angel-125)
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
    public class WBIPropSpinner : PartModule
    {
        const string kForwardThrust = "Thrust: Forward";
        const string kReverseThrust = "Thrust: Reverse";

        [KSPField(isPersistant = true)]
        public bool reverseThrust;

        [KSPField]
        public string thrustTransform = "thrustTransform";

        [KSPField]
        public string reverseThrustTransform = "reverseThrustTransform";

        [KSPField]
        public string rotorTransformName = string.Empty;

        [KSPField()]
        public string rotorRotationAxis = "0,0,1";

        [KSPField]
        public float rotorRPM = 30.0f;

        [KSPField]
        public float rotorSpoolTime = 3.0f;

        [KSPField]
        public float minThrustRotorBlur = 0.25f;

        [KSPField(isPersistant = true)]
        public bool mirrorRotation;

        [KSPField]
        public bool guiVisible = true;

        protected Transform rotorTransform = null;
        protected Transform fwdThrustTransform = null;
        protected Transform revThrustTransform = null;
        protected Vector3 axisRate = new Vector3(0, 0, 1);
        protected float currentThrustNormalized = 0f;
        protected float targetThrustNormalized = 0f;
        protected float currentSpoolRate;
        protected ModuleEnginesFX engine;

        public void MirrorRotation(bool isMirrored)
        {
            mirrorRotation = isMirrored;
            setupMirrorTransforms();
        }

        [KSPAction("Toggle Fwd/Rev Thrust")]
        public virtual void ToggleThrustTransformAction(KSPActionParam param)
        {
            ToggleThrustTransform();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Thrust: Forward")]
        public void ToggleThrustTransform()
        {
            reverseThrust = !reverseThrust;
            SetupThrustTransform();

            //Don't forget the symmetrical parts...
            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    WBIPropSpinner propController = symmetryPart.GetComponent<WBIPropSpinner>();
                    if (propController != null)
                    {
                        propController.reverseThrust = this.reverseThrust;
                        propController.SetupThrustTransform();
                    }
                }
            }

        }

        public void ToggleThrust()
        {
            reverseThrust = !reverseThrust;
            SetupThrustTransform();
        }

        public virtual void SetGUIVisible(bool isVisible)
        {
            Events["ToggleThrustTransform"].active = isVisible;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            rotorTransform = this.part.FindModelTransform(rotorTransformName);
            engine = this.part.FindModuleImplementing<ModuleEnginesFX>();

            //Setup events
            WBIRotationController rotationController = this.part.FindModuleImplementing<WBIRotationController>();
            if (rotationController != null)
            {
                mirrorRotation = rotationController.mirrorRotation;
                rotationController.onRotatorMirrored += MirrorRotation;
            }

            //Get the rotation axis
            if (string.IsNullOrEmpty(rotorRotationAxis) == false)
            {
                string[] axisValues = rotorRotationAxis.Split(',');
                float value;
                if (axisValues.Length == 3)
                {
                    if (float.TryParse(axisValues[0], out value))
                        axisRate.x = value;
                    if (float.TryParse(axisValues[1], out value))
                        axisRate.y = value;
                    if (float.TryParse(axisValues[2], out value))
                        axisRate.z = value;
                }
            }

            //Set gui visible state
            SetGUIVisible(guiVisible);

            //Setup the thrust transform
            fwdThrustTransform = this.part.FindModelTransform(thrustTransform);
            revThrustTransform = this.part.FindModelTransform(reverseThrustTransform);
            if (fwdThrustTransform != null && revThrustTransform != null)
            {
                SetupThrustTransform();
            }
            else
            {
                Events["ToggleThrustTransform"].active = false;
                Actions["ToggleThrustTransformAction"].active = false;
            }

            //Editor tools
            setupMirrorTransforms();
        }

        public void FixedUpdate()
        {
            if (engine == null || rotorTransform == null)
                return;
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            if (!engine.isOperational || !engine.EngineIgnited)
            {
                //If the rotors are stopped then we're done.
                if (currentSpoolRate <= 0.001f)
                    return;

                //If needed, show the non-blurred rotors

                //Start spinning down the rotors
                currentSpoolRate = Mathf.Lerp(currentSpoolRate, 0f, TimeWarp.fixedDeltaTime / rotorSpoolTime);
                if (currentSpoolRate <= 0.002f)
                    currentSpoolRate = 0f;

                float rotationPerFrame = ((rotorRPM * 60.0f) * TimeWarp.fixedDeltaTime) * currentSpoolRate;
                if (mirrorRotation)
                    rotationPerFrame *= -1.0f;

                rotorTransform.Rotate(axisRate.x * rotationPerFrame, axisRate.y * rotationPerFrame, axisRate.z * rotationPerFrame);
                return;
            }

            //If the rotor is deployed, and the engine is running, then rotate the rotors.
            //If the engine thrust is >= 25% then show the blurred rotors
            float thrustRatio = engine.finalThrust / engine.maxThrust;
            if (thrustRatio >= (minThrustRotorBlur/ 100.0f))
            {
                //Temporary!
                float rotationPerFrame = ((rotorRPM * 60.0f) * TimeWarp.fixedDeltaTime) * 4.0f;
                if (mirrorRotation)
                    rotationPerFrame *= -1.0f;

                rotorTransform.Rotate(axisRate.x * rotationPerFrame, axisRate.y * rotationPerFrame, axisRate.z * rotationPerFrame);
            }

            //Rotate the non-blurred rotor until thrust >= 25%
            else
            {
                currentSpoolRate = Mathf.Lerp(currentSpoolRate, 1.0f, TimeWarp.fixedDeltaTime / rotorSpoolTime);
                if (currentSpoolRate > 0.995f)
                    currentSpoolRate = 1.0f;

                float rotationPerFrame = ((rotorRPM * 60.0f) * TimeWarp.fixedDeltaTime) * currentSpoolRate;
                if (mirrorRotation)
                    rotationPerFrame *= -1.0f;

                rotorTransform.Rotate(axisRate.x * rotationPerFrame, axisRate.y * rotationPerFrame, axisRate.z * rotationPerFrame);
            }
        }

        public void SetupThrustTransform()
        {
            engine.thrustTransforms.Clear();
            if (!reverseThrust)
            {
                Events["ToggleThrustTransform"].guiName = kForwardThrust;
                engine.thrustTransforms.Add(fwdThrustTransform);
            }

            else
            {
                Events["ToggleThrustTransform"].guiName = kReverseThrust;
                engine.thrustTransforms.Add(revThrustTransform);
            }
        }

        protected void setupMirrorTransforms()
        {
        }
    }
}
