using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using UnityEngine;

public class InverKinematicServer : MonoBehaviour
{
    public Transform joint1;      // First joint (shoulder)
    public Transform joint2;      // Second joint (elbow)
    public Transform tip;         // End effector

    public float stepAngle = 5f;  // Angle to rotate per step
    private volatile int signal = 0;       // Signal received from Python
    private TcpListener server;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread listenerThread;

    private string tipAngleToSend = "0.0"; // Value sent back to Python

    private void Start()
    {
        listenerThread = new Thread(StartServer);
        listenerThread.Start();
    }

    private void Update()
    {
        // Apply movement based on signal
        if (signal == 1)
        {
            joint1.localEulerAngles += new Vector3(0, 0, stepAngle);
        }
        else if (signal == -1)
        {
            joint1.localEulerAngles -= new Vector3(0, 0, stepAngle);
        }

        // Reset signal so it moves only one step per signal
        signal = 0;

        // Update tip angle to be sent back to Python
        float tipAngle = joint1.localEulerAngles.z;
        if (tipAngle > 180f) tipAngle -= 360f;

        tipAngleToSend = tipAngle.ToString("F2");
    }

    private void StartServer()
    {
        server = new TcpListener(IPAddress.Parse("127.0.0.1"), 10002);
        server.Start();
        Debug.Log("Server started...");

        using (TcpClient client = server.AcceptTcpClient())
        using (NetworkStream stream = client.GetStream())
        using (reader = new StreamReader(stream))
        using (writer = new StreamWriter(stream))
        {
            writer.AutoFlush = true;

            while (true)
            {
                string received = reader.ReadLine();
                if (int.TryParse(received, out int value))
                {
                    signal = value;
                }

                // Send tip angle safely (written in Update)
                writer.WriteLine(tipAngleToSend);
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (listenerThread != null && listenerThread.IsAlive)
            listenerThread.Abort();

        if (server != null)
            server.Stop();
    }
}
