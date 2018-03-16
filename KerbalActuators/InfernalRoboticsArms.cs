using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using DebugStuff;
using UnityEngine;


namespace InfernalRoboticsArms
{
    public class InfernalRoboticsArms : PartModule
    {
        [KSPField(isPersistant = true)]
        public string transformName = "";

        [KSPField(isPersistant = true)]
        public string speed = "";

        [KSPField(isPersistant = false)]
        public string vector = "";

        [KSPField(isPersistant = true)]
        public string forwardLimit = "";

        [KSPField(isPersistant = true)]
        public string reverseLimit = "";

        [KSPField(isPersistant = true)]
        public float coordinate;

        [KSPField(isPersistant = false)]
        public string fixedMesh;

        [KSPField(isPersistant = true)]
        public string cameraName;

        [KSPField(isPersistant = true)]
        public bool drawAttachRay;

        [KSPField(isPersistant = true)]
        public string vector2 = "";

        [KSPField(isPersistant = true)]
        public string mode = "rotate";

        [KSPField(isPersistant = true)]
        public string savedCoordinates;

        [KSPField(isPersistant = true)]
        public string realName = "";


        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Show Controller Menu", active = true)]
        public void ShowMainMenu()
        {
            controlWindowID = Guid.NewGuid().GetHashCode();
            guiEnabled = !guiEnabled;
        }


        [KSPField(isPersistant = false)]
        public string attachNode = "";
        [KSPField(isPersistant = false)]
        public float attachRange = 0.9f;

        int controlWindowID;

        class RoboticArmPart
        {
            public List<string> transformNames;
            public List<string> realTransformNames;
            public List<Transform> transforms;
            public List<string> speeds;
            public List<string> vectors;
            public List<string> forwardLimits;
            public List<string> reverseLimits;
            public List<float> coordinates;
            public List<string> modeofmovement;
            public List<Vector3> directionVectors = new List<Vector3>();
            public List<Vector3> savedCoordinates = new List<Vector3>();
            public List<Vector3> initialCoordinates = new List<Vector3>();
        }

        //public List<string> transformNames;
        //public List<string> speeds;
        //public List<string> vectors;
        //public List<string> forwardLimits;
        //public List<string> reverseLimits;
        public List<float> coordinates;
        public List<string> vectors2;
        public List<string> modes;
        public List<string> partCoordinateVectors;

        public Dictionary<string, Transform> RobotTransforms = new Dictionary<string, Transform>();
        public static InfernalRoboticsArms Instance { get; protected set; }
        //public Color[] colors = {Color.red, Color.blue, Color.cyan, Color.green, Color.magenta, Color.yellow, Color.white, Color.gray};
        public Color[] colors = { Color.red, Color.blue, Color.red, Color.blue, Color.red, Color.blue, Color.red, Color.blue };

        protected static InfernalRoboticsArms gui_controller;
        protected static Rect controlWinPos;
        protected static bool resetWin = false;
        bool guiEnabled = false;

        //private List<AttachNode> aNList;
        String[] vectorArray;
        String[] coordinateArray;
        RoboticArmPart armpart;
        public override void OnAwake()
        {

            armpart = new RoboticArmPart();
            StringBuilder sb = new StringBuilder();
            armpart.transformNames = transformName.Split(',').Select(sValue => sValue.Trim()).ToList();
           
            armpart.speeds = speed.Split(',').Select(sValue => sValue.Trim()).ToList();
            armpart.vectors = vector.Split(',').Select(sValue => sValue.Trim()).ToList();
            armpart.forwardLimits = forwardLimit.Split(',').Select(sValue => sValue.Trim()).ToList();
            armpart.reverseLimits = reverseLimit.Split(',').Select(sValue => sValue.Trim()).ToList();
            armpart.modeofmovement = mode.Split(',').Select(sValue => sValue.Trim()).ToList();
            armpart.savedCoordinates = Enumerable.Repeat(Vector3.zero, armpart.transformNames.Count).ToList();
            if(realName != "")
                armpart.realTransformNames = realName.Split(',').Select(sValue => sValue.Trim()).ToList();

            //armpart.realTransformNames = realName.Split(',').ToList();
            string pattern = @",(?![^\(\[]*[\]\)])";
            Regex rgx = new Regex(pattern);
            //string input = "123ABCDE456FGHIJKL789MNOPQ012";

            if (vector2 != "")
            {
                vectorArray = rgx.Split(vector2);
                foreach (String tempvector in vectorArray)
                {
                    armpart.directionVectors.Add(convertToVector3(tempvector));
                }
            }


            List<Transform> tempTransforms = new List<Transform>();
            foreach (string s in armpart.transformNames)
            {
                //  var g = part.FindModelTransform(tList[i]);  <---from RoverDude
                if (RobotTransforms.Count() == 0)
                {

                    Transform tempTransform = transform.Find(s);
                    //armpart.transforms.Add(tempTransform);
                    RobotTransforms.Add(s, tempTransform);
                    //RobotTransforms.Add(s, tempTransform);
                    tempTransforms.Add(tempTransform);
                    //Debug.Log("Added: "+s);
                }
                else
                {
                    Transform tempTransform2 = RobotTransforms.Last().Value;
                    RobotTransforms.Add(s, tempTransform2.Find(s));
                    tempTransforms.Add(tempTransform2.Find(s));
                    //Debug.Log("Added: " + s);
                }
            }

            coordinates = Enumerable.Repeat(0f, tempTransforms.Count()).ToList();
            armpart.coordinates = coordinates;
            armpart.transforms = tempTransforms;
            Instance = this;
        }


        Vector3 initialVectors;
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            string pattern = @",(?![^\(\[]*[\]\)])";
            Regex rgx = new Regex(pattern);
            //Debug.Log("RoboticArmy savedCoordinates loadded: " + savedCoordinates);
            if (savedCoordinates != "" && savedCoordinates != null)
            {
                coordinateArray = rgx.Split(savedCoordinates);
                //foreach (string tempCoord in coordinateArray)
                for (int j = 0; j < armpart.savedCoordinates.Count; j++)
                {
                    armpart.savedCoordinates[j] = convertToVector3(coordinateArray[j]);

                    //Debug.Log("RoboticArmy TempCoord: " + tempCoord);
                    //armpart.savedCoordinates = (convertToVector3(tempCoord));

                    //if (armpart.modeofmovement == "rotate")
                    {
                        //GUILayout.Label("x: " + Math.Round(armpart.transforms[count].localRotation.eulerAngles.x, 2), GUILayout.ExpandWidth(true));
                        //GUILayout.Label("y: " + Math.Round(armpart.transforms[count].localRotation.eulerAngles.y, 2), GUILayout.ExpandWidth(true));
                        //GUILayout.Label("z: " + Math.Round(armpart.transforms[count].localRotation.eulerAngles.z, 2), GUILayout.ExpandWidth(true));
                        //savedCoordinates = SerializeVector3Array(armpart.savedCoordinates.ToArray());

                 
                    }
                   // else if (armpart.modeofmovement[count] == "translate")
                    {
                        //GUILayout.Label("x: " + Math.Round(armpart.transforms[count].localPosition.x, 2), GUILayout.ExpandWidth(true));
                        //GUILayout.Label("y: " + Math.Round(armpart.transforms[count].localPosition.y, 2), GUILayout.ExpandWidth(true));
                        //GUILayout.Label("z: " + Math.Round(armpart.transforms[count].localPosition.z, 2), GUILayout.ExpandWidth(true));
                    }
                }
                
                Debug.Log("savedCoordinates: " + coordinateArray.ToString());
            }
            else if (savedCoordinates == "")
            {
                for (int j = 0; j < armpart.savedCoordinates.Count; j++)
                {
                    armpart.savedCoordinates[j] = Vector3.zero;
                    //initialVectors = armpart.transforms[j].localEulerAngles;
                    armpart.initialCoordinates[j] = armpart.transforms[j].localEulerAngles;

                    //armpart.transforms[j].localEulerAngles = Vector3.zero;
                }
            }

            //int countcoordinates = 0;
            //foreach(Vector3 vector in armpart.savedCoordinates)
            //{
            //    if (armpart.modeofmovement[countcoordinates] == "rotate")
            //    {
            //        //armpart.transforms[count].Rotate(armpart.directionVectors[count], 0.3f * float.Parse(armpart.speeds[count]));
            //        armpart.transforms[countcoordinates].Rotate(vector);
            //    }
            //}

            for(int i = 0; i<armpart.savedCoordinates.Count; i++)
            {
                if (armpart.modeofmovement[i] == "rotate")
                {
                    //armpart.transforms[count].Rotate(armpart.directionVectors[count], 0.3f * float.Parse(armpart.speeds[count]));
                    armpart.transforms[i].Rotate(armpart.savedCoordinates[i]);
                }
            }

        }

        public Vector3 convertToVector3(String parenVector)
        {
            String outString;
            Vector3 outVector3;
            String[] splitString;

            // Trim extranious parenthesis

            outString = parenVector.Substring(1, parenVector.Length - 2);

            // Split delimted values into an array

            splitString = outString.Split(","[0]);

            // Build new Vector3 from array elements

            outVector3.x = float.Parse(splitString[0]);
            outVector3.y = float.Parse(splitString[1]);
            outVector3.z = float.Parse(splitString[2]);

            return outVector3;
        }


        Vector3 returnVector(string vectorDesignator)
        {
            Vector3 tempVector = Vector3.zero;
            switch (vectorDesignator)
            {
                case "u":
                    //(0,1,0)
                    tempVector = Vector3.up;
                    break;
                case "r":
                    //(1,0,0)
                    tempVector = Vector3.right;
                    break;
                case "l":
                    //(-1,0,0)
                    tempVector = Vector3.left;
                    break;
                case "f":
                    //(0,0,1)
                    tempVector = Vector3.forward;
                    break;
            }
            return tempVector;
        }

        bool withinLimits(float coordinate, string forwardLimit, string reverseLimit)
        {
            //List<string> limitarray = limits.Split(':').Select(sValue => sValue.Trim()).ToList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("coordnate: " + coordinate);
            sb.AppendLine("Convert.ToDouble(forwardLimit): " + Convert.ToDouble(forwardLimit));
            sb.AppendLine("Convert.ToDouble(reverseLimits): " + Convert.ToDouble(reverseLimit));
            //Debug.Log(sb.ToString());
            //if (coordinate >= Convert.ToDouble(limitarray.First()) && coordinate <= Convert.ToDouble(limitarray.Last()))
            if (coordinate < Convert.ToDouble(forwardLimit) && coordinate > Convert.ToDouble(reverseLimit))
                return true;
            else
                return false;
        }

        [KSPField(guiName = "Ray", guiActive = true)]
        public string ravInfo;

        //private Rigidbody hitBody = null; //current hit reference
        //bool connectable;
        bool connected = false;
        //Part hitPart;
        private ConfigurableJoint joint_rotate;
        //DockedVesselInfo vesselRef;
        Part ref_hitPart;
        //String partRef;

        RaycastHit hit;
        public Vector3 cameraPosition; //{ get { return armTip.position + armTip.up.normalized * 0.4f; } }
        public Vector3 cameraForward; //{ get { return armTip.up; } }
        //public Vector3 cameraNormal; //{ get { return armTip.forward; } }
        void FixedUpdate()
        {

            if (drawAttachRay && HighLogic.LoadedSceneIsFlight)
            {
//                DebugDrawer.DebugLine(cameraPosition, cameraPosition + cameraForward * attachRange, Color.red);
                //DrawLine
                
            }

            cameraPosition = part.FindModelTransform(attachNode).position;
            cameraForward = part.FindModelTransform(attachNode).forward;
            RaycastHit hit;
            int tempLayerMask = ~layerMask;
            if (!connected && Physics.Raycast(cameraPosition, cameraForward, out hit, attachRange, tempLayerMask))
            {
                ravInfo = hit.collider.gameObject.name.ToString();
                try
                {
                    _target = hit.collider.gameObject;
                    _targetRb = (hit.rigidbody);
                }
                catch (NullReferenceException) { }
            }
            else
            {
                ravInfo = "";
                _target = null;
            }
        }


        public GameObject attachNodePrefab;
        private void ControlWindow(int windowID)
        {
            GUILayout.BeginVertical();
            int count = 0;

            foreach (string s in armpart.transformNames)
            {
                GUILayout.BeginHorizontal();
                //GUILayout.Label(s, GUILayout.ExpandWidth(true));
                GUILayout.Label(armpart.realTransformNames[count], GUILayout.ExpandWidth(true));
                //GUILayout.Label("coordinate: " + Math.Round(armpart.coordinates[count], 2));

                if (armpart.modeofmovement[count] == "rotate")
                {
                    GUILayout.Label("x: " + Math.Round(armpart.transforms[count].localRotation.eulerAngles.x, 2), GUILayout.ExpandWidth(true));
                    GUILayout.Label("y: " + Math.Round(armpart.transforms[count].localRotation.eulerAngles.y, 2), GUILayout.ExpandWidth(true));
                    GUILayout.Label("z: " + Math.Round(armpart.transforms[count].localRotation.eulerAngles.z, 2), GUILayout.ExpandWidth(true));
                    savedCoordinates = SerializeVector3Array(armpart.savedCoordinates.ToArray());
                }
                else if (armpart.modeofmovement[count] == "translate")
                {
                    GUILayout.Label("x: " + Math.Round(armpart.transforms[count].localPosition.x, 2), GUILayout.ExpandWidth(true));
                    GUILayout.Label("y: " + Math.Round(armpart.transforms[count].localPosition.y, 2), GUILayout.ExpandWidth(true));
                    GUILayout.Label("z: " + Math.Round(armpart.transforms[count].localPosition.z, 2), GUILayout.ExpandWidth(true));
                }
                int subTransforms = 0;
                subTransforms = armpart.transformNames.FindIndex(a => a.Contains(s));

                if (GUILayout.RepeatButton("<", GUILayout.Width(21)))
                {
                    if (armpart.forwardLimits[count].Contains("Full"))
                    {
                        if (armpart.modeofmovement[count] == "rotate")
                        {
                            //armpart.transforms[count].Rotate(returnVector(armpart.vectors[count]), 0.3f * float.Parse(armpart.speeds[count]));
                            armpart.transforms[count].Rotate(armpart.directionVectors[count], 0.3f * float.Parse(armpart.speeds[count]));
                        }
                        else if (armpart.modeofmovement[count] == "translate")
                        {
                            armpart.transforms[count].Translate(armpart.directionVectors[count] * 0.01f * float.Parse(armpart.speeds[count]), Space.Self);
                        }
                        armpart.coordinates[count] = 0.3f * float.Parse(armpart.speeds[count]) + armpart.coordinates[count];
                        armpart.savedCoordinates[count] = armpart.transforms[count].localEulerAngles;

                    }
                    else
                    {
                        //works
                        armpart.coordinates[count] = 0.3f * float.Parse(armpart.speeds[count]) + armpart.coordinates[count];
                        if (withinLimits(armpart.coordinates[count], armpart.forwardLimits[count], armpart.reverseLimits[count]))
                        {
                            //armpart.transforms.ElementAt(count).Rotate(returnVector(armpart.vectors[count]), 0.3f * float.Parse(armpart.speeds[count]));
                            if (armpart.modeofmovement[count] == "rotate")
                            {
                                armpart.transforms.ElementAt(count).Rotate(armpart.directionVectors[count], 0.3f * float.Parse(armpart.speeds[count]));
                            }
                            else if (armpart.modeofmovement[count] == "translate")
                            {
                                armpart.transforms[count].Translate(armpart.directionVectors[count] * 0.01f * float.Parse(armpart.speeds[count]), Space.Self);
                            }

                        }

                        if (armpart.coordinates[count] < float.Parse(armpart.reverseLimits[count]) || armpart.coordinates[count] > float.Parse(armpart.forwardLimits[count]))
                            armpart.coordinates[count] = Mathf.Clamp(armpart.coordinates[count], float.Parse(armpart.reverseLimits[count]), float.Parse(armpart.forwardLimits[count]));
                        armpart.savedCoordinates[count] = armpart.transforms[count].localEulerAngles;
                    }
                }
                if (GUILayout.RepeatButton("O", GUILayout.Width(21)))
                {
                    //reset to zero
                    //this.part.transform.Rotate(returnVector(armpart.vectors[count]), -0.3f * float.Parse(armpart.speeds[count]));


                }
                if (GUILayout.RepeatButton(">", GUILayout.Width(21)))
                {
                    if (armpart.forwardLimits[count].Contains("Full"))
                    {
                        //convertToVector3(vectorArray[count])
                        //armpart.transforms[count].Rotate(returnVector(armpart.vectors[count]), -0.3f * float.Parse(armpart.speeds[count]));
                        if (armpart.modeofmovement[count] == "rotate")
                        {
                            armpart.transforms[count].Rotate(armpart.directionVectors[count], -0.3f * float.Parse(armpart.speeds[count]));
                        }
                        else if (armpart.modeofmovement[count] == "translate")
                        {
                            armpart.transforms[count].Translate(armpart.directionVectors[count] * -0.01f * float.Parse(armpart.speeds[count]), Space.Self);
                        }
                        armpart.coordinates[count] = -0.3f * float.Parse(armpart.speeds[count]) + armpart.coordinates[count];
                        armpart.savedCoordinates[count] = armpart.transforms[count].localEulerAngles;
                    }
                    else
                    {
                        //works
                        armpart.coordinates[count] = -0.3f * float.Parse(armpart.speeds[count]) + armpart.coordinates[count];
                        if (withinLimits(armpart.coordinates[count], armpart.forwardLimits[count], armpart.reverseLimits[count]))
                        {
                            //armpart.transforms[count].Rotate(returnVector(armpart.vectors[count]), -0.3f * float.Parse(armpart.speeds[count]));
                            if (armpart.modeofmovement[count] == "rotate")
                            {
                                armpart.transforms[count].Rotate(armpart.directionVectors[count], -0.3f * float.Parse(armpart.speeds[count]));
                            }
                            else if (armpart.modeofmovement[count] == "translate")
                            {
                                armpart.transforms[count].Translate(armpart.directionVectors[count] * -0.01f * float.Parse(armpart.speeds[count]), Space.Self);
                            }
                        }
                        if (armpart.coordinates[count] < float.Parse(armpart.reverseLimits[count]) || armpart.coordinates[count] > float.Parse(armpart.forwardLimits[count]))
                        {
                            armpart.coordinates[count] = Mathf.Clamp(armpart.coordinates[count], float.Parse(armpart.reverseLimits[count]), float.Parse(armpart.forwardLimits[count]));

                        }
                        armpart.savedCoordinates[count] = armpart.transforms[count].localEulerAngles;
                    }
                }

                GUILayout.EndHorizontal();
                count++;

            }

            //savedCoordinates = armpart.savedCoordinates.ToString();
            //savedCoordinates = SerializeVector3Array(armpart.savedCoordinates.ToArray());
            //savedCoordinates - armpart.savedCoordinates.ToString();

            if (GUILayout.Button("Close"))
            {
                //saveConfigXML();
                guiEnabled = false;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public static Vector3[] DeserializeVector3Array(string aData)
        {
            string[] vectors = aData.Split('|');
            Vector3[] result = new Vector3[vectors.Length];
            for (int i = 0; i < vectors.Length; i++)
            {
                string[] values = vectors[i].Split(' ');
                if (values.Length != 3)
                    throw new System.FormatException("component count mismatch. Expected 3 components but got " + values.Length);
                result[i] = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
            }
            return result;
        }

        public static string SerializeVector3Array(Vector3[] aVectors)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Vector3 v in aVectors)
            {
                sb.Append("(").Append(v.x).Append(",").Append(v.y).Append(",").Append(v.z).Append("),");
            }
            if (sb.Length > 0) // remove last "|"
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

    
        //[KSPField]
        //public PartRefs partRefs;
       
        [KSPField]
        public string hookObjectName;
        [KSPField]
        public int layerMask = 0;

        GameObject _hook;
        GameObject _target;
        Rigidbody _rb;
        Rigidbody _targetRb;
        ConfigurableJoint _joint;
        bool isReady;


        //bool connected = false;
        [KSPEvent(guiActive = true, guiName = "Magnet: Off", active = true, guiActiveUnfocused = true, unfocusedRange = 40f)]
        public void MagnetToggle()
        {
            if (_target != null && connected == false)
            {
                //_joint = new ConfigurableJoint();
                _rb = _hook.AddComponent<Rigidbody>();
                _rb.isKinematic = true;
                //_rb.mass = 0.1f;
                //_rb.useGravity = false;
                //_rb.constraints = RigidbodyConstraints.FreezeAll;

                _joint = _hook.AddComponent<ConfigurableJoint>();

                _joint.xMotion = ConfigurableJointMotion.Locked;
                _joint.yMotion = ConfigurableJointMotion.Locked;
                _joint.zMotion = ConfigurableJointMotion.Locked;

                _joint.angularXMotion = ConfigurableJointMotion.Locked;
                _joint.angularYMotion = ConfigurableJointMotion.Locked;
                _joint.angularZMotion = ConfigurableJointMotion.Locked;

                _joint.connectedBody = _targetRb;
                connected = true;
                Events["MagnetToggle"].guiName = "Magnet: On";

            }
            else if (connected == true)
            {
                UnityEngine.Object.Destroy(_joint.GetComponent<ConfigurableJoint>());
                UnityEngine.Object.Destroy(_rb.GetComponent<Rigidbody>());
                connected = false;
                Events["MagnetToggle"].guiName = "Magnet: Off";
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight)
            {
                //Debug.LogError(hookObjectName);
                //_hook = transform.Search(hookObjectName).gameObject;
                _hook = part.FindModelTransform(attachNode).gameObject;
                isReady = true;

            }

        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (GameScenes.EDITOR == GameScenes.FLIGHT)
            {
                for (int j = 0; j < armpart.savedCoordinates.Count; j++)
                {
                    armpart.savedCoordinates[j] = -armpart.initialCoordinates[j];
                }
            }
            //initialVectors
            //foreach(Vector3 tempvector in armpart.savedCoordinates)
            //{
            //    //armpart.savedCoordinates = -armpart.initialCoordinates;
            //    tempvector 
            //}

        }

        void Update()
        {
            //DebugDrawer.DebugLine(gameObject.transform.position, Planetarium.fetch.Home.transform.position, Color.yellow);

        }

        Transform _forceTransform;
        private Part hitPart = null; //current hit reference
        private Rigidbody hitBody = null; //current hit reference
        private bool connectable;


        void OnGUI()
        {
            if (InputLockManager.IsLocked(ControlTypes.LINEAR))
                return;
            if (controlWinPos.x == 0 && controlWinPos.y == 0)
            {
                controlWinPos = new Rect(Screen.width - 510, 70, 10, 10);
                //cameraWindoPos = new Rect(300, 300, 10, 10);
            }
            if (resetWin)
            {
                controlWinPos = new Rect(controlWinPos.x, controlWinPos.y,
                            10, 10);
                //cameraWindoPos = new Rect(cameraWindoPos.x, cameraWindoPos.y, 10, 10);
                resetWin = false;
            }
            GUI.skin = HighLogic.Skin;
            var scene = HighLogic.LoadedScene;

            //Call the DragAndDrop GUI Setup stuff
            if (scene == GameScenes.EDITOR || scene == GameScenes.FLIGHT)
            {
                var height = GUILayout.Height(Screen.height / 2);
                if (guiEnabled)
                {
                    controlWinPos = GUILayout.Window(controlWindowID, controlWinPos,
                                                     ControlWindow,
                                                     "Servo Control",
                                                     GUILayout.Width(500),
                                                     GUILayout.Height(80));
                }



            }
        }
    }

    //[System.Serializable]
    //public class PartRefs : IConfigNode
    //{
    //    [SerializeField]
    //    private List<uint> partRefs;

    //    public PartRefs()
    //    {
    //        partRefs = new List<uint>();
    //    }

    //    public void Add(uint r)
    //    {
    //        partRefs.Add(r);
    //    }

    //    public List<uint>.Enumerator GetEnumerator()
    //    {
    //        return partRefs.GetEnumerator();
    //    }

    //    public void Load(ConfigNode node)
    //    {
    //        string[] values = node.GetValues("part");
    //        for (int i = 0; i < values.Length; i++)
    //            Add(uint.Parse(values[i]));
    //    }

    //    public void Save(ConfigNode node)
    //    {
    //        for (int i = 0; i < partRefs.Count; i++)
    //            node.AddValue("part", partRefs[i].ToString());
    //    }
    //}
    //public class JointBreakCallback : MonoBehaviour
    //{
    //    Axtion action;
    //    public void AddCallback(Axtion action)
    //    {
    //        this.action = action;
    //    }
    //    public void OnJointBreak(float breakForce)
    //    {
    //        print("JOINTBREAK");
    //        action();
    //        Destroy(this);
    //    }
    //}
}