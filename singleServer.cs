using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class DualJointControll : MonoBehaviour
{
    public Transform joint1; // Assign in Inspector (e.g., base joint)
    public Transform joint2; // Assign in Inspector (e.g., elbow joint)
    //public bool gripper = false; //Assign the gripper joint

    private float[] receivedAngles = new float[2];
    private bool updateJoints = false;

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
                    string[] parts = line.Split(',');

                    if (parts.Length == 2 &&
                        float.TryParse(parts[0], out float angle1) &&
                        float.TryParse(parts[1], out float angle2))
                    {
                        receivedAngles[0] = angle1;
                        receivedAngles[1] = angle2;
                        updateJoints = true;
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
        if (updateJoints)
        {
            if (joint1 != null && joint2 != null)
            {
                joint1.localRotation = Quaternion.Euler(0f, receivedAngles[0], 0f); // Pitch on X
                joint2.localRotation = Quaternion.Euler(receivedAngles[1], 0f, -90f);
            }

            updateJoints = false;
        }
    }

    void OnApplicationQuit()
    {
        server?.Stop();
        serverThread?.Abort();
    }

}
