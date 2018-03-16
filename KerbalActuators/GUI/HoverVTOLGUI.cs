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
        const float kSmallIncrementFactor = 100.0f;

        public WBIVTOLManager vtolManager;
        public HoverControlSetupGUI hoverSetupGUI = new HoverControlSetupGUI();
        public bool canDrawParkingControls;
        public bool canDrawRotationControls;
        public bool canDrawThrustControls;
        public bool canDrawHoverControls;
        public bool enginesActive;
        public bool canRotateMin;
        public bool canRotateMax;
        public ICustomController[] customControllers;

        Texture settingsIcon;
        Texture leftArrow, doubleLeftArrow, rightArrow, doubleRightArrow, okButton, forwardIcon, upIcon, downIcon;
        GUILayoutOption[] configButtonOptions = new GUILayoutOption[] { GUILayout.Width(24), GUILayout.Height(24) };
        GUILayoutOption[] rotateButtonOptions = new GUILayoutOption[] { GUILayout.Width(36), GUILayout.Height(36) };
        GUIStyle badTextStyle;
        GUIStyle goodTextStyle;
        GUIStyle textFieldStyle;
        bool isParked = false;

        public HoverVTOLGUI(string title = "VTOL Manager", int height = 15, int width = 310) :
        base(title, width, height)
        {
            string baseIconURL = "WildBlueIndustries/001KerbalActuators/Icons/";

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
            forwardIcon = GameDatabase.Instance.GetTexture(baseIconURL + "RotateForward", false);
            upIcon = GameDatabase.Instance.GetTexture(baseIconURL + "RotateUp", false);
            downIcon = GameDatabase.Instance.GetTexture(baseIconURL + "RotateDown", false);

            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (hoverSetupGUI != null)
                hoverSetupGUI.vtolManager = this.vtolManager;

            if (canDrawParkingControls)
                isParked = vtolManager.IsParked();
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

            if (canDrawParkingControls)
                drawParkingControls();

            if (canDrawRotationControls)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=white><b>--- Engine Rotation ---</b></color>");
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                drawRotationControls();
                drawRotationFineTuneControls();
                GUILayout.EndHorizontal();
            }

            if (canDrawHoverControls)
                drawHoverControls();

            if (canDrawThrustControls)
                drawThrustControls();

            if (customControllers != null)
            {
                for (int index = 0; index < customControllers.Length; index++)
                {
                    if (customControllers[index].IsVisible())
                        customControllers[index].DrawCustomController();
                }
            }
        }

        protected void drawThrustControls()
        {
            GUILayout.BeginVertical();

            string fwdLabel;
            string revLabel;
            string vtolLabel;
            switch (vtolManager.thrustMode)
            {
                default:
                case WBIThrustModes.Forward:
                    fwdLabel = "<color=yellow>FWD</color>";
                    revLabel = "REV";
                    vtolLabel = "VTOL";
                    break;

                case WBIThrustModes.Reverse:
                    revLabel = "<color=yellow>REV</color>";
                    fwdLabel = "FWD";
                    vtolLabel = "VTOL";
                    break;

                case WBIThrustModes.VTOL:
                    vtolLabel = "<color=yellow>VTOL</color>";
                    fwdLabel = "FWD";
                    revLabel = "REV";
                    break;
            }

            //Forward Thrust
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(fwdLabel, rotateButtonOptions))
                vtolManager.SetForwardThrust();

            //Reverse Thrust
            if (GUILayout.Button(revLabel, rotateButtonOptions))
                vtolManager.SetReverseThrust();

            //VTOL Thrust
            if (GUILayout.Button(vtolLabel, rotateButtonOptions))
                vtolManager.SetVTOLThrust();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        protected void drawParkingControls()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=white><b>Situation: </b>" + vtolManager.GetSituation() + "</color>");
            GUILayout.Label("<color=white><b>Is parked: </b>" + vtolManager.IsParked() + "</color>");
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Toggle Park"))
                vtolManager.TogglePark();

            GUILayout.EndVertical();
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
                if (GUILayout.Button(downIcon, rotateButtonOptions))
                {
                    vtolManager.RotateToMin();
                }
            }

            if (GUILayout.Button(forwardIcon, rotateButtonOptions))
            {
                vtolManager.RotateToNeutral();
            }

            if (canRotateMax)
            {
                if (GUILayout.Button(upIcon, rotateButtonOptions))
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

        protected void drawHoverControls()
        {
            GUILayout.BeginVertical();

            GUILayout.Label(string.Format("<color=white><b>Vertical Speed: </b>{0:f1}m/s</color>", vtolManager.verticalSpeed));

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            if (vtolManager.hoverActive)
            {
                if (GUILayout.Button("<color=yellow>HOVR</color>\r\n"))
                {
                    vtolManager.ToggleHover();
                }
            }
            else
            {
                if (GUILayout.Button("HOVR\r\n"))
                {
                    vtolManager.ToggleHover();
                }
            }
            GUILayout.Label(vtolManager.codeToggleHover.ToString());
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("VSPD\r\n--"))
                vtolManager.DecreaseVerticalSpeed(vtolManager.verticalSpeedIncrements);

            GUILayout.Label(vtolManager.LabelForKeyCode(vtolManager.codeDecreaseVSpeed));
            GUILayout.EndVertical();

            if (GUILayout.RepeatButton("VSPD\r\n-"))
                vtolManager.DecreaseVerticalSpeed(vtolManager.verticalSpeedIncrements/kSmallIncrementFactor);

            GUILayout.BeginVertical();
            if (GUILayout.Button("VSPD\r\n0"))
                vtolManager.KillVerticalSpeed();
            GUILayout.Label(vtolManager.LabelForKeyCode(vtolManager.codeZeroVSpeed));
            GUILayout.EndVertical();

            if (GUILayout.RepeatButton("VSPD\r\n+"))
                vtolManager.IncreaseVerticalSpeed(vtolManager.verticalSpeedIncrements/kSmallIncrementFactor);

            GUILayout.BeginVertical();
            if (GUILayout.Button("VSPD\r\n++"))
                vtolManager.IncreaseVerticalSpeed(vtolManager.verticalSpeedIncrements);

            GUILayout.Label(vtolManager.LabelForKeyCode(vtolManager.codeIncreaseVSpeed));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
