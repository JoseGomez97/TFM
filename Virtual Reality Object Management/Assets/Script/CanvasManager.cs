using TMPro;
using UnityEngine;
using Vuforia;
using RosSharp;
using RosSharp.RosBridgeClient;
using UnityEngine.UI;
using System;
using System.Globalization;

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

    public RosConnector rosConnector;

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

    public string current = "main"; // Can be "main", "settings", "markers" or "exit"

    // Start is called before the first frame update
    void Start()
    {
        EnableMainUI();
        DisableExitMenu();
        DisableSettingsMenu();
        DisableDebugInfo();
        DisableMarkersMenu();

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
            debugTextUI.text = "Cylinder position:\n" + "  x: "+cylinder.transform.position.x + "\n  y: " + cylinder.transform.position.y + "\n  z: " + cylinder.transform.position.z;
        }
        if (current == "settings")
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
        string[] ip =  serverIP.text.Split('.');

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

    public void OnApplySettings()
    {
        rosConnector.RosSocket.Close();
        rosConnector.RosBridgeServerUrl = "ws://"+serverIP.text+":"+serverPort.text;
        rosConnector.Awake();

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

    public void EnableSettingsMenu()
    {
        Debug.Log("Opening settings menu...");
        // Fill all layouts with actual data
        string[] roscore = rosConnector.RosBridgeServerUrl.Split(':');
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
}
