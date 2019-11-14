using UnityEngine;

namespace PupilLabs
{
    public class ActivateOnCalibration : MonoBehaviour
    {
        public CalibrationController calibrationController;
        public ExperimentRunner runner;

        void OnEnable()
        {
            calibrationController.OnCalibrationSucceeded += DoActivate;
        }

        void OnDisable()
        {
            calibrationController.OnCalibrationSucceeded -= DoActivate;
        }

        void DoActivate()
        {
            runner.BeginExperiment();
        }
    }
}
