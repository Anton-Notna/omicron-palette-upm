using UnityEditor;
using UnityEngine;

namespace OmicronPalette
{
    [CustomPropertyDrawer(typeof(PaletteUnit))]
    public class PaletteUnitDrawer : PropertyDrawer
    {
        private const float _textureHeight = 20f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            PaletteUnit unit = GetPaletteUnit(property);
            Texture2D texture = null;
            if (unit != null)
            {
                texture = PaletteEditor.GetUnitTexture(unit, out var hash);
                PaletteEditor.MarkTexture(hash);
            }

            Rect textureRect = new Rect(position.x, position.y, position.width, _textureHeight);
            Rect propertiesRect = new Rect(position.x, position.y + _textureHeight + EditorGUIUtility.standardVerticalSpacing, position.width, position.height - _textureHeight - EditorGUIUtility.standardVerticalSpacing);

            if (texture != null)
                GUI.DrawTexture(textureRect, texture, ScaleMode.StretchToFill);

            EditorGUI.PropertyField(propertiesRect, property, GUIContent.none, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float propertiesHeight = EditorGUI.GetPropertyHeight(property, true);
            return _textureHeight + EditorGUIUtility.standardVerticalSpacing + propertiesHeight;
        }

        private PaletteUnit GetPaletteUnit(SerializedProperty property)
        {
            Palette palette = property.serializedObject.targetObject as Palette;

            if (palette == null)
                return null;

            string propertyPath = property.propertyPath;
            int startIndex = propertyPath.IndexOf('[') + 1;
            int endIndex = propertyPath.IndexOf(']');
            if (startIndex == 0 || endIndex == -1)
                return null;

            string indexString = propertyPath.Substring(startIndex, endIndex - startIndex);
            if (int.TryParse(indexString, out int index))
                return palette.Units[index];

            return null;
        }
    }
}