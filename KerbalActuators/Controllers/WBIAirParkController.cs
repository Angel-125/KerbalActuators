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

Adapted from AirPark by gomker.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace KerbalActuators
{
    #region IAirParkController
    /// <summary>
    /// This controller interface defines an air park controller. The controller lets you "park" a vessel in mid-air and treat it as if landed on the ground.
    /// </summary>
    public interface IAirParkController : IGenericController
    {
        /// <summary>
        /// Sets the parking mode.
        /// </summary>
        /// <param name="parked">True if parked, false if not.</param>
        void SetParking(bool parked);

        /// <summary>
        /// Determines whether or not the vessel is parked.
        /// </summary>
        /// <returns>True if parked, false if not.</returns>
        bool IsParked();

        /// <summary>
        /// Toggles the parking state from parked to unparked.
        /// </summary>
        void TogglePark();

        /// <summary>
        /// Returns the current situation of the vesel.
        /// </summary>
        /// <returns></returns>
        string GetSituation();
    }
    #endregion

    /// <summary>
    /// This class is designed to let you "park" a vessel in mid-air and treat it as if landed on the ground.
    /// </summary>
    public class WBIAirParkController : PartModule, IAirParkController
    {
        #region Constants
        public const int kGForceFrameIgnore = 240;
        public const string kParkGuiName = "Enable Airpark";
        public const string kUnparkGuiName = "Disable Airpark";
        const bool kDebugMode = true;
        #endregion

        #region Fields
        /// <summary>
        /// Displays the current vessel situation. This is used in debug mode.
        /// </summary>
        [KSPField(guiActive = kDebugMode, guiName = "Current Situation")]
        public string currentSituation;

        /// <summary>
        /// Displays the previous vessel situation. This is used in debug mode.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Previous Situation", guiActive = kDebugMode)]
        public Vessel.Situations previousSituation;

        /// <summary>
        /// This flag indicates whether or not the vessel is parked.
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiName = "Airpark Enabled")]
        public bool isParked;

        /// <summary>
        /// The altitude at which the vessel is parked.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double parkedAltitude;

        /// <summary>
        /// A flag to indicate whether or not the vessel is on rails.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isOnRails;
        #endregion

        #region Housekeeping
        protected Vector3 zeroVector = Vector3.zero;
        protected Quaternion parkedRotation;
        protected Vector3 parkedPosition = Vector3.zero;
        float mainThrottle;
        public bool physicsLoaded = false;
        #endregion

        #region API
        /// <summary>
        /// This event tells the controller to set the vessel state as landed. It's not perfect, and you have to F5/F9 for it to take effect, but it basically works.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Set Landed")]
        public void SetLanded()
        {
            this.part.vessel.Landed = true;
            this.part.vessel.Splashed = false;
            this.part.vessel.situation = Vessel.Situations.LANDED;
            physicsLoaded = true;
        }

        /// <summary>
        /// This event tells the controller to set the vessel state as flying.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Set Flying")]
        public void SetFlying()
        {
            this.part.vessel.Landed = false;
            this.part.vessel.situation = Vessel.Situations.FLYING;
            physicsLoaded = false;
        }

        /// <summary>
        /// This event toggles the vessel flying/landed state.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Park Vessel")]
        public void TogglePark()
        {
            if (this.part.vessel.situation == Vessel.Situations.ORBITING || 
                this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL || 
                this.part.vessel.situation == Vessel.Situations.DOCKED)
                return;

            isParked = !isParked;

            //Set the parking brake...
            SetParking(isParked);
        }

        /// <summary>
        /// This action sets the parking state on/off.
        /// </summary>
        /// <param name="param">A KSPActionParam containing state information for the action.</param>
        [KSPAction]
        public void ToggleParkAction(KSPActionParam param)
        {
            TogglePark();
        }

        /// <summary>
        /// Attemps to kill the vessel velocity. Best used when under 100m/sec.
        /// </summary>
        public void KillVelocity()
        {
            this.part.vessel.IgnoreGForces(kGForceFrameIgnore);
            this.part.vessel.SetWorldVelocity(zeroVector);
            this.part.vessel.acceleration = zeroVector;
            this.part.vessel.angularVelocity = zeroVector;
            this.part.vessel.geeForce = 0.0;
            this.part.vessel.orbitDriver.pos = parkedPosition;
            if (physicsLoaded)
            {

                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;
                this.part.vessel.Splashed = false;
            }

            //Maintain altitude...
            this.part.vessel.altitude = parkedAltitude;
        }
        #endregion

        #region IAirParkController
        /// <summary>
        /// Determines whether or not the controller is active. For instance, you might only have the first controller on a vessel set to active while the rest are inactive.
        /// </summary>
        /// <returns>True if the controller is active, false if not.</returns>
        public bool IsActive()
        {
            return true;
        }

        public string GetSituation()
        {
            return this.part.vessel.SituationString;
        }

        public bool IsParked()
        {
            return isParked;
        }

        public void SetParking(bool parked)
        {
            //Record new state
            isParked = parked;

            //If we're parked then set the landed state. That lets kerbals walk around the vessel.
            if (isParked)
            {
                parkedPosition = getVesselPosition();
                parkedRotation = this.part.vessel.transform.rotation;
                physicsLoaded = true;
                previousSituation = this.part.vessel.situation;
                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;
                this.part.vessel.Splashed = false;
                parkedAltitude = this.part.vessel.altitude;
                Events["TogglePark"].guiName = kUnparkGuiName;
                mainThrottle = FlightInputHandler.state.mainThrottle;
            }

            else
            {
                Events["TogglePark"].guiName = kParkGuiName;
                this.part.vessel.situation = previousSituation;
                if (this.part.vessel.situation == Vessel.Situations.LANDED)
                {
                    this.part.vessel.Landed = true;
                    this.part.vessel.Splashed = false;
                }
                else if (this.part.vessel.situation != Vessel.Situations.SPLASHED)
                {
                    this.part.vessel.Landed = false;
                    this.part.vessel.Splashed = false;
                }
                else
                    this.part.vessel.Landed = false;
            }
        }
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            currentSituation = this.part.vessel.situation.ToString();
            if (!isParked)
                return;
            if (this.part.vessel.situation == Vessel.Situations.DOCKED)
                return;

            //Make sure to remove all torque and forces.
            KillVelocity();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if ((int)previousSituation == 0)
                previousSituation = this.part.vessel.situation;

            //Setup GUI
            Fields["isParked"].guiActive = true;
            Fields["previousSituation"].guiActive = kDebugMode;
            Fields["currentSituation"].guiActive = kDebugMode;
            if (isParked)
                Events["TogglePark"].guiName = kUnparkGuiName;
            else
                Events["TogglePark"].guiName = kParkGuiName;

            //If we aren't parked, then we're done.
            if (!isParked)
                return;

            //Game events
            GameEvents.onPhysicsEaseStop.Add(onPhysicsEaseStop);
            GameEvents.onPartUndock.Add(onPartUndock);

            //When the vessel is loaded, switch to the flying state
            //to avoid potentially crashing into the water.
            //That will happen because the altitude is considered absolute by the game,
            //and we could end up below sea level. Just adjusting the altitude to account
            //for the seabed floor doesn't help.
            //Once physics has loaded we'll reset our situation.
            this.part.vessel.situation = Vessel.Situations.FLYING;
            this.part.vessel.Landed = false;
            this.part.vessel.Splashed = false;
            parkedPosition = getVesselPosition();
        }
        #endregion

        #region Game Events
        public void onPartUndock(Part undockedPart)
        {
            if (undockedPart != this.part)
                return;

            //For convenience, unset the parking brake when we undock.
            isParked = false;
            this.part.vessel.situation = previousSituation;
            if (this.part.vessel.situation == Vessel.Situations.LANDED)
            {
                this.part.vessel.Landed = true;
                this.part.vessel.Splashed = false;
            }
            else if (this.part.vessel.situation != Vessel.Situations.SPLASHED)
            {
                this.part.vessel.Landed = false;
                this.part.vessel.Splashed = false;
            }
            else
                this.part.vessel.Landed = false;
        }

        public void onPhysicsEaseStop(Vessel ves)
        {
            //Once we know that physics is fully loaded,
            //we can set the landed state if we're parked.
            if (ves == this.part.vessel && isParked)
            {
                ScreenMessages.PostScreenMessage("Physics loaded", 10.0f);
                //Kill current velocity
                KillVelocity();

                physicsLoaded = true;
                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;
                this.part.vessel.Splashed = false;

                //Get volitile parked vessel params
                parkedPosition = getVesselPosition();
                parkedRotation = this.part.vessel.transform.rotation;
                mainThrottle = FlightInputHandler.state.mainThrottle;
                if (isOnRails)
                    this.part.vessel.GoOnRails();
                GameEvents.onPhysicsEaseStop.Remove(onPhysicsEaseStop);
            }
        }
        #endregion

        #region Helpers
        public void Destroy()
        {
            GameEvents.onPartUndock.Remove(onPartUndock);
        }

        protected Vector3 getVesselPosition()
        {
            PQS pqs = this.part.vessel.mainBody.pqsController;
            double alt = pqs.GetSurfaceHeight(vessel.mainBody.GetRelSurfaceNVector(this.part.vessel.latitude, this.part.vessel.longitude)) - vessel.mainBody.Radius;
            alt = Math.Max(alt, 0); // Underwater!
            return this.part.vessel.mainBody.GetRelSurfacePosition(this.part.vessel.latitude, this.part.vessel.longitude, alt);
        }
        #endregion
    }
}
