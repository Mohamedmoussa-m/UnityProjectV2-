using UnityEngine;
using UnityEngine.UI;

public class AB_RobotController : MonoBehaviour
{
    [Header("Assign rotating joints in order (Link1 .. EndEff) – DO NOT include the fixed base")]
    public ArticulationBody[] joints;   //

    [Header("Motion Speeds")]
    [Tooltip("Degrees per second when holding an inc/dec key (without Shift/Alt).")]
    public float speedDegPerSec = 10f;   // low-ish default
    [Tooltip("Hold Shift to go faster (multiplies speed).")]
    public float shiftMultiplier = 1.5f; // modest boost
    [Tooltip("Hold Alt to go slower (multiplies speed).")]
    public float altMultiplier = 0.5f;   // fine control

    [Header("Joint Selection")]
    [Tooltip("Shortcut keys to select each joint (X,C,V,B,N by default). Optional; leave empty to disable shortcuts.")]
    public KeyCode[] selectJointKeys = { KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N };
    [Tooltip("Degrees to rotate per arrow-key press for the currently selected joint.")]
    public float arrowStepDegrees = 5f;
    [Tooltip("Default selected joint (0-based index). Set -1 to start with no selection.")]
    public int defaultSelectedJoint = 0;

    [Header("Keys (+/- per joint) – T/Y/U/I/O = right, G/H/J/K/L = left")]
    public KeyCode[] increaseKeys = { KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O };
    public KeyCode[] decreaseKeys = { KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

    [Header("Extras")]
    public bool clampToLimits = true;          // keep target inside xDrive limits
    public KeyCode zeroAllKey = KeyCode.Space; // zero all joint targets

    [Header("Selection Highlight")]
    [Tooltip("Renderers to tint for each joint (one per joint). Leave empty to skip highlights.")]
    public Renderer[] jointRenderers;
    public Color highlightColor = Color.yellow;
    [Tooltip("Emission multiplier for highlight color (makes it pop even in dark scenes).")]
    public float highlightEmission = 6f;

    [Header("UI Display")]
    [Tooltip("Optional UI Text to display selected joint info. Leave empty to disable.")]
    public Text jointInfoText;

    private float[] targets; // degrees
    private int selectedJointIndex = -1;
    private Color[] originalColors;

    void Start()
    {
        if (joints == null || joints.Length == 0)
        {
            Debug.LogError("Assign 'joints' (Link1..EndEff). Do not include the fixed base.");
            enabled = false; return;
        }

        targets = new float[joints.Length];
        CacheOriginalColors();

        // Initialize targets from current joint positions (radians -> degrees)
        for (int i = 0; i < joints.Length; i++)
        {
            var ab = joints[i]; if (!ab) continue;
            var pose = ab.jointPosition; // radians
            targets[i] = (pose.dofCount > 0 ? pose[0] : 0f) * Mathf.Rad2Deg;
        }

        if (defaultSelectedJoint >= 0 && defaultSelectedJoint < joints.Length && joints[defaultSelectedJoint] != null)
        {
            selectedJointIndex = defaultSelectedJoint;
            ApplyHighlight(-1, selectedJointIndex);
        }
    }

    void Update()
    {
        if (targets == null || joints == null) return;

        HandleJointSelectionInput();

        float mult = 1f;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.LeftShift) || Assets.Scripts.GlobalInputManager.GetKey(KeyCode.RightShift)) mult *= shiftMultiplier;
        if (Assets.Scripts.GlobalInputManager.GetKey(KeyCode.LeftAlt) || Assets.Scripts.GlobalInputManager.GetKey(KeyCode.RightAlt)) mult *= altMultiplier;

        float step = speedDegPerSec * mult * Time.deltaTime;

        for (int i = 0; i < joints.Length; i++)
        {
            var ab = joints[i]; if (!ab) continue;

            bool inc = (i < increaseKeys.Length) && increaseKeys[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(increaseKeys[i]);
            bool dec = (i < decreaseKeys.Length) && decreaseKeys[i] != KeyCode.None && Assets.Scripts.GlobalInputManager.GetKey(decreaseKeys[i]);

            if (inc && !dec) targets[i] += step;
            if (dec && !inc) targets[i] -= step;

            ApplyTargetToJoint(i);
        }

        HandleArrowKeySteps();

        // Zero all joints (optional)
        if (Assets.Scripts.GlobalInputManager.GetKeyDown(zeroAllKey))
        {
            for (int i = 0; i < joints.Length; i++)
            {
                var ab = joints[i]; if (!ab) continue;
                var d = ab.xDrive;
                float zero = Mathf.Clamp(0f, d.lowerLimit, d.upperLimit);
                d.target = zero;
                ab.xDrive = d;
                targets[i] = zero;
            }
        }

        // Update UI display
        UpdateJointInfoUI();
    }

    private void HandleJointSelectionInput()
    {
        if (selectJointKeys == null || selectJointKeys.Length == 0)
        {
            Debug.LogWarning("AB_RobotController: selectJointKeys array is empty!");
            return;
        }

        for (int i = 0; i < joints.Length && i < selectJointKeys.Length; i++)
        {
            if (selectJointKeys[i] == KeyCode.None) continue;
            
            // TEMPORARY: Use Input.GetKeyDown directly to bypass GlobalInputManager
            if (Input.GetKeyDown(selectJointKeys[i]))
            {
                Debug.LogWarning($"AB_RobotController: Key {selectJointKeys[i]} pressed for joint {i}");
                
                if (joints[i] == null)
                {
                    Debug.LogWarning($"AB_RobotController: Joint {i} is null, cannot select!");
                    continue;
                }

                Debug.LogWarning($"AB_RobotController: Selecting joint {i} (was {selectedJointIndex})");
                ApplyHighlight(selectedJointIndex, i);
                selectedJointIndex = i;
                break;
            }
        }
    }

    private void HandleArrowKeySteps()
    {
        if (selectedJointIndex < 0 || selectedJointIndex >= joints.Length || joints[selectedJointIndex] == null) return;

        float delta = 0f;
        bool increase = Input.GetKeyDown(KeyCode.Q);
        bool decrease = Input.GetKeyDown(KeyCode.E);

        // Q to increase, E to decrease
        if (increase) delta += arrowStepDegrees;
        if (decrease) delta -= arrowStepDegrees;

        if (Mathf.Approximately(delta, 0f)) return;

        targets[selectedJointIndex] += delta;
        ApplyTargetToJoint(selectedJointIndex);
    }

    private void ApplyTargetToJoint(int jointIndex)
    {
        if (jointIndex < 0 || jointIndex >= joints.Length) return;
        var ab = joints[jointIndex]; if (!ab) return;

        var d = ab.xDrive; // struct
        if (clampToLimits && (!Mathf.Approximately(d.lowerLimit, 0f) || !Mathf.Approximately(d.upperLimit, 0f)))
            targets[jointIndex] = Mathf.Clamp(targets[jointIndex], d.lowerLimit, d.upperLimit);

        d.target = targets[jointIndex]; // degrees
        ab.xDrive = d;
    }

    private void CacheOriginalColors()
    {
        if (jointRenderers == null || jointRenderers.Length == 0) return;
        int len = jointRenderers.Length;
        originalColors = new Color[len];
        for (int i = 0; i < len; i++)
        {
            originalColors[i] = GetRendererColor(jointRenderers[i], Color.white);
        }
    }


    private static Color GetRendererColor(Renderer r, Color fallback)
    {
        if (!r) return fallback;
        
        // Try to get color from shared material first (more reliable)
        if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
        {
            return r.sharedMaterial.color;
        }
        
        // Fallback to material instance
        if (r.material != null && r.material.HasProperty("_Color"))
        {
            return r.material.color;
        }
        
        return fallback;
    }

    private static void SetRendererColor(Renderer r, Color c)
    {
        if (!r) return;
        
        // Use MaterialPropertyBlock for runtime color changes (doesn't modify the asset)
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", c);
        mpb.SetColor("_EmissionColor", c * Mathf.Max(1f, c.grayscale * 2f)); // boost visibility
        r.SetPropertyBlock(mpb);
    }

    private void ApplyHighlight(int prevIndex, int newIndex)
    {
        if (jointRenderers == null || jointRenderers.Length == 0)
        {
            Debug.LogWarning("AB_RobotController: jointRenderers array is empty or null. Please assign renderers in the Inspector to enable highlighting.");
            return;
        }

        Debug.LogWarning($"AB_RobotController: ApplyHighlight called - prev: {prevIndex}, new: {newIndex}");

        // Restore previous
        if (prevIndex >= 0 && prevIndex < jointRenderers.Length && jointRenderers[prevIndex])
        {
            Color baseColor = (originalColors != null && prevIndex < originalColors.Length) ? originalColors[prevIndex] : Color.white;
            SetRendererColor(jointRenderers[prevIndex], baseColor);
            Debug.LogWarning($"AB_RobotController: Restored joint {prevIndex} to color {baseColor}");
        }

        // Apply new
        if (newIndex >= 0 && newIndex < jointRenderers.Length && jointRenderers[newIndex])
        {
            SetRendererColor(jointRenderers[newIndex], highlightColor);
            Debug.LogWarning($"AB_RobotController: Highlighted joint {newIndex} with color {highlightColor}");
        }
        else if (newIndex >= 0)
        {
            Debug.LogWarning($"AB_RobotController: Cannot highlight joint {newIndex} - renderer not assigned or out of range.");
        }
    }

    private void UpdateJointInfoUI()
    {
        if (jointInfoText == null) return;

        if (selectedJointIndex < 0 || selectedJointIndex >= joints.Length)
        {
            jointInfoText.text = "";
            return;
        }

        float currentAngle = targets[selectedJointIndex];
        jointInfoText.text = $"<color=lime>Selected Joint: {selectedJointIndex}\nJoint Angle: {currentAngle:F1}°</color>";
    }
}
