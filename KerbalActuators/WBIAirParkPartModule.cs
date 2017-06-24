using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace KerbalActuators
{
    public class WBIAirPark : PartModule, IAirParkController
    {
        static WBIAirPark instance;
        public static WBIAirPark Instance
        {
         get
        { return instance;}
        }

        #region Fields / Globals
        [KSPField(isPersistant = true, guiActive = true, guiName = "AirParked")]
        public Boolean Parked;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Auto UnPark")]
        public Boolean autoPark;

        //Velocity and Postion
        [KSPField(isPersistant = true, guiActive = false)]
        //private Vector3 ParkPosition = new Vector3(0f, 0f, 0f);
        private Vector3 ParkPosition;

        [KSPField(isPersistant = true, guiActive = false)]
        Vector3 ParkVelocity = new Vector3(0f, 0f, 0f);
        private static Vector3 zeroVector = new Vector3(0f, 0f, 0f);
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3 ParkAcceleration = new Vector3(0f, 0f, 0f);
        [KSPField(isPersistant = true, guiActive = false)]
        private Vector3 ParkAngularVelocity = new Vector3(0f, 0f, 0f);

        //Vessel State
        [KSPField(isPersistant = true, guiActive = true)] //flip to false guiactive on release
        Vessel.Situations previousState;
        [KSPField(isPersistant = true, guiActive = false)]

        //have you ever clicked "AirParked"? Rember to keep interesting things from happening
        public bool isActive = false;

        #region Debug Fields

        [KSPField(isPersistant = true)]
        public bool partDebug = true;

        [KSPField(guiActive = true, isPersistant = false, guiName = "Current Situation")]
        public string vesselSituation;

        #endregion DebugFields

        #endregion

        #region Toggles
        [KSPEvent(guiActive = true, guiName = "Toggle Park")]
        public void TogglePark()
        {
            if (!FlightGlobals.ActiveVessel) { return; }
            // cannot Park in orbit or sub-orbit
            if (vessel.situation != Vessel.Situations.SUB_ORBITAL && vessel.situation != Vessel.Situations.ORBITING)
            {
                if (!Parked)
                {
                    ParkPosition = GetVesselPostion();

                    //we only want to remember the initial velocity, not subseqent updates by onFixedUpdate()
                    ParkVelocity = vessel.GetSrfVelocity();
                    ParkAcceleration = vessel.acceleration;
                    ParkAngularVelocity = vessel.angularVelocity;

                    ParkVessel();
                }
                else
                {
                    RestoreVesselState();
                }
                isActive = true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Auto UnPark")] //auto park on will awake the vessel and set Parked = false if closer than 1.5 KM and inactive
        public void ToggleAutoPark()
        {
            autoPark = !autoPark;
        }
        #endregion

        #region GameEvents
        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                if (this.part.vessel != null)
                {
                    part.force_activate();
                    ParkPosition = vessel.transform.position;
                    SetAltitudeToCurrent();
                    instance = this;
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (vessel != null)
            {
                ParkPosition = GetVesselPostion();
            }

        }

        public void FixedUpdate()
        {
            if (vessel == null) { return; }
            vesselSituation = vessel.situation.ToString();

            // can't Park if we're orbitingParkPosition
            if (vessel.situation == Vessel.Situations.SUB_ORBITAL || vessel.situation == Vessel.Situations.ORBITING)
            {
                autoPark = false;
                Parked = false;
            }

            // If we are the Inactive Vessel and AutoPark is set
            if (!vessel.isActiveVessel & autoPark)
            {
                //ParkPosition = vessel.GetWorldPos3D();
                // if we're less than 1.5km from the active vessel and Parked, then wake up
                if ((vessel.GetWorldPos3D() - FlightGlobals.ActiveVessel.GetWorldPos3D()).magnitude < 1500.0f & Parked)
                {
                    vessel.GoOffRails();
                    RestoreVesselState();
                }
                // if we're farther than 2km, auto Park if needed
                if ((vessel.GetWorldPos3D() - FlightGlobals.ActiveVessel.GetWorldPos3D()).magnitude > 2000.0f & Parked == false)
                {
                    ParkVessel();
                }
            }

            //If Parked is True, Park the Vessel
            if (Parked)
            {
                ParkVessel();
            }

        }

        public void OnDestroy()
        {
            instance = null;       
        }

        #endregion

        #region vessel states
        private void RememberPreviousState()
        {
            if (!Parked & vessel.situation != Vessel.Situations.LANDED) //Keep from Vessel Situation from Sticking to Landed permanently
            {
                previousState = vessel.situation;
            }
        }

        private void RestoreVesselState()
        {
            if (isActive == false) { return; } //we only want to restore the state if you have parked somewhere intentionally
            vessel.situation = previousState;
            if (vessel.situation != Vessel.Situations.LANDED) { vessel.Landed = false; }
            if (Parked) { Parked = false; }

            setVesselStill();

            //Restore Velocity and Accleration
            vessel.IgnoreGForces(240);
            vessel.SetWorldVelocity(ParkVelocity);
            vessel.acceleration = ParkAcceleration;
            vessel.angularVelocity = ParkAngularVelocity;

        }

        private void ParkVessel()
        {
            RememberPreviousState();
            setVesselStill();

            vessel.situation = Vessel.Situations.LANDED;
            vessel.Landed = true;
            Parked = true;
        }

        private void setVesselStill()
        {
            vessel.IgnoreGForces(240);
            vessel.SetWorldVelocity(zeroVector);
            vessel.acceleration = zeroVector;
            vessel.angularVelocity = zeroVector;
            vessel.geeForce = 0.0;
            setVesselPosition();
        }
        #endregion

        #region Postion
        //Code Adapted from Hyperedit landing functions 
        //https://github.com/Ezriilc/HyperEdit

        public CelestialBody Body { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double alt;
        public Vector3d teleportPosition;

        public void SetAltitudeToCurrent()
        {
            var pqs = Body.pqsController;
            if (pqs == null)
            {
                Destroy(this);
                return;
            }
            var alt = pqs.GetSurfaceHeight(QuaternionD.AngleAxis(Longitude, Vector3d.down) * QuaternionD.AngleAxis(Latitude, Vector3d.forward) * Vector3d.right) - pqs.radius;
            //alt = Math.Max(alt, 0); // No need for underwater check, allow park subs
            Altitude = GetComponent<Vessel>().altitude - alt;
        }

        private Vector3d GetVesselPostion()
        {
            var pqs = vessel.mainBody.pqsController;
            if (pqs == null)
            {
                Destroy(this);
                return zeroVector;
            }

            alt = pqs.GetSurfaceHeight(vessel.mainBody.GetRelSurfaceNVector(Latitude, Longitude)) - vessel.mainBody.Radius;
            alt = Math.Max(alt, 0); // Underwater!

            teleportPosition = vessel.mainBody.GetRelSurfacePosition(Latitude, Longitude, alt + Altitude);

            return teleportPosition;
        }

        private void setVesselPosition()
        {
            vessel.IgnoreGForces(240);
            vessel.orbitDriver.pos = ParkPosition;
        }
        #endregion

        public void SetParking(bool parkingState)
        {
            TogglePark();
        }

        public bool IsParked()
        {
            return Parked;
        }

        public string GetSituation()
        {
            return this.part.vessel.SituationString;
        }
    }
}
