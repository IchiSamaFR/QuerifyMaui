using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Queries;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Methode Run de la classe UserQuery d'une requete : le contexte en premier parametre, puis les parametres du
/// dashboard, lies par leur nom. Statique ou d'instance, de toute visibilite ; void ou Task : rien a afficher.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : reflexion assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "App non publiee en trimmed : reflexion assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "App non publiee en trimmed : reflexion assumee.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "App non publiee en trimmed : reflexion assumee.")]
internal sealed class LinqEntryPoint
{
    /// <summary>Code des erreurs de liaison, rapportees comme des erreurs de compilation.</summary>
    internal const string DiagnosticCode = "LINQ001";

    private const BindingFlags AnyMethod = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly string Usage = $"{LinqQuerySource.EntryClass} {{ object {LinqQuerySource.EntryMethod}({LinqQuerySource.ContextAlias} context, ...) }}";

    private readonly MethodInfo _method;
    private readonly object?[] _arguments;
    private readonly bool _isTaskOfResult;

    private LinqEntryPoint(MethodInfo method, object?[] arguments)
    {
        _method = method;
        _arguments = arguments;

        var returnType = method.ReturnType;
        _isTaskOfResult = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
        ReturnsValue = returnType != typeof(void) && returnType != typeof(Task);
    }

    /// <summary>Faux pour void ou Task : la valeur rendue (null) n'est pas un resultat.</summary>
    public bool ReturnsValue { get; }

    /// <summary>Lie la methode et ses parametres ; <paramref name="error"/> explique un echec.</summary>
    public static bool TryBind(Assembly assembly, Type contextType, IReadOnlyList<LinqParameter> parameters, [NotNullWhen(true)] out LinqEntryPoint? entryPoint, [NotNullWhen(false)] out string? error)
    {
        entryPoint = null;

        var types = assembly.GetTypes().Where(t => t.Name == LinqQuerySource.EntryClass).ToArray();
        if (types is not [var type])
        {
            error = types.Length == 0
                ? $"Classe {LinqQuerySource.EntryClass} introuvable : la requete declare class {Usage}."
                : $"Plusieurs classes {LinqQuerySource.EntryClass} : une seule par requete.";
            return false;
        }

        var methods = type.GetMethods(AnyMethod).Where(m => m.Name == LinqQuerySource.EntryMethod).ToArray();
        if (methods is not [var method] || method.IsGenericMethodDefinition)
        {
            error = $"{LinqQuerySource.EntryClass} doit declarer une seule methode {LinqQuerySource.EntryMethod}, non generique : {Usage}.";
            return false;
        }

        var declared = method.GetParameters();
        if (declared.Length == 0 || !declared[0].ParameterType.IsAssignableFrom(contextType))
        {
            error = $"{LinqQuerySource.EntryMethod} recoit le contexte en premier parametre : {Usage}.";
            return false;
        }

        if (!method.IsStatic && (type.IsAbstract || type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes) is null))
        {
            error = $"{LinqQuerySource.EntryClass} doit pouvoir etre creee sans argument, ou {LinqQuerySource.EntryMethod} etre statique.";
            return false;
        }

        var arguments = new object?[declared.Length];
        for (var i = 1; i < declared.Length; i++)
        {
            var parameter = declared[i];

            // Nom du dashboard sans casse ni @ : userId recoit @UserId.
            var source = parameters.FirstOrDefault(p => string.Equals(p.Name.TrimStart('@'), parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (source is null)
            {
                if (!parameter.HasDefaultValue)
                {
                    error = $"Parametre « {parameter.Name} » sans valeur : il doit porter le nom d'un parametre du dashboard, ou avoir une valeur par defaut.";
                    return false;
                }

                arguments[i] = parameter.DefaultValue;
                continue;
            }

            if (!TryConvert(source.Value, parameter.ParameterType, out arguments[i]))
            {
                error = $"Parametre « {parameter.Name} » : la valeur « {source.Value ?? "null"} » ne convient pas au type {parameter.ParameterType.Name}.";
                return false;
            }
        }

        entryPoint = new LinqEntryPoint(method, arguments);
        error = null;
        return true;
    }

    /// <summary>Appelle Run sur <paramref name="context"/> ; une exception de la requete est relevee telle quelle.</summary>
    public object? Invoke(DbContext context)
    {
        var arguments = (object?[])_arguments.Clone();
        arguments[0] = context;

        try
        {
            var instance = _method.IsStatic ? null : Activator.CreateInstance(_method.DeclaringType!, nonPublic: true);
            var result = _method.Invoke(instance, arguments);

            // Requete async : attendue ici, l'execution est deja hors du thread de l'interface.
            if (result is not Task task)
                return result;

            task.GetAwaiter().GetResult();
            return _isTaskOfResult ? task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task) : null;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static bool TryConvert(object? value, Type type, out object? converted)
    {
        converted = value;
        if (value is null)
            return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

        if (type.IsInstanceOfType(value))
            return true;

        var target = Nullable.GetUnderlyingType(type) ?? type;
        try
        {
            converted = value switch
            {
                string text when target == typeof(Guid) => Guid.Parse(text),
                DateTime date when target == typeof(DateOnly) => DateOnly.FromDateTime(date),
                _ when target.IsEnum => Enum.ToObject(target, value),
                _ => Convert.ChangeType(value, target, CultureInfo.InvariantCulture),
            };
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            converted = null;
            return false;
        }
    }
}
