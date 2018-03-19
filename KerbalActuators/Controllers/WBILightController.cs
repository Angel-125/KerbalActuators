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
    /// <summary>
    /// Derived from the stock ModuleLight, this controller lets you control a light through the Servo Manager.
    /// Unlike a stock light, you can change the color tint in flight.
    /// </summary>
    public class WBILightController : ModuleLight, IServoController
    {
        const int kPanelHeight = 130;
        public static string kLightOn = "On";
        public static string kRed = "<color=red>Red</color>";
        public static string kGreen = "Green";
        public static string kBlue = "<color=lightblue>Blue</color>";

        /// <summary>
        /// Servo group ID. Default is "Light"
        /// </summary>
        [KSPField]
        public string groupID = "Light";

        /// <summary>
        /// Name of the servo. Used to identify it in the servo manager and the sequence file.
        /// </summary>
        [KSPField]
        public string servoName = "Light";

        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Fields["lightB"].guiActive = true;
            Fields["lightG"].guiActive = true;
            Fields["lightR"].guiActive = true;
        }

        #region IServoController
        /// <summary>
        /// Returns the group ID of the servo. Used by the servo manager to know what servos it controlls.
        /// </summary>
        /// <returns>A string containing the group ID</returns>
        public string GetGroupID()
        {
            return groupID;
        }

        /// <summary>
        /// Tells the servo to draw its GUI controls. It's used by the servo manager.
        /// </summary>
        public void DrawControls()
        {
            GUILayout.BeginScrollView(scrollVector, panelOptions);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white><b>" + servoName + "</b></color>");

            isOn = GUILayout.Toggle(isOn, kLightOn);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(kRed);
            float currentLightR = lightR;
            lightR = GUILayout.HorizontalSlider(lightR, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(kGreen);
            lightG = GUILayout.HorizontalSlider(lightG, 0.0f, 1.0f);
            float currentLightG = lightG;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(kBlue);
            lightB = GUILayout.HorizontalSlider(lightB, 0.0f, 1.0f);
            float currentLightB = lightB;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (lightR != currentLightR || lightB != currentLightB || lightG != currentLightG)
                UpdateLightColors();
        }

        /// <summary>
        /// Hides the GUI controls in the Part Action Window.
        /// </summary>
        public void HideGUI()
        {
            Fields["lightB"].guiActive = false;
            Fields["lightG"].guiActive = false;
            Fields["lightR"].guiActive = false;
            Fields["displayStatus"].guiActive = false;
        }

        /// <summary>
        /// Returns the panel height for the servo manager's GUI.
        /// </summary>
        /// <returns>An Int containing the height of the panel.</returns>
        public int GetPanelHeight()
        {
            return kPanelHeight;
        }

        /// <summary>
        /// Takes a snapshot of the current state of the servo.
        /// </summary>
        /// <returns>A SERVODATA_NODE ConfigNode containing the servo's state</returns>
        public ConfigNode TakeSnapshot()
        {
            ConfigNode node = new ConfigNode(WBIServoManager.SERVODATA_NODE);

            node.AddValue("servoName", servoName);
            node.AddValue("lightR", lightR);
            node.AddValue("lightG", lightG);
            node.AddValue("lightB", lightB);
            node.AddValue("isOn", isOn);

            return node;
        }

        /// <summary>
        /// Sets the servo's state based upon the supplied config node.
        /// </summary>
        /// <param name="node">A SERVODAT_NODE ConfigNode containing servo state data.</param>
        public void SetFromSnapshot(ConfigNode node)
        {
            float.TryParse(node.GetValue("lightR"), out lightR);
            float.TryParse(node.GetValue("lightG"), out lightG);
            float.TryParse(node.GetValue("lightB"), out lightB);
            bool.TryParse(node.GetValue("isOn"), out isOn);
            
            UpdateLightColors();
            if (isOn)
                LightsOff();
            else
                LightsOff();
        }
        #endregion

    }
}
