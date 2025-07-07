using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class LastTcpServer : MonoBehaviour
{
    public Transform joint1;  // Assign this in the Unity Inspector
    public Transform joint2;  // Assign this in the Unity Inspector
    public float smoothSpeed = 100f;

    private float[] targetAngles = new float[2];  // [0] = joint1, [1] = joint2
    private bool updateReceived = false;

    private TcpListener server;
    private Thread serverThread;

    void Start()
    {
        // Start TCP listener in background thread
        serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, 10001);
            server.Start();
            Debug.Log("TCP Server started on port 10001");

            while (true)
            {
                using TcpClient client = server.AcceptTcpClient();
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream);
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 &&
                        float.TryParse(parts[0], out float angle1) &&
                        float.TryParse(parts[1], out float angle2))
                    {
                        targetAngles[0] = angle1;
                        targetAngles[1] = angle2;
                        updateReceived = true;
                    }
                    else
                    {
                        Debug.LogWarning("Invalid data: " + line);
                    }
                }
            }
        }
        }
        catch (Exception e)
        {
            Debug.LogError("Server error: " + e.Message);
        }
    }

    void Update()
{
    if (updateReceived)
    {
        // Smooth rotation for joint1 (e.g., rotates around Y-axis)
        if (targetAngles[0]>1f)
        {
            Quaternion targetRot1 = Quaternion.Euler(0f, targetAngles[0], 0f);
            joint1.localRotation = Quaternion.RotateTowards(joint1.localRotation, targetRot1, smoothSpeed * Time.deltaTime);
        }

        // Smooth rotation for joint2 (e.g., rotates around X-axis)
        if (joint2 != null)
        {
            Quaternion targetRot2 = Quaternion.Euler(targetAngles[1], 0f ,-90f);
            joint2.localRotation = Quaternion.RotateTowards(joint2.localRotation, targetRot2, smoothSpeed * Time.deltaTime);
        }
    }
}

void OnApplicationQuit()
{
    server?.Stop();
    serverThread?.Abort();
}
}
