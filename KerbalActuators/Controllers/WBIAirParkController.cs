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
    public interface IAirParkController
    {
        void SetParking(bool parked);
        bool IsParked();
        void TogglePark();
        string GetSituation();
    }

    public class WBIAirParkController : PartModule, IAirParkController
    {
        public const int kGForceFrameIgnore = 240;
        public const string kParkGuiName = "Enable Airpark";
        public const string kUnparkGuiName = "Disable Airpark";
        const bool kDebugMode = true;

        [KSPField(guiActive = kDebugMode, guiName = "Current Situation")]
        public string currentSituation;

        [KSPField(isPersistant = true, guiName = "Previous Situation", guiActive = kDebugMode)]
        public Vessel.Situations previousSituation;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Airpark Enabled")]
        public bool isParked;

        [KSPField(isPersistant = true)]
        public double parkedAltitude;

        [KSPField(isPersistant = true)]
        public bool isOnRails;

        protected Vector3 zeroVector = Vector3.zero;
        protected Quaternion parkedRotation;
        protected Vector3 parkedPosition = Vector3.zero;
        float mainThrottle;
        public bool physicsLoaded = false;

        [KSPEvent(guiActive = true, guiName = "Set Landed")]
        public void SetLanded()
        {
            this.part.vessel.Landed = true;
            this.part.vessel.Splashed = false;
            this.part.vessel.situation = Vessel.Situations.LANDED;
            physicsLoaded = true;
        }

        [KSPEvent(guiActive = true, guiName = "Set Flying")]
        public void SetFlying()
        {
            this.part.vessel.Landed = false;
            this.part.vessel.situation = Vessel.Situations.FLYING;
            physicsLoaded = false;
        }

        /*
        public void SetRails()
        {
            if (isOnRails)
            {
                Events["ToggleRails"].guiName = "Go Off Rails";
                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;
                this.part.vessel.GoOnRails();
            }
            else
            {
                Events["ToggleRails"].guiName = "Go On Rails";
                this.part.vessel.GoOffRails();
                this.part.vessel.situation = Vessel.Situations.FLYING;
                this.part.vessel.Landed = false;
            }
        }
         */

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

        [KSPAction]
        public void ToggleParkAction(KSPActionParam param)
        {
            TogglePark();
        }

        #region IAirParkController
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
//            GameEvents.onVesselLoaded.Add(onVesselLoaded);
//            GameEvents.onFlightReady.Add(onFlightReady);
//            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelWasLoadedGUIReady);

            //Make sure to remove all torque and forces.
//            KillVelocity();

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

        /*
        public void onFlightReady()
        {
            if (FlightGlobals.ActiveVessel == this.part.vessel && isParked)
            {
                Debug.Log("FRED physics loaded");
                ScreenMessages.PostScreenMessage("onFlightReady", 10.0f);
                //Kill current velocity
                KillVelocity();

                updateLandedState = true;
                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;

                //Get volitile parked vessel params
                parkedPosition = getVesselPosition();
                parkedRotation = this.part.vessel.transform.rotation;
                mainThrottle = FlightInputHandler.state.mainThrottle;
                if (isOnRails)
                    this.part.vessel.GoOnRails();
                GameEvents.onPhysicsEaseStop.Remove(onPhysicsEaseStop);
                physicsLoaded = true;
            }
        }
        */
        /*
        public void onVesselLoaded(Vessel ves)
        {
            //Once we know that physics is fully loaded,
            //we can set the landed state if we're parked.
            if (ves == this.part.vessel && isParked)
            {
                ScreenMessages.PostScreenMessage("onVesselLoaded", 10.0f);
                //Kill current velocity
                KillVelocity();

                updateLandedState = true;
                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;

                //Get volitile parked vessel params
                parkedPosition = getVesselPosition();
                parkedRotation = this.part.vessel.transform.rotation;
                mainThrottle = FlightInputHandler.state.mainThrottle;
                if (isOnRails)
                    this.part.vessel.GoOnRails();
                GameEvents.onPhysicsEaseStop.Remove(onPhysicsEaseStop);
                physicsLoaded = true;
            }
        }
        /*
        public void onLevelWasLoadedGUIReady(GameScenes scene)
        {
            if (scene != GameScenes.FLIGHT)
                return;
            if (FlightGlobals.ActiveVessel == this.part.vessel && isParked)
            {
                Debug.Log("FRED physics loaded");
                ScreenMessages.PostScreenMessage("onLevelWasLoadedGUIReady", 10.0f);
                //Kill current velocity
                KillVelocity();

                updateLandedState = true;
                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;

                //Get volitile parked vessel params
                parkedPosition = getVesselPosition();
                parkedRotation = this.part.vessel.transform.rotation;
                mainThrottle = FlightInputHandler.state.mainThrottle;
                if (isOnRails)
                    this.part.vessel.GoOnRails();
                GameEvents.onPhysicsEaseStop.Remove(onPhysicsEaseStop);
                physicsLoaded = true;
            }
        }
         */

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

        public void Destroy()
        {
            GameEvents.onPartUndock.Remove(onPartUndock);
        }

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

                //            this.part.vessel.SetPosition(parkedPosition);
                //            this.part.vessel.transform.rotation = parkedRotation;

                this.part.vessel.situation = Vessel.Situations.LANDED;
                this.part.vessel.Landed = true;
                this.part.vessel.Splashed = false;
            }
            //Maintain altitude...
            this.part.vessel.altitude = parkedAltitude;
        }

        protected Vector3 getVesselPosition()
        {
            PQS pqs = this.part.vessel.mainBody.pqsController;
            double alt = pqs.GetSurfaceHeight(vessel.mainBody.GetRelSurfaceNVector(this.part.vessel.latitude, this.part.vessel.longitude)) - vessel.mainBody.Radius;
            alt = Math.Max(alt, 0); // Underwater!
            return this.part.vessel.mainBody.GetRelSurfacePosition(this.part.vessel.latitude, this.part.vessel.longitude, alt);
        }
    }
}
