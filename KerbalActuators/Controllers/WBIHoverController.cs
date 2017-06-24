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
    public interface IHoverController
    {
        bool IsEngineActive();
        void StartEngine();
        void StopEngine();
        void UpdateHoverState(float throttleValue);
        void SetHoverMode(bool isActive);
        void SetVerticalSpeed(float verticalSpeed);
        void KillVerticalSpeed();
    }

    public delegate void HoverUpdateEvent(bool hoverActive, float verticalSpeed);

    public class WBIHoverController : PartModule, IHoverController
    {
        [KSPField]
        public float verticalSpeedIncrements = 1f;

        [KSPField(guiActive = true, guiName = "Vertical Speed")]
        public float verticalSpeed = 0f;

        [KSPField(isPersistant = true)]
        public bool hoverActive = false;

        [KSPField]
        public bool guiVisible = true;

        public event HoverUpdateEvent onHoverUpdate;
        public ModuleEnginesFX engine;

        protected MultiModeEngine engineSwitcher;
        protected Dictionary<string, ModuleEnginesFX> multiModeEngines = new Dictionary<string, ModuleEnginesFX>();

        [KSPEvent(guiActive = true, guiName = "Toggle Hover")]
        public virtual void ToggleHoverMode()
        {
            hoverActive = !hoverActive;
            if (hoverActive)
                ActivateHover();
            else
                DeactivateHover();

            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    WBIHoverController hoverController = symmetryPart.GetComponent<WBIHoverController>();
                    if (hoverController != null)
                    {
                        if (hoverActive)
                            hoverController.ActivateHover();
                        else
                            hoverController.DeactivateHover();
                    }
                }
            }
        }

        public virtual bool IsEngineActive()
        {
            getCurrentEngine();

            return engine.isOperational;
        }

        public virtual void StartEngine()
        {
            getCurrentEngine();
            if (engine == null)
                return;

            engine.Activate();
        }

        public virtual void StopEngine()
        {
            getCurrentEngine();
            if (engine == null)
                return;

            engine.Shutdown();
        }

        public virtual void UpdateHoverState(float throttleValue)
        {
            getCurrentEngine();
            if (engine == null)
                return;

            //This is crude but effective. What we do is jitter the engine throttle up and down to maintain desired vertical speed.
            //It tends to vibrate the engines but they're ok. This will have to do until I can figure out the relation between
            //engine.finalThrust, engine.maxThrust, and the force needed to make the craft hover.
            float throttleState = 0;
            if (this.part.vessel.verticalSpeed >= verticalSpeed)
                throttleState = 0f;
            else
                throttleState = 1.0f;

            engine.currentThrottle = throttleState * engine.thrustPercentage / 100.0f;
        }

        public virtual void SetHoverMode(bool isActive)
        {
            hoverActive = isActive;

            if (hoverActive)
                ActivateHover();
            else
                DeactivateHover();
        }

        [KSPEvent(guiActive = true, guiName = "Vertical Speed +")]
        public virtual void IncreaseVerticalSpeed()
        {
            SetVerticalSpeed(verticalSpeed + verticalSpeedIncrements);
            printSpeed();
            updateSymmetricalSpeeds();
        }

        [KSPEvent(guiActive = true, guiName = "Vertical Speed -")]
        public virtual void DecreaseVerticalSpeed()
        {
            SetVerticalSpeed(verticalSpeed + verticalSpeedIncrements);
            printSpeed();
            updateSymmetricalSpeeds();
        }

        [KSPAction("Toggle Hover")]
        public virtual void toggleHoverAction(KSPActionParam param)
        {
            ToggleHoverMode();
        }

        [KSPAction("Vertical Speed +")]
        public virtual void increaseVerticalSpeed(KSPActionParam param)
        {
            IncreaseVerticalSpeed();
        }

        [KSPAction("Vertical Speed -")]
        public virtual void decreaseVerticalSpeed(KSPActionParam param)
        {
            DecreaseVerticalSpeed();
        }

        public virtual void SetVerticalSpeed(float verticalSpeed)
        {
            //Just set the vertical speed, don't print the new speed or inform symmetry counterparts.
            this.verticalSpeed = verticalSpeed;

            if (onHoverUpdate != null)
                onHoverUpdate(hoverActive, verticalSpeed);
        }

        public virtual void KillVerticalSpeed()
        {
            verticalSpeed = 0f;
            printSpeed();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Setup engine(s)
            setupEngines();

            //Set hover state
            if (hoverActive)
                ActivateHover();

            //Set gui visible state
            SetGUIVisible(guiVisible);
        }

        public virtual void SetGUIVisible(bool isVisible)
        {
            guiVisible = isVisible;
            Events["ToggleHoverMode"].guiActive = isVisible;
            Events["IncreaseVerticalSpeed"].guiActive = isVisible;
            Events["DecreaseVerticalSpeed"].guiActive = isVisible;
            Fields["verticalSpeed"].guiActive = isVisible;
        }

        public virtual void printSpeed()
        {
            if (guiVisible)
                ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Climb Rate: " + verticalSpeed, 1f, ScreenMessageStyle.UPPER_CENTER));
        }

        public virtual void ActivateHover()
        {
            hoverActive = true;
            verticalSpeed = 0f;

            if (guiVisible)
            {
                Events["ToggleHoverMode"].guiName = "Turn Off Hover Mode";
                Events["IncreaseVerticalSpeed"].guiActive = true;
                Events["DecreaseVerticalSpeed"].guiActive = true;
                Fields["verticalSpeed"].guiActive = true;
                ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Mode On", 1f, ScreenMessageStyle.UPPER_CENTER));
            }

            if (onHoverUpdate != null)
                onHoverUpdate(hoverActive, verticalSpeed);
        }

        public virtual void DeactivateHover()
        {
            hoverActive = false;
            verticalSpeed = 0f;

            if (guiVisible)
            {
                Events["ToggleHoverMode"].guiName = "Turn On Hover Mode";
                Events["IncreaseVerticalSpeed"].guiActive = false;
                Events["DecreaseVerticalSpeed"].guiActive = false;
                Fields["verticalSpeed"].guiActive = false;
                ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Mode Off", 1f, ScreenMessageStyle.UPPER_CENTER));
            }

            if (onHoverUpdate != null)
                onHoverUpdate(hoverActive, verticalSpeed);
        }

        protected void getCurrentEngine()
        {
            //If we have multiple engines, make sure we have the current one.
            if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
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

                foreach (string key in multiModeEngines.Keys)

                engine = multiModeEngines[engineSwitcher.primaryEngineID];
                return;
            }

            //Normal case: we only have one engine to support
            engine = this.part.FindModuleImplementing<ModuleEnginesFX>();
        }

        protected virtual void updateSymmetricalSpeeds()
        {
            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    WBIHoverController hoverController = symmetryPart.GetComponent<WBIHoverController>();
                    if (hoverController != null)
                        hoverController.SetVerticalSpeed(this.verticalSpeed);
                }
            }
        }
    }
}
