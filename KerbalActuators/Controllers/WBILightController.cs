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
    public class WBILightController : PartModule, IServoController
    {
        const int kPanelHeight = 130;
        public static string kLightOn = "On";
        public static string kRed = "<color=red>Red</color>";
        public static string kGreen = "Green";
        public static string kBlue = "<color=lightblue>Blue</color>";

        #region Fields
        /// <summary>
        /// Sets the visibility state of the Part Action Window controls.
        /// </summary>
        public bool guiVisible = true;

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

        protected const int kDefaultLightAnimationLayer = 3;

        [KSPField()]
        public int animationLayer = kDefaultLightAnimationLayer;

        [KSPField()]
        public string animationName;

        [KSPField()]
        public string startEventGUIName;

        [KSPField()]
        public string endEventGUIName;

        [KSPField(isPersistant = true)]
        public double ecRequired;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "red")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float red;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "green")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float green;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "blue")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float blue;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "intensity")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float intensity = -1f;

        [KSPField(isPersistant = true)]
        public bool lightsOn = false;
        #endregion

        #region Housekeeping
        public Animation animation = null;
        protected AnimationState animationState;
        Light[] lights;
        float prevRed;
        float prevGreen;
        float prevBlue;
        float prevLevel;
        Vector2 scrollVector = new Vector2();
        GUILayoutOption[] panelOptions = new GUILayoutOption[] { GUILayout.Height(kPanelHeight) };
        #endregion

        #region API
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            SetupAnimations();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            animationLayer = kDefaultLightAnimationLayer;
            Animation anim = this.part.FindModelAnimators(animationName)[0];
            anim[animationName].layer = animationLayer;
            SetupAnimations();

            //Find the lights
            lights = this.part.gameObject.GetComponentsInChildren<Light>();
            Log("THERE! ARE! " + lights.Length + " LIGHTS!");
            setupLights();

            if (!guiVisible)
                HideGUI();
        }

        /// <summary>
        /// KSP event to toggle the lights on and off
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "ToggleLights", active = true, externalToEVAOnly = false, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public virtual void ToggleLights()
        {
            //Play animation for current state
            PlayAnimation(lightsOn);

            //Toggle state
            lightsOn = !lightsOn;
            if (lightsOn)
            {
                Events["ToggleLights"].guiName = endEventGUIName;
            }
            else
            {
                Events["ToggleLights"].guiName = startEventGUIName;
            }

            setupLights();
            Log("Animation toggled new gui name: " + Events["ToggleLights"].guiName);
        }
        
        [KSPAction("Toggle Lights", KSPActionGroup.Light)]
        public void ToggleLightsAction(KSPActionParam param)
        {
            ToggleLights();
        }

        /// <summary>
        /// Turns on the lights
        /// </summary>
        public void TurnOnLights()
        {
            //Play animation for current state
            PlayAnimation(false);
            lightsOn = true;
            Events["ToggleLights"].guiName = endEventGUIName;
            setupLights();
        }

        /// <summary>
        /// Turns off the lights
        /// </summary>
        public void TurnOffLights()
        {
            //Play animation for current state
            PlayAnimation(true);
            lightsOn = false;
            Events["ToggleLights"].guiName = startEventGUIName;
            setupLights();
        }

        /// <summary>
        /// Toggles the light animation On/Off
        /// </summary>
        /// <param name="deployed">Set to true to turn on lights of false to turn them off</param>
        public virtual void ToggleLights(bool deployed)
        {
            lightsOn = deployed;

            //Play animation for current state
            PlayAnimation(lightsOn);

            if (lightsOn)
                Events["ToggleLights"].guiName = endEventGUIName;
            else
                Events["ToggleLights"].guiName = startEventGUIName;

            setupLights();
        }

        #endregion

        #region Animation
        public virtual void SetupAnimations()
        {
            Log("SetupAnimations called.");

            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null)
            {
                Log("No animations found.");
                return;
            }
            if (animations.Length == 0)
            {
                Log("No animations found.");
                return;
            }

            animation = animations[0];
            if (animation == null)
                return;

            //Set layer
            animationState = animation[animationName];
            animation[animationName].layer = animationLayer;

            if (lightsOn)
            {
                Events["ToggleLights"].guiName = endEventGUIName;

                animation[animationName].normalizedTime = 1.0f;
                animation[animationName].speed = 10000f;
            }
            else
            {
                Events["ToggleLights"].guiName = startEventGUIName;

                animation[animationName].normalizedTime = 0f;
                animation[animationName].speed = -10000f;
            }
            animation.Play(animationName);
        }

        public virtual void PlayAnimation(bool playInReverse = false)
        {
            if (string.IsNullOrEmpty(animationName))
                return;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;
            Animation anim = this.part.FindModelAnimators(animationName)[0];

            if (playInReverse)
            {
                anim[animationName].time = anim[animationName].length;
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }

            else
            {
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }
        }
        #endregion

        #region Helpers
        protected void Log(string message)
        {
            Debug.Log("[WBILightController] - " + message);
        }

        public void FixedUpdate()
        {
            //Get the required EC
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (lightsOn && ecRequired > 0f)
                {
                    double ecPerTimeTick = ecRequired * TimeWarp.fixedDeltaTime;
                    double ecObtained = this.part.RequestResource("ElectricCharge", ecPerTimeTick, ResourceFlowMode.ALL_VESSEL);

                    if (ecObtained / ecPerTimeTick < 0.999)
                        ToggleLights();
                }
            }

            //If the settings have changed then re-setup the lights.
            if (prevRed != red || prevGreen != green || prevBlue != blue || prevLevel != intensity)
            {
                if (lightsOn)
                {
                    lightsOn = false;
                    setupLights();
                    lightsOn = true;
                }
                setupLights();
            }
        }

        protected void setupLights()
        {
            if (lights == null)
                return;
            if (lights.Length == 0)
                return;
            Color color = new Color(red, green, blue, intensity);

            try
            {
                foreach (Light light in lights)
                {
                    light.color = color;

                    if (lightsOn)
                        light.intensity = intensity;
                    else
                        light.intensity = 0;
                }
            }
            catch
            {
            }

            //Get baseline values
            prevRed = red;
            prevGreen = green;
            prevBlue = blue;
            prevLevel = intensity;

        }
        #endregion

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

            bool wasOn = lightsOn;
            lightsOn = GUILayout.Toggle(lightsOn, kLightOn);
            if (lightsOn != wasOn)
            {
                if (lightsOn)
                    TurnOnLights();
                else
                    TurnOffLights();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(kRed);
            prevRed = red;
            red = GUILayout.HorizontalSlider(red, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(kGreen);
            prevGreen = green;
            green = GUILayout.HorizontalSlider(green, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(kBlue);
            prevBlue = blue;
            blue = GUILayout.HorizontalSlider(blue, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Hides the GUI controls in the Part Action Window.
        /// </summary>
        public void HideGUI()
        {
            Fields["red"].guiActive = false;
            Fields["green"].guiActive = false;
            Fields["blue"].guiActive = false;
            Fields["intensity"].guiActive = false;

            Fields["red"].guiActiveEditor = false;
            Fields["green"].guiActiveEditor = false;
            Fields["blue"].guiActiveEditor = false;
            Fields["intensity"].guiActiveEditor = false;

            Events["ToggleLights"].guiActive = false;
            Events["ToggleLights"].guiActiveEditor = false;
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
        /// Returns whether or not the animation is moving
        /// </summary>
        /// <returns>True if moving, false if not.</returns>
        public bool IsMoving()
        {
            return false;
        }

        /// <summary>
        /// Tells the servo to stop moving.
        /// </summary>
        public void StopMoving()
        {
            //NOP
        }

        /// <summary>
        /// Takes a snapshot of the current state of the servo.
        /// </summary>
        /// <returns>A SERVODATA_NODE ConfigNode containing the servo's state</returns>
        public ConfigNode TakeSnapshot()
        {
            ConfigNode node = new ConfigNode(WBIServoManager.SERVODATA_NODE);

            node.AddValue("servoName", servoName);
            node.AddValue("red", red);
            node.AddValue("green", green);
            node.AddValue("blue", blue);
            node.AddValue("lightsOn", lightsOn);

            return node;
        }

        /// <summary>
        /// Sets the servo's state based upon the supplied config node.
        /// </summary>
        /// <param name="node">A SERVODAT_NODE ConfigNode containing servo state data.</param>
        public void SetFromSnapshot(ConfigNode node)
        {
            float.TryParse(node.GetValue("red"), out red);
            float.TryParse(node.GetValue("green"), out green);
            float.TryParse(node.GetValue("blue"), out blue);
            bool.TryParse(node.GetValue("lightsOn"), out lightsOn);

            setupLights();
            if (lightsOn)
                TurnOnLights();
            else
                TurnOffLights();
        }
        #endregion

    }
}
