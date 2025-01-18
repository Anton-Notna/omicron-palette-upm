using System;
using System.Collections.Generic;
using UnityEngine;

namespace OmicronPalette
{
    [CreateAssetMenu(menuName = "New Palette")]
    public class Palette : ScriptableObject
    {
        [SerializeField, PaletteUnitDraw]
        private List<PaletteUnit> _units = new List<PaletteUnit>();
        [Header("Sorting Priority")]
        [SerializeField, Range(0, 4)]
        private int _contrast = 4;
        [SerializeField, Range(0, 4)]
        private int _saturation = 3;
        [SerializeField, Range(0, 4)]
        private int _hue = 2;
        [SerializeField, Range(0, 4)]
        private int _brightness = 1;
        [Header("Texture")]
        [SerializeField, Range(4, 11)]
        private int _resolutionFactor = 4;
        [SerializeField, Range(1, 32)]
        private int _columns = 2;
        [SerializeField, Range(1, 32)]
        private int _raws = 2;

        public IReadOnlyList<PaletteUnit> Units => _units;

        public int Resolution => Mathf.RoundToInt(Mathf.Pow(2, _resolutionFactor));

        public int Columns => _columns;

        public int Raws => _raws;

        public void Sort()
        {
            _units.Sort(Compare);
        }

        private int Compare(PaletteUnit p0, PaletteUnit p1)
        {
            float order0 = p0.ComputeSortingOrder(_contrast, _saturation, _hue, _brightness);
            float order1 = p1.ComputeSortingOrder(_contrast, _saturation, _hue, _brightness);

            if (order0 == order1)
                return 0;

            return order0 < order1 ? 1 : -1;
        }
    }
}