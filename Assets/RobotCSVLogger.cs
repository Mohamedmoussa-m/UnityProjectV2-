using UnityEngine;
using System.IO;
using System.Text;

public class RobotCSVLogger : MonoBehaviour
{
    [Header("Sensors Script")]
    public RobotSensors sensors;

    [Header("Logging Settings")]
    public string fileName = "robot_log.csv";
    public float logInterval = 0.02f; // 50 Hz

    float timer = 0f;
    string fullPath;

    void Start()
    {
        if (sensors == null)
        {
            Debug.LogError("RobotCSVLogger: sensors reference missing!");
            enabled = false;
            return;
        }

        string dir = Application.dataPath + "/Logs/";
        Directory.CreateDirectory(dir);
        fullPath = Path.Combine(dir, fileName);

        // Write header
        using (StreamWriter writer = new StreamWriter(fullPath, false, Encoding.UTF8))
        {
            string header = "time";

            int n = sensors.joints.Length;
            for (int i = 0; i < n; i++)
                header += $",q{i}";

            header += ",x,y,z,roll,pitch,yaw";
            writer.WriteLine(header);
        }

        Debug.Log("CSV logging to: " + fullPath);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < logInterval) return;
        timer = 0f;

        WriteOneLine();
    }

    void WriteOneLine()
    {
        float t = Time.time;

        float[] q = sensors.GetJointAnglesDegrees();
        Vector3 pos = sensors.GetEEPosition();
        Vector3 rot = sensors.GetEERotation();

        using (StreamWriter writer = new StreamWriter(fullPath, true, Encoding.UTF8))
        {
            string line = t.ToString("F4");

            for (int i = 0; i < q.Length; i++)
                line += "," + q[i].ToString("F4");

            line += $",{pos.x:F4},{pos.y:F4},{pos.z:F4}";
            line += $",{rot.x:F4},{rot.y:F4},{rot.z:F4}";

            writer.WriteLine(line);
        }
    }
}
