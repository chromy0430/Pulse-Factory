using System;
using System.Collections;
using System.Reflection;

namespace CodexHarness
{
    public static class CodexHarnessReferenceRules
    {
        // Honor the project's existing Odin contracts without adding an assembly dependency.
        public static bool IsRequired(Type componentType, string propertyPath)
        {
            Type current = componentType;
            FieldInfo field = null;
            foreach (string segment in propertyPath.Split('.'))
            {
                if (segment == "Array") continue;
                if (segment.StartsWith("data[", StringComparison.Ordinal))
                {
                    current = ElementType(current);
                    if (current == null) return false;
                    continue;
                }
                field = FindField(current, segment);
                if (field == null) return false;
                current = field.FieldType;
            }
            if (field == null) return false;
            foreach (CustomAttributeData attribute in field.GetCustomAttributesData())
                if (attribute.AttributeType.FullName == "Sirenix.OdinInspector.RequiredAttribute")
                    return true;
            return false;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

        private static Type ElementType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
                return type.GetGenericArguments()[0];
            return null;
        }
    }
}
