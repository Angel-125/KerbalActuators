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
    public class WBIHoverController : PartModule
    {
        [KSPField]
        public float verticalSpeedIncrements = 1f;

        [KSPField(guiActive = true, guiName = "Vertical Speed")]
        public float verticalSpeed = 0f;

        [KSPField(isPersistant = true)]
        public bool hoverActive = false;

        [KSPField]
        public bool guiVisible = true;

        public ModuleEnginesFX engine;

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

        public virtual void StartEngine()
        {
            engine.Activate();
        }

        public virtual void StopEngine()
        {
            engine.Shutdown();
        }

        public virtual void SetEngineThrottle(float throttleValue)
        {
            engine.currentThrottle = throttleValue;
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
            verticalSpeed += verticalSpeedIncrements;
            printSpeed();
            updateSymmetricalSpeeds();
        }

        [KSPEvent(guiActive = true, guiName = "Vertical Speed -")]
        public virtual void DecreaseVerticalSpeed()
        {
            verticalSpeed -= verticalSpeedIncrements;
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
        }

        public virtual void KillVerticalSpeed()
        {
            verticalSpeed = 0f;
            printSpeed();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engine = this.part.FindModuleImplementing<ModuleEnginesFX>();

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
        }

        protected virtual void updateSymmetricalSpeeds()
        {
            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    WBIHoverController hoverController = symmetryPart.GetComponent<WBIHoverController>();
                    if (hoverController != null)
                        hoverController.verticalSpeed = this.verticalSpeed;
                }
            }
        }
    }
}
