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
    #region IPropSpinner
    /// <summary>
    /// This interface is used to toggle the thrust transform of a prop spinner.
    /// </summary>
    public interface IPropSpinner
    {
        /// <summary>
        /// Toggles the thrust from forward to reverse and back again.
        /// </summary>
        void ToggleThrust();

        /// <summary>
        /// Sets the reverse thrust.
        /// </summary>
        /// <param name="isReverseThrust">True if reverse thrust, false if forward thrust.</param>
        void SetReverseThrust(bool isReverseThrust);
    }
    #endregion

    /// <summary>
    /// This class is designed to spin propeller meshes for propeller-driven engines. It supports both propeller blades and blurred propeller meshes.
    /// </summary>
    public class WBIPropSpinner : PartModule, IPropSpinner
    {
        #region Fields
        /// <summary>
        /// Localized name of forward thrust action
        /// </summary>
        [KSPField]
        public string forwardThrustActionName = "Set Forward Thrust";

        /// <summary>
        /// Localized name of reverse thrust action
        /// </summary>
        [KSPField]
        public string reverseThrustActionName = "Set Reverse Thrust";

        /// <summary>
        /// Flag to indicate if the controller is operating in reverse-thrust mode.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool reverseThrust;

        /// <summary>
        /// Name of the thrust transform for forward thrust.
        /// </summary>
        [KSPField]
        public string thrustTransform = "thrustTransform";

        /// <summary>
        /// Name of the thrust transform for reverse-thrust.
        /// </summary>
        [KSPField]
        public string reverseThrustTransform = "reverseThrustTransform";

        /// <summary>
        /// Flag to indicate whether or not the controller can reverse thrust.
        /// </summary>
        [KSPField]
        public bool canReverseThrust = true;

        /// <summary>
        /// Name of animation for reversed thrust
        /// </summary>
        [KSPField]
        public string reverseThrustAnimation = string.Empty;

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
        /// At what percentage of thrust to switch to the blurred rotor/mesh rotor.
        /// </summary>
        [KSPField]
        public float minThrustRotorBlur = 0.25f;

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
        /// Flag to indicate that the controller should be in hover mode.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isHovering;

        /// <summary>
        /// Flag to indicate whether or not the Part Action Window gui controls are visible.
        /// </summary>
        [KSPField]
        public bool guiVisible = true;

        /// <summary>
        /// Flag to indicate whether or not part module actions are visible.
        /// </summary>
        [KSPField]
        public bool actionsVisible = true;

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

        /// <summary>
        /// Indicates whether or not turning on/off RCS will start/stop the rotors. This only applies if the part has no engine part module.
        /// Default is false.
        /// </summary>
        [KSPField]
        public bool enabledByRCS = false;

        /// <summary>
        /// When the spinner is controlled by RCS, the throttle controls spin rate.
        /// This field specifies at which level of throttle to blur the rotors.
        /// </summary>
        [KSPField]
        public float minThrottleBlur = 20.0f;
        #endregion

        #region Housekeeping
        protected float currentRotationAngle;
        protected ERotationStates rotationState = ERotationStates.Locked;
        protected float degPerUpdate;
        protected Transform rotorTransform = null;
        protected Transform[] fwdThrustTransform;
        protected Transform[] revThrustTransform;
        protected Vector3 rotationAxis = new Vector3(0, 0, 1);
        protected float currentThrustNormalized = 0f;
        protected float targetThrustNormalized = 0f;
        protected float currentSpoolRate;
        protected ModuleEnginesFX engine;
        protected MultiModeEngine engineSwitcher;
        protected Dictionary<string, ModuleEnginesFX> multiModeEngines = new Dictionary<string, ModuleEnginesFX>();
        Transform blurredRotorTransform = null;
        Transform[] standardBlades = null;
        Transform[] mirroredBlades = null;
        AnimationState reverseThrustAnimationState;
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (rotorTransform == null)
                return;
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Spin the rotors if the engine/RCS is running
            if (engineIsRunning())
                rotatePropellersRunning();

            //Shut down the rotors if the engine isn't running.
            else
                rotatePropellersShutdown();
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

            //Setup engine(s)
            setupEngines();

            //Setup events
            WBIRotationController rotationController = this.part.FindModuleImplementing<WBIRotationController>();
            if (rotationController != null)
            {
                mirrorRotation = rotationController.mirrorRotation;
                rotationController.onRotatorMirrored += MirrorRotation;
            }

            WBIHoverController hoverController = this.part.FindModuleImplementing<WBIHoverController>();
            if (hoverController != null)
                hoverController.onHoverUpdate += onHoverUpdate;

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

            //Setup actions
            if (!actionsVisible || engine == null)
            {
                Actions["ToggleThrustTransformAction"].actionGroup = KSPActionGroup.None;
                Actions["ToggleThrustTransformAction"].active = false;
            }

            //Setup the thrust transform
            if (engine != null)
            {
                if (canReverseThrust)
                {
                    fwdThrustTransform = this.part.FindModelTransforms(thrustTransform);
                    revThrustTransform = this.part.FindModelTransforms(reverseThrustTransform);

                    if (fwdThrustTransform.Length >= 0 && revThrustTransform.Length >= 0)
                    {
                        SetupThrustTransform();
                        SetupAnimation();
                    }

                    else
                    {
                        Actions["ToggleThrustTransformAction"].actionGroup = KSPActionGroup.None;
                        Events["ToggleThrustTransform"].active = false;
                        Actions["ToggleThrustTransformAction"].active = false;
                    }
                }
                else
                {
                    fwdThrustTransform = this.part.FindModelTransforms(thrustTransform);
                    revThrustTransform = fwdThrustTransform;
                }
            }

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
        /// This action toggles the thrust transforms from forward to reverse and back.
        /// </summary>
        /// <param name="param">A KSPActionParam with action state information.</param>
        [KSPAction("Toggle Fwd/Rev Thrust")]
        public virtual void ToggleThrustTransformAction(KSPActionParam param)
        {
            ToggleThrustTransform();
        }

        /// <summary>
        /// This event toggles the thrust transforms from forward to reverse and back. It also plays the thrust reverse animation, if any.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Thrust: Forward")]
        public void ToggleThrustTransform()
        {
            if (engine == null)
                return;

            reverseThrust = !reverseThrust;
            SetReverseThrust(reverseThrust);

            //Don't forget the symmetrical parts...
            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    WBIPropSpinner propController = symmetryPart.GetComponent<WBIPropSpinner>();
                    if (propController != null)
                    {
                        propController.SetReverseThrust(reverseThrust);
                    }
                }
            }

        }

        /// <summary>
        /// Sets the thrust mode and plays the associated reverse-thrust animation if any.
        /// </summary>
        /// <param name="isReverseThrust">True if the thrust is reversed, false if not.</param>
        public virtual void SetReverseThrust(bool isReverseThrust)
        {
            if (engine == null)
                return;

            reverseThrust = isReverseThrust;
            SetupThrustTransform();
            HandleReverseThrustAnimation();

            //Look for other prop spinners in this part and reverse them too.
            List<WBIPropSpinner> spinners = this.part.FindModulesImplementing<WBIPropSpinner>();
            int count = spinners.Count;
            for (int index = 0; index < count; index++)
            {
                if (spinners[index] == this)
                {
                    continue;
                }
                spinners[index].reverseThrust = this.reverseThrust;
                spinners[index].SetupThrustTransform();
                spinners[index].HandleReverseThrustAnimation();
            }
        }

        /// <summary>
        /// Toggles the thrust from forward to back or back to forward and plays the animation, if any.
        /// </summary>
        public virtual void ToggleThrust()
        {
            if (engine == null)
                return;

            reverseThrust = !reverseThrust;
            SetupThrustTransform();
            HandleReverseThrustAnimation();
        }

        /// <summary>
        /// Shows or Hides the Part Action Window GUI controls associated with the controller.
        /// </summary>
        /// <param name="isVisible">True if the controls should be shown, false if not.</param>
        public virtual void SetGUIVisible(bool isVisible)
        {
            Events["ToggleThrustTransform"].active = isVisible;
        }

        /// <summary>
        /// Sets up the thrust transforms.
        /// </summary>
        public void SetupThrustTransform()
        {
            if (engine == null)
                return;

            //We have separate forward and reverse thrust transforms. Switch them out.
            engine.thrustTransforms.Clear();
            if (reverseThrust)
            {
                Events["ToggleThrustTransform"].guiName = Localizer.Format(forwardThrustActionName);
                for (int i = 0; i < revThrustTransform.Length; i++)
                {
                    engine.thrustTransforms.Add(revThrustTransform[i]);
                }

            }

            else
            {
                Events["ToggleThrustTransform"].guiName = Localizer.Format(reverseThrustActionName);
                for (int i = 0; i < fwdThrustTransform.Length; i++)
                {
                    engine.thrustTransforms.Add(fwdThrustTransform[i]);
                }
            }
        }

        /// <summary>
        /// Sets up the thrust animation.
        /// </summary>
        protected void SetupAnimation()
        {
            // Set up animation if needed
            if (!String.IsNullOrEmpty(reverseThrustAnimation))
            {
                Animation anim = part.FindModelAnimator(reverseThrustAnimation);
                reverseThrustAnimationState = anim[reverseThrustAnimation];
                reverseThrustAnimationState.time = 0;
                reverseThrustAnimationState.speed = 0;
                reverseThrustAnimationState.layer = animationLayer;
                reverseThrustAnimationState.enabled = true;
                reverseThrustAnimationState.wrapMode = WrapMode.ClampForever;
                anim.Blend(reverseThrustAnimation);

            }
        }

        /// <summary>
        /// Plays the reverse thrust animation, if any.
        /// </summary>
        public void HandleReverseThrustAnimation()
        {
            if (reverseThrustAnimationState != null)
            {

                if (reverseThrust)
                {
                    reverseThrustAnimationState.enabled = true;
                    reverseThrustAnimationState.speed = 1f;
                }
                else
                {
                    reverseThrustAnimationState.enabled = true;
                    reverseThrustAnimationState.speed = -1f;
                }
            }
        }

        #endregion

        #region Helpers
        protected void setupEngines()
        {
            //See if we have multiple engines that we need to support
            engineSwitcher = this.part.FindModuleImplementing<MultiModeEngine>();
            if (engineSwitcher != null)
            {
                List<ModuleEnginesFX> engines = this.part.FindModulesImplementing<ModuleEnginesFX>();

                foreach (ModuleEnginesFX multiEngine in engines)
                {
                    multiModeEngines.Add(multiEngine.engineID, multiEngine);

                }

                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];

                return;
            }

            //Normal case: we only have one engine to support
            engine = this.part.FindModuleImplementing<ModuleEnginesFX>();
        }

        protected bool engineIsRunning()
        {
            //Check RCS state if needed
            if (engine == null)
            {
                if (!enabledByRCS)
                    return false;

                return FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS];
            }

            //If we have multiple engines, make sure we have the current one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }

            //No engine? Then it's clearly not running...
            if (engine == null)
                return false;

            //Check operation status
            if (!engine.isOperational || !engine.EngineIgnited)
                return false;
            else
                return true;
        }

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

            if (reverseThrust)
                rotationPerFrame *= -1.0f;


            rotorTransform.Rotate(rotationAxis * rotationPerFrame);
        }

        protected float getThrustThrottleRatio()
        {
            if (engine != null)
            {
                return engine.finalThrust / engine.maxThrust;
            }

            else if (enabledByRCS && FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS])
            {
                return FlightInputHandler.state.mainThrottle;
            }

            else
            {
                return 0f;
            }
        }

        protected void rotatePropellersRunning()
        {
            rotationState = ERotationStates.Spinning;
            float minRatio = minThrustRotorBlur / 100.0f;
            if (enabledByRCS)
                minRatio = minThrottleBlur / 100.0f;

            //If the thrust/throttle ratio is >= minimum ratio then show the blurred rotors
            float thrustThrottleRatio = getThrustThrottleRatio();
            if (thrustThrottleRatio >= minRatio || (isBlurred && isHovering))
            {
                if (!isBlurred)
                {
                    isBlurred = true;
                    currentSpoolRate = 1.0f;
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

                if (reverseThrust)
                    rotationPerFrame *= -1.0f;
                blurredRotorTransform.Rotate(rotationAxis * rotationPerFrame);
            }

            //Rotate the non-blurred rotor until thrust/throttle ratio >= minRatio
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

                if (reverseThrust)
                    rotationPerFrame *= -1.0f;
                rotorTransform.Rotate(rotationAxis * rotationPerFrame);
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

        protected void onHoverUpdate(bool hoverActive, float verticalSpeed)
        {
            isHovering = hoverActive;
        }
        #endregion
    }
}
