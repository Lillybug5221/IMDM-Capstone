using Photon.Deterministic;
using System;
using UnityEngine;


namespace Quantum
{
    [Serializable]
    public struct SimCurveKey
    {
        public FP time;  // time of the keyframe
        public FP value; // value at that time
    }

    [CreateAssetMenu(fileName = "SimCurve", menuName = "Quantum/SimCurve")]
    public class SimCurve : ScriptableObject
    {
        public SimCurveKey[] keys;

        /// <summary>
        /// Evaluate the curve at a given time using linear interpolation.
        /// </summary>
        public FP Evaluate(FP t)
        {
            if (keys == null || keys.Length == 0)
                return FP._0;

            // Before first key
            if (t <= keys[0].time)
                return keys[0].value;

            // After last key
            if (t >= keys[keys.Length - 1].time)
                return keys[keys.Length - 1].value;

            // Find the segment
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (t >= keys[i].time && t <= keys[i + 1].time)
                {
                    FP segmentT = (t - keys[i].time) / (keys[i + 1].time - keys[i].time);
                    return keys[i].value + (keys[i + 1].value - keys[i].value) * segmentT;
                }
            }

            return FP._0; // fallback (should never hit)
        }
    }
}