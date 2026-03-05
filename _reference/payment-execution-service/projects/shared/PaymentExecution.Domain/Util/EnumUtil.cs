using System.Reflection;
using System.Runtime.Serialization;

namespace PaymentExecution.Domain.Util;

public static class EnumUtil
{
    public static string GetEnumMemberValue<T>(T enumValue) where T : Enum
    {
        var memberInfo = typeof(T).GetMember(enumValue.ToString()).FirstOrDefault();
        if (memberInfo == null)
        {
            return enumValue.ToString();
        }

        var attribute = memberInfo.GetCustomAttribute<EnumMemberAttribute>(false);
        return attribute?.Value ?? enumValue.ToString();
    }
}
