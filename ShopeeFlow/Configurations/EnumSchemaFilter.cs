using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ShopeeFlow.Configurations;

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
            return;

        var lines = new StringBuilder();
        schema.Enum.Clear();

        foreach (var name in Enum.GetNames(context.Type))
        {
            var value = Convert.ToInt32(Enum.Parse(context.Type, name));
            var description = context.Type
                .GetField(name)?
                .GetCustomAttribute<DescriptionAttribute>()?
                .Description;

            if (lines.Length > 0)
                lines.Append(" | ");

            lines.Append(value);
            lines.Append('=');
            lines.Append(name);

            if (!string.IsNullOrWhiteSpace(description))
                lines.Append(" (").Append(description).Append(')');

            schema.Enum.Add(new OpenApiInteger(value));
        }

        schema.Description = lines.ToString();
    }
}
