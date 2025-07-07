
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ArmIKController : MonoBehaviour
{
    public Transform joint1; // rotates on X-axis (base)
    public Transform joint2; // rotates on X-axis (elbow)
    public float segment1Length = 0.388f;
    public float segment2Length = 1.724f;

    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;

    private float targetY = 0f;
    private float targetZ = 0f;

    void Start()
    {
        Thread serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void Update()
    {
        ApplyInverseKinematics(targetY, targetZ);
    }

    void StartServer()
    {
        try
        {
            server = new TcpListener(System.Net.IPAddress.Any, 10001);
            server.Start();
            Debug.Log("Server started...");

            client = server.AcceptTcpClient();
            stream = client.GetStream();

            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    string[] parts = data.Split(':');
                    if (parts.Length == 2)
                    {
                        float.TryParse(parts[0], out targetY);
                        float.TryParse(parts[1], out targetZ);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("TCP Error: " + e.Message);
        }
    }

    void ApplyInverseKinematics(float y, float z)
    {
        float l1 = segment1Length;
        float l2 = segment2Length;
        float D = Mathf.Sqrt(y * y + z * z);

        if (D > l1 + l2 || D < Mathf.Abs(l1 - l2)) return; // unreachable

        float a2 = Mathf.Acos((D * D - l1 * l1 - l2 * l2) / (2 * l1 * l2));
        float k1 = l1 + l2 * Mathf.Cos(a2);
        float k2 = l2 * Mathf.Sin(a2);
        float a1 = Mathf.Atan2(z, y) - Mathf.Atan2(k2, k1);

        if (joint1 != null)
            joint1.localRotation = Quaternion.Euler(0f, Mathf.Rad2Deg * a1, 0f);

        if (joint2 != null)
            joint2.localRotation = Quaternion.Euler(Mathf.Rad2Deg * a2, 0f, 90f);
    }

    void OnApplicationQuit()
    {
        if (stream != null) stream.Close();
        if (client != null) client.Close();
        if (server != null) server.Stop();
    }
}
