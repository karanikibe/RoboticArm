using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class AngleControlledRotation : MonoBehaviour
{
    public Transform controlledObject; // The object you want to rotate
    public float rotationSpeed = 100f;

    private float receivedAngle = 0f;
    private bool updateRotation = false;

    private TcpListener server;
    private Thread serverThread;

    void Start()
    {
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
                Debug.Log("Client connected.");
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (float.TryParse(line, out float angle))
                    {
                        receivedAngle = angle;
                        updateRotation = true;
                    }
                    else
                    {
                        Debug.LogWarning("Invalid input: " + line);
                    }
                }

                Debug.Log("Client disconnected.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Server error: " + ex.Message);
        }
    }

    void Update()
    {
        if (updateRotation && controlledObject != null)
        {
            // Logic to simulate key press effects using angle thresholds
            if (receivedAngle > 1f)
            {
                // Rotate left (Y+)
                controlledObject.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
            }
            else if (receivedAngle < -1f)
            {
                // Rotate right (Y-)
                controlledObject.Rotate(0f, -rotationSpeed * Time.deltaTime, 0f);
            }

            updateRotation = false; // Only trigger once per frame
        }
    }

    void OnApplicationQuit()
    {
        server?.Stop();
        serverThread?.Abort();
    }
}
