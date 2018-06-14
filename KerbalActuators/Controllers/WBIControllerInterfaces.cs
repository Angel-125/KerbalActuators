using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * This file contains interfaces that don't belong to a specific PartModule.
 */
namespace KerbalActuators
{
    /// <summary>
    /// This enumerator specifies the thrust modes.
    /// </summary>
    public enum WBIThrustModes
    {
        Forward,
        Reverse,
        VTOL
    }

    /// <summary>
    /// This is the base level controller interface from which all KerbalAcuators controller interfaces derive from. It makes it easier for third party apps to obtain all the controllers that a manager manages.
    /// You can use the "is" keyword to query an IGenericController for a specific interface.
    /// </summary>
    public interface IGenericController
    {
        /// <summary>
        /// Determines whether or not the controller is active. For instance, you might only have the first controller on a vessel set to active while the rest are inactive.
        /// </summary>
        /// <returns>True if the controller is active, false if not.</returns>
        bool IsActive();
    }

    /// <summary>
    /// This interface is used by WBIVTOLManager to control the forward, reverse, and VTOL thrust for engines that implement the interface.
    /// </summary>
    public interface IThrustVectorController : IGenericController
    {
        /// <summary>
        /// Instructs the engine to use forward thrust.
        /// </summary>
        /// <param name="vtolManager">The WBIVTOLManager that's making the request.</param>
        void SetForwardThrust(WBIVTOLManager vtolManager);

        /// <summary>
        /// Instructs the engine to use reverse thrust.
        /// </summary>
        /// <param name="vtolManager">The WBIVTOLManager that's making the request.</param>
        void SetReverseThrust(WBIVTOLManager vtolManager);

        /// <summary>
        /// Instructs the engine to use VTOL thrust.
        /// </summary>
        /// <param name="vtolManager">The WBIVTOLManager that's making the request.</param>
        void SetVTOLThrust(WBIVTOLManager vtolManager);

        /// <summary>
        /// Returns the current thrust mode of the engine.
        /// </summary>
        /// <returns>A WBIThrustModes enumerator specifying the current thrust mode.</returns>
        WBIThrustModes GetThrustMode();
    }

    /// <summary>
    /// The custom controller interface is used by both the WBIVTOLManager and WBIServoManager to display controls.
    /// Unlike other interfaces, the managers rely upon the implementor to draw the GUI controls.
    /// </summary>
    public interface ICustomController : IGenericController
    {
        /// <summary>
        /// Instructs the implementor to draw its GUI controls.
        /// </summary>
        void DrawCustomController();
    }
}
