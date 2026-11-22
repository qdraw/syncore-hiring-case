using System.Text.Json.Nodes;
using Cubing.Api.Contracts;
using Microsoft.AspNetCore.OpenApi;

namespace Cubing.Api.Extensions;

public static class OpenApiExamples
{
    /// <summary>Adds example payloads to the OpenAPI document; Scalar uses them to prefill requests.</summary>
    public static OpenApiOptions AddCubingExamples(this OpenApiOptions options) =>
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type == typeof(CubingApiRequest))
            {
                schema.Examples = [new JsonObject
                {
                    ["quantity"] = 28,
                    ["articleName"] = "Article1",
                    ["articleDimensions"] = new JsonObject { ["length"] = 10, ["width"] = 10, ["height"] = 10 },
                    ["boxTypeName"] = "Box",
                    ["boxDimensions"] = new JsonObject { ["length"] = 35, ["width"] = 35, ["height"] = 35 },
                }];
            }

            return Task.CompletedTask;
        });
}
