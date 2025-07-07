/*using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class InverseServerCode : MonoBehaviour
{
    public JointManager baseJoint;
    public JointManager tip;
    public GameObject Target;
    public float speed = 5f;
    public float threshold = 0.01f;
    public int steps = 10;

    private Thread tcpListenerThread;
    private TcpListener tcpListener;
    private string receivedMessage = "";

    void Start()
    {
        tcpListenerThread = new Thread(ListenForIncomingRequests);
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    void ListenForIncomingRequests()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 10002);
            tcpListener.Start();
            Debug.Log("Server started");

            while (true)
            {
                using (TcpClient client = tcpListener.AcceptTcpClient())
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] buffer = new byte[1024];
                        int length = stream.Read(buffer, 0, buffer.Length);
                        receivedMessage = Encoding.ASCII.GetString(buffer, 0, length);
                        Debug.Log("Received: " + receivedMessage);
                    }
                }
            }
        }
        catch (SocketException e)
        {
            Debug.Log("SocketException: " + e);
        }
    }

    void Update()
    {
        // Handle inverse kinematics
        for (int i = 0; i < steps; i++)
        {
            if (Vector2.Distance(To2D(tip.transform.position), To2D(Target.transform.position)) > threshold)
            {
                JointManager currentJoint = baseJoint;
                while (currentJoint != null)
                {
                    float slope = CalculateSlope(currentJoint);
                    currentJoint.RotateJoint(-slope * speed);
                    currentJoint = currentJoint.GetJointChild();
                }
            }
        }

        // Apply new target if message received
        if (!string.IsNullOrEmpty(receivedMessage))
        {
            string[] parts = receivedMessage.Split(',');
            if (parts.Length == 2 && float.TryParse(parts[0], out float y) && float.TryParse(parts[1], out float z))
            {
                Vector3 currentPos = Target.transform.position;
                Target.transform.position = new Vector3(currentPos.x, y, z);
            }
            receivedMessage = ""; // Reset
        }
    }

    float CalculateSlope(JointManager joint)
    {
        float deltaTheta = 0.01f;
        float originalDistance = Vector2.Distance(To2D(tip.transform.position), To2D(Target.transform.position));
        joint.RotateJoint(deltaTheta);
        float newDistance = Vector2.Distance(To2D(tip.transform.position), To2D(Target.transform.position));
        joint.RotateJoint(-deltaTheta);
        return (newDistance - originalDistance) / deltaTheta;
    }

    Vector2 To2D(Vector3 v)
    {
        return new Vector2(v.y, v.z); // Treat Y-Z plane as 2D workspace
    }

    void OnApplicationQuit()
    {
        tcpListener?.Stop();
        tcpListenerThread?.Abort();
    }
}*/
