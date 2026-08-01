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
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class WBIActuatorsGUIMgr : MonoBehaviour
    {

        public static WBIActuatorsGUIMgr Instance;
        List<IManagedActuatorWindow> managedWindows = new List<IManagedActuatorWindow>();
        bool uiVisible = true;

        public void Awake()
        {
            Instance = this;
            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
        }

        public void OnDestroy()
        {
            GameEvents.onHideUI.Remove(onHideUI);
            GameEvents.onShowUI.Remove(onShowUI);
        }

        public void OnGUI()
        {
            if (!uiVisible)
                return;

            int totalWindows = managedWindows.Count;
            IManagedActuatorWindow managedWindow;

            for (int index = 0; index < totalWindows; index++)
            {
                managedWindow = managedWindows[index];
                if (managedWindow.IsVisible())
                    managedWindow.DrawWindow();
            }
        }

        public void onShowUI()
        {
            uiVisible = true;
        }

        public void onHideUI()
        {
            uiVisible = false;
        }

        public void RegisterWindow(IManagedActuatorWindow managedWindow)
        {
            if (managedWindows.Contains(managedWindow) == false)
                managedWindows.Add(managedWindow);
        }

        public void UnregisterWindow(IManagedActuatorWindow managedWindow)
        {
            if (managedWindows.Contains(managedWindow))
                managedWindows.Remove(managedWindow);
        }
    }
}
