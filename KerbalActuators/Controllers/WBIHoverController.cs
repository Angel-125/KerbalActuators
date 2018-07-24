using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

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
    #region IHoverController
    /// <summary>
    /// This interface is used by the WBIVTOLManager to control the hover state of an engine.
    /// </summary>
    public interface IHoverController : IGenericController
    {
        /// <summary>
        /// Determines whether or not the engine is active.
        /// </summary>
        /// <returns>True if active, false if not.</returns>
        bool IsEngineActive();

        /// <summary>
        /// Tells the engine to start.
        /// </summary>
        void StartEngine();

        /// <summary>
        /// Tells the engine to shut down.
        /// </summary>
        void StopEngine();

        /// <summary>
        /// Updates the hover state with the current throttle value.
        /// </summary>
        /// <param name="throttleValue">A float containing the throttle value.</param>
        void UpdateHoverState(float throttleValue);

        /// <summary>
        /// Returns the current hover state
        /// </summary>
        /// <returns>true if hover is active, false if not.</returns>
        bool GetHoverState();

        /// <summary>
        /// Tells the controller to set the hover mode.
        /// </summary>
        /// <param name="isActive">True if hover mode is active, false if not.</param>
        void SetHoverMode(bool isActive);

        /// <summary>
        /// Sets the desired vertical speed of the craft.
        /// </summary>
        /// <param name="verticalSpeed">A float containing the desired vertical speed in meters/sec.</param>
        void SetVerticalSpeed(float verticalSpeed);

        /// <summary>
        /// Returns the current vertical speed of the hover controller, in meters/sec.
        /// </summary>
        /// <returns>A float containing the current vertical speed in meters/sec.</returns>
        float GetVerticalSpeed();

        /// <summary>
        /// Tells the hover controller to that the craft should be at 0 vertical speed.
        /// </summary>
        void KillVerticalSpeed();
    }

    /// <summary>
    /// This event tells interested parties that the hover state has been updated.
    /// </summary>
    /// <param name="hoverActive">A flag to indicate whether or not the hover mode is active.</param>
    /// <param name="verticalSpeed">A float value telling the interested party what the vertical speed is, in meters/second.</param>
    public delegate void HoverUpdateEvent(bool hoverActive, float verticalSpeed);
    #endregion

    /// <summary>
    /// The WBIHoverController is designed to help engines figure out what thrust is needed to maintain a desired vertical speed. The hover controller can support multiple engines.
    /// </summary>
    public class WBIHoverController : PartModule, IHoverController
    {
        #region Fields
        /// <summary>
        /// Desired vertical speed. Increments in meters/sec.
        /// </summary>
        [KSPField]
        public float verticalSpeedIncrements = 1f;

        /// <summary>
        /// The current vertical speed.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Vertical Speed")]
        public float verticalSpeed = 0f;

        /// <summary>
        /// A field to indicate whether or not the hover mode is active.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool hoverActive = false;

        /// <summary>
        /// A flag to indicate whether or not the Part Action Window GUI is active.
        /// </summary>
        [KSPField]
        public bool guiVisible = true;

        /// <summary>
        /// Tells the hover controller to update the throttle.
        /// </summary>
        [KSPField]
        public bool updateThrottle = false;

        /// <summary>
        /// A HoverUpdateEvent that's fired when the hover state changes.
        /// </summary>
        public event HoverUpdateEvent onHoverUpdate;

        /// <summary>
        /// The current engine to update during hover state updates.
        /// </summary>
        public ModuleEnginesFX engine;
        #endregion

        #region Housekeeping
        protected MultiModeEngine engineSwitcher;
        protected WBIMultiModeEngine wbiMultiModeEngine;
        protected Dictionary<string, ModuleEnginesFX> multiModeEngines = new Dictionary<string, ModuleEnginesFX>();
        #endregion

        #region API
        /// <summary>
        /// Determines whether or not the controller is active. For instance, you might only have the first controller on a vessel set to active while the rest are inactive.
        /// </summary>
        /// <returns>True if the controller is active, false if not.</returns>
        public bool IsActive()
        {
            return true;
        }

        /// <summary>
        /// This event toggles the hover mode.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Toggle Hover")]
        public virtual void ToggleHoverMode()
        {
            setupEngines();
            if (engine == null)
                return;

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

        /// <summary>
        /// Determines whether or not the hover is active
        /// </summary>
        /// <returns>True if active, false if not</returns>
        public virtual bool GetHoverState()
        {
            return hoverActive;
        }

        /// <summary>
        /// Determines whether or not the engine is active.
        /// </summary>
        /// <returns>True if the engine is active, false if not.</returns>
        public virtual bool IsEngineActive()
        {
            getCurrentEngine();
            if (engine == null)
                return false;

            return engine.isOperational;
        }

        /// <summary>
        /// Tells the controller to start the engine.
        /// </summary>
        [KSPEvent()]
        public virtual void StartEngine()
        {
            getCurrentEngine();
            if (engine == null)
                return;

            engine.Activate();
        }

        /// <summary>
        /// Tells the hover controller to stop the engine.
        /// </summary>
        [KSPEvent()]
        public virtual void StopEngine()
        {
            getCurrentEngine();
            if (engine == null)
                return;

            engine.Shutdown();
        }

        /// <summary>
        /// Tells the hover controller to update its hover state.
        /// </summary>
        /// <param name="throttleValue">A float containing the throttle value to account for during the hover state</param>
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
            {
                throttleState = 0f;

                //Throttle 0 will kill the power effect so play it manually.
                this.part.Effect(engine.powerEffectName, 1.0f);
            }
            else
            {
                throttleState = 1.0f;
            }

            if (updateThrottle)
                this.vessel.ctrlState.mainThrottle = throttleState * (engine.thrustPercentage / 100.0f);
            else
                engine.currentThrottle = throttleState * (engine.thrustPercentage / 100.0f);
        }

        /// <summary>
        /// Sets the hover state in the controller.
        /// </summary>
        /// <param name="isActive">True if hover mode is active, false if not.</param>
        public virtual void SetHoverMode(bool isActive)
        {
            hoverActive = isActive;

            if (hoverActive)
                ActivateHover();
            else
                DeactivateHover();
        }

        /// <summary>
        /// This event increases the vertical speed by verticalSpeedIncrements (in meters/sec)/
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Vertical Speed +")]
        public virtual void IncreaseVerticalSpeed()
        {
            SetVerticalSpeed(verticalSpeed + verticalSpeedIncrements);
            printSpeed();
            updateSymmetricalSpeeds();
        }

        /// <summary>
        /// This event decreases the vertical speed by verticalSpeedIncrements (in meters/sec).
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Vertical Speed -")]
        public virtual void DecreaseVerticalSpeed()
        {
            SetVerticalSpeed(verticalSpeed + verticalSpeedIncrements);
            printSpeed();
            updateSymmetricalSpeeds();
        }

        /// <summary>
        /// This action toggles the hover mode.
        /// </summary>
        /// <param name="param">A KSPActionParam containing action state information.</param>
        [KSPAction("Toggle Hover")]
        public virtual void toggleHoverAction(KSPActionParam param)
        {
            ToggleHoverMode();
        }

        /// <summary>
        /// This action increases the vertical speed by verticalSpeedIncrements (in meters/sec).
        /// </summary>
        /// <param name="param">A KSPActionParam containing action state information.</param>
        [KSPAction("Vertical Speed +")]
        public virtual void increaseVerticalSpeed(KSPActionParam param)
        {
            IncreaseVerticalSpeed();
        }

        /// <summary>
        /// This action decreases the vertical speed by verticalSpeedIncrements (in meters/sec).
        /// </summary>
        /// <param name="param">A KSPActionParam containing action state information.</param>
        [KSPAction("Vertical Speed -")]
        public virtual void decreaseVerticalSpeed(KSPActionParam param)
        {
            DecreaseVerticalSpeed();
        }

        /// <summary>
        /// Sets the desired vertical speed in meters/sec.
        /// </summary>
        /// <param name="verticalSpeed">A float containing the vertical speed in meters/sec.</param>
        public virtual void SetVerticalSpeed(float verticalSpeed)
        {
            //Just set the vertical speed, don't print the new speed or inform symmetry counterparts.
            this.verticalSpeed = verticalSpeed;

            if (onHoverUpdate != null)
                onHoverUpdate(hoverActive, verticalSpeed);
        }

        /// <summary>
        /// Returns the current vertical speed of the hover controller, in meters/sec.
        /// </summary>
        /// <returns>A float containing the current vertical speed in meters/sec.</returns>
        public float GetVerticalSpeed()
        {
            return verticalSpeed;
        }

        /// <summary>
        /// Sets the desired vertical speed to 0.
        /// </summary>
        [KSPEvent()]
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

        /// <summary>
        /// Show or hides the GUI controls in the Part Action Window.
        /// </summary>
        /// <param name="isVisible">True if the controls are visible, false if not.</param>
        public virtual void SetGUIVisible(bool isVisible)
        {
            guiVisible = isVisible;
            Events["ToggleHoverMode"].guiActive = isVisible;
            Events["IncreaseVerticalSpeed"].guiActive = isVisible;
            Events["DecreaseVerticalSpeed"].guiActive = isVisible;
            Fields["verticalSpeed"].guiActive = isVisible;
        }

        /// <summary>
        /// Prints the vertical speed on the screen.
        /// </summary>
        public virtual void printSpeed()
        {
            if (guiVisible)
                ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Climb Rate: " + verticalSpeed, 1f, ScreenMessageStyle.UPPER_CENTER));
        }

        /// <summary>
        /// Activates hover mode.
        /// </summary>
        [KSPEvent()]
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

        /// <summary>
        /// Deactivates hover mode.
        /// </summary>
        [KSPEvent()]
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
        #endregion

        #region Helpers
        protected void getCurrentEngine()
        {
            if (engine == null)
                setupEngines();

            if (wbiMultiModeEngine != null)
                engine = wbiMultiModeEngine.currentEngine;

            //If we have multiple engines, make sure we have the current one.
            else if (engineSwitcher != null)
            {
                if (engineSwitcher.runningPrimary)
                    engine = multiModeEngines[engineSwitcher.primaryEngineID];
                else
                    engine = multiModeEngines[engineSwitcher.secondaryEngineID];
            }
        }

        protected void setupEngines()
        {
            wbiMultiModeEngine = this.part.FindModuleImplementing<WBIMultiModeEngine>();
            if (wbiMultiModeEngine != null)
            {
                engine = wbiMultiModeEngine.currentEngine;
                return;
            }

            //See if we have multiple engines that we need to support
            engineSwitcher = this.part.FindModuleImplementing<MultiModeEngine>();
            if (engineSwitcher != null)
            {
                List<ModuleEnginesFX> engines = this.part.FindModulesImplementing<ModuleEnginesFX>();

                foreach (ModuleEnginesFX multiEngine in engines)
                {
                    multiModeEngines.Add(multiEngine.engineID, multiEngine);
                }

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
        #endregion
    }
}
