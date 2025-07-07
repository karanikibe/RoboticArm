using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class InverseArm : MonoBehaviour
{
    public Transform tip;
    public Transform target;
    public Transform baseJoint;
    public Transform elbowJoint;
    public float baseRotationSpeed = 45f;
    public float elbowRotationSpeed = 30f;
    public float stepDistance = 0.01f; // Distance the tip should move per signal step

    private Thread requestThread;
    private Thread signalThread;
    private object positionLock = new object();
    private object signalLock = new object();

    private float upperArmLength;
    private float lowerArmLength;

    private float theta1;
    private float theta2;

    private int controlSignal = 0;

    void Start()
    {
        upperArmLength = Vector3.Distance(baseJoint.position, elbowJoint.position);
        lowerArmLength = Vector3.Distance(elbowJoint.position, tip.position);

        requestThread = new Thread(PositionSync);
        requestThread.IsBackground = true;
        requestThread.Start();

        signalThread = new Thread(ReceiveSignal);
        signalThread.IsBackground = true;
        signalThread.Start();
    }

    void Update()
    {
        lock (signalLock)
        {
            if (controlSignal != 0)
            {
                Vector3 direction = (tip.position - target.position).normalized;
                target.position -= direction * stepDistance * controlSignal;
            }
        }

        CalculateInverseKinematics();
        ApplyJointRotations();
    }

    void CalculateInverseKinematics()
    {
        Vector3 localTarget = baseJoint.InverseTransformPoint(target.position);
        float x = localTarget.y;  // Treat Y as forward
        float z = localTarget.z;  // Treat Z as up

        float distance = Mathf.Sqrt(x * x + z * z);
        float a = upperArmLength;
        float b = lowerArmLength;
        float c = Mathf.Clamp(distance, Mathf.Abs(a - b) + 0.001f, a + b - 0.001f);

        if (distance != c)
        {
            float ratio = c / distance;
            x *= ratio;
            z *= ratio;
        }

        float cosTheta2 = Mathf.Clamp((a * a + b * b - c * c) / (2f * a * b), -1f, 1f);
        float angleElbow = Mathf.Acos(cosTheta2);

        float cosAngleA = Mathf.Clamp((a * a + c * c - b * b) / (2f * a * c), -1f, 1f);
        float angleShoulder = Mathf.Atan2(z, x) - Mathf.Acos(cosAngleA);

        theta1 = angleShoulder;
        theta2 = angleElbow;
    }

    void ApplyJointRotations()
    {
        if (baseJoint != null)
        {
            Vector3 toTarget = target.position - baseJoint.position;
            Vector3 direction = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
            if (direction.sqrMagnitude > 0.0001f)
            {
                float signedAngle = Vector3.SignedAngle(baseJoint.forward, direction, Vector3.up);
                float newY = Mathf.MoveTowardsAngle(baseJoint.localEulerAngles.y, baseJoint.localEulerAngles.y + signedAngle, baseRotationSpeed * Time.deltaTime);
                baseJoint.localEulerAngles = new Vector3(0f, newY, 0f);
            }
        }

        if (elbowJoint != null)
        {
            float shoulderAngleDeg = theta1 * Mathf.Rad2Deg;
            float elbowBendDeg = (180f - theta2 * Mathf.Rad2Deg);

            float currentX = elbowJoint.localEulerAngles.x;
            float newX = Mathf.MoveTowardsAngle(currentX, shoulderAngleDeg + elbowBendDeg, elbowRotationSpeed * Time.deltaTime);
            elbowJoint.localEulerAngles = new Vector3(newX, 0f, -90f);
        }
    }

    void ReceiveSignal()
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 10005);
            listener.Start();
            Debug.Log("Listening for control signals on port 10005...");

            using TcpClient client = listener.AcceptTcpClient();
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (int.TryParse(line.Trim(), out int signal))
                {
                    signal = Mathf.Clamp(signal, -1, 1);
                    lock (signalLock)
                    {
                        controlSignal = signal;
                    }
                    Debug.Log($"Received control signal: {signal}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Signal receive error: " + e.Message);
        }
    }

    void PositionSync()
    {
        try
        {
            TcpClient client = new TcpClient("127.0.0.1", 10003);
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            while (true)
            {
                lock (positionLock)
                {
                    string message = $"{tip.position.y},{tip.position.z}|{target.position.y},{target.position.z}";
                    writer.WriteLine(message);
                }
                Thread.Sleep(50); // ~20Hz
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Connection error: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (requestThread != null && requestThread.IsAlive)
            requestThread.Abort();
        if (signalThread != null && signalThread.IsAlive)
            signalThread.Abort();
    }
}
