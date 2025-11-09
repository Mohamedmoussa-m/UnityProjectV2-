using UnityEngine;
using UnityEngine.UI;

public class AB_UI_Sliders : MonoBehaviour
{
    [System.Serializable]
    public class JointSlider
    {
        public ArticulationBody joint;
        public Slider slider;
        public Text label;
    }

    public JointSlider[] items;

    void Start()
    {
        foreach (var it in items)
        {
            if (!it.joint || !it.slider) continue;
            var d = it.joint.xDrive;
            it.slider.minValue = d.lowerLimit;
            it.slider.maxValue = d.upperLimit;

            // init from current joint position
            var pose = it.joint.jointPosition;
            float currentDeg = (pose.dofCount > 0 ? pose[0] : 0f) * Mathf.Rad2Deg;
            it.slider.value = Mathf.Clamp(currentDeg, it.slider.minValue, it.slider.maxValue);

            // update label
            if (it.label) it.label.text = $"{it.joint.name}: {Mathf.RoundToInt(it.slider.value)}°";

            it.slider.onValueChanged.AddListener(val => {
                var drive = it.joint.xDrive;
                drive.target = val;
                it.joint.xDrive = drive;
                if (it.label) it.label.text = $"{it.joint.name}: {Mathf.RoundToInt(val)}°";
            });
        }
    }
}
