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

    public interface IServoController
    {
        string GetGroupID();
        void DrawControls();
        void HideGUI();
        int GetPanelHeight();
        ConfigNode TakeSnapshot();
        void SetFromSnapshot(ConfigNode node);
        bool IsMoving();
    }

    public class WBIServoManager : PartModule
    {
        public const string ICON_PATH = "WildBlueIndustries/001KerbalActuators/Icons/";
        public const string SERVODATA_NODE = "SERVODATA";
        public const string SNAPSHOT_NODE = "SNAPSHOT";
        public const string SEQUENCE_NODE = "SEQUENCE";
        const string kHomeSequenceName = "Home";

        [KSPField]
        public int maxWindowHeight = 600;

        [KSPField(isPersistant = true)]
        public int managerStateID;

        [KSPField(isPersistant = true)]
        public int sequenceID = -1;

        [KSPField(isPersistant = true)]
        public int snapshotID = -1;

        /// <summary>
        /// Name of the effect to play while a servo controller is running
        /// </summary>
        [KSPField]
        public string runningEffectName = string.Empty;

        protected ServoGUI servoGUI = new ServoGUI();
        protected IServoController[] servoControllers;
        protected List<ConfigNode> sequences = new List<ConfigNode>();
        protected ConfigNode[] snapshots;
        protected ConfigNode currentSnapshot;
        protected ConfigNode[] snapshotServoData;
        protected EServoManagerStates managerState = EServoManagerStates.Locked;

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

            //Find servo controllers
            List<IServoController> controllers = this.part.FindModulesImplementing<IServoController>();
            servoControllers = controllers.ToArray();

            //Setup servo GUI & create home sequence if needed
            for (int index = 0; index < servoControllers.Length; index++)
                servoControllers[index].HideGUI();
            
            //Check to see if we need to create the home sequence
            createHomeSequence();

            //If we're in the middle of playing a sequence, then start working the current snapshot.
            managerState = (EServoManagerStates)managerStateID;
            if (managerState == EServoManagerStates.PlayingSequence && sequenceID < sequences.Count)
            {
                snapshots = sequences[sequenceID].GetNodes(SNAPSHOT_NODE);
                PlaySnapshot(snapshotID);
            }

            //Setup effects
            if (!string.IsNullOrEmpty(runningEffectName))
                this.part.Effect(runningEffectName, -1.0f);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Servo GUI")]
        public void ToggleGUI()
        {
            servoGUI.servoManager = this;
            servoGUI.sequences = this.sequences;
            servoGUI.maxWindowHeight = this.maxWindowHeight;
            servoGUI.servoControllers = this.servoControllers;
            servoGUI.SetVisible(!servoGUI.IsVisible());
            servoGUI.WindowTitle = this.part.partInfo.title;

            if (servoGUI.IsVisible())
                WBIActuatorsGUIMgr.Instance.RegisterWindow(servoGUI);
            else
                WBIActuatorsGUIMgr.Instance.UnregisterWindow(servoGUI);
        }

        public void FixedUpdate()
        {
            //A sequence consists of a series of snapshots.
            //A snapshot is done when all the contollers are locked.
            //Play each snapshot in succession until we reach the end.
            bool playNextSnapshot = true;
            for (int index = 0; index < servoControllers.Length; index++)
            {
                if (servoControllers[index].IsMoving())
                {
                    playNextSnapshot = false;

                    //Play servo sound
                    if (!string.IsNullOrEmpty(runningEffectName) && HighLogic.LoadedSceneIsFlight)
                        this.part.Effect(runningEffectName, 1.0f);
                    break;
                }

                else
                {
                    //Stop servo sound.
                    if (!string.IsNullOrEmpty(runningEffectName) && HighLogic.LoadedSceneIsFlight)
                        this.part.Effect(runningEffectName, -1.0f);
                }
            }

            if (managerState == EServoManagerStates.Locked)
                return;

            //If all servo controllers have completed their movement and we're just playing one
            //snapshot, then we're done.
            if (playNextSnapshot && managerState == EServoManagerStates.PlayingSnapshot)
            {
                managerState = EServoManagerStates.Locked;
                managerStateID = (int)managerState;
                return;
            }

            //If all servo controllers have completed their movement, then play the next snapshot
            else if (playNextSnapshot)
            {
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
                    managerStateID = (int)managerState;
                }
            }
        }

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

        public void PlaySequence(int sequenceIndex)
        {
            if (sequenceIndex < 0 || sequenceIndex > sequences.Count)
                return;

            //A sequence consists of a series of snapshots.
            //A snapshot is done when all the contollers are locked.
            //Play each snapshot in succession until we reach the end.
            managerState = EServoManagerStates.PlayingSequence;
            managerStateID = (int)managerState;

            sequenceID = sequenceIndex;
            snapshotID = 0;

            snapshots = sequences[sequenceID].GetNodes(SNAPSHOT_NODE);
            PlaySnapshot(snapshotID);
        }

        public void PlaySnapshot(List<ConfigNode> snapshotList)
        {
            managerState = EServoManagerStates.PlayingSequence;
            managerStateID = (int)managerState;

            snapshotID = 0;

            snapshots = snapshotList.ToArray();
            PlaySnapshot(snapshotID);
        }

        public void PlaySnapshot(ConfigNode snapshotNode)
        {
            managerState = EServoManagerStates.PlayingSnapshot;
            managerStateID = (int)managerState;

            currentSnapshot = snapshotNode;

            //Set servo data from current snapshot. Make sure the number of controllers match the number of servo data items.
            snapshotServoData = currentSnapshot.GetNodes(SERVODATA_NODE);
            if (snapshotServoData.Length == servoControllers.Length)
            {
                for (int index = 0; index < servoControllers.Length; index++)
                    servoControllers[index].SetFromSnapshot(snapshotServoData[index]);
            }
        }

        public void PlaySnapshot(int snapshotIndex)
        {
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

        protected void createHomeSequence()
        {
            ConfigNode homeSequence = null;
            ConfigNode snapshot = null;

            if (sequences.Count > 0)
                return;

            //Create initial sequence node
            snapshot = new ConfigNode(SNAPSHOT_NODE);
            homeSequence = new ConfigNode(SEQUENCE_NODE);
            homeSequence.AddValue("name", kHomeSequenceName);
            homeSequence.AddNode(snapshot);

            //Setup servo GUI & create home sequence if needed
            for (int index = 0; index < servoControllers.Length; index++)
                snapshot.AddNode(servoControllers[index].TakeSnapshot());

            //Add the home sequence
            sequences.Add(homeSequence);
        }
    }

}
