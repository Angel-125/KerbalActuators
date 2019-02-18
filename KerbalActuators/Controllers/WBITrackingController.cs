using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Text.RegularExpressions;

namespace KerbalActuators
{
    /// <summary>
    /// This class has the ability to track objects in two dimensions. It can track player-selected targets as well as random targets.
    /// </summary>
    [KSPModule("Tracking Controller")]
    public class WBITrackingController : PartModule, ICustomController
    {
        #region Constants
        const string kVesselIDNone = "NONE";
        const string kTrackingNone = "Nothing";
        const string kNotEnoughResource = "Insufficient ";
        const string kBlockedBy = "Blocked by ";
        const string kTracking = "Tracking: ";
        const string kSearching = "Searching for ";
        const string kLOS = " (LOS)";
        const int kPanelHeight = 130;
        const float kMessageDuration = 3.0f;
        const float kOverheadAquisitionAngle = 5.0f;
        const float kAcquisitionRotationThresholdAngle = 30.0f;
        #endregion

        #region Fields
        /// <summary>
        /// Flag to enable/disable debug fields.
        /// </summary>
        [KSPField]
        public bool debugEnabled = false;

        /// <summary>
        /// Name of the rotation transform
        /// </summary>
        [KSPField]
        public string rotationTransformName = string.Empty;

        /// <summary>
        /// Minimum rotation angle, from 0 to 180. Set to -1 if there is no rotation limit.
        /// </summary>
        [KSPField]
        public float minRotationAngle = -1;

        /// <summary>
        /// maximum rotation angle, from 180 to 360. Set to -1 if there is no rotation limit.
        /// </summary>
        [KSPField]
        public float maxRotationAngle = -1;

        /// <summary>
        /// Fixed reference relative to the rotation transform. Make sure z-axis is facing the same direction.
        /// </summary>
        [KSPField]
        public string referenceRotationTransformName = string.Empty;
        Transform rotationReferenceTransform;

        /// <summary>
        /// Name of the elevation transform.
        /// </summary>
        [KSPField]
        public string elevationTransformName = string.Empty;

        /// <summary>
        /// Minimum elevation angle, from 0 to 180. Set to -1 if there is no elevation limit.
        /// </summary>
        [KSPField]
        public float minElevationAngle = -1;

        /// <summary>
        /// Maximum elevation angle, from 180 to 360. Set to -1 if there is no elevation limit.
        /// </summary>
        [KSPField]
        public float maxElevationAngle = -1;

        /// <summary>
        /// Fixed reference relative to the elevation transform. Make sure the z-axis is facing the same direction.
        /// </summary>
        [KSPField]
        public string referenceElevationTransformName = "referenceTransform";
        Transform elevationReferenceTransform;

        /// <summary>
        /// Reference transform used to determine if the tracking controller is pointing at a planet. if so, then the target is declared loss of signal.
        /// </summary>
        [KSPField]
        public string secondaryTransformName = "secondaryTransform";

        /// <summary>
        /// The rotation angle that we want to rotate to.
        /// </summary>
        [KSPField]
        public float targetRotation;

        /// <summary>
        /// The elevation angle that we want to elevate to.
        /// </summary>
        [KSPField]
        public float targetElevation;

        /// <summary>
        /// The current angle between rotationTransform and rotationReferenceTransform
        /// </summary>
        [KSPField]
        public float currentRotation;

        /// <summary>
        /// The current angle between elevationTransform and elevationReferenceTransform
        /// </summary>
        [KSPField]
        public float currentElevation;
            
        /// <summary>
        /// Flag to indicate whether or not tracking is enabled.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Tracking Systems", guiActive = true)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool enableTracking = false;

        /// <summary>
        /// Flag to indicate whether or not we can track random objects.
        /// </summary>
        [KSPField]
        public bool canTrackRandomObjects = true;

        /// <summary>
        /// Flag to indicate whether or not we are currently tracking random objects.
        /// If the vessel has one or more tracking controllers and they're all tracking random objects, then the first controller will select the random target and all other
        /// controllers will track the same target.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Track random objects", guiActive = true)]
        [UI_Toggle(enabledText = "Yes", disabledText = "No")]
        public bool trackRandomObjects = false;

        /// <summary>
        /// Current tracking status
        /// </summary>
        [KSPField(guiActive = true, guiName = "Tracking")]
        public string trackingStatus = string.Empty;

        /// <summary>
        /// How fast we can move the rotationTransform and elevationTransform.
        /// </summary>
        [KSPField]
        public float trackingSpeed = 0.25f;

        /// <summary>
        /// The resource, if any, that is consumed while tracking is enabled.
        /// </summary>
        [KSPField]
        public string upkeepResource = string.Empty;

        /// <summary>
        /// The amount of resource that is consumed while tracking is enabled.
        /// </summary>
        [KSPField]
        public float upkeepAmountPerSec = 0;

        /// <summary>
        /// How long to wait in seconds after we have a loss of signal before tracking another random target.
        /// </summary>
        [KSPField]
        public double losCooldown = 0;

        /// <summary>
        /// How long in seconds to track a random target.
        /// </summary>
        [KSPField]
        public double randomTrackDuration = 0;

        /// <summary>
        /// Flag to indicate if we can track player-selected targets.
        /// </summary>
        [KSPField]
        public bool canTrackPlayerTargets = true;
        #endregion

        #region Housekeeping
        public Vessel targetVessel;
        public CelestialBody targetBody;
        public Transform targetTransform;
        public string targetName;
        bool targetObjectSet;

        Transform rotationTransform;
        Transform elevationTransform;
        Transform secondaryTransform;
        protected LayerMask layerMask = -1;

        public int minimumVesselPercentEC = 5;
        private double minECLevel = 0;
        PartResourceDefinition resourceDef = null;

        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };

        double losCooldownStart;
        double randomTrackStart = -1f;
        #endregion

        #region Events
        /// <summary>
        /// Clears the current target and readies the tracking controller to track something else.
        /// This event isn't enabled unless the controller can track random targets, and it is currently tracking random targets.
        /// </summary>
        [KSPEvent(guiName = "Track something else", guiActive = true)]
        public void ClearTarget()
        {
            FlightGlobals.fetch.SetVesselTarget((ITargetable)null, false);

            List<WBITrackingController> controllers = this.part.vessel.FindPartModulesImplementing<WBITrackingController>();
            int count = controllers.Count;
            for (int index = 0; index < count; index++)
            {
                if (controllers[index].trackRandomObjects)
                {
                    randomTrackStart = -1f;
                    controllers[index].targetTransform = null;
                    controllers[index].targetVessel = null;
                    controllers[index].targetBody = null;
                }
            }
        }
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Only track if we're tracking
            if (!enableTracking)
                return;

            //Consume upkeep resource
            if (!string.IsNullOrEmpty(upkeepResource) && !debugEnabled)
            {
                double amount;
                double maxAmount;
                this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount, true);
                if ((amount / maxAmount) < minECLevel)
                {
                    trackingStatus = kNotEnoughResource + resourceDef.displayName;
                    return;
                }

                this.part.RequestResource(resourceDef.id, upkeepAmountPerSec * TimeWarp.deltaTime, ResourceFlowMode.ALL_VESSEL);
            }

            //Update the tracking target
            bool hasTargetToTrack = UpdateTarget();

            //Update GUI
            Fields["trackRandomObjects"].guiActive = !targetObjectSet && canTrackRandomObjects;
            Events["ClearTarget"].active = trackRandomObjects && !targetObjectSet && canTrackRandomObjects;

            //If we have no target to track then we're done.
            if (!hasTargetToTrack)
                return;

            //Rotate towards target
            Vector3 inversePosition;
            Quaternion minRotation;
            Quaternion maxRotation;
            if (rotationTransform != null && rotationReferenceTransform != null)
            {
                //Calculate rotation
                inversePosition = rotationTransform.InverseTransformPoint(targetTransform.position);
                targetRotation = Mathf.Atan2(inversePosition.x, inversePosition.z) * Mathf.Rad2Deg;
                rotationTransform.rotation = Quaternion.Lerp(rotationTransform.rotation, rotationTransform.rotation * Quaternion.Euler(0, targetRotation, 0), TimeWarp.deltaTime * trackingSpeed);

                //Apply limits
                currentRotation = Quaternion.Angle(rotationTransform.localRotation, rotationReferenceTransform.localRotation);
                minRotation = rotationReferenceTransform.localRotation * Quaternion.Euler(0, minRotationAngle, 0);
                maxRotation = rotationReferenceTransform.localRotation * Quaternion.Euler(0, maxRotationAngle, 0);
                if ((currentRotation >= 0 && currentRotation > minRotationAngle) && (minRotationAngle != -1))
                    rotationTransform.localRotation = minRotation;
                else if ((currentRotation >= 180 && currentRotation < maxRotationAngle) && (maxRotationAngle != -1))
                    rotationTransform.localRotation = maxRotation;
            }

            //Elevate towards target
            if (elevationTransform != null && elevationReferenceTransform != null)
            {
                //Calculate target elevation.
                inversePosition = elevationTransform.InverseTransformPoint(targetTransform.position);
                targetElevation = Mathf.Atan2(inversePosition.x, inversePosition.z) * Mathf.Rad2Deg;
                elevationTransform.localRotation = Quaternion.Lerp(elevationTransform.localRotation, elevationTransform.localRotation * Quaternion.Euler(0, targetElevation, 0), TimeWarp.deltaTime * trackingSpeed);

                //Apply limits
                currentElevation = Quaternion.Angle(elevationTransform.localRotation, elevationReferenceTransform.localRotation);
                minRotation = elevationReferenceTransform.localRotation * Quaternion.Euler(0, minElevationAngle, 0);
                maxRotation = elevationReferenceTransform.localRotation * Quaternion.Euler(0, maxElevationAngle, 0);
                if ((currentElevation >= 0 && currentElevation > minElevationAngle) && (minElevationAngle != -1))
                    elevationTransform.localRotation = minRotation;
                else if ((currentElevation >= 180 && currentElevation < maxElevationAngle) && (maxElevationAngle != -1))
                    elevationTransform.localRotation = maxRotation;
            }

            //Check for target aquisition If we've rotated close to it
            if (Mathf.Abs(targetRotation) <= kAcquisitionRotationThresholdAngle)
            {
                //Check to see if we're blocked by another object.
                RaycastHit raycastHit;
                Ray ray = new Ray(ScaledSpace.LocalToScaledSpace(this.secondaryTransform.position),
                    (ScaledSpace.LocalToScaledSpace(this.targetTransform.position) - ScaledSpace.LocalToScaledSpace(this.elevationTransform.position)).normalized);
                if (Physics.Raycast(ray, out raycastHit, float.MaxValue, layerMask))
                {
                    trackingStatus = targetName + kLOS;
                    randomTrackStart = -1f;

                    //If we're tracking random objects then find another one.
                    if (trackRandomObjects && this.part.vessel.targetObject == null)
                    {
                        if (Planetarium.GetUniversalTime() - losCooldownStart >= losCooldown)
                            targetTransform = null;
                    }
                }
                //Target acquired
                else
                {
                    trackingStatus = targetName;
                    losCooldownStart = Planetarium.GetUniversalTime();

                    //If we will only track for a limited time, then check our expiration timer
                    if (randomTrackDuration > 0)
                    {
                        if (randomTrackStart == -1f)
                        {
                            randomTrackStart = Planetarium.GetUniversalTime();
                        }
                        else if (Planetarium.GetUniversalTime() - randomTrackStart >= randomTrackDuration)
                        {
                            targetTransform = null;
                            randomTrackStart = -1f;
                        }
                    }
                }
            }

            //We haven't rotated towards the target yet
            //Edge case: if the target is passing directly overhead, then we're still tracking it. Otherwise we're searching for it.
            else if (currentElevation > kOverheadAquisitionAngle)
            {
                trackingStatus = kSearching + targetName;
                losCooldownStart = Planetarium.GetUniversalTime();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Debug fields
            Fields["targetRotation"].guiActive = debugEnabled;
            Fields["targetElevation"].guiActive = debugEnabled;
            Fields["currentElevation"].guiActive = debugEnabled;

            //Find the transforms
            layerMask = 1 << LayerMask.NameToLayer("TerrainColliders") | 1 << LayerMask.NameToLayer("Local Scenery");
            rotationTransform = this.part.FindModelTransform(rotationTransformName);
            elevationTransform = this.part.FindModelTransform(elevationTransformName);

            //This is needed to calculate the angle between reference straight up and elevationTransform.
            elevationReferenceTransform = this.part.FindModelTransform(referenceElevationTransformName);

            //This is needed to calculate the reference angle between a fixed point and rotationTransform.
            rotationReferenceTransform = this.part.FindModelTransform(referenceRotationTransformName);

            //Similar to a solar panel tracking the sun, this is used to determine if the tracking unit is blocked by something.
            secondaryTransform = this.part.FindModelTransform(secondaryTransformName);

            //Upkeep resource
            if (!string.IsNullOrEmpty(upkeepResource))
            {
                PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                resourceDef = definitions[upkeepResource];
                minECLevel = (double)minimumVesselPercentEC / 100.0f;
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Forces the controller to pick a target to track. In order of preference:
        /// 1) A player-selected target.
        /// 2) A target vessel specified by the targetVessel variable.
        /// 3) A target celestial body specified by the targetBody variable.
        /// 4) A random target if trackRandomTargets is set to true.
        /// </summary>
        /// <returns></returns>
        public bool UpdateTarget()
        {
            ITargetable targetObject = this.part.vessel.targetObject;

            //chek for unset
            if (targetObject == null && targetObjectSet)
            {
                targetTransform = null;
                targetObjectSet = false;
            }

            //If we already have a target to track then we're good to go.
            if (targetTransform != null && targetObject == null)
                return true;

            //First check to see if the vessel has selected a target.
            if (targetObject != null && canTrackPlayerTargets)
            {
                targetTransform = targetObject.GetTransform();
                trackingStatus = targetObject.GetDisplayName().Replace("^N","");
                targetName = trackingStatus;
                targetObjectSet = true;
                return true;
            }

            //Next check to see if we have a target vessel
            if (targetVessel != null)
            {
                targetTransform = targetVessel.vesselTransform;
                trackingStatus = targetVessel.vesselName;
                targetName = trackingStatus;
                return true;
            }

            //Now check target planet
            if (targetBody != null)
            {
                targetTransform = targetBody.scaledBody.transform;
                trackingStatus = targetBody.displayName.Replace("^N", "");
                targetName = trackingStatus;
                return true;
            }

            //Lastly, if random tracking is enabled and we don't have a target, then randomly select one from the unloaded vessels list.
            if (trackRandomObjects && targetTransform == null)
            {
                //get the tracking controllers on the vessel
                List<WBITrackingController> trackingControllers = this.part.vessel.FindPartModulesImplementing<WBITrackingController>();

                //If we aren't the first controller, then we're done.
                if (trackingControllers[0] != this)
                {
                    trackingStatus = trackingControllers[0].trackingStatus;
                    targetTransform = trackingControllers[0].targetTransform;
                    targetName = trackingStatus;
                    return false;
                }

                //Find a random vessel to track
                int vesselCount = FlightGlobals.VesselsUnloaded.Count;
                Vessel trackedVessel;
                for (int index = 0; index < vesselCount; index++)
                {
                    trackedVessel = FlightGlobals.VesselsUnloaded[UnityEngine.Random.Range(0, vesselCount - 1)];

                    //If we find a vessel we're interested in, tell all the other tracking controllers.
                    if (trackedVessel.vesselType != VesselType.Flag && 
                        trackedVessel.vesselType != VesselType.EVA &&
                        trackedVessel.vesselType != VesselType.Unknown)
                    {
                        int controllerCount = trackingControllers.Count;
                        for (int controllerIndex = 0; controllerIndex < controllerCount; controllerIndex++)
                        {
                            if (!trackingControllers[controllerIndex].trackRandomObjects)
                                continue;

                            trackingControllers[controllerIndex].targetTransform = trackedVessel.vesselTransform;
                            trackingControllers[controllerIndex].trackingStatus = trackedVessel.vesselName;
                            trackingControllers[controllerIndex].targetName = trackedVessel.vesselName;
                        }
                        return true;
                    }
                }
            }

            //Nothing to track
            trackingStatus = kTrackingNone;
            targetName = trackingStatus;
            return false;
        }
        #endregion

        #region ICustomController
        public bool IsActive()
        {
            return enableTracking;
        }

        public void DrawCustomController()
        {
            GUILayout.BeginScrollView(scrollVector, panelOptions);
            enableTracking = GUILayout.Toggle(enableTracking, "Enable Tracking");
            trackRandomObjects = GUILayout.Toggle(trackRandomObjects, "Track random objects");
            GUILayout.Label("<color=white>Tracking: " + trackingStatus + "</color>");
            GUILayout.EndScrollView();
        }
        #endregion
    }
}
