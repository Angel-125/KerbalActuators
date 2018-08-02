using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

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
    /// <summary>
    /// This class is designed to spin propeller meshes for propeller-driven engines. It supports both propeller blades and blurred propeller meshes.
    /// </summary>
    public class WBIRCSPropSpinner : PartModule
    {
        #region Fields
        /// <summary>
        /// Layer of the animation
        /// </summary>
        [KSPField]
        public int animationLayer = 1;

        /// <summary>
        /// Name of the non-blurred rotor. The whole thing spins including any child meshes.
        /// </summary>
        [KSPField]
        public string rotorTransformName = string.Empty;

        /// <summary>
        /// (Optional) To properly mirror the engine, these parameters specify
        /// the standard and mirrored (symmetrical) rotor blade transforms.
        /// If included, they MUST be child meshes of the mesh specified by rotorTransformName.
        /// </summary>
        [KSPField]
        public string standardBladesName = string.Empty;

        /// <summary>
        /// Name of the mirrored rotor blades
        /// </summary>
        [KSPField]
        public string mirrorBladesName = string.Empty;

        /// <summary>
        /// Rotor axis of rotation
        /// </summary>
        [KSPField()]
        public string rotorRotationAxis = "0,0,1";

        /// <summary>
        /// How fast to spin the rotor
        /// </summary>
        [KSPField]
        public float rotorRPM = 30.0f;

        /// <summary>
        /// How fast to spin up or slow down the rotors until they reach rotorRPM
        /// </summary>
        [KSPField]
        public float rotorSpoolTime = 3.0f;

        /// <summary>
        /// How fast to spin the rotor when blurred; multiply rotorRPM by blurredRotorFactor
        /// </summary>
        [KSPField]
        public float blurredRotorFactor = 4.0f;

        /// <summary>
        /// At what percentage of throttle to switch to the blurred rotor/mesh rotor.
        /// </summary>
        [KSPField]
        public float minThrottleRotorBlur = 25f;

        /// <summary>
        /// Name of the blurred rotor
        /// </summary>
        [KSPField]
        public string blurredRotorName = string.Empty;

        /// <summary>
        /// How fast to spin the blurred rotor
        /// </summary>
        [KSPField]
        public float blurredRotorRPM;

        /// <summary>
        /// Is the rotor system currently blurred.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isBlurred;

        /// <summary>
        /// Flag to indicate that the rotors are mirrored.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool mirrorRotation;

        /// <summary>
        /// Flag to indicate whether or not the Part Action Window gui controls are visible.
        /// </summary>
        [KSPField]
        public bool guiVisible = true;

        /// <summary>
        /// During the shutdown process, how fast, in degrees/sec, do the rotors rotate to neutral?
        /// </summary>
        [KSPField]
        public float neutralSpinRate = 10.0f;

        /// <summary>
        /// Flag to indicate whether or not to rotate the propeller(s) back to their neutral position after they stop.
        /// Default is true.
        /// </summary>
        [KSPField]
        public bool restoreToNeutralRotation = true;
        #endregion

        #region Housekeeping
        protected float currentRotationAngle;
        protected ERotationStates rotationState = ERotationStates.Locked;
        protected float degPerUpdate;
        protected Transform rotorTransform = null;
        protected Vector3 rotationAxis = new Vector3(0, 0, 1);
        protected float currentSpoolRate;
        Transform blurredRotorTransform = null;
        Transform[] standardBlades = null;
        Transform[] mirroredBlades = null;
        ModuleRCSFX rcsModule = null;
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (rcsModule == null || rotorTransform == null)
                return;
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //If the engine isn't running, then slow and stop the rotors.
            if (!FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS])
                rotatePropellersShutdown();

            //If the rotor is deployed, and the engine is running, then rotate the rotors.
            else
                rotatePropellersRunning();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            //During load, we just want the non-blurred rotors so that the part will look good in the catalog.
            setupRotorTransforms();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get rotor transforms
            rotorTransform = this.part.FindModelTransform(rotorTransformName);

            //Setup RCS
            rcsModule = this.part.FindModuleImplementing<ModuleRCSFX>();

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
                        rotationAxis.x = value;
                    if (float.TryParse(axisValues[1], out value))
                        rotationAxis.y = value;
                    if (float.TryParse(axisValues[2], out value))
                        rotationAxis.z = value;
                }
            }

            //Set gui visible state
            SetGUIVisible(guiVisible);

            //Rotor transforms
            setupRotorTransforms();
        }        
        #endregion

        #region API
        /// <summary>
        /// Sets mirrored rotation.
        /// </summary>
        /// <param name="isMirrored">True if rotation is mirrored, false if not.</param>
        public void MirrorRotation(bool isMirrored)
        {
            mirrorRotation = isMirrored;
            setupRotorTransforms();
        }

        /// <summary>
        /// Shows or Hides the Part Action Window GUI controls associated with the controller.
        /// </summary>
        /// <param name="isVisible">True if the controls should be shown, false if not.</param>
        public virtual void SetGUIVisible(bool isVisible)
        {
        }

        #endregion

        #region Helpers
        protected void rotatePropellersShutdown()
        {
           
            //If our spool rate is 0 then spin them back to the neutral position.
            //Useful for making sure our rotors are in the right position for folding.
            if (currentSpoolRate <= 0.001f)
            {
                switch (rotationState)
                {
                    //Calcualte direction
                    case ERotationStates.SlowingDown:
                        if (!restoreToNeutralRotation)
                        {
                            rotationState = ERotationStates.Locked;
                            return;
                        }
                        degPerUpdate = neutralSpinRate * TimeWarp.fixedDeltaTime;
                        if ((0f - currentRotationAngle + 360f) % 360f <= 180f)
                            rotationState = ERotationStates.RotatingUp;
                        else
                            rotationState = ERotationStates.RotatingDown;
                        break;

                    //Update angle
                    case ERotationStates.RotatingUp:
                        currentRotationAngle += degPerUpdate;
                        currentRotationAngle = currentRotationAngle % 360.0f;
                        break;

                    case ERotationStates.RotatingDown:
                        currentRotationAngle -= degPerUpdate;
                        if (currentRotationAngle < 0f)
                            currentRotationAngle = 360f - currentRotationAngle;
                        break;

                    case ERotationStates.Locked:
                    default:
                        return;
                }

                //If we're rotating, position the mesh and see if we've met our target.
                if (rotationState == ERotationStates.RotatingUp || rotationState == ERotationStates.RotatingDown)
                {
                    if (currentRotationAngle > 0f - degPerUpdate && currentRotationAngle < 0f + degPerUpdate)
                    {
                        currentRotationAngle = 0;
                        rotationState = ERotationStates.Locked;
                    }

                    //Rotate the mesh
                    if (!mirrorRotation)
                        rotorTransform.localEulerAngles = (rotationAxis * currentRotationAngle);
                    else
                        rotorTransform.localEulerAngles = (rotationAxis * -currentRotationAngle);
                }

                return;
            }

            //If needed, show the non-blurred rotors
            if (isBlurred)
            {
                isBlurred = false;
                setupRotorTransforms();
            }

            //Slow down the rotors
            rotationState = ERotationStates.SlowingDown;
            currentSpoolRate = Mathf.Lerp(currentSpoolRate, 0f, TimeWarp.fixedDeltaTime / rotorSpoolTime);
            if (currentSpoolRate <= 0.002f)
                currentSpoolRate = 0f;

            float rotationPerFrame = ((rotorRPM * 60.0f) * TimeWarp.fixedDeltaTime) * currentSpoolRate;
            currentRotationAngle += rotationPerFrame;
            currentRotationAngle = currentRotationAngle % 360.0f;
            if (mirrorRotation)
                rotationPerFrame *= -1.0f;

            rotorTransform.Rotate(rotationAxis * rotationPerFrame);
        }

        protected bool updateBlurredState()
        {
            float minThrottleRatio = minThrottleRotorBlur / 100.0f;

            //If the RCS thruster is using the throttle, then check throttle state for minimum ratio
            if (rcsModule.useThrottle && FlightInputHandler.state.mainThrottle >= minThrottleRatio)
            {
                isBlurred = true;
            }

            //If the thruster is firing then we are blurred.
            //GameSettings.EVA_forward.GetKey(false)
            return isBlurred;
        }

        protected void rotatePropellersRunning()
        {
            rotationState = ERotationStates.Spinning;
            float minThrottleRatio = minThrottleRotorBlur / 100.0f;

            //If the engine thrust is >= 25% then show the blurred rotors
            float thrustRatio = engine.finalThrust / engine.maxThrust;
            if (thrustRatio >= minThrustRatio || isBlurred)
            {
                if (!isBlurred)
                {
                    isBlurred = true;
                    setupRotorTransforms();
                }

                //Spin the rotor (blades should be hidden at this point)
                float rotationPerFrame = ((rotorRPM * 60.0f) * TimeWarp.fixedDeltaTime) * blurredRotorFactor;
                currentRotationAngle += rotationPerFrame;
                currentRotationAngle = currentRotationAngle % 360.0f;
                if (mirrorRotation)
                    rotationPerFrame *= -1.0f;

                rotorTransform.Rotate(rotationAxis * rotationPerFrame);

                //Now spin the blurred rotor
                rotationPerFrame = ((blurredRotorRPM * 60.0f) * TimeWarp.fixedDeltaTime);
                if (mirrorRotation)
                    rotationPerFrame *= -1.0f;

                blurredRotorTransform.Rotate(rotationAxis * rotationPerFrame);
            }

            //Rotate the non-blurred rotor until thrust % >= minThrustRotorBlur
            else
            {
                if (isBlurred)
                {
                    isBlurred = false;
                    setupRotorTransforms();
                }

                currentSpoolRate = Mathf.Lerp(currentSpoolRate, 1.0f, TimeWarp.fixedDeltaTime / rotorSpoolTime);
                if (currentSpoolRate > 0.995f)
                    currentSpoolRate = 1.0f;

                float rotationPerFrame = ((rotorRPM * 60.0f) * TimeWarp.fixedDeltaTime) * currentSpoolRate;
                currentRotationAngle += rotationPerFrame;
                currentRotationAngle = currentRotationAngle % 360.0f;
                if (mirrorRotation)
                    rotationPerFrame *= -1.0f;

            }
        }

        protected void setupRotorTransforms()
        {
            //Get the transforms
            if (!string.IsNullOrEmpty(standardBladesName) && standardBlades == null)
                standardBlades = getTransforms(standardBladesName);
            if (!string.IsNullOrEmpty(mirrorBladesName) && mirroredBlades == null)
                mirroredBlades = getTransforms(mirrorBladesName);
            if (!string.IsNullOrEmpty(blurredRotorName) && blurredRotorTransform == null)
                blurredRotorTransform = this.part.FindModelTransform(blurredRotorName);

            //If the propellers are blurred, then hide the non-blurred propellers
            if (isBlurred)
            {
                if (standardBlades != null)
                    setMeshVisible(standardBlades, false);
                if (mirroredBlades != null)
                    setMeshVisible(mirroredBlades, false);
                if (blurredRotorTransform != null)
                    setMeshVisible(blurredRotorTransform, true);
                return;
            }

            else //Hide the blurred rotors
            {
                if (blurredRotorTransform != null)
                    setMeshVisible(blurredRotorTransform, false);
            }

            //Show/hide the non-blurred propellers depending on whether or not we're a mirrored engine.
            //Normal case: we have standardBlades and mirroredBlades
            if (standardBlades != null && mirroredBlades != null)
            {
                if (!mirrorRotation)
                {
                    if (standardBlades != null)
                        setMeshVisible(standardBlades, true);
                    if (mirroredBlades != null)
                        setMeshVisible(mirroredBlades, false);
                }

                else
                {
                    if (standardBlades != null)
                        setMeshVisible(standardBlades, false);
                    if (mirroredBlades != null)
                        setMeshVisible(mirroredBlades, true);
                }
            }

            //Special case: mirroredBlades is null, but we have standardBlades
            else if (standardBlades != null)
            {
                setMeshVisible(standardBlades, true);
            }
        }

        protected Transform[] getTransforms(string transformNames)
        {
            List<Transform> targets = new List<Transform>();
            Transform target;
            string[] targetNames = transformNames.Split(new char[] {','});

            for (int index = 0; index < targetNames.Length; index++)
            {
                target = this.part.FindModelTransform(targetNames[index]);
                if (target != null)
                    targets.Add(target);
            }

            return targets.ToArray();
        }

        protected void setMeshVisible(Transform[] targets, bool isVisible)
        {
            for (int index = 0; index < targets.Length; index++)
                setMeshVisible(targets[index], isVisible);
        }

        protected void setMeshVisible(Transform target, bool isVisible)
        {
            target.gameObject.SetActive(isVisible);
            Collider collider = target.gameObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = isVisible;
        }
        #endregion
    }
}
