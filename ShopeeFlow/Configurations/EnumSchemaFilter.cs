using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ShopeeFlow.Configurations;

/// <summary>
/// Renders enums in Swagger as: 5=CommissionDesc (Highest commission rate first)
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
            return;

        var lines = new StringBuilder();

        foreach (var name in Enum.GetNames(context.Type))
        {
            var value = Convert.ToInt32(Enum.Parse(context.Type, name));
            var description = GetDescription(context.Type, name);

            if (lines.Length > 0)
                lines.Append(" | ");

            lines.Append(value);
            lines.Append('=');
            lines.Append(name);

            if (!string.IsNullOrWhiteSpace(description))
            {
                lines.Append(" (");
                lines.Append(description);
                lines.Append(')');
            }
        }

        schema.Description = lines.ToString();
        schema.Enum.Clear();

        foreach (var name in Enum.GetNames(context.Type))
        {
            var value = Convert.ToInt32(Enum.Parse(context.Type, name));
            schema.Enum.Add(new OpenApiInteger(value));
        }
    }

    private static string? GetDescription(Type enumType, string name)
    {
        return enumType
            .GetField(name)?
            .GetCustomAttribute<DescriptionAttribute>()?
            .Description;
    }
}
