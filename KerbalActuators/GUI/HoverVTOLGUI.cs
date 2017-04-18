using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
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
    public class HoverVTOLGUI : Window<HoverVTOLGUI>
    {
        const float kRotationAngle = 10.0f;

        public WBIVTOLManager vtolManager;
        public HoverControlSetupGUI hoverSetupGUI = new HoverControlSetupGUI();
        public bool canDrawRotationControls;
        public bool canDrawThrustControls;
        public bool canDrawHoverControls;
        public bool enginesActive;
        public bool canRotateMin;
        public bool canRotateMax;

        Texture settingsIcon;
        Texture leftArrow, doubleLeftArrow, rightArrow, doubleRightArrow, okButton;
        GUILayoutOption[] configButtonOptions = new GUILayoutOption[] { GUILayout.Width(24), GUILayout.Height(24) };
        GUILayoutOption[] rotateButtonOptions = new GUILayoutOption[] { GUILayout.Width(36), GUILayout.Height(36) };
        GUIStyle badTextStyle;
        GUIStyle goodTextStyle;
        GUIStyle textFieldStyle;

        public HoverVTOLGUI(string title = "", int height = 15, int width = 310) :
        base(title, width, height)
        {
            string baseIconURL = "WildBlueIndustries/KerbalActuators/Icons/";

            string settingsPath = AssemblyLoader.loadedAssemblies.GetPathByType(typeof(WBIVTOLAppButton)) + "/KerbalActuatorsSettings.cfg";
            ConfigNode settingsNode = ConfigNode.Load(settingsPath);
            if (settingsNode != null)
                baseIconURL = settingsNode.GetValue("iconsFolder");

            settingsIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Gear", false);
            leftArrow = GameDatabase.Instance.GetTexture(baseIconURL + "LeftArrow", false);
            doubleLeftArrow = GameDatabase.Instance.GetTexture(baseIconURL + "DoubleLeftArrow", false);
            rightArrow = GameDatabase.Instance.GetTexture(baseIconURL + "RightArrow", false);
            doubleRightArrow = GameDatabase.Instance.GetTexture(baseIconURL + "DoubleRightArrow", false);
            okButton = GameDatabase.Instance.GetTexture(baseIconURL + "WBIOK", false);

            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (hoverSetupGUI != null)
                hoverSetupGUI.vtolManager = this.vtolManager;
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUIStyle sty = new GUIStyle(GUI.skin.button);
            sty.normal.textColor = sty.focused.textColor = Color.white;
            sty.hover.textColor = sty.active.textColor = Color.yellow;
            sty.onNormal.textColor = sty.onFocused.textColor = sty.onHover.textColor = sty.onActive.textColor = Color.green;
            sty.padding = new RectOffset(8, 8, 8, 8);

            badTextStyle = new GUIStyle();
            badTextStyle.normal.textColor = Color.red;

            goodTextStyle = new GUIStyle();
            goodTextStyle.normal.textColor = Color.white;

            textFieldStyle = goodTextStyle;

            drawHeaderControls();

            drawEngineControls();

            if (canDrawHoverControls)
                drawHoverControls();
        }

        protected void drawHeaderControls()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(settingsIcon, configButtonOptions))
                hoverSetupGUI.SetVisible(true);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        protected void drawRotationControls()
        {
            if (canRotateMin)
            {
                if (GUILayout.Button("ROT\r\nDN"))
                {
                    vtolManager.RotateToMin();
                }
            }

            if (GUILayout.Button("ROT\r\nFWD"))
            {
                vtolManager.RotateToNeutral();
            }

            if (canRotateMax)
            {
                if (GUILayout.Button("ROT\r\nUP"))
                {
                    vtolManager.RotateToMax();
                }
            }
        }

        protected void drawRotationFineTuneControls()
        {
            GUILayout.BeginHorizontal();

            badTextStyle.normal.textColor = Color.red;

            if (GUILayout.Button(doubleLeftArrow, rotateButtonOptions))
                vtolManager.IncreaseRotationAngle(kRotationAngle);

            if (GUILayout.RepeatButton(leftArrow, rotateButtonOptions))
                vtolManager.IncreaseRotationAngle(kRotationAngle * TimeWarp.fixedDeltaTime);

            if (GUILayout.RepeatButton(rightArrow, rotateButtonOptions))
                vtolManager.DecreaseRotationAngle(kRotationAngle * TimeWarp.fixedDeltaTime);

            if (GUILayout.Button(doubleRightArrow, rotateButtonOptions))
                vtolManager.DecreaseRotationAngle(kRotationAngle);

            GUILayout.EndHorizontal();
        }

        protected void drawEngineControls()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            enginesActive = vtolManager.EnginesAreActive();
            if (enginesActive)
            {
                if (GUILayout.Button("ENG\r\nOFF"))
                {
                    vtolManager.StopEngines();
                }
            }

            else
            {
                if (GUILayout.Button("ENG\r\nON"))
                {
                    vtolManager.StartEngines();
                }
            }

            if (canDrawThrustControls)
            {
                if (GUILayout.Button("THRUST\r\nF/R"))
                    vtolManager.ToggleThrust();
            }

            //Coarse rotation (up, down, neutral)
            if (canDrawRotationControls)
                drawRotationControls();

            GUILayout.EndHorizontal();

            //Fine tune rotation controls
            if (canDrawRotationControls)
                drawRotationFineTuneControls();

            GUILayout.EndVertical();
        }

        protected void drawHoverControls()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            if (vtolManager.hoverActive)
            {
                if (GUILayout.Button("<color=yellow>HOVR</color>\r\n"))
                    vtolManager.ToggleHover();
            }
            else
            {
                if (GUILayout.Button("HOVR\r\n"))
                    vtolManager.ToggleHover();
            }
            GUILayout.Label(vtolManager.codeToggleHover.ToString());
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("VSPD\r\n+"))
                vtolManager.IncreaseVerticalSpeed();

            GUILayout.Label(vtolManager.codeIncreaseVSpeed.ToString());
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("VSPD\r\n0"))
                vtolManager.KillVerticalSpeed();
            GUILayout.Label(vtolManager.codeZeroVSpeed.ToString());
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("VSPD\r\n-"))
                vtolManager.DecreaseVerticalSpeed();
            GUILayout.Label(vtolManager.codeDecreaseVSpeed.ToString());
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
