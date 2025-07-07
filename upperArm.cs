using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using UnityEngine;

public class upperJointRotation : MonoBehaviour
{
    public Transform joint; // Assign this in the Unity Inspector

    private TcpListener server;
    private float receivedAngle = 0f;
    private bool updateRotation = false;

    void Start()
    {
        // Start the server on a background thread
        Thread serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Parse("127.0.0.1"), 10001);
            server.Start();
            Debug.Log("Unity TCP server started on port 10001...");

            using TcpClient client = server.AcceptTcpClient();
            Debug.Log("Client connected!");

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
                    Debug.LogWarning("Invalid angle received: " + line);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("TCP Server Error: " + ex.Message);
        }
        finally
        {
            server?.Stop();
        }
    }

    void Update()
    {
        if (updateRotation && joint != null)
        {
            joint.localEulerAngles = new Vector3(receivedAngle, 0f, -90f); // Only change X, fix Y and Z
            updateRotation = false;
        }
    }


    void OnApplicationQuit()
    {
        server?.Stop();
    }
}
