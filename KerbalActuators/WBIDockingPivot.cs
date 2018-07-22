using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125) & Sirkut
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
    public class WBIDockingPivot: PartModule, IActiveJointHost, IJointLockState
    {
        ActiveJointPivot jointPivot;

        [KSPEvent(guiActive = true, guiName = "Free Pivot")]
        public void TogglePivot()
        {
            ModuleDockingNode dockingNode = this.part.FindModuleImplementing<ModuleDockingNode>();
            if (dockingNode == null)
                return;

            jointPivot = ActiveJointPivot.Create((IActiveJointHost)this, dockingNode.referenceNode);
            jointPivot.SetPivotAngleLimit(10.0f);
            jointPivot.SetDriveMode(ActiveJoint.DriveMode.Neutral);
        }

        public Part GetHostPart()
        {
            return this.part;
        }

        public Transform GetLocalTransform()
        {
            return this.part.partTransform;
        }

        public void OnDriveModeChanged(ActiveJoint.DriveMode mode)
        {
            if (mode != ActiveJoint.DriveMode.NoJoint)
            {
                this.Events["TogglePivot"].guiName = mode != ActiveJoint.DriveMode.Neutral ? "Free Pivot" : "Lock Pivot";
            }
            else
            {
                this.Events["TogglePivot"].active = false;
            }
        }

        public void OnJointInit(ActiveJoint joint)
        {
            this.Events["TogglePivot"].active = joint != null;
        }

        public bool IsJointUnlocked()
        {
            if (this.jointPivot != null)
                return this.jointPivot.driveMode != ActiveJoint.DriveMode.Park;
            else
                return false;
        }
    }
}
