using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TcpClientReceiver : MonoBehaviour
{
    public Transform joint1;  // Base joint (Y rotation)
    public Transform joint2;  // Elbow joint (X rotation)
    public Transform tip;     // End effector

    public float rotationSpeed = 3f;
    private float joint2Bend = 0f;

    private TcpClient client;
    private StreamReader reader;
    private Thread clientThread;
    private int receivedInt = 0;
    private bool updateRotation = false;

    // Update with your Ubuntu host IP and port where Python server listens
    private string serverIp = "172.16.121.76";
    private int serverPort = 10005;

    void Start()
    {
        clientThread = new Thread(ConnectToServer);
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIp, serverPort);
            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream);
            Debug.Log($"Connected to server at {serverIp}:{serverPort}");

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (int.TryParse(line.Trim(), out int input))
                {
                    receivedInt = Mathf.Clamp(input, -1, 1);
                    updateRotation = true;
                }
                else
                {
                    Debug.LogWarning("Invalid input from server: " + line);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Client error: " + ex.Message);
        }
    }

    void Update()
    {
        if (updateRotation)
        {
            if (joint1 != null)
            {
                float deltaY = rotationSpeed * Time.deltaTime;
                if (receivedInt == 1)
                    joint1.Rotate(0f, deltaY, 0f);
                else if (receivedInt == -1)
                    joint1.Rotate(0f, -deltaY, 0f);
            }

            if (joint2 != null)
            {
                float deltaX = rotationSpeed * Time.deltaTime;
                if (receivedInt == 1)
                    joint2Bend = Mathf.Clamp(joint2Bend + deltaX, -90f, 90f);
                else if (receivedInt == -1)
                    joint2Bend = Mathf.Clamp(joint2Bend - deltaX, -90f, 90f);

                joint2.localRotation = Quaternion.Euler(joint2Bend, 0f, -90f);
            }

            updateRotation = false;

            if (tip != null)
               // Debug.Log("Tip Position: " + tip.position);
                Debug.Log("Control: " + receivedInt + " | Tip Position: " + tip.position);
        }
    }

    void OnApplicationQuit()
    {
        client?.Close();
        clientThread?.Abort();
    }
}
