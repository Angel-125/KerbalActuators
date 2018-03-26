using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125)
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
    public class WBICameraController: PartModule
    {
        const float kFovMin = 45.0f;
        const float kFovMax = 90.0f;

        /// <summary>
        /// Sets the visibility state of the Part Action Window controls.
        /// </summary>
        public bool guiVisible = true;

        /// <summary>
        /// Name of the camera.
        /// </summary>
        [KSPField]
        public string cameraTransformName = string.Empty;

        /// <summary>
        /// Name of the camera. Makes it easy to identify in a Sequence.
        /// </summary>
        [KSPField]
        public string cameraName = "Camera";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "FoV"),
        UI_FloatRange(minValue = 45.0f, maxValue = 90.0f, stepIncrement = 1.0f)]
        public float camFoV = 60.0f;

        protected Camera camera;
        protected RenderTexture renderTexture;
        protected WBICameraGUI cameraWindow = new WBICameraGUI();
        protected bool cameraWindowVisible;
        protected Transform cameraTransform;

        [KSPEvent(guiName = "Toggle Camera", guiActive = true)]
        public void ToggleCamera()
        {
            cameraWindowVisible = !cameraWindowVisible;
            if (cameraWindowVisible)
            {
                SetupCamera();
                cameraWindow.camera = camera;
                cameraWindow.renderTexture = renderTexture;
                cameraWindow.SetVisible(true);
            }
            else
            {
                cameraWindow.SetVisible(false);
                //DestroyCamera();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            cameraWindow = new WBICameraGUI();
        }

        public virtual void SetupCamera()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(WBICameraGUI.kStartingWidth, WBICameraGUI.kStartingHeight, 24);
                renderTexture.isPowerOfTwo = true;
                renderTexture.antiAliasing = 4;
                renderTexture.filterMode = FilterMode.Trilinear;
                renderTexture.Create();
                while (!renderTexture.IsCreated()) { }
            }

            Camera[] cameras = this.part.gameObject.GetComponentsInChildren<Camera>();
            for (int index = 0; index < cameras.Length; index++)
            {
                Debug.Log("Camera " + index + " name: " + cameras[index].name);
            }
            camera = this.part.gameObject.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                camera.targetTexture = renderTexture;
            }
        }
    }
}
