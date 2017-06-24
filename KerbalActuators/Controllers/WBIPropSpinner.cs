using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

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
    public interface IPropSpinner
    {
        void ToggleThrust();
        void SetReverseThrust(bool isReverseThrust);
    }

    public class WBIPropSpinner : PartModule, IPropSpinner
    {
        // Localized name of forward thrust action
        [KSPField]
        public string forwardThrustActionName = "Set Forward Thrust";

        // Localized name of reverse thrust action
        [KSPField]
        public string reverseThrustActionName = "Set Reverse Thrust";

        [KSPField(isPersistant = true)]
        public bool reverseThrust;

        [KSPField]
        public string thrustTransform = "thrustTransform";

        [KSPField]
        public string reverseThrustTransform = "reverseThrustTransform";

        [KSPField]
        public bool canReverseThrust = true;

        // Name of part animation for reversed thrust
        [KSPField]
        public string reverseThrustAnimation = string.Empty;

        // Layer of the animation
        [KSPField]
        public int animationLayer = 1;

        //Name of the non-blurred rotor
        //The whole thing spins
        [KSPField]
        public string rotorTransformName = string.Empty;

        //(Optional) To properly mirror the engine, these parameters specify
        //the standard and mirrored (symmetrical) rotor blade transforms.
        //If included, they MUST be child meshes of the mesh specified by rotorTransformName.
        [KSPField]
        public string standardBladesName = string.Empty;

        [KSPField]
        public string mirrorBladesName = string.Empty;

        //Rotor axis of rotation
        [KSPField()]
        public string rotorRotationAxis = "0,0,1";

        //How fast to spin the rotor
        [KSPField]
        public float rotorRPM = 30.0f;

        //How fast to spin up or slow down the rotors until they reach rotorRPM
        [KSPField]
        public float rotorSpoolTime = 3.0f;

        //How fast to spin the rotor when blurred; multiply rotorRPM by blurredRotorFactor
        [KSPField]
        public float blurredRotorFactor = 4.0f;

        //At what percentage of thrust to switch to the blurred rotor/mesh rotor.
        [KSPField]
        public float minThrustRotorBlur = 0.25f;

        //Name of the blurred rotor
        [KSPField]
        public string blurredRotorName = string.Empty;

        //How fast to spin the blurred rotor
        [KSPField]
        public float blurredRotorRPM;

        [KSPField(isPersistant = true)]
        public bool isBlurred;

        [KSPField(isPersistant = true)]
        public bool mirrorRotation;

        [KSPField(isPersistant = true)]
        public bool isHovering;

        [KSPField]
        public bool guiVisible = true;

        [KSPField]
        public bool actionsVisible = true;

        //During the shutdown process, how fast, in degrees/sec, do the rotors rotate to neutral?
        [KSPField]
        public float neutralSpinRate = 10.0f;

        protected float currentRotationAngle;
        protected ERotationStates rotationState = ERotationStates.Locked;
        protected float degPerUpdate;
        protected Transform rotorTransform = null;
        protected Transform fwdThrustTransform = null;
        protected Transform revThrustTransform = null;
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

        public void MirrorRotation(bool isMirrored)
        {
            mirrorRotation = isMirrored;
            setupRotorTransforms();
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
            HandleReverseThrustAnimation();

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
                        propController.HandleReverseThrustAnimation();
                    }
                }
            }

        }

        public virtual void SetReverseThrust(bool isReverseThrust)
        {
            reverseThrust = isReverseThrust;
            SetupThrustTransform();
            HandleReverseThrustAnimation();
        }

        public virtual void ToggleThrust()
        {
            reverseThrust = !reverseThrust;
            SetupThrustTransform();
            HandleReverseThrustAnimation();
        }

        public virtual void SetGUIVisible(bool isVisible)
        {
            Events["ToggleThrustTransform"].active = isVisible;
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
            if (!actionsVisible)
            {
                Actions["ToggleThrustTransformAction"].actionGroup = KSPActionGroup.None;
                Actions["ToggleThrustTransformAction"].active = false;
            }

            //Setup the thrust transform
            if (canReverseThrust)
            {
                fwdThrustTransform = this.part.FindModelTransform(thrustTransform);
                revThrustTransform = this.part.FindModelTransform(reverseThrustTransform);

                if (fwdThrustTransform != null && revThrustTransform != null)
                {
                    SetupThrustTransform();
                    SetupAnimation();
                }

                else
                {
                    Events["ToggleThrustTransform"].active = false;
                    Actions["ToggleThrustTransformAction"].active = false;
                }
                

            }

            //Rotor transforms
            setupRotorTransforms();
        }

        public void FixedUpdate()
        {
            if (engine == null || rotorTransform == null)
                return;
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //If the engine isn't running, then slow and stop the rotors.
            if (!engineIsRunning())
                rotatePropellersShutdown();

            //If the rotor is deployed, and the engine is running, then rotate the rotors.
            else
                rotatePropellersRunning();
        }

        public void SetupThrustTransform()
        {
            //We have separate forward and reverse thrust transforms. Switch them out.
            engine.thrustTransforms.Clear();
            if (reverseThrust)
            {
                Events["ToggleThrustTransform"].guiName = Localizer.Format(forwardThrustActionName);
                engine.thrustTransforms.Add(revThrustTransform);
            }

            else
            {
                Events["ToggleThrustTransform"].guiName = Localizer.Format(reverseThrustActionName);
                engine.thrustTransforms.Add(fwdThrustTransform);
            }
        }

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
                        rotorTransform.transform.localEulerAngles = (rotationAxis * currentRotationAngle);
                    else
                        rotorTransform.transform.localEulerAngles = (rotationAxis * -currentRotationAngle);
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

        protected void rotatePropellersRunning()
        {
            rotationState = ERotationStates.Spinning;
            float minThrustRatio = minThrustRotorBlur / 100.0f;

            //If the engine thrust is >= 25% then show the blurred rotors
            float thrustRatio = engine.finalThrust / engine.maxThrust;
            if (thrustRatio >= minThrustRatio || (isBlurred && isHovering))
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

                if (reverseThrust)
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
    }
}
