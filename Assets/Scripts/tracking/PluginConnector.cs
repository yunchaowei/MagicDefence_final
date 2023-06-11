using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using TMPro;
using UnityEngine;
using Newtonsoft.Json;



public class PluginConnector : MonoBehaviour
{
    //Enable or disable tracking plugin.
    [Header("On / Off")]
    [SerializeField] private bool enableTracking;

    //options for configuration
    [Header("Tracking Options")]
    [SerializeField] private int numberOfPlayers;
    private int numberOfBaseStations = 4;
    private bool enableRotation = false;
    [SerializeField] private bool enableYAxis;
    private bool swapXZ = true;
    private bool invertX = true;
    private bool invertZ = true;
    private Vector3 unityWorldCenter = new Vector3(0,0,51.3f); //unity world center !!NOT tracking center!!

    //list of players (filled from editor)
    [Header("Players")]
    [SerializeField] private List<GameObject> players;

    

    //attributes for non-tracking input
    private int playerSelected = 1;
    private int trackingDisabledPlayerSpeed = 5;

    //attributes for tracking calibration
    [Header("Calibration")]
    private Vector3 calibrationCenter = new Vector3(0,0,51.3f);
    private float calibrationScale = 15.7f;
    private Mat2x2 calibrationTransform;
    private float sheerX;
    private float sheerY;
    private float yOffset;
    private Vector3 horizontal = Vector3.zero;
    private Vector3 vertical = Vector3.zero;
    private bool calibrated = false;

    //save calibration attributes
    [Header("Save File Path")]
    [Tooltip("Provided path must be absolute <C:/usr/...> . If no path provided, file will be saved at default location")]
    private string calibrationSaveFilePath = "D:/Unity_Tracking_Default_Config";
    private string calibrationSaveFileName = "trackingCalibration";
    private string fullCalibrationSaveFilePath;

    //attributes and options for interface
    private bool isInterfaceActive = false;
    [Header("Interface")]
    [SerializeField] private GameObject trackingInterface;
    [SerializeField] private TextMeshProUGUI scaleText;
    [SerializeField] private TextMeshProUGUI sheerXText;
    [SerializeField] private TextMeshProUGUI sheerYText;
    [SerializeField] private TextMeshProUGUI baseStationNumberText;
    [SerializeField] private TextMeshProUGUI playersNumberText;
    [SerializeField] private TextMeshProUGUI calibrationFileText;
    [SerializeField] private List<TextMeshProUGUI> playersPositionsText;
    [SerializeField] private List<TextMeshProUGUI> playersRotationsText;

    //internal attributes
    private int arraySize;
    private int playerDataSize; //Data size for player from OpenVr Array
    private float positionUpdateInterval = 0.01f;

    //dll function imports
    private const string dllName = "tracking";
    [DllImport(dllName)] private static extern void startTracking(int numberOfPlayers, int numberOfBaseStations);
    [DllImport(dllName)] private static extern void stopTracking();
    [DllImport(dllName)] private static extern void updatePositions(int arraySize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] float[] array, bool rotation);
    [DllImport(dllName)] private static extern int getNumberOfTrackers();
    [DllImport(dllName)] private static extern int getNumberOfBaseStations();
    [DllImport(dllName)] private static extern int getSize(bool rotation); //not used

    void Awake()
    {
        if(enableTracking){
            startTracking(numberOfPlayers, numberOfBaseStations);

            //set interface text for base station number
            int detectedBaseStations = getNumberOfBaseStations();
            if (detectedBaseStations == numberOfBaseStations)
            {
                baseStationNumberText.text = detectedBaseStations.ToString();
            }
            else
            {
                baseStationNumberText.text = "Discrepancy";
            }
            //set interface text for player number
            int detectedPlayers = getNumberOfTrackers();
            if (detectedPlayers == numberOfPlayers)
            {
                playersNumberText.text = detectedPlayers.ToString();
            }
            else
            {
                playersNumberText.text = "Discrepancy";
            }
            for (int i = 0; i < numberOfPlayers; i++)
            {
                if (i<numberOfPlayers)
                {
                    playersPositionsText[i].text = Vector3ToString(new Vector3(0, 0, 0));
                }
            }
        }
    }

    private void OnDisable()
    {
        if(enableTracking){
            stopTracking();
        }
    }

    void Start()
    {
        //set visibility for trackingInterface to false
        trackingInterface.SetActive(isInterfaceActive);

        //set visibility for number of playres
        for (int i = 0; i < players.Count; i++)
        {
            if ( i >= numberOfPlayers)
            {
                players[i].SetActive(false);
            }
        }

        //set the size of the data coming from OpenVR for each player 
        playerDataSize = enableRotation ? 12 : 3; 

        //start getNewPositions loop when tracking enabled
        if(enableTracking){
            //assign calibration save File
            if (calibrationSaveFilePath == "")
            {
                calibrationSaveFilePath = Application.persistentDataPath;
            }
            fullCalibrationSaveFilePath = calibrationSaveFilePath + "/" + calibrationSaveFileName + ".json";

            //load calibration if saved
            LoadCalibration();
            arraySize = numberOfPlayers * playerDataSize;
            StartCoroutine("ExperimentalPositions");
            //startCoroutine("ExperimentalPositions");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        ListenToControls();

        //if tracking is not enabled move players with keyboard
        if (!enableTracking)
        {

            UpdateInterfaceText();

            DisabledTrackingPlayerSelector();

            DisabledTrackingPlayerMovement(); 
        }
    }

    IEnumerator ExperimentalPositions()
    {
        for(; ; )
        {
            float[] openVrOutputArr = new float[arraySize];
            updatePositions(arraySize * sizeof(float), openVrOutputArr, enableRotation);
            
            
            for(int i = 0; i< numberOfPlayers; i++)
            {
                int playerIndex = i * playerDataSize;
                //get position from openvr array
                Vector3 rawPos = enableRotation ? new Vector3(openVrOutputArr[9+playerIndex], openVrOutputArr[10 + playerIndex], openVrOutputArr[11 + playerIndex]) :
                                                                                new Vector3(openVrOutputArr[0 + playerIndex], openVrOutputArr[1 + playerIndex], openVrOutputArr[2 + playerIndex]);
                
                
                //apply orientation options to position without changing calibration.
                ApplyOrientationOptionsToPos(ref rawPos);
                if (calibrated)
                {
                    CalculateCalibrationTransform();

                    //calibrate x, z axis.
                    Vector2 calibratedAxis = new Vector2(rawPos.x, rawPos.z);
                    calibratedAxis = calibrationTransform.MultiplyVector(calibratedAxis);
                    calibratedAxis *= calibrationScale;

                    Vector3 calibratedPos = Vector3.zero;
                        
                    calibratedPos.x = calibratedAxis.x;
                    calibratedPos.y = (rawPos.y + yOffset) * calibrationScale;
                    calibratedPos.z = calibratedAxis.y;

                    calibratedPos += unityWorldCenter;

                    players[i].GetComponent<PlayerMovement>().setPosition(calibratedPos);
                    playersPositionsText[i].text = Vector3ToString(calibratedPos);

                    if(enableRotation)
                    {
                        //TODO calculate rotatiion
                        Vector3 column1 = new Vector3(openVrOutputArr[0 + playerIndex], openVrOutputArr[1 + playerIndex], openVrOutputArr[2 + playerIndex]);
                        Vector3 column2 = new Vector3(openVrOutputArr[3 + playerIndex], openVrOutputArr[4 + playerIndex], openVrOutputArr[5 + playerIndex]);
                        Vector3 column3 = new Vector3(openVrOutputArr[6 + playerIndex], openVrOutputArr[7 + playerIndex], openVrOutputArr[8 + playerIndex]);

                        Quaternion playerRotation = CalculateRotation(column1, column2, column3);
                        playersRotationsText[i].text = QuaternionToString(playerRotation);
                        //players[i].GetComponent<PlayerMovement>().setRotation(playerRotation);
                    }
                }
                else
                {
                    players[i].GetComponent<PlayerMovement>().setPosition(rawPos);
                    playersPositionsText[i].text = Vector3ToString(rawPos);
                }    
            }
            yield return new WaitForSeconds(positionUpdateInterval);
        }
    }

    private Quaternion CalculateRotation(Vector3 c1, Vector3 c2, Vector3 c3)
    {
        //Code from Mike Day 

        //row 1 c1.x, c2.x, c3.x -> m00 m01 m02
        //row 2 c1.y, c2.y, c3.y -> m10 m11 m12
        //row 3 c1.z, c2.z, c3.z -> m20 m21 m22
        float trace;
        Quaternion q; 

        if (c3.z < 0)
        {
            if (c1.x > c2.y)
            {
                trace = 1 + c1.x - c2.y - c3.z;
                q = new Quaternion(trace, c2.x + c1.y, c1.z + c3.x, c3.y - c2.z);
            }
            else
            {
                trace = 1 - c1.x + c2.y - c3.z;
                q = new Quaternion(c2.x + c1.y, trace, c3.y+c2.z, c1.z - c3.x);
            }
        }
        else
        {
            if(c1.x < -c2.y)
            {
                trace = 1 - c1.x - c2.y + c3.z;
                q = new Quaternion(c1.z + c3.x, c3.y + c2.z, trace, c2.x - c1.y);
            }
            else
            {
                trace = 1 - c1.x + c2.y + c3.z;
                q = new Quaternion(c3.y - c2.z, c1.z - c3.x, c2.x - c1.y, trace);
            }
        }
        q.w *= 0.5f / Mathf.Sqrt(trace);
        q.x *= 0.5f / Mathf.Sqrt(trace);
        q.y *= 0.5f / Mathf.Sqrt(trace);
        q.z *= 0.5f / Mathf.Sqrt(trace);

        Quaternion testQ = new Quaternion(q.z, q.y, q.z, q.w);
        return testQ;
    }
    private void ApplyOrientationOptionsToPos(ref Vector3 pos)
    {
        pos.x = invertX ? -pos.x : pos.x;
        pos.y = enableYAxis ? pos.y : 0;
        pos.z = invertZ ? -pos.z : pos.z;

        if (swapXZ)
        {
            float temp = pos.x;
            pos.x = pos.z;
            pos.z = temp;
        }
    }

    private void CalculateCalibrationTransform()
    {
        Vector2 column1 = new Vector2(1, sheerY);
        Vector2 column2 = new Vector2(sheerX, 1);
        calibrationTransform = new Mat2x2(column1, column2);
    }

    private void Calibrate()
    {
        vertical = GetSouthNorth();
        horizontal = GetWestEast();
        sheerX = -vertical.x;
        sheerY = -horizontal.z;
        SetYOffset();
        calibrated = true;
    }

    private Vector3 GetSouthNorth()
    {
        Vector3 posNorth = new Vector3(0f, 0f, 0f);
        Vector3 posSouth = new Vector3(0f, 0f, 0f);
        float zMax = -10f;
        float zMin = 10f;

        //find south and north trackers
        for (int i = 0; i< 4; i++)
        {
            if (players[i].transform.position.z < zMin)
            {
                zMin = players[i].transform.position.z;
                posSouth = players[i].transform.position;
            }
            if (players[i].transform.position.z > zMax)
            {
                zMax = players[i].transform.position.z;
                posNorth = players[i].transform.position;
            }
        }

        return (posNorth - posSouth).normalized;
    }

    private Vector3 GetWestEast()
    {
        Vector3 posEast = new Vector3(0f, 0f, 0f);
        Vector3 posWest = new Vector3(0f, 0f, 0f);
        float xMax = -10f;
        float xMin = 10f;

        //find west and east trackers
        for (int i = 0; i < 4; i++)
        {
            if (players[i].transform.position.x < xMin)
            {
                xMin = players[i].transform.position.x;
                posWest = players[i].transform.position;
            }
            if (players[i].transform.position.x > xMax)
            {
                xMax = players[i].transform.position.x;
                posEast = players[i].transform.position;
            }
        }
        return (posEast - posWest).normalized;
    }

    private void SetYOffset()
    {
        float min = 1000;
        for (int i = 0; i < 4; i++)
        {
            if (players[i].transform.position.y < min)
            {
                min = players[i].transform.position.y;
            }
        }
        yOffset = Math.Abs(min);
    }

    private void ListenToControls()
    {
        //shows and hides tracking interface
        if (Input.GetKeyDown(KeyCode.I))
        {
            isInterfaceActive = !isInterfaceActive;
            trackingInterface.SetActive(isInterfaceActive);
        }

        if (Input.GetKeyDown(KeyCode.C) && enableTracking)
        {
            Calibrate();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) && enableTracking)
        {
            sheerY += 0.01f;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && enableTracking)
        {
            sheerY -= 0.01f;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) && enableTracking)
        {
            sheerX += 0.01f;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) && enableTracking)
        {
            sheerX -= 0.01f;
        }

        if (Input.GetKeyDown(KeyCode.W) && enableTracking)
        {
            calibrationScale += 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.S) && enableTracking)
        {
            calibrationScale -= 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    //select the player that will move when trackingDisabled (default player 1)
    private void DisabledTrackingPlayerSelector()
    {
        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                playerSelected = 1;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                playerSelected = 2;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                playerSelected = 3;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                playerSelected = 4;
            }
        }
    }

    //read inputs form keyboard and move player selected when tracking is diabled
    private void DisabledTrackingPlayerMovement()
    {
        if (Input.GetKey(KeyCode.W))
        {
            players[playerSelected - 1].transform.Translate(Vector3.forward * Time.deltaTime * trackingDisabledPlayerSpeed);
        }
        if (Input.GetKey(KeyCode.A))
        {
            players[playerSelected - 1].transform.Translate(Vector3.left * Time.deltaTime * trackingDisabledPlayerSpeed);
        }
        if (Input.GetKey(KeyCode.S))
        {
            players[playerSelected - 1].transform.Translate(Vector3.back * Time.deltaTime * trackingDisabledPlayerSpeed);
        }
        if (Input.GetKey(KeyCode.D))
        {
            players[playerSelected - 1].transform.Translate(Vector3.right * Time.deltaTime * trackingDisabledPlayerSpeed);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            players[playerSelected - 1].transform.Translate(Vector3.up * Time.deltaTime * trackingDisabledPlayerSpeed);
        }
        if (Input.GetKey(KeyCode.E))
        {
            players[playerSelected - 1].transform.Translate(Vector3.down * Time.deltaTime * trackingDisabledPlayerSpeed);
        }
    }

    private void UpdateInterfaceText()
    {
        scaleText.text = calibrationScale.ToString();
        if(sheerX != 0)
        {
            sheerXText.text = sheerX.ToString().Substring(0, 5);
            sheerYText.text = sheerY.ToString().Substring(0, 5);
        }
    }

    //returns a string from a given vector3 (used for display in the interface)
    private string Vector3ToString(Vector3 vector)
    {
        //show only 2 decimal points
        float x = Mathf.Round(vector.x * 100f) / 100f;
        float y = Mathf.Round(vector.y * 100f) / 100f;
        float z = Mathf.Round(vector.z * 100f) / 100f;
        return $"({x} , {y} , {z})";
    }

    private string QuaternionToString(Quaternion q)
    {
        float x = Mathf.Round(q.x * 100f) / 100f;
        float y = Mathf.Round(q.y * 100f) / 100f;
        float z = Mathf.Round(q.z * 100f) / 100f;
        float w = Mathf.Round(q.w * 100f) / 100f;
        return $"({x} , {y} , {z}, {w})";
    }

    private void LoadCalibration()
    {
        Debug.Log("Fetching file at: " + fullCalibrationSaveFilePath);

        try
        {
            string jsonString = File.ReadAllText(fullCalibrationSaveFilePath);
            Calibration loadedCalibration = JsonConvert.DeserializeObject<Calibration>(jsonString);
            calibrationCenter = loadedCalibration.GetCalibrationGenter();
            horizontal = loadedCalibration.GetHorizontal();
            vertical = loadedCalibration.GetVertical();
            calibrationScale = loadedCalibration.calibrationScale;
            sheerX = loadedCalibration.sheerX;
            sheerY= loadedCalibration.sheerY;
            yOffset= loadedCalibration.yOffset;

            calibrationFileText.text = "Loaded Calibration!";
            calibrated = true;
        }
        catch (Exception)
        {
            Debug.Log("Calibration not found. If you want to Start with a preloaded calibration, please generate a file with 'Save Current Calibration' button");  
        }
    }

    public void OnSaveCalibration()
    {
        //callback for interface "Save current calibration" button
        //if (calibrated)
        if(true)
        {
            Debug.Log("Calibration File sotred at: " + fullCalibrationSaveFilePath);

            Calibration calibrationToJson = new Calibration
            {
                calibrationScale = calibrationScale,
                sheerX= sheerX,
                sheerY= sheerY,
                yOffset=yOffset,
            };


            calibrationToJson.SetCalibrationCenter(calibrationCenter);
            calibrationToJson.SetVertical(vertical);
            calibrationToJson.SetHorizontal(horizontal);


            string jsonString = JsonConvert.SerializeObject(calibrationToJson);
            
            File.WriteAllText(fullCalibrationSaveFilePath, jsonString);
        }
    }

    public void OnRemoveCalibration()
    {
        if (File.Exists(fullCalibrationSaveFilePath))
        {
            File.Delete(fullCalibrationSaveFilePath);
        }
        calibrated = false;
    }

    struct Mat2x2
    {
        Vector2 column1, column2;
        public Mat2x2(Vector2 column1, Vector2 column2)
        {
            this.column1 = column1;
            this.column2 = column2;
        }

        public void SetColumns(Vector2 column1, Vector2 column2)
        {
            this.column1 = column1;
            this.column2 = column2;
        }

        public Vector2 MultiplyVector(Vector2 vec)
        {
            float x = (this.column1.x * vec.x) + (this.column2.x * vec.y);
            float y = (this.column1.y * vec.x) + (this.column2.y * vec.y);
            return new Vector2(x, y);
        }

        public void Show()
        {
            Debug.Log(column1.x.ToString() + " " + column2.x.ToString());
            Debug.Log(column1.y.ToString() + " " + column2.y.ToString());
        }
    }
}

public class Calibration
{
    public float[] calibrationCenter = new float[3];
    public float calibrationScale;
    public float sheerX;
    public float sheerY;
    public float yOffset;
    public float[] horizontal = new float[3];
    public float[] vertical = new float[3];

    public void SetCalibrationCenter(Vector3 vec)
    {
        calibrationCenter[0] = vec.x;
        calibrationCenter[1] = vec.y;
        calibrationCenter[2] = vec.z;
    }
    public void SetHorizontal(Vector3 vec)
    {
        horizontal[0] = vec.x;
        horizontal[1] = vec.y;
        horizontal[2] = vec.z;
    }

    public void SetVertical(Vector3 vec)
    {
        vertical[0] = vec.x;
        vertical[1] = vec.y;
        vertical[2] = vec.z;
    }

    public Vector3 GetCalibrationGenter()
    {
        return new Vector3(calibrationCenter[0], calibrationCenter[1], calibrationCenter[2]);
    }
    public Vector3 GetHorizontal()
    {
        return new Vector3(horizontal[0], horizontal[1], horizontal[2]);
    }
    public Vector3 GetVertical()
    {
        return new Vector3(vertical[0], vertical[1], vertical[2]);
    }
}