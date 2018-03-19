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
        /// Name of the camera transform.
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
                DestroyCamera();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            cameraTransform = this.part.FindModelTransform(cameraTransformName);
            cameraWindow = new WBICameraGUI();
        }

        public virtual void SetupCamera()
        {
            if (cameraTransform == null)
            {
                Debug.Log("No cameraTransform!!!");
                return;
            }
            renderTexture = new RenderTexture(WBICameraGUI.kStartingWidth, WBICameraGUI.kStartingHeight, 24);
            renderTexture.isPowerOfTwo = true;
            renderTexture.antiAliasing = 4;
            renderTexture.filterMode = FilterMode.Trilinear;
            renderTexture.Create();
            while (!renderTexture.IsCreated()) { }

            /*
            camera = this.part.gameObject.AddComponent<UnityEngine.Camera>();
            camera.transform.position = cameraTransform.position;
            camera.transform.rotation = cameraTransform.rotation;

            camera.nearClipPlane = 0.001f;
            camera.farClipPlane = 1000000000f;
            camera.fieldOfView = camFoV;
            camera.cullingMask = (1 << 0) | (1 << 1) | (1 << 4) | (1 << 9) | (1 << 10) | (1 << 15) | (1 << 18) | (1 << 20) | (1 << 23);
//            camera.targetTexture = renderTexture;
            */

            camera = this.part.gameObject.AddComponent<UnityEngine.Camera>();
            Camera[] cameras = UnityEngine.Camera.allCameras;
            for (int index = 0; index < cameras.Length; index++)
            {
                if (cameras[index].name == "Camera 01")//"Camera 00"
                {
                    camera.CopyFrom(cameras[index]);
                    camera.name = cameraName;
                    camera.targetTexture = renderTexture;
                    camera.Render();
                    break;
                }
            }
        }

        public virtual void DestroyCamera()
        {
            if (camera != null)
            {
                DestroyImmediate(camera);
            }
        }
    }
}
