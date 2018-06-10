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
    public class WBIMultiModeEngine : PartModule, IEngineStatus
    {
        #region Fields
        [KSPField(guiName = "Current Mode", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        public string currentEngineID = "Multi";

        [KSPField(isPersistant = true)]
        public int currentEngineIndex;

        [KSPField(guiName = "Switch mode on flameout", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        [UI_Toggle(enabledText = "Yes", disabledText = "No")]
        public bool autoSwitch;
        #endregion

        #region Housekeeping
        public ModuleEnginesFX currentEngine;
        List<ModuleEnginesFX> engineList;
        #endregion

        #region Overrides
        public void OnDestroy()
        {
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Get the engine list
            engineList = this.part.FindModulesImplementing<ModuleEnginesFX>();

            //Set up the engines
            int count = engineList.Count;
            for (int index = 0; index < count; index++)
            {
                //Set manual override
                engineList[index].manuallyOverridden = true;
                engineList[index].isEnabled = false;

                //Hide the actions.
                int actionCount = engineList[index].Actions.Count;
                for (int actionIndex = 0; actionIndex < actionCount; actionIndex++)
                    engineList[index].Actions[actionIndex].active = false;
            }

            //Setup current engine
            currentEngine = engineList[currentEngineIndex];
            currentEngineID = currentEngine.engineID;
            if (HighLogic.LoadedSceneIsFlight)
            {
                currentEngine.manuallyOverridden = false;
                currentEngine.isEnabled = true;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Check for flameout
            if (!autoSwitch)
                return;
            if (currentEngine.flameout)
            {
                //Find an engine that can start
                int count = engineList.Count;
                for (int index = 0; index < count; index++)
                {
                    if (index == currentEngineIndex)
                        continue;

                    if (engineList[index].CanStart())
                    {
                        currentEngine.manuallyOverridden = true;
                        currentEngine.isEnabled = false;

                        currentEngine = engineList[index];
                        currentEngineID = currentEngine.engineID;
                        currentEngineIndex = index;
                        currentEngine.manuallyOverridden = false;
                        currentEngine.isEnabled = true;
                        return;
                    }
                }
            }
        }
        #endregion

        #region Events
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next Engine")]
        public void NextEngine()
        {
            //Shut off current engine
            bool isIgnited = currentEngine.EngineIgnited;
            if (isIgnited)
                currentEngine.Shutdown();
            currentEngine.manuallyOverridden = true;
            currentEngine.isEnabled = false;

            //Fire up the new engine
            currentEngineIndex = (currentEngineIndex + 1) % engineList.Count;
            currentEngine = engineList[currentEngineIndex];
            currentEngineID = currentEngine.engineID;
            currentEngine.manuallyOverridden = false;
            currentEngine.isEnabled = true;
            if (isIgnited)
                currentEngine.Activate();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous Engine")]
        public void PreviousEngine()
        {
            //Shut off current engine
            bool isIgnited = currentEngine.EngineIgnited;
            if (isIgnited)
                currentEngine.Shutdown();
            currentEngine.Shutdown();
            currentEngine.manuallyOverridden = true;
            currentEngine.isEnabled = false;

            currentEngineIndex = (currentEngineIndex - 1) % engineList.Count;
            currentEngine = engineList[currentEngineIndex];
            currentEngineID = currentEngine.engineID;
            currentEngine.manuallyOverridden = false;
            currentEngine.isEnabled = true;
            if (isIgnited)
                currentEngine.Activate();
        }
        #endregion

        #region Actions
        [KSPAction("Activate Engine")]
        public void ActivateAction(KSPActionParam param)
        {
            currentEngine.Activate();
        }

        [KSPAction("Shutdown Engine")]
        public void ShutdownAction(KSPActionParam param)
        {
            currentEngine.Shutdown();
        }

        [KSPAction("Activate/Shutdown Engine")]
        public void OnAction(KSPActionParam param)
        {
            if (currentEngine.EngineIgnited)
                currentEngine.Shutdown();
            else
                currentEngine.Activate();
        }

        [KSPAction("Toggle Engine Mode")]
        public void OnToggleModeAction(KSPActionParam param)
        {
            NextEngine();
        }
        #endregion

        #region Helpers
        #endregion

        #region IEngineStatus
        public string engineName
        {
            get 
            {
                return currentEngineID;
            }
        }

        public bool isOperational
        {
            get 
            {
                return currentEngine.isOperational;
            }
        }

        public float normalizedOutput
        {
            get 
            {
                return currentEngine.normalizedOutput;
            }
        }

        public float throttleSetting
        {
            get 
            {
                return currentEngine.throttleSetting;
            }
        }
        #endregion
    }
}
