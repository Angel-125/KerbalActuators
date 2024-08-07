﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;

/*
Source code copyrighgt 2017-2019, by Michael Billard (Angel-125)
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
    public delegate void DrawControllerDelegate();

    public class KAEvents
    {
        public static EventData<Part, float> onControllerUpdatedVerticalSpeed = new EventData<Part, float>("onControllerUpdatedVerticalSpeed");
        public static EventData<Part, bool> onControllerUpdatedHoverActive = new EventData<Part, bool>("onControllerUpdatedHoverActive");
    }

    public struct WBICustomDrawController
    {
        public PartModule partModule;
        public string methodName;
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class WBIVTOLManager : MonoBehaviour
    {
        public const string kEngineGroup = "Engine";
        public const string LABEL_HOVER = "HOVR";
        public const string LABEL_VSPDINC = "VSPD +";
        public const string LABEL_VSPDZERO = "VSPD 0";
        public const string LABEL_VSPDDEC = "VSPD -";

        public static WBIVTOLManager Instance;
        public List<WBICustomDrawController> drawControllers = new List<WBICustomDrawController>();

        public WBIThrustModes thrustMode;
        public bool hoverActive = false;
        public float verticalSpeed = 0f;
        public float verticalSpeedIncrements = 1f;
        public Vessel vessel;
        public Dictionary<string, KeyCode> controlCodes = new Dictionary<string, KeyCode>();

        private IAirParkController airParkController;
        private IHoverController[] hoverControllers;
        private IRotationController[] rotationControllers;
        private IThrustVectorController[] thrustVectorControllers;
        private ICustomController[] customControllers;
        private HoverVTOLGUI hoverGUI = null;

        #region API
        public static void AddCustomDrawController(PartModule module, string methodName)
        {
            if (!CustomDrawControllerExists(module))
            {
                WBICustomDrawController controller = new WBICustomDrawController();
                controller.partModule = module;
                controller.methodName = methodName;

                Instance.drawControllers.Add(controller);
            }
        }

        public static void RemoveCustomDrawController(PartModule module)
        {
            int count = Instance.drawControllers.Count;
            WBICustomDrawController[] controllers = Instance.drawControllers.ToArray();
            for (int index = 0; index < count; index++)
            {
                if (controllers[index].partModule == module)
                {
                    Instance.drawControllers.Remove(controllers[index]);
                    return;
                }
            }
        }

        public static bool CustomDrawControllerExists(PartModule module)
        {
            int count = Instance.drawControllers.Count;
            for (int index = 0; index < count; index++)
            {
                if (Instance.drawControllers[index].partModule == module)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an array of IGenericController interfaces if there are any. IGenericController is the base interface for controllers like IHoverController and IRotationController.
        /// </summary>
        /// <returns>An array containing IGenericController interfaces if there are any controllers, or null if there are none.</returns>
        public IGenericController[] GetAllControllers()
        {
            List<IGenericController> controllers = new List<IGenericController>();

            //Air Park
            if (airParkController != null)
                controllers.Add(airParkController);

            //Hover controllers
            if (hoverControllers != null)
            {
                for (int index = 0; index < hoverControllers.Length; index++)
                    controllers.Add(hoverControllers[index]);
            }

            //Rotation controllers
            if (hoverControllers != null)
            {
                for (int index = 0; index < hoverControllers.Length; index++)
                    controllers.Add(hoverControllers[index]);
            }

            //Thrust Vector controllers
            if (thrustVectorControllers != null)
            {
                for (int index = 0; index < thrustVectorControllers.Length; index++)
                    controllers.Add(thrustVectorControllers[index]);
            }

            //Custom controllers
            if (customControllers != null)
            {
                for (int index = 0; index < customControllers.Length; index++)
                    controllers.Add(customControllers[index]);
            }

            //Return the controllers
            if (controllers.Count > 0)
                return controllers.ToArray();
            else
                return null;
        }

        public void FindControllers(Vessel vessel)
        {
            this.vessel = vessel;
            FindHoverControllers();
            FindRotationControllers();
            FindAirParkControllers();
            FindThrustVectorControllers();
            FindCustomControllers();
        }

        public void Start()
        {
            WBIVTOLManager.Instance = this;
            GameEvents.onVesselLoaded.Add(VesselWasLoaded);
            GameEvents.onVesselChange.Add(VesselWasChanged);
            GameEvents.onStageActivate.Add(OnStageActivate);
            KAEvents.onControllerUpdatedVerticalSpeed.Add(onControllerUpdatedVerticalSpeed);
            KAEvents.onControllerUpdatedHoverActive.Add(onControllerUpdatedHoverActive);

            hoverGUI = new HoverVTOLGUI();
            hoverGUI.vtolManager = this;
            hoverGUI.hoverSetupGUI.vtolManager = this;

            this.vessel = FlightGlobals.ActiveVessel;
        }

        public void Destroy()
        {
            GameEvents.onVesselLoaded.Remove(VesselWasLoaded);
            GameEvents.onVesselChange.Remove(VesselWasChanged);
            GameEvents.onStageActivate.Remove(OnStageActivate);
            KAEvents.onControllerUpdatedVerticalSpeed.Remove(onControllerUpdatedVerticalSpeed);
            KAEvents.onControllerUpdatedHoverActive.Remove(onControllerUpdatedHoverActive);
        }

        public void ToggleGUI()
        {
            if (!hoverGUI.IsVisible())
            {
                ShowGUI();
            }
            else
            {
                hoverGUI.SetVisible(false);
            }
        }

        public void ShowGUI()
        {
            FindControllers(FlightGlobals.ActiveVessel);

            hoverGUI.canDrawParkingControls = airParkController != null ? true : false;
            hoverGUI.canDrawHoverControls = hoverControllers != null ? true : false;
            hoverGUI.canDrawRotationControls = rotationControllers != null ? true : false;
            hoverGUI.canDrawThrustControls = thrustVectorControllers != null ? true : false;
            hoverGUI.enginesActive = EnginesAreActive();
            hoverGUI.canRotateMax = CanRotateMax();
            hoverGUI.canRotateMin = CanRotateMin();

            hoverGUI.SetVisible(true);
        }

        public void Update()
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyUp(KeyCode.H))
            {
                ToggleHover();

                if (hoverActive)
                    ScreenMessages.PostScreenMessage(new ScreenMessage("Hover ON", 1f, ScreenMessageStyle.UPPER_LEFT));
                else
                    ScreenMessages.PostScreenMessage(new ScreenMessage("Hover OFF", 1f, ScreenMessageStyle.UPPER_LEFT));
            }

            if (hoverActive)
            {
                if (GameSettings.TRANSLATE_UP.GetKeyUp())
                {
                    IncreaseVerticalSpeed();
                    if (verticalSpeed.Equals(0))
                        KillVerticalSpeed();
                    printSpeed();
                }

                if (GameSettings.TRANSLATE_DOWN.GetKeyUp())
                {
                    DecreaseVerticalSpeed();
                    if (verticalSpeed.Equals(0))
                        KillVerticalSpeed();
                    printSpeed();
                }

                if (GameSettings.BRAKES.GetKeyUp())
                {
                    KillVerticalSpeed();
                    printSpeed();
                }
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (hoverControllers == null)
                return;
            if (hoverControllers.Length == 0)
                return;
            if (!hoverActive)
                return;

            //Check to see if there is any hover controller that is deactivated and if so, toggle our state.
            bool allHoverStatesActive = true;
            for (int index = 0; index < hoverControllers.Length; index++)
            {
                allHoverStatesActive = hoverControllers[index].GetHoverState();
            }

            if (!allHoverStatesActive)
                ToggleHover();
        }
        #endregion

        #region IAirParkController
        public void FindAirParkControllers()
        {
            airParkController = FlightGlobals.ActiveVessel.FindPartModuleImplementing<IAirParkController>();
        }

        /// <summary>
        /// Determines whether or not the air park controller is active.
        /// </summary>
        /// <returns>True if active, false if not.</returns>
        public bool AirParkControllerActive()
        {
            return (airParkController == null) ? false : true;
        }

        /// <summary>
        /// Returns the Air Park controller, if any.
        /// </summary>
        /// <returns>An IAirParkController interface if the vessel has an air park controller, or null if not.</returns>
        public IAirParkController GetAirParkController()
        {
            return airParkController;
        }

        public bool IsParked()
        {
            if (airParkController == null)
                return true;

            return airParkController.IsParked();
        }

        public void TogglePark()
        {
            if (airParkController != null)
                airParkController.TogglePark();
        }

        public string GetSituation()
        {
            if (airParkController != null)
                return airParkController.GetSituation();
            else
                return "N/A";
        }

        public void SetPark(bool isParked)
        {
            if (airParkController != null)
            {
                airParkController.SetParking(isParked);
            }
        }
        #endregion

        #region IRotationController
        public void FindRotationControllers()
        {
            List<IRotationController> controllers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<IRotationController>();
            List<IRotationController> rotationControllerList = new List<IRotationController>();

            //Find all the controllers that belong to the Engine list.
            IRotationController[] rotationItems = controllers.ToArray();
            for (int index = 0; index < rotationItems.Length; index++)
            {
                if (rotationItems[index].GetGroupID() == kEngineGroup)
                    rotationControllerList.Add(rotationItems[index]);
            }

            if (rotationControllerList.Count > 0)
                rotationControllers = rotationControllerList.ToArray();
        }

        /// <summary>
        /// Determines whether or not the vessel has rotation controllers.
        /// </summary>
        /// <returns>True if there are rotation controllers, false if not.</returns>
        public bool HasRotationControllers()
        {
            return (rotationControllers == null || rotationControllers.Length == 0) ? false : true;
        }

        /// <summary>
        /// Returns the rotation controllers, if any.
        /// </summary>
        /// <returns>An array of IRotationController interfaces if the vessel has rotation controllers, or null if not.</returns>
        public IRotationController[] GetRotationControllers()
        {
            return rotationControllers;
        }

        public float GetMinRotation()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return -1.0f;

            //Making the assumption that the vessel could have more than one type of rotating engine, and we want the ones that are active.
            for (int index = 0; index < rotationControllers.Length; index++)
            {
                if (rotationControllers[index].IsActive())
                {
                    return rotationControllers[index].GetMinRotation();
                }
            }

            return -1.0f;
        }

        public float GetMaxRotation()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return -1.0f;

            //Making the assumption that the vessel could have more than one type of rotating engine, and we want the ones that are active.
            for (int index = 0; index < rotationControllers.Length; index++)
            {
                if (rotationControllers[index].IsActive())
                {
                    return rotationControllers[index].GetMaxRotation();
                }
            }

            return -1.0f;
        }

        public float GetCurrentRotation()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return -1.0f;

            //Making the assumption that the vessel could have more than one type of rotating engine, and we want the ones that are active.
            for (int index = 0; index < rotationControllers.Length; index++)
            {
                if (rotationControllers[index].IsActive())
                {
                    return rotationControllers[index].GetCurrentRotation();
                }
            }

            return -1.0f;
        }

        public bool CanRotateMin()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return false;

            for (int index = 0; index < rotationControllers.Length; index++)
            {
                if (rotationControllers[index].CanRotateMin() == false)
                    return false;
            }

            return true;
        }

        public bool CanRotateMax()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return false;

            for (int index = 0; index < rotationControllers.Length; index++)
            {
                if (rotationControllers[index].CanRotateMax() == false)
                    return false;
            }

            return true;
        }

        public void RotateToMin()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return;

            for (int index = 0; index < rotationControllers.Length; index++)
                rotationControllers[index].RotateMin(false);
        }

        public void RotateToMax()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return;

            for (int index = 0; index < rotationControllers.Length; index++)
                rotationControllers[index].RotateMax(false);
        }

        public void RotateToNeutral()
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return;

            for (int index = 0; index < rotationControllers.Length; index++)
                rotationControllers[index].RotateNeutral(false);
        }

        public void IncreaseRotationAngle(float rotationDelta)
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return;

            for (int index = 0; index < rotationControllers.Length; index++)
                rotationControllers[index].RotateUp(rotationDelta);
        }

        public void DecreaseRotationAngle(float rotationDelta)
        {
            if (rotationControllers == null || rotationControllers.Length == 0)
                return;

            for (int index = 0; index < rotationControllers.Length; index++)
                rotationControllers[index].RotateDown(rotationDelta);
        }
        #endregion

        #region IHoverController
        public void FindHoverControllers()
        {
            List<IHoverController> controllers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<IHoverController>();

            if (controllers.Count > 0)
                hoverControllers = controllers.ToArray();
        }

        /// <summary>
        /// Determines whether or not the hover controller is active.
        /// </summary>
        /// <returns>True if active, false if not.</returns>
        public bool HoverControllerActive()
        {
            return (hoverControllers == null || hoverControllers.Length == 0) ? false : true;
        }

        /// <summary>
        /// Returns the hover controllers, if any.
        /// </summary>
        /// <returns>An array of IHoverController interfaces if the vessel has hover controllers, or null if not.</returns>
        public IHoverController[] GetHoverControllers()
        {
            return hoverControllers;
        }

        public bool EnginesAreActive()
        {
            if (hoverControllers == null || hoverControllers.Length == 0)
                return false;

            for (int index = 0; index < hoverControllers.Length; index++)
            {
                if (hoverControllers[index].IsEngineActive() == false)
                    return false;
            }

            return true;
        }

        public void StartEngines()
        {
            if (hoverControllers == null || hoverControllers.Length == 0)
                return;

            for (int index = 0; index < hoverControllers.Length; index++)
                hoverControllers[index].StartEngine();
        }

        public void StopEngines()
        {
            if (hoverControllers == null || hoverControllers.Length == 0)
                return;

            if (hoverActive)
                ToggleHover();

            hoverGUI.enginesActive = false;

            for (int index = 0; index < hoverControllers.Length; index++)
                hoverControllers[index].StopEngine();
        }

        public void DecreaseVerticalSpeed(float amount = 1.0f)
        {
            if (hoverControllers.Length == 0)
                return;
            if (hoverActive == false)
                ToggleHover();

            verticalSpeed -= amount;

            for (int index = 0; index < hoverControllers.Length; index++)
                hoverControllers[index].SetVerticalSpeed(verticalSpeed);
        }

        public void IncreaseVerticalSpeed(float amount = 1.0f)
        {
            if (hoverControllers.Length == 0)
                return;
            if (hoverActive == false)
                ToggleHover();

            verticalSpeed += amount;

            for (int index = 0; index < hoverControllers.Length; index++)
                hoverControllers[index].SetVerticalSpeed(verticalSpeed);
        }

        public void KillVerticalSpeed()
        {
            if (hoverControllers.Length == 0)
                return;
            if (hoverActive == false)
                ToggleHover();

            verticalSpeed = 0f;

            for (int index = 0; index < hoverControllers.Length; index++)
                hoverControllers[index].KillVerticalSpeed();
        }

        public void ToggleHover()
        {
            if (hoverControllers == null)
                FindControllers(FlightGlobals.ActiveVessel);
            if (hoverControllers != null && hoverControllers.Length == 0)
                return;
            hoverActive = !hoverActive;
            if (!hoverActive)
                verticalSpeed = 0f;

            //Set hover mode
            //We actually DON'T want to calculate the throttle setting because other engines that aren't in hover mode might need it.
            for (int index = 0; index < hoverControllers.Length; index++)
            {
                hoverControllers[index].SetHoverMode(hoverActive);
            }
            if (thrustVectorControllers != null)
                thrustMode = thrustVectorControllers[0].GetThrustMode();
        }
        #endregion

        #region IThrustVector
        public void FindThrustVectorControllers()
        {
            List<IThrustVectorController> controllers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<IThrustVectorController>();
            if (controllers == null)
                return;
            if (controllers.Count > 0)
            {
                thrustVectorControllers = controllers.ToArray();
                thrustMode = thrustVectorControllers[0].GetThrustMode();
            }
        }

        /// <summary>
        /// Determines whether or not the thrust vector controller is active.
        /// </summary>
        /// <returns>True if active, false if not.</returns>
        public bool ThrustVectorControllerActive()
        {
            return (thrustVectorControllers == null || thrustVectorControllers.Length == 0) ? false : true;
        }

        /// <summary>
        /// Returns the thrust vector controllers, if any.
        /// </summary>
        /// <returns>An array of IThrustVectorController interfaces if the vessel has thrust vector controllers, or null if not.</returns>
        public IThrustVectorController[] GetThrustVectorControllers()
        {
            return thrustVectorControllers;
        }

        public void SetForwardThrust()
        {
            thrustMode = WBIThrustModes.Forward;
            for (int index = 0; index < thrustVectorControllers.Length; index++)
                thrustVectorControllers[index].SetForwardThrust(this);
        }

        public void SetReverseThrust()
        {
            thrustMode = WBIThrustModes.Reverse;
            for (int index = 0; index < thrustVectorControllers.Length; index++)
                thrustVectorControllers[index].SetReverseThrust(this);
        }

        public void SetVTOLThrust()
        {
            thrustMode = WBIThrustModes.VTOL;
            for (int index = 0; index < thrustVectorControllers.Length; index++)
                thrustVectorControllers[index].SetVTOLThrust(this);
        }
        #endregion

        #region ICustomController
        public void FindCustomControllers()
        {
            List<ICustomController> controllers = vessel.FindPartModulesImplementing<ICustomController>();
            if (controllers.Count > 0)
            {
                customControllers = controllers.ToArray();
                hoverGUI.customControllers = customControllers;
            }

            else
            {
                customControllers = null;
                hoverGUI.customControllers = null;
            }
        }

        /// <summary>
        /// Returns the custom controllers, if any.
        /// </summary>
        /// <returns>An array of ICustomController interfaces if the vessel has custom controllers, or null if not.</returns>
        public ICustomController[] GetCustomControllers()
        {
            return customControllers;
        }
        #endregion

        #region Helpers
        public virtual void printSpeed()
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Climb Rate: " + verticalSpeed + "m/s", 1f, ScreenMessageStyle.UPPER_LEFT));
        }
        #endregion

        #region GameEvents
        public void OnStageActivate(int stageID)
        {
            hoverGUI.enginesActive = EnginesAreActive();
        }

        public void VesselWasChanged(Vessel vessel)
        {
            if (vessel != FlightGlobals.ActiveVessel)
                return;
            FindControllers(vessel);
        }

        public void VesselWasLoaded(Vessel vessel)
        {
            if (vessel != FlightGlobals.ActiveVessel)
                return;
            FindControllers(vessel);
        }

        void onControllerUpdatedVerticalSpeed(Part part, float speed)
        {
            if (part.vessel != FlightGlobals.ActiveVessel)
                return;

            verticalSpeed = speed;
        }

        void onControllerUpdatedHoverActive(Part part, bool isActive)
        {
            if (part.vessel != FlightGlobals.ActiveVessel)
                return;

            hoverActive = isActive;
        }
        #endregion
    }
}
