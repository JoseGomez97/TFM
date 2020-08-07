using TMPro;
using UnityEngine;
using Vuforia;
using UnityEngine.UI;
using System.Globalization;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;
using geometry_msgs = RosSharp.RosBridgeClient.MessageTypes.Geometry;

namespace RosSharp.RosBridgeClient
{
    public class CanvasManager : MonoBehaviour
    {
        public bool showDebug = false;

        public TextMeshProUGUI debugTextUI;

        public ObjectTargetBehaviour cylinder;
        public ImageTargetBehaviour cilindros;
        public ImageTargetBehaviour cajas;
        public ImageTargetBehaviour esferas;

        public CanvasGroup debugInfoCanvasGroup;
        public CanvasGroup mainUiCanvasGroup;
        public CanvasGroup settingsMenuCanvasGroup;
        public CanvasGroup exitMenuCanvasGroup;
        public CanvasGroup markersMenuCanvasGroup;
        public CanvasGroup actionMenuCanvasGroup;

        public InputField serverIP;
        public InputField serverPort;

        public InputField cilindrosPosX;
        public InputField cilindrosPosY;
        public InputField cilindrosPosZ;
        public InputField cajasPosX;
        public InputField cajasPosY;
        public InputField cajasPosZ;
        public InputField esferasPosX;
        public InputField esferasPosY;
        public InputField esferasPosZ;

        public Text warningSettingsText;

        public Button applySettingsButton;
        public Button applyMarkersButton;

        private GameObject lastObject;

        private RosSocket rosSocket;
        private string rosMasterUri;
        private string takeTopic;
        private string releaseTopic;
        private string action1Topic;
        private string action2Topic;

        public string current = "main"; // Can be "main", "settings", "markers" or "exit"

        public void startRosSocket(string rosMasterUri)
        {
            this.rosMasterUri = rosMasterUri;

            // WebSocket Connection
            rosSocket = new RosSocket(
                new Protocols.WebSocketNetProtocol(
                    this.rosMasterUri));

            takeTopic = rosSocket.Advertise<geometry_msgs.PoseStamped>("take_object");
            releaseTopic = rosSocket.Advertise<geometry_msgs.PoseStamped>("release_object");
            action1Topic = rosSocket.Advertise<std_msgs.String>("action1");
            action2Topic = rosSocket.Advertise<std_msgs.String>("action2");
        }

        // Start is called before the first frame update
        void Start()
        {
            EnableMainUI();
            DisableExitMenu();
            DisableSettingsMenu();
            DisableDebugInfo();
            DisableMarkersMenu();
            DisableActionMenu();

            startRosSocket("ws://192.168.2.103:9090");

            Application.targetFrameRate = 60;
        }

        // Update is called once per frame
        void Update()
        {
            // Go back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (current == "main")
                {
                    EnableExitMenu();
                }
                else if (current == "settings")
                {
                    DisableSettingsMenu();
                }
                else if (current == "exit")
                {
                    DisableExitMenu();
                }
                else if (current == "markers")
                {
                    DisableMarkersMenu();
                }
            }

            if (showDebug == true)
            {
                if (lastObject != null)
                {
                    debugTextUI.text = lastObject.name + " position:" +
                        "\n  x: " + lastObject.transform.position.x +
                        "\n  y: " + lastObject.transform.position.y +
                        "\n  z: " + lastObject.transform.position.z;
                }
            }
            if (current == "main")
            {
#if UNITY_EDITOR
                // Press object with mouse
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray_mouse = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray_mouse, out RaycastHit hit_mouse))
                    {
                        // Object selected
                        lastObject = hit_mouse.transform.gameObject;
                        EnableActionMenu();
                    }
                }
#elif UNITY_ANDROID
        // Press object with a touchscreen
        if (Input.GetTouch(0).phase == TouchPhase.Stationary)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Object selected
                lastObject = hit.transform.gameObject;
                EnableActionMenu();
            }
        }
#endif
            }
            else if (current == "settings")
            {
                if (CheckServerIP())
                {
                    applySettingsButton.interactable = true;
                }
                else
                {
                    applySettingsButton.interactable = false;
                }
            }
            else if (current == "markers")
            {
                if (CheckMarkersFormat())
                {
                    applyMarkersButton.interactable = true;
                }
                else
                {
                    applyMarkersButton.interactable = false;
                }
            }
        }

        public bool CheckServerIP()
        {
            bool isCorrect = true;

            // Check that the IP has a correct format
            string[] ip = serverIP.text.Split('.');

            // Has 4 fields
            if (ip.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    // Each field is an integer
                    if (int.TryParse(ip[i], out int n))
                    {
                        // Each field is between 0 and 255
                        if (n < 0 || n > 255)
                        {
                            isCorrect = false;
                        }
                    }
                    else
                    {
                        isCorrect = false;
                    }
                }
            }
            else
            {
                isCorrect = false;
            }

            if (isCorrect == true)
            {
                warningSettingsText.gameObject.SetActive(false);
            }
            else
            {
                warningSettingsText.gameObject.SetActive(true);
            }

            return isCorrect;
        }

        public bool CheckMarkersFormat()
        {
            if (IsFloat(cilindrosPosX.text) &&
                IsFloat(cilindrosPosY.text) &&
                IsFloat(cilindrosPosZ.text) &&
                IsFloat(cajasPosX.text) &&
                IsFloat(cajasPosY.text) &&
                IsFloat(cajasPosZ.text) &&
                IsFloat(esferasPosX.text) &&
                IsFloat(esferasPosY.text) &&
                IsFloat(esferasPosZ.text))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsFloat(string str)
        {
            return (float.TryParse(str.Replace(",", "."), NumberStyles.Float,
                    new CultureInfo("en-US").NumberFormat, out _));
        }

        public void OnTakeButton()
        {
            geometry_msgs.PoseStamped msg = new geometry_msgs.PoseStamped();
            msg.header.frame_id = lastObject.name;
            GetGeometryPoint(lastObject.gameObject.transform.position.Unity2Ros(), msg.pose.position);
            GetGeometryQuaternion(lastObject.gameObject.transform.rotation.Unity2Ros(), msg.pose.orientation);

            rosSocket.Publish(takeTopic, msg);

            DisableActionMenu();
        }
        public void OnReleaseButton()
        {
            geometry_msgs.PoseStamped msg = new geometry_msgs.PoseStamped();
            msg.header.frame_id = lastObject.name;
            GetGeometryPoint(lastObject.gameObject.transform.position.Unity2Ros(), msg.pose.position);
            GetGeometryQuaternion(lastObject.gameObject.transform.rotation.Unity2Ros(), msg.pose.orientation);

            rosSocket.Publish(releaseTopic, msg);

            DisableActionMenu();
        }
        public void OnAction1Button()
        {
            std_msgs.String msg = new std_msgs.String("Test action 1 with object '"+lastObject.name+"'");

            rosSocket.Publish(action1Topic, msg);

            DisableActionMenu();
        }
        public void OnAction2Button()
        {
            std_msgs.String msg = new std_msgs.String("Test action 2 with object '" + lastObject.name + "'");

            rosSocket.Publish(action2Topic, msg);

            DisableActionMenu();
        }



        public void OnApplySettings()
        {
            rosSocket.Close();
            startRosSocket("ws://" + serverIP.text + ":" + serverPort.text);

            DisableSettingsMenu();
        }

        public void OnApplyMarkers()
        {
            cilindros.gameObject.transform.position =
                new Vector3(float.Parse(cilindrosPosX.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat),
                            float.Parse(cilindrosPosY.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat),
                            float.Parse(cilindrosPosZ.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat));
            cajas.gameObject.transform.position =
                new Vector3(float.Parse(cajasPosX.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat),
                            float.Parse(cajasPosY.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat),
                            float.Parse(cajasPosZ.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat));
            esferas.gameObject.transform.position =
                new Vector3(float.Parse(esferasPosX.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat),
                            float.Parse(esferasPosY.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat),
                            float.Parse(esferasPosZ.text.Replace(",", "."), new CultureInfo("en-US").NumberFormat));

            DisableMarkersMenu();
        }

        public void OnEnableDebug()
        {
            if (showDebug == true)
            {
                showDebug = false;
                DisableDebugInfo();
            }
            else
            {
                showDebug = true;
                EnableDebugInfo();
            }
        }
        public void OnEnableSettings()
        {
            if (current == "settings")
            {
                DisableSettingsMenu();
            }
            else
            {
                current = "settings";
                EnableSettingsMenu();
            }
        }
        public void OnEnableMarkers()
        {
            if (current == "markers")
            {
                DisableMarkersMenu();
            }
            else
            {
                current = "markers";
                EnableMarkersMenu();
            }
        }

        public void EnableMainUI()
        {
            // Enable the main ui
            mainUiCanvasGroup.alpha = 1;
            mainUiCanvasGroup.interactable = true;
            mainUiCanvasGroup.blocksRaycasts = true;
            current = "main";
        }
        public void DisableMainUI()
        {
            // Reduce the visibility of the main UI, and disable all interraction
            mainUiCanvasGroup.alpha = 0.5f;
            mainUiCanvasGroup.interactable = false;
            mainUiCanvasGroup.blocksRaycasts = false;
        }

        public void EnableDebugInfo()
        {
            debugInfoCanvasGroup.alpha = 1;
            debugInfoCanvasGroup.interactable = true;
            debugInfoCanvasGroup.blocksRaycasts = true;
        }
        public void DisableDebugInfo()
        {
            debugInfoCanvasGroup.alpha = 0;
            debugInfoCanvasGroup.interactable = false;
            debugInfoCanvasGroup.blocksRaycasts = false;
        }

        public void EnableActionMenu()
        {
            Debug.Log("Opening action menu...");
            actionMenuCanvasGroup.alpha = 1;
            actionMenuCanvasGroup.interactable = true;
            actionMenuCanvasGroup.blocksRaycasts = true;
            current = "markers";
            DisableMainUI();
        }
        public void DisableActionMenu()
        {
            Debug.Log("Going back to the app...");
            // Disable the action menu
            actionMenuCanvasGroup.alpha = 0;
            actionMenuCanvasGroup.interactable = false;
            actionMenuCanvasGroup.blocksRaycasts = false;
            EnableMainUI();
        }

        public void EnableSettingsMenu()
        {
            Debug.Log("Opening settings menu...");
            // Fill all layouts with actual data
            // TODO
            string[] roscore = rosMasterUri.Split(':');
            serverIP.text = roscore[1].Substring(2);
            serverPort.text = roscore[2];
            // Enable interraction with confirmation gui and make it visible
            settingsMenuCanvasGroup.alpha = 1;
            settingsMenuCanvasGroup.interactable = true;
            settingsMenuCanvasGroup.blocksRaycasts = true;
            DisableMainUI();
            current = "settings";
        }
        public void DisableSettingsMenu()
        {
            Debug.Log("Going back to the app...");
            // Disable the exit menu
            settingsMenuCanvasGroup.alpha = 0;
            settingsMenuCanvasGroup.interactable = false;
            settingsMenuCanvasGroup.blocksRaycasts = false;
            EnableMainUI();
        }

        public void EnableMarkersMenu()
        {
            Debug.Log("Opening markers menu...");
            // Fill all layouts with actual data
            cilindrosPosX.text = cilindros.gameObject.transform.position.x.ToString();
            cilindrosPosY.text = cilindros.gameObject.transform.position.y.ToString();
            cilindrosPosZ.text = cilindros.gameObject.transform.position.z.ToString();
            cajasPosX.text = cajas.gameObject.transform.position.x.ToString();
            cajasPosY.text = cajas.gameObject.transform.position.y.ToString();
            cajasPosZ.text = cajas.gameObject.transform.position.z.ToString();
            esferasPosX.text = esferas.gameObject.transform.position.x.ToString();
            esferasPosY.text = esferas.gameObject.transform.position.y.ToString();
            esferasPosZ.text = esferas.gameObject.transform.position.z.ToString();
            // Enable interraction with confirmation gui and make it visible
            markersMenuCanvasGroup.alpha = 1;
            markersMenuCanvasGroup.interactable = true;
            markersMenuCanvasGroup.blocksRaycasts = true;
            DisableMainUI();
            current = "markers";
        }
        public void DisableMarkersMenu()
        {
            Debug.Log("Going back to the app...");
            // Disable the markers menu
            markersMenuCanvasGroup.alpha = 0;
            markersMenuCanvasGroup.interactable = false;
            markersMenuCanvasGroup.blocksRaycasts = false;
            EnableMainUI();
        }

        public void EnableExitMenu()
        {
            Debug.Log("Opening exit menu...");
            // Enable interraction with confirmation gui and make it visible
            exitMenuCanvasGroup.alpha = 1;
            exitMenuCanvasGroup.interactable = true;
            exitMenuCanvasGroup.blocksRaycasts = true;
            DisableMainUI();
            current = "exit";
        }
        public void DisableExitMenu()
        {
            Debug.Log("Going back to the app...");
            // Disable the exit menu
            exitMenuCanvasGroup.alpha = 0;
            exitMenuCanvasGroup.interactable = false;
            exitMenuCanvasGroup.blocksRaycasts = false;
            EnableMainUI();
        }

        public void CloseApplication()
        {
            Debug.Log("Closing Virtual Reality Object Management app...");
            Application.Quit();
        }

        private static void GetGeometryPoint(Vector3 position, geometry_msgs.Point geometryPoint)
        {
            geometryPoint.x = position.x;
            geometryPoint.y = position.y;
            geometryPoint.z = position.z;
        }

        private static void GetGeometryQuaternion(Quaternion quaternion, geometry_msgs.Quaternion geometryQuaternion)
        {
            geometryQuaternion.x = quaternion.x;
            geometryQuaternion.y = quaternion.y;
            geometryQuaternion.z = quaternion.z;
            geometryQuaternion.w = quaternion.w;
        }
    }
}