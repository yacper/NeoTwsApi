/********************************************************************
    created:	2022/8/12 15:20:42
    author:		rush
    email:		yacper@gmail.com	
	
    purpose:
    modifiers:	
*********************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NeoTwsApi.Helpers
{
internal static class EnumEx
{
    public static string GetDescription(this Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString());

        DescriptionAttribute attribute
            = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                  as DescriptionAttribute;

        return attribute == null ? value.ToString() : attribute.Description;
    }

    public static T GetValueFromDescription<T>(this string description) where T : Enum
    {
        foreach (var field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field,
                                             typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                if (attribute.Description == description)
                    return (T)field.GetValue(null);
            }
            else
            {
                if (field.Name == description)
                    return (T)field.GetValue(null);
            }
        }

        throw new ArgumentException("Not found.", nameof(description));
        // Or return default(T);
    }
}
}