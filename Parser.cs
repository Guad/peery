using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Peery
{
    public static class Parser
    {
        public static string GenerateHelpText<T>()
        {
            TypeInfo model = typeof(T).GetTypeInfo();
            T modelObj = Activator.CreateInstance<T>();
            StringBuilder sb = new StringBuilder("Options:\n");

            foreach (PropertyInfo property in model.DeclaredProperties)
            {
                var sw = property.GetCustomAttribute<CustomSwitchAttribute>() ?? new CustomSwitchAttribute("-");
                string propName = string.Format("{1}{0}", property.Name.ToLower(), sw.Switch);
                sb.Append(propName + "\n");
            }

            return sb.ToString();
        }

        public static T Parse<T>(params string[] arguments)
        {
            TypeInfo model = typeof(T).GetTypeInfo();
            T modelObj = Activator.CreateInstance<T>();

            foreach (PropertyInfo property in model.DeclaredProperties)
            {
                var sw = property.GetCustomAttribute<CustomSwitchAttribute>() ?? new CustomSwitchAttribute("-");
                string propName = string.Format("{1}{0}", property.Name.ToLower(), sw.Switch);
                object propOutput;

                if ((propOutput = ParseMember(arguments, propName, property.PropertyType,
                        property.GetCustomAttribute<NotOptionalAttribute>() != null)) != null)
                {
                    property.SetValue(modelObj, propOutput);
                }
            }

            foreach (FieldInfo property in model.DeclaredFields)
            {
                var sw = property.GetCustomAttribute<CustomSwitchAttribute>() ?? new CustomSwitchAttribute("-");
                string propName = string.Format("{1}{0}", property.Name.ToLower(), sw.Switch);
                object propOutput;

                if ((propOutput =
                        ParseMember(arguments, propName, property.FieldType,
                            property.GetCustomAttribute<NotOptionalAttribute>() != null)) != null)
                {
                    property.SetValue(modelObj, propOutput);
                }
            }

            return modelObj;
        }

        private static object ParseMember(string[] arguments, string propName, Type propType, bool obligatory)
        {
            int index = Array.IndexOf(arguments, propName);

            if (arguments.Contains(propName))
            {
                if (propType == typeof(bool))
                {
                    return true;
                }
                if (propType == typeof(string))
                {
                    char[] delimiters = new[] { '"', '\'' };

                    foreach (char delimiter in delimiters)
                    {
                        if (arguments[index + 1].StartsWith(delimiter.ToString()))
                        {
                            for (int i = index + 1; i < arguments.Length; i++)
                            {
                                if (arguments[i].EndsWith(delimiter.ToString()))
                                {
                                    string[] newArr = new string[i - index];
                                    Array.Copy(arguments, index + 1, newArr, 0, i - index);

                                    if (newArr[0].Length > 1)
                                        newArr[0] = newArr[0].Substring(1, newArr[0].Length - 1);
                                    newArr[newArr.Length - 1] = newArr[newArr.Length - 1].Substring(0,
                                        newArr[newArr.Length - 1].Length - 1);
                                    return string.Join(" ", newArr);
                                }
                            }
                        }
                    }

                    return arguments[index + 1];
                }
                else
                {
                    try
                    {
                        return Convert.ChangeType(arguments[index + 1], propType);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new ArgumentException("Invalid value for \"" + propName + "\" argument!");
                    }
                }
            }
            else if (obligatory)
            {
                throw new ArgumentException("Argument \"" + propName + "\" is not optional!");
            }

            return null;
        }
    }

    public class NotOptionalAttribute : System.Attribute
    { }

    public class CustomSwitchAttribute : System.Attribute
    {
        public string Switch;

        public CustomSwitchAttribute(string sw)
        {
            Switch = "-";

            if (!string.IsNullOrWhiteSpace(sw))
                Switch = sw;
        }

    }
    
}
