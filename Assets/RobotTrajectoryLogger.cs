using UnityEngine;
using System.IO;
using System.Text;

public class RobotTrajectoryLogger : MonoBehaviour
{
    [Header("Configuration")]
    public Transform endEffectorTransform; // Drag your Gripper/Tip Transform here

    private Transform rootTransform;
    private KinematicLinkAngleSensor[] jointSensors;

    [Header("File Settings")]
    public string fileName = "robot_trajectory_log.csv";
    public float logInterval = 0.02f;

    private float timer = 0f;
    private string fullPath;
    private bool isReadyToLog = false;

    void Start()
    {
        rootTransform = this.transform;
        jointSensors = GetComponentsInChildren<KinematicLinkAngleSensor>();

        if (jointSensors.Length == 0 || endEffectorTransform == null)
        {
            Debug.LogError("Missing Sensors or End Effector assignment!");
            enabled = false;
            return;
        }

        string dir = Application.dataPath + "/Logs/";
        Directory.CreateDirectory(dir);
        fullPath = Path.Combine(dir, fileName);

        // Write Header
        using (StreamWriter writer = new StreamWriter(fullPath, false, Encoding.UTF8))
        {
            string header = "time";
            for (int i = 0; i < jointSensors.Length; i++)
                header += $",q{i + 1}_{jointSensors[i].name}";

            header += ",RootX,RootY,RootZ,EE_x,EE_y,EE_z,EE_roll,EE_pitch,EE_yaw";
            writer.WriteLine(header);
        }

        isReadyToLog = true;
        Debug.Log("Logging to: " + fullPath);
    }

    // LateUpdate ensures we record AFTER your movement scripts run
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

        // 1. Get Angles
        foreach (var sensor in jointSensors)
        {
            line.Append($",{sensor.CurrentAngle:F4}");
        }

        // 2. Get Positions
        Vector3 rootPos = rootTransform.position;
        Vector3 eePos = endEffectorTransform.position;
        Vector3 eeRot = endEffectorTransform.rotation.eulerAngles;

        line.Append($",{rootPos.x:F4},{rootPos.y:F4},{rootPos.z:F4}"); // Diagnostics
        line.Append($",{eePos.x:F4},{eePos.y:F4},{eePos.z:F4}");       // Actual Trajectory
        line.Append($",{eeRot.x:F4},{eeRot.y:F4},{eeRot.z:F4}");       // Orientation

        using (StreamWriter writer = new StreamWriter(fullPath, true, Encoding.UTF8))
        {
            writer.WriteLine(line.ToString());
        }
    }
}