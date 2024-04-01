﻿using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace {{NAMESPACE}};

[Guid("{{GUID}}"), Home]
internal sealed class {{PROJ}} : Instance<{{PROJ}}>
{
}

// ReSharper disable once UnusedType.Global
public sealed class ShareDefinition : IShareResources
{
    // ReSharper disable once EmptyConstructor
    public ShareDefinition(){}
    #pragma warning disable CA1822
    public bool ShouldShareResources => {{SHARE_RESOURCES}};
    #pragma warning restore CA1822
}