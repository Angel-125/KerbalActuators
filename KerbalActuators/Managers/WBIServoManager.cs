using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
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
    public enum EServoManagerStates
    {
        Locked,
        PlayingSequence,
        PlayingSnapshot
    }

    #region IServoController
    /// <summary>
    /// Generic servo controller interface
    /// </summary>
    public interface IServoController
    {
        /// <summary>
        /// Specifies the group identifier string for the servo controller. Enables you to have servos in distinct groups like an engine and an arm, on the same part.
        /// </summary>
        /// <returns>A string containing the identifier</returns>
        string GetGroupID();

        /// <summary>
        /// Tells the servo to draw its GUI controls
        /// </summary>
        void DrawControls();

        /// <summary>
        /// Tells the servo to hide its part action window controls
        /// </summary>
        void HideGUI();

        /// <summary>
        /// Asks for the height of the GUI panel
        /// </summary>
        /// <returns>An int containing the height of the panel</returns>
        int GetPanelHeight();

        /// <summary>
        /// Tells the servo to take a snapshot of its current state. This is used to produce sequences for the servo.
        /// </summary>
        /// <returns>A SERVODATA_NODE ConfigNode containing the current state of the servo</returns>
        ConfigNode TakeSnapshot();

        /// <summary>
        /// Instructs the servo to update its current state by parsing the supplied ConfigNode.
        /// </summary>
        /// <param name="node">A SERVODATA_NODE ConfigNode containing the desired servo state.</param>
        void SetFromSnapshot(ConfigNode node);

        /// <summary>
        /// Indicates whether or not the servo controller is moving in some way.
        /// </summary>
        /// <returns>True if moving, false if not.</returns>
        bool IsMoving();

        /// <summary>
        /// Tells the servo to stop moving.
        /// </summary>
        void StopMoving();
    }
    #endregion

    /// <summary>
    /// The Servo Manager is designed to manage the states of one or more servos located in the part. The part module should be placed after the last servo controller part module in the config file.
    /// The manager is responsible for presenting the individual servo GUI panels as well as the GUI needed to create, load, update, delete, and play various sequences. These sequences are a way to 
    /// programmatically control the positioning of various servos without having to manually enter in their positions.
    /// </summary>
    public class WBIServoManager : PartModule
    {
        #region Constants
        public const string ICON_PATH = "WildBlueIndustries/001KerbalActuators/Icons/";
        public const string SERVODATA_NODE = "SERVODATA";
        public const string SNAPSHOT_NODE = "SNAPSHOT";
        public const string SEQUENCE_NODE = "SEQUENCE";
        public const string kHomeSequenceName = "Home";
        #endregion

        #region Fields
        /// <summary>
        /// Maximum height of the GUI
        /// </summary>
        [KSPField]
        public int maxWindowHeight = 600;

        /// <summary>
        /// Current state of the manager
        /// </summary>
        [KSPField(isPersistant = true)]
        public EServoManagerStates managerState;

        /// <summary>
        /// Current sequence that's being played.
        /// </summary>
        [KSPField(isPersistant = true)]
        public int sequenceID = -1;

        /// <summary>
        /// Current snapshot 
        /// </summary>
        [KSPField(isPersistant = true)]
        public int snapshotID = -1;

        /// <summary>
        /// Name of the effect to play while a servo controller is running
        /// </summary>
        [KSPField]
        public string runningEffectName = string.Empty;
        #endregion

        #region Housekeeping
        protected ServoGUI servoGUI = null;
        protected IServoController[] servoControllers;
        protected List<ConfigNode> sequences = new List<ConfigNode>();
        protected ConfigNode[] snapshots;
        protected ConfigNode currentSnapshot;
        protected ConfigNode[] snapshotServoData;
        #endregion

        #region Overrides
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            ConfigNode[] sequenceNodes;

            //Load up all the sequences
            if (node.HasNode(SEQUENCE_NODE))
            {
                sequenceNodes = node.GetNodes(SEQUENCE_NODE);
                for (int index = 0; index < sequenceNodes.Length; index++)
                    sequences.Add(sequenceNodes[index]);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            
            //Save the sequences
            if (sequences.Count > 0)
            {
                for (int index = 0; index < sequences.Count; index++)
                    node.AddNode(sequences[index]);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
                return;

            //Find servo controllers
            List<IServoController> controllers = this.part.FindModulesImplementing<IServoController>();
            servoControllers = controllers.ToArray();

            //Setup servo GUI & create home sequence if needed
            for (int index = 0; index < servoControllers.Length; index++)
                servoControllers[index].HideGUI();
            
            //Check to see if we need to create the home sequence
            CreateHomeSequence();

            //If we're in the middle of playing a sequence, then start working the current snapshot.
            if (managerState == EServoManagerStates.PlayingSequence && sequenceID < sequences.Count)
            {
                snapshots = sequences[sequenceID].GetNodes(SNAPSHOT_NODE);
                PlaySnapshot(snapshotID);
            }

            //Setup effects
            if (!string.IsNullOrEmpty(runningEffectName))
                this.part.Effect(runningEffectName, -1.0f);
        }

        public void FixedUpdate()
        {
            //A sequence consists of a series of snapshots.
            //A snapshot is done when all the contollers are locked.
            //Play each snapshot in succession until we reach the end.
            bool playNextSnapshot = true;
            bool servoIsMoving = false;
            for (int index = 0; index < servoControllers.Length; index++)
            {
                if (servoControllers[index].IsMoving())
                {
                    playNextSnapshot = false;
                    servoIsMoving = true;
                    break;
                }
            }

            //Play servo sound
            if (!string.IsNullOrEmpty(runningEffectName) && HighLogic.LoadedSceneIsFlight)
                this.part.Effect(runningEffectName, servoIsMoving == true ? 1.0f : -1.0f, -1);

            //Nothing more to do if we're not playing a snapshot.
            if (managerState == EServoManagerStates.Locked)
                return;

            //If all servo controllers have completed their movement and we're just playing one
            //snapshot, then we're done.
            if (playNextSnapshot && managerState == EServoManagerStates.PlayingSnapshot)
            {
                managerState = EServoManagerStates.Locked;
                return;
            }

            //If all servo controllers have completed their movement, then play the next snapshot
            else if (playNextSnapshot)
            {
                Debug.Log("[WBIServoManager] - Snapshot playback complete.");
                snapshotID += 1;

                //Stay in bounds...
                if (snapshotID <= snapshots.Length - 1)
                {
                    PlaySnapshot(snapshotID);
                }

                //We're done, no more snapshots to play.
                else
                {
                    snapshotID = -1;
                    managerState = EServoManagerStates.Locked;
                    Debug.Log("[WBIServoManager] - Sequence complete.");
                }
            }
        }
        #endregion

        #region API
        /// <summary>
        /// This event shows or hides the servo manager GUI.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Servo GUI")]
        public void ToggleGUI()
        {
            if (servoGUI == null)
                servoGUI = new ServoGUI();
            servoGUI.servoManager = this;
            servoGUI.sequences = this.sequences;
            servoGUI.maxWindowHeight = this.maxWindowHeight;
            servoGUI.servoControllers = this.servoControllers;
            servoGUI.SetVisible(!servoGUI.IsVisible());
            servoGUI.WindowTitle = this.part.partInfo.title;
        }

        /// <summary>
        /// Takes a snapshot of the current state of the servo controllers
        /// </summary>
        /// <returns>A SNAPSHOT ConfigNode containing the current state of the servo controllers</returns>
        public ConfigNode TakeSnapshot()
        {
            ConfigNode snapshotNode = new ConfigNode(SNAPSHOT_NODE);
            ConfigNode snapshotData = null;

            for (int index = 0; index < servoControllers.Length; index++)
            {
                snapshotData = servoControllers[index].TakeSnapshot();
                snapshotNode.AddNode(snapshotData);
            }

            return snapshotNode;
        }

        /// <summary>
        /// Plays the desired sequence.
        /// </summary>
        /// <param name="sequenceIndex">An integer containing the desired sequence index.</param>
        public void PlaySequence(int sequenceIndex)
        {
            if (sequenceIndex < 0 || sequenceIndex > sequences.Count)
                return;
            Debug.Log("[WBIServoManager] - Playing sequence: " + sequences[sequenceIndex].GetValue("name") + " (" + sequenceIndex + ")");

            //A sequence consists of a series of snapshots.
            //A snapshot is done when all the contollers are locked.
            //Play each snapshot in succession until we reach the end.
            managerState = EServoManagerStates.PlayingSequence;

            sequenceID = sequenceIndex;
            snapshotID = 0;

            snapshots = sequences[sequenceID].GetNodes(SNAPSHOT_NODE);
            PlaySnapshot(snapshotID);
        }

        /// <summary>
        /// Plays the home sequence. Home sequence is the "stored" state of the part's servos.
        /// </summary>
        public void PlayHomeSequence()
        {
            Debug.Log("[WBIServoManager] - Sequence count: " + sequences.Count);
            Debug.Log("[WBIServoManager] - Playing home sequence");
            PlaySequence(0);
        }

        /// <summary>
        /// Plays a list of supplied snapshots
        /// </summary>
        /// <param name="snapshotList">A list containing SNAPSHOT ConfigNode objects to play.</param>
        public void PlaySnapshot(List<ConfigNode> snapshotList)
        {
            managerState = EServoManagerStates.PlayingSequence;

            snapshotID = 0;

            snapshots = snapshotList.ToArray();
            PlaySnapshot(snapshotID);
        }

        /// <summary>
        /// Plays a single snapshot
        /// </summary>
        /// <param name="snapshotNode">A SNAPSHOT ConfigNode containing servo state information</param>
        public void PlaySnapshot(ConfigNode snapshotNode)
        {
            managerState = EServoManagerStates.PlayingSnapshot;

            currentSnapshot = snapshotNode;

            //Set servo data from current snapshot. Make sure the number of controllers match the number of servo data items.
            snapshotServoData = currentSnapshot.GetNodes(SERVODATA_NODE);
            if (snapshotServoData.Length == servoControllers.Length)
            {
                for (int index = 0; index < servoControllers.Length; index++)
                    servoControllers[index].SetFromSnapshot(snapshotServoData[index]);
            }
        }

        /// <summary>
        /// Plays the desired snapshot from the current sequence
        /// </summary>
        /// <param name="snapshotIndex">An integer containing the desired snampshot index.</param>
        public void PlaySnapshot(int snapshotIndex)
        {
            Debug.Log("[WBIServoManager] - Playing snapshot: " + snapshots[snapshotIndex].GetValue("name"));
            snapshotID = snapshotIndex;

            currentSnapshot = snapshots[snapshotID];

            //Set servo data from current snapshot. Make sure the number of controllers match the number of servo data items.
            snapshotServoData = currentSnapshot.GetNodes(SERVODATA_NODE);
            if (snapshotServoData.Length == servoControllers.Length)
            {
                for (int index = 0; index < servoControllers.Length; index++)
                    servoControllers[index].SetFromSnapshot(snapshotServoData[index]);
            }
        }

        /// <summary>
        /// Adds a new sequence node to the sequence list.
        /// </summary>
        /// <param name="node">A SEQUENCE_NODE ConfigNode containing the sequence to add.</param>
        public void AddSequence(ConfigNode node)
        {
            this.sequences.Add(node);
        }

        /// <summary>
        /// Uses the current servo states to define the "Home" sequence. When the user presses the Home button, the part's servos will return the mesh transforms to this recorded state.
        /// </summary>
        public void CreateHomeSequence()
        {
            ConfigNode homeSequence = null;
            ConfigNode snapshot = null;

            if (sequences.Count > 0)
                return;

            //Create initial sequence node
            snapshot = new ConfigNode(SNAPSHOT_NODE);
            homeSequence = new ConfigNode(SEQUENCE_NODE);
            homeSequence.AddValue("name", kHomeSequenceName);
            homeSequence.AddValue("partName", this.part.name);
            homeSequence.AddNode(snapshot);

            //Setup servo GUI & create home sequence if needed
            for (int index = 0; index < servoControllers.Length; index++)
                snapshot.AddNode(servoControllers[index].TakeSnapshot());

            //Add the home sequence
            sequences.Add(homeSequence);
        }

        /// <summary>
        /// Creates a home sequence from the supplied config node.
        /// </summary>
        /// <param name="node">A SEQUENCE_NOD ConfigNode containing the new home sequence</param>
        public void CreateHomeSequence(ConfigNode node)
        {
            if (sequences.Count == 0)
                sequences.Add(node);
            else
                sequences[0] = node;
        }

        /// <summary>
        /// Immediately stops all servos from moving.
        /// </summary>
        public void StopAllServos()
        {
            for (int index = 0; index < servoControllers.Length; index++)
                servoControllers[index].StopMoving();
        }
        #endregion
    }
}
