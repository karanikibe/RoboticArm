using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PersistentClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread commsThread;
    private bool running = false;

    void Start()
    {
        client = new TcpClient("127.0.0.1", 10003); // Python server port
        stream = client.GetStream();
        running = true;

        commsThread = new Thread(Communicate);
        commsThread.Start();
    }

    void Communicate()
    {
        byte[] buffer = new byte[1024];

        while (running)
        {
            // Example: Send position (you can replace this with actual data)
            string message = "0.5,-0.3";  // tipY,targetY
            byte[] data = Encoding.ASCII.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);

            // Wait for response
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
            Debug.Log("Received from Python: " + response);

            Thread.Sleep(100); // Limit frequency
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        stream.Close();
        client.Close();
        commsThread.Abort();
    }
}
