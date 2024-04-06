#nullable enable
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;

namespace T3.Serialization;

public static class JsonUtils
{
    public static T ReadEnum<T>(JToken o, string name) where T : struct, Enum
    {
        var dirtyFlagJson = o[name];
        return dirtyFlagJson != null
                   ? Enum.Parse<T>(dirtyFlagJson.Value<string>() ?? string.Empty)
                   : default;
    }

    public static T? ReadToken<T>(JToken o, string name, T? defaultValue = default)
    {
        var jSettingsToken = o[name];
        return jSettingsToken == null ? defaultValue : jSettingsToken.Value<T>();
    }

    public static T? TryLoadingJson<T>(string filepath) where T : class
    {
        if(!TryLoadingJson(filepath, out T? result))
        {
            return default;
        }
        
        return result;
    }

    public static bool TryLoadingJson<T>(string filepath, [NotNullWhen(true)] out T? result)
    {
        if (!File.Exists(filepath))
        {
            Log.Warning($"{filepath} doesn't exist yet");
            result = default;
            return false;
        }

        var jsonBlob = File.ReadAllText(filepath);
        var serializer = JsonSerializer.Create();
        var fileTextReader = new StringReader(jsonBlob);
        try
        {
            if (serializer.Deserialize(fileTextReader, typeof(T)) is T configurations)
            {
                result = configurations;
                return true;
            }

            Log.Error($"Can't load {filepath}");
            result = default;
            return false;
        }
        catch (Exception e)
        {
            Log.Error($"Can't load {filepath}:" + e.Message);
            result = default;
            return false;
        }
    }

    public static bool TrySaveJson<T>(T dataObject, string filepath) 
    {
        if (string.IsNullOrEmpty(filepath))
        {
            Log.Warning($"Can't save {typeof(T)} to empty filename...");
            return false;
        }

        var serializer = JsonSerializer.Create();
        serializer.Formatting = Formatting.Indented;
        try
        {
            using var streamWriter = File.CreateText(filepath);
            serializer.Serialize(streamWriter, dataObject);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning($"Can't create file {filepath} to save {typeof(T)} " + e.Message);
            return false;
        }
    }

    public static void WriteValue<T>(this JsonTextWriter writer, string name, T value) where T : struct
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value);
    }

    public static void WriteObject(this JsonTextWriter writer, string name, object value)
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value.ToString());
    }
}