﻿using System.Text.Json.Serialization;
using MemoryPack;

namespace SlimFaas;

[MemoryPackable]
public partial record struct CustomRequest(List<CustomHeader> Headers, byte[]? Body, string FunctionName, string Path,
    string Method, string Query);

public partial record struct CustomHeader(string Key, string?[] Values);

[JsonSerializable(typeof(CustomRequest))]
[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class CustomRequestSerializerContext : JsonSerializerContext;
