using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class DualJointController : MonoBehaviour
{
    public Transform joint1;         // Base joint
    public Transform joint2;         // Elbow joint
    public Transform leftGripper;    // Left claw
    public Transform rightGripper;   // Right claw

    public float rotationSpeed = 100f;
    public float maxOpenAngle = 20f;

    public Quaternion leftOpenRotation;
    public Quaternion leftCloseRotation;
    public Quaternion rightOpenRotation;
    public Quaternion rightCloseRotation;

    public float[] receivedAngles = new float[2];
    public bool updateJoints = false;
    public bool gripperOpened = false;

    private TcpListener server;
    private Thread serverThread;

    void Start()
    {
        // Setup gripper open/close rotations
        leftOpenRotation = Quaternion.Euler(0, 0, maxOpenAngle);
        leftCloseRotation = Quaternion.Euler(0, 0, 0);

        rightOpenRotation = Quaternion.Euler(0, 0, -maxOpenAngle);
        rightCloseRotation = Quaternion.Euler(0, 0, 0);

        // Start TCP server
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

                string line;
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
                joint1.localRotation = Quaternion.Euler(0f, receivedAngles[0], 0f);
                joint2.localRotation = Quaternion.Euler(receivedAngles[1], 0f, -90f);

                CheckGripper(receivedAngles[1]); // Pass tip angle for logic
            }

            updateJoints = false;
        }

        // Smoothly animate gripper based on current state
        if (leftGripper != null && rightGripper != null)
        {
            if (gripperOpened)
            {
                leftGripper.localRotation = Quaternion.RotateTowards(leftGripper.localRotation, leftOpenRotation, rotationSpeed * Time.deltaTime);
                rightGripper.localRotation = Quaternion.RotateTowards(rightGripper.localRotation, rightOpenRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                leftGripper.localRotation = Quaternion.RotateTowards(leftGripper.localRotation, leftCloseRotation, rotationSpeed * Time.deltaTime);
                rightGripper.localRotation = Quaternion.RotateTowards(rightGripper.localRotation, rightCloseRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void CheckGripper(float tipAngle)
    {
        if (tipAngle >= 170f && !gripperOpened)
        {
            gripperOpened = true;
            Debug.Log("Opening Gripper");
        }
        else if (tipAngle <= 10f && gripperOpened)
        {
            gripperOpened = false;
            Debug.Log("Closing Gripper");
        }
    }

    void OnApplicationQuit()
    {
        server?.Stop();
        serverThread?.Abort();
    }
}
