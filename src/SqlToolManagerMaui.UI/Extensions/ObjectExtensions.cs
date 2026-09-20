using Newtonsoft.Json;
using System.Reflection;

namespace SqlToolManagerMaui.UI.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    /// Updates the properties of the target object with the non-null properties of the source object.
    /// </summary>
    public static void MergeFrom<T>(this T target, T source, params string[] excludedProperties) where T : new()
    {
        if (source == null)
            return;

        target ??= new T();

        var type = typeof(T);
        var exclusions = new HashSet<string>(excludedProperties ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite || !prop.CanRead)
                continue;
            if (exclusions.Contains(prop.Name))
                continue;

            var value = prop.GetValue(source);
            prop.SetValue(target, value);
        }
    }

    /// <summary>
    /// Creates a deep copy of the object using JSON serialization.
    /// </summary>
    public static T Clone<T>(this T source)
    {
        if (ReferenceEquals(source, null))
            return default!;

        var serializedObject = JsonConvert.SerializeObject(source);
        var clone = JsonConvert.DeserializeObject<T>(serializedObject);

        if (clone == null)
            throw new InvalidOperationException($"Unable to clone instance of type '{typeof(T).FullName}'.");

        return clone;
    }

    /// <summary>
    /// Creates a deep copy of the source object and converts it to the specified target type using JSON serialization.
    /// </summary>
    /// <typeparam name="TSource">The type of the source object.</typeparam>
    /// <typeparam name="TTarget">The type of the target object.</typeparam>
    /// <param name="source">The source object to clone and convert.</param>
    /// <returns>A deep copy of the source object converted to the target type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the cloning or conversion fails.</exception>
    public static TTarget CloneTo<TSource, TTarget>(this TSource source)
    {
        if (ReferenceEquals(source, null))
            return default!;

        var serializedObject = JsonConvert.SerializeObject(source);
        var clone = JsonConvert.DeserializeObject<TTarget>(serializedObject);

        if (clone == null)
            throw new InvalidOperationException($"Unable to clone instance of type '{typeof(TTarget).FullName}'.");

        return clone;
    }
}