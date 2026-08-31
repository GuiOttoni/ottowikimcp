using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using OttoWikiMcp.McpServer.Tools;

namespace OttoWikiMcp.McpServer.Endpoints;

/// <summary>
/// Endpoint de introspecção das tools MCP registradas — usa reflexão sobre as classes
/// <c>[McpServerToolType]</c> em vez de manter uma lista escrita à mão, pra nunca ficar
/// desatualizado em relação ao código real (se alguém adicionar/remover uma tool, a página de
/// documentação do frontend reflete isso automaticamente no próximo load, sem precisar editar
/// nada além do código da tool em si).
/// </summary>
public static class McpInfoEndpoints
{
    private static readonly Type[] ToolTypes = [typeof(WikiTools), typeof(WikiSyncTool), typeof(WorkApiTools)];

    public static void MapMcpInfoEndpoints(this WebApplication app)
    {
        app.MapGet("/api/mcp/tools", () =>
        {
            var tools = ToolTypes
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Select(method => (type, method, attr: method.GetCustomAttribute<McpServerToolAttribute>()))
                    .Where(x => x.attr is not null))
                .Select(x => new
                {
                    name = x.attr!.Name ?? x.method.Name,
                    description = x.method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                    category = x.type.Name,
                    parameters = x.method.GetParameters().Select(p => new
                    {
                        name = p.Name,
                        type = FriendlyTypeName(p.ParameterType),
                        description = p.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                        required = !p.HasDefaultValue,
                    }),
                })
                .OrderBy(t => t.category).ThenBy(t => t.name)
                .ToList();

            return Results.Ok(tools);
        });
    }

    private static string FriendlyTypeName(Type t)
    {
        var underlying = Nullable.GetUnderlyingType(t);
        var baseType = underlying ?? t;
        var name = baseType.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Boolean" => "bool",
            _ => baseType.Name,
        };
        return underlying is not null ? $"{name}?" : name;
    }
}
