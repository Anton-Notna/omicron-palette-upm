using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmicronPalette
{
    [Serializable]
    public class PaletteUnit
    {
        private enum Mode
        {
            Lineral = 0,
            Gamma = 1,
            Fixed = 2,
        }


        [SerializeField]
        private Mode _mode = Mode.Lineral;
        [SerializeField, ColorUsage(false)]
        private List<Color> _colors = new List<Color>() { Color.white };
        
        public override int GetHashCode()
        {
            int hash = _mode.GetHashCode();
            if (_colors != null)
            {
                for (int i = 0; i < _colors.Count; i++)
                    hash = HashCode.Combine(hash, _colors[i].GetHashCode());
            }

            return hash;
        }

        public float ComputeSortingOrder(int contrastPriority, int saturationPriority, int huePriority, int valuePriority)
        {
            contrastPriority = Mathf.RoundToInt(Mathf.Pow(10, contrastPriority));
            saturationPriority = Mathf.RoundToInt(Mathf.Pow(10, saturationPriority));
            huePriority = Mathf.RoundToInt(Mathf.Pow(10, huePriority));
            valuePriority = Mathf.RoundToInt(Mathf.Pow(10, valuePriority));

            var statistics = ComputeAverage();
            Color.RGBToHSV(statistics.color, out float hue, out float saturation, out float value);

            return (statistics.contrast * contrastPriority) + (saturation * saturationPriority) + (hue * huePriority) + (value * valuePriority);
        }

        public Color Interpolate(float t)
        {
            if (_colors == null || _colors.Count == 0)
                return Color.black;

            if (_colors.Count == 1)
            {
                var color = _colors[0];
                color.a = 1f;
                return color;
            }

            t = Mathf.Clamp01(t);

            if (_mode == Mode.Fixed)
            {
                int index = Mathf.Min(Mathf.FloorToInt(t * (_colors.Count)), _colors.Count - 1);
                var fixedColor = _colors[index];
                fixedColor.a = 1f;
                return fixedColor;
            }

            int startIndex = Mathf.FloorToInt(t * (_colors.Count - 1));
            int endIndex = Mathf.CeilToInt(t * (_colors.Count - 1));

            Color startColor = _colors[startIndex];
            Color endColor = _colors[endIndex];
            float localT = Mathf.InverseLerp(startIndex, endIndex, t * (_colors.Count - 1));

            var result =
                _mode == Mode.Gamma
                ? GammaLerp(startColor, endColor, localT)
                : Color.Lerp(startColor, endColor, localT);

            result.a = 1f;
            return result;
        }

        private static Color GammaLerp(Color color1, Color color2, float t)
        {
            Color linearColor1 = color1.linear;
            Color linearColor2 = color2.linear;
            Color linearInterpolatedColor = Color.Lerp(linearColor1, linearColor2, t);
            return linearInterpolatedColor.gamma;
        }

        private static Vector3 AsVector(Color color) => new Vector3(color.r, color.g, color.b);

        private static Color AsColor(Vector3 vector) => new Color(vector.x, vector.y, vector.z, 1f);

        private (float contrast, Color color) ComputeAverage()
        {
            if (_colors == null || _colors.Count == 0)
                return (0f, Color.black);

            Vector3 average = Vector3.zero;
            for (int i = 0; i < _colors.Count; i++)
                average += AsVector(_colors[i]);
            average /= _colors.Count;

            float contrast = 0f;
            for (int i = 0; i < _colors.Count; i++)
                contrast += Vector3.SqrMagnitude(average - AsVector(_colors[i]));
            contrast /= _colors.Count;

            return (contrast, AsColor(average));
        }
    }
}