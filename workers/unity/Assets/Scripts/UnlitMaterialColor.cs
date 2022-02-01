using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[MaterialProperty("_UnlitColor", MaterialPropertyFormat.Float4, -1)]
public struct UnlitMaterialColor : IComponentData
{
    public float4 Value;
}
