using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AcademiaDoZe.Application.Enums;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var type = value.GetType();
        var field = type.GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();

        if (attribute != null)
            return attribute.Name ?? value.ToString();

        if (type.GetCustomAttribute<FlagsAttribute>() != null)
        {
            var names = new List<string>();
            foreach (Enum flag in Enum.GetValues(type))
            {
                if (Convert.ToInt64(flag) == 0 || !value.HasFlag(flag))
                    continue;

                var flagField = type.GetField(flag.ToString());
                var flagAttribute = flagField?.GetCustomAttribute<DisplayAttribute>();
                names.Add(flagAttribute?.Name ?? flag.ToString());
            }

            if (names.Count > 0)
                return string.Join(", ", names);
        }

        return value.ToString();
    }
}
