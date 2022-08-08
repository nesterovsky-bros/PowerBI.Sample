namespace NesterovskyBros.Collections;

using System.Collections;
using System.Xml;
using System.Xml.Linq;

public static class Functions
{
  /// <summary>
  /// Converts a enumerable of objects into an enumerable of 
  /// <see cref="XElement"/>.
  /// </summary>
  /// <param name="source">An input source..</param>
  /// <param name="name">Optional element name.</param>
  /// <returns>
  /// A enumerable of <see cref="XElement"/>.
  /// </returns>
  public static IEnumerable<XElement?> ToXml(
    this IEnumerable<object?> source, 
    string? name = null) =>
    source.Select(item => ToXml(item, name));

  /// <summary>
  /// Converts an anonymous type to an XElement.
  /// </summary>
  /// <param name="input">The input.</param>
  /// <param name="name">The element name.</param>
  /// <returns>
  /// Returns the object as it's XML representation in an 
  /// <see cref="XElement"/>.
  /// </returns>
  public static XElement? ToXml(object? input, string? name = null)
  {
    if (input == null)
    {
      return null;
    }

    if (string.IsNullOrEmpty(name))
    {
      var typeName = input.GetType().Name;

      name = typeName.Contains("AnonymousType") ? "Object" : typeName;
    }

    var inputType = input.GetType();

    if (IsSimpleType(inputType))
    {
      if ((input is DateTime date) && (date.TimeOfDay == TimeSpan.Zero))
      {
        return new XElement(name, date.ToString("yyyy-MM-dd"));
      }
      else
      {
        return new XElement(name, input);
      }
    }
   
    if (input is IEnumerable enumerable)
    {
      return ToXml(new { items = enumerable }, name);
    }

    var children = new List<object>();

    foreach(var property in input.GetType().GetProperties())
    {
      var value = property.GetValue(input, null);

      if (value == null)
      {
        continue;
      }

      var propertyName = XmlConvert.EncodeName(property.Name);
      var type = Nullable.GetUnderlyingType(property.PropertyType) ??
        property.PropertyType;

      if (IsSimpleType(type))
      {
        children.Add((value is string text) && 
          (propertyName.Equals("text", StringComparison.OrdinalIgnoreCase) ||
            propertyName.Equals("value", StringComparison.OrdinalIgnoreCase)) ?
            new XText(text) :
          (value is DateTime date) && (date.TimeOfDay == TimeSpan.Zero) ?
            new XAttribute(propertyName, date.ToString("yyyy-MM-dd")) :
            new XAttribute(propertyName, value));
      }
      else if (value is IEnumerable enumerableValue)
      {
        var itemName = propertyName.EndsWith("ies") ? 
          propertyName[0..^3] + "y" :
          propertyName.EndsWith("s") ? propertyName[0..^1] :
          propertyName;

        foreach(var item in enumerableValue)
        {
          children.Add(ToXml(item, itemName) ?? new XElement(itemName));
        }
      }
      else
      {
        var child = ToXml(value, propertyName);

        if (child != null)
        {
          children.Add(child);
        }
      }
    }

    return new XElement(XmlConvert.EncodeName(name), children);
  }

  private static bool IsSimpleType(Type type)
  {
    return type.IsPrimitive || type.IsEnum || WriteTypes.Contains(type);
  }

  private static readonly Type[] WriteTypes = new[]
  {
    typeof(string),
    typeof(DateTime),
    typeof(Enum),
    typeof(decimal),
    typeof(Guid),
  };
}