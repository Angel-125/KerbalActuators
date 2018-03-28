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
    public class WBIMovableJoint : PartModule
    {
        [KSPField]
        public string jointTransformName = "nodeJib";

        [KSPField]
        public string partJointName = "jibNode";

        protected Part targetPart;
        protected Rigidbody jointRigidBody;
        protected ConfigurableJoint attachmentJoint;
        protected Transform jointTransform = null;

        [KSPEvent(guiActive = true, guiActiveEditor = true)]
        public void AddJoint()
        {
            jointTransform = this.part.FindModelTransform(jointTransformName);
            jointRigidBody = jointTransform.gameObject.AddComponent<Rigidbody>();
            jointRigidBody.isKinematic = true;

            foreach (AttachNode node in this.part.attachNodes)
            {
                if (node.id == partJointName)
                {
                    targetPart = node.attachedPart;
                }
            }

            attachmentJoint = jointTransform.gameObject.AddComponent<ConfigurableJoint>();

            attachmentJoint.xMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.yMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.zMotion = ConfigurableJointMotion.Locked;

            attachmentJoint.angularXMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.angularYMotion = ConfigurableJointMotion.Locked;
            attachmentJoint.angularZMotion = ConfigurableJointMotion.Locked;

            attachmentJoint.connectedBody = targetPart.attachJoint.Joint.connectedBody;
            targetPart.Rigidbody.isKinematic = true;
            Debug.Log("Added attachment joint");

        }

    }
}
