﻿/**
 * Window.cs
 * 
 * Thunder Aerospace Corporation's library for the Kerbal Space Program, by Taranis Elsu
 * 
 * (C) Copyright 2013, Taranis Elsu
 * 
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Thunder Aerospace Corporation is a ficticious entity created for entertainment
 * purposes. It is in no way meant to represent a real entity. Any similarity to a real entity
 * is purely coincidental.
 */

using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Dialogs;

namespace KerbalActuators
{
    public interface IManagedActuatorWindow
    {
        bool IsVisible();
        void DrawWindow();
    }

    public abstract class Window<T> : MonoBehaviour, IManagedActuatorWindow
    {
        private int windowId;
        private string configNodeName;
        private bool mouseDown;
        private bool visible;

        protected GUIStyle closeButtonStyle;
        private GUIStyle resizeStyle;
        private GUIContent resizeContent;

        public Rect windowPos;
        public string WindowTitle;
        public bool Resizable { get; set; }
        public bool HideCloseButton { get; set; }

        protected Window(string windowTitle, float defaultWidth, float defaultHeight)
        {
            this.WindowTitle = windowTitle;
            this.windowId = windowTitle.GetHashCode() + new System.Random().Next(65536) + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.GetHashCode();

            configNodeName = windowTitle.Replace(" ", "");

            windowPos = new Rect((Screen.width - defaultWidth) / 2, (Screen.height - defaultHeight) / 2, defaultWidth, defaultHeight);
            mouseDown = false;
            visible = false;

            var texture = WindowUtils.LoadImage<T>(IOUtils.GetFilePathFor(typeof(T), "resize.png"));
            resizeContent = (texture != null) ? new GUIContent(texture, "Drag to resize the window.") : new GUIContent("R", "Drag to resize the window.");

            Resizable = true;
            HideCloseButton = false;
        }

        public bool IsVisible()
        {
            return visible;
        }

        public virtual void SetVisible(bool newValue)
        {
            this.visible = newValue;

            if (!newValue)
            {
                if (HighLogic.LoadedSceneIsFlight)
                    InputLockManager.RemoveControlLock("WindowLock" + windowId);
                else if (HighLogic.LoadedSceneIsEditor)
                    EditorLogic.fetch.Unlock("WindowLock" + windowId);
            }
        }

        public void ToggleVisible()
        {
            SetVisible(!visible);
        }

        public void SetSize(int width, int height)
        {
            windowPos.width = width;
            windowPos.height = height;
        }

        public virtual ConfigNode Load(ConfigNode config)
        {
            if (config.HasNode(configNodeName))
            {
                ConfigNode windowConfig = config.GetNode(configNodeName);

                windowPos.x = WindowUtils.GetValue(windowConfig, "x", windowPos.x);
                windowPos.y = WindowUtils.GetValue(windowConfig, "y", windowPos.y);
                windowPos.width = WindowUtils.GetValue(windowConfig, "width", windowPos.width);
                windowPos.height = WindowUtils.GetValue(windowConfig, "height", windowPos.height);

                bool newValue = WindowUtils.GetValue(windowConfig, "visible", visible);
                SetVisible(newValue);

                return windowConfig;
            }
            else
            {
                return null;
            }
        }

        public virtual ConfigNode Save(ConfigNode config)
        {
            ConfigNode windowConfig;
            if (config.HasNode(configNodeName))
            {
                windowConfig = config.GetNode(configNodeName);
                windowConfig.ClearData();
            }
            else
            {
                windowConfig = config.AddNode(configNodeName);
            }

            windowConfig.AddValue("visible", visible);
            windowConfig.AddValue("x", windowPos.x);
            windowConfig.AddValue("y", windowPos.y);
            windowConfig.AddValue("width", windowPos.width);
            windowConfig.AddValue("height", windowPos.height);
            return windowConfig;
        }

        //Note: parent object needs to call DrawWindow in its OnGUI method.
        public virtual void DrawWindow()
        {
            if (visible)
            {
                bool paused = false;
                if (HighLogic.LoadedSceneIsFlight)
                {
                    try
                    {
                        paused = PauseMenu.isOpen || FlightResultsDialog.isDisplaying;
                    }
                    catch (Exception)
                    {
                        // ignore the error and assume the pause menu is not open
                    }
                }

                if (!paused)
                {
                    GUI.skin = HighLogic.Skin;
                    ConfigureStyles();

                    windowPos = WindowUtils.EnsureVisible(windowPos);
                    windowPos = GUILayout.Window(windowId, windowPos, PreDrawWindowContents, WindowTitle, GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true), GUILayout.MinWidth(64), GUILayout.MinHeight(64));
                }

            }
        }

        protected virtual void ConfigureStyles()
        {
            if (closeButtonStyle == null)
            {
                closeButtonStyle = new GUIStyle(GUI.skin.button);
                closeButtonStyle.padding = new RectOffset(5, 5, 3, 0);
                closeButtonStyle.margin = new RectOffset(1, 1, 1, 1);
                closeButtonStyle.stretchWidth = false;
                closeButtonStyle.stretchHeight = false;
                closeButtonStyle.alignment = TextAnchor.MiddleCenter;

                resizeStyle = new GUIStyle(GUI.skin.button);
                resizeStyle.alignment = TextAnchor.MiddleCenter;
                resizeStyle.padding = new RectOffset(1, 1, 1, 1);
            }
        }

        private void PreDrawWindowContents(int windowId)
        {
            DrawWindowContents(windowId);

            if (!HideCloseButton)
            {
                if (GUI.Button(new Rect(windowPos.width - 24, 4, 20, 20), "X", closeButtonStyle))
                {
                    SetVisible(false);
                }
            }

            if (Resizable)
            {
                var resizeRect = new Rect(windowPos.width - 16, windowPos.height - 16, 16, 16);
                GUI.Label(resizeRect, resizeContent, resizeStyle);

                HandleWindowEvents(resizeRect);
            }

            GUI.DragWindow();
        }

        bool lockedUI;
        private void preventClickthrough()
        {
            bool mouseInWindow = windowPos.Contains(Input.mousePosition);

            if (mouseInWindow && !lockedUI)
            {
                lockedUI = true;

                //Lock game controls
                if (HighLogic.LoadedSceneIsFlight)
                    InputLockManager.SetControlLock(ControlTypes.All, "WindowLock" + windowId);
                else if (HighLogic.LoadedSceneIsEditor)
                    EditorLogic.fetch.Lock(true, true, true, "WindowLock" + windowId);
            }

            else if (!mouseInWindow && lockedUI)
            {
                lockedUI = false;

                //Unlock game controls
                if (HighLogic.LoadedSceneIsFlight)
                    InputLockManager.RemoveControlLock("WindowLock" + windowId);
                else if (HighLogic.LoadedSceneIsEditor)
                    EditorLogic.fetch.Unlock("WindowLock" + windowId);
            }
        }

        protected abstract void DrawWindowContents(int windowId);

        private void HandleWindowEvents(Rect resizeRect)
        {
            var theEvent = Event.current;
            if (theEvent != null)
            {
                if (!mouseDown)
                {
                    if (theEvent.type == EventType.MouseDown && theEvent.button == 0 && resizeRect.Contains(theEvent.mousePosition))
                    {
                        mouseDown = true;
                        theEvent.Use();
                    }
                }
                else if (theEvent.type != EventType.Layout)
                {
                    if (Input.GetMouseButton(0))
                    {
                        // Flip the mouse Y so that 0 is at the top
                        float mouseY = Screen.height - Input.mousePosition.y;

                        windowPos.width = Mathf.Clamp(Input.mousePosition.x - windowPos.x + (resizeRect.width / 2), 50, Screen.width - windowPos.x);
                        windowPos.height = Mathf.Clamp(mouseY - windowPos.y + (resizeRect.height / 2), 50, Screen.height - windowPos.y);
                    }
                    else
                    {
                        mouseDown = false;
                    }
                }
            }
        }
    }

}
