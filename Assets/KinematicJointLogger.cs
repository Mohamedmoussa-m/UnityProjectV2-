using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

/// <summary>
/// Logs the actual calibrated joint angles (q_actual) for all joints 
/// over time. Reads data from KinematicLinkAngleSensor components.
/// </summary>
public class KinematicJointLogger : MonoBehaviour
{
    private KinematicLinkAngleSensor[] sensors;

    [Header("Logging Settings")]
    public string fileName = "joint_angle_data.csv";
    public float logInterval = 0.02f; // 50 Hz

    private float timer = 0f;
    private string fullPath;
    private bool isReadyToLog = false;

    void Start()
    {
        // Auto-discover all Kinematic Link Angle Sensors in children
        sensors = GetComponentsInChildren<KinematicLinkAngleSensor>();

        if (sensors.Length == 0)
        {
            Debug.LogError("No KinematicLinkAngleSensor scripts found. Joint Angle logging disabled.");
            enabled = false;
            return;
        }

        SetupCSVFile();
        isReadyToLog = true;
        Debug.Log($"CSV Joint Angle logging initialized for {sensors.Length} joints to: {fullPath}");
    }

    private void SetupCSVFile()
    {
        string dir = Application.dataPath + "/Logs/";
        Directory.CreateDirectory(dir);
        fullPath = Path.Combine(dir, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8))
            {
                string header = "time";

                for (int i = 0; i < sensors.Length; i++)
                {
                    // Log only the actual measured angle (q_actual)
                    header += $",{sensors[i].gameObject.name}_q_actual";
                }

                writer.WriteLine(header);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to create Joint Angle CSV file (Is it open?): " + e.Message);
            enabled = false;
            isReadyToLog = false;
        }
    }

    void LateUpdate()
    {
        if (!isReadyToLog) return;

        timer += Time.deltaTime;
        if (timer < logInterval) return;
        timer = 0f;

        WriteOneLine();
    }

    void WriteOneLine()
    {
        float t = Time.time;
        StringBuilder line = new StringBuilder();
        line.Append(t.ToString("F4"));

        // 1. Collect Joint Angle Data
        for (int i = 0; i < sensors.Length; i++)
        {
            line.Append($",{sensors[i].CurrentAngle:F4}");
        }

        // 2. Write to file
        try
        {
            using (StreamWriter writer = new StreamWriter(fullPath, true, System.Text.Encoding.UTF8))
            {
                writer.WriteLine(line.ToString());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to write to Joint Angle CSV file: " + e.Message);
            enabled = false;
            isReadyToLog = false;
        }
    }
}