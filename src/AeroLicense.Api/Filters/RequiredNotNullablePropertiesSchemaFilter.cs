using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AeroLicense.Api.Filters;

/// <summary>
/// C#'ta null olamayan her özelliği şemada "required" işaretler (değer tipleri dahil). Böylece üretilen
/// TypeScript tiplerinde "id: string" olur, "id?: string" değil. Null olabilenler isteğe bağlı kalır.
/// </summary>
public sealed class RequiredNotNullablePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema { Properties.Count: > 0 } concrete) return;

        var nullability = new NullabilityInfoContext(); // thread-safe değil: her çağrıda yeni
        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (concrete.Properties.ContainsKey(name) && nullability.Create(property).ReadState == NullabilityState.NotNull)
                (concrete.Required ??= new HashSet<string>()).Add(name);
        }
    }
}
