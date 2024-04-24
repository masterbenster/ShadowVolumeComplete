cbuffer ConstantBuffer : register(b0)
{
    matrix WorldMatrix;
    matrix ViewMatrix;
    matrix ProjectionMatrix;
    float3 CameraPosition;
    float4 LightPosition; // Assuming this represents both position and intensity
    int Robust;
    int ZPass; // Is it safe to do Z-pass?
};

struct VS_INPUT
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VS_OUTPUT
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float2 TextureCoordinate : TEXCOORD0;
};

VS_OUTPUT PhongVertexShader(VS_INPUT input)
{
    VS_OUTPUT output;
    output.Position = mul(input.Position, WorldMatrix);
    output.Position = mul(output.Position, ViewMatrix);
    output.Position = mul(output.Position, ProjectionMatrix);
    output.Normal = mul(input.Normal, (float3x3) WorldMatrix); // Transform normal to world space
    output.TextureCoordinate = input.TextureCoordinate;
    return output;
}

float4 PhongPixelShader(VS_OUTPUT input) : SV_TARGET
{
    return float4(1, 1, 1, 1); // Placeholder for ambient light
}

[maxvertexcount(6)]
void PhongGeometryShader(triangle VS_OUTPUT input[3], inout TriangleStream<VS_OUTPUT> OutputStream)
{
    // Compute normals at vertices
    float3 ns[3];
    ns[0] = cross(input[1].Position.xyz - input[0].Position.xyz, input[2].Position.xyz - input[0].Position.xyz);
    ns[1] = cross(input[2].Position.xyz - input[1].Position.xyz, input[0].Position.xyz - input[1].Position.xyz);
    ns[2] = cross(input[0].Position.xyz - input[2].Position.xyz, input[1].Position.xyz - input[2].Position.xyz);

    // Compute direction from vertices to light
    float3 d[3];
    d[0] = LightPosition.xyz - LightPosition.w * input[0].Position.xyz;
    d[1] = LightPosition.xyz - LightPosition.w * input[1].Position.xyz;
    d[2] = LightPosition.xyz - LightPosition.w * input[2].Position.xyz;

    // Check if the main triangle faces the light
    bool faces_light = dot(ns[0], d[0]) > 0 || dot(ns[1], d[1]) > 0 || dot(ns[2], d[2]);
    
    // Far cap: extrude positions to infinity
    if (ZPass == 0)
    {
        // Near cap: simply render triangle
        OutputStream.Append(input[0]);
        OutputStream.Append(input[1]);
        OutputStream.Append(input[2]);

        // Extrude vertices to infinity and append to OutputStream
        for (int i = 0; i < 3; ++i)
        {
            VS_OUTPUT extrudedVertex;
            extrudedVertex.Position = float4(0, 0, 0, 1); // Initialize to some default value
            extrudedVertex.Position = mul(input[i].Position, WorldMatrix);
            extrudedVertex.Position = mul(extrudedVertex.Position, ViewMatrix);
            extrudedVertex.Position = mul(extrudedVertex.Position, ProjectionMatrix);
            extrudedVertex.Position += float4(LightPosition.w * input[i].Position.xyz - LightPosition.xyz, 0);
            extrudedVertex.Normal = input[i].Normal; // Keep the normal unchanged
            extrudedVertex.TextureCoordinate = input[i].TextureCoordinate; // Keep the texture coordinate unchanged
            OutputStream.Append(extrudedVertex);
        }
    }

    // Loop over all main vertices and extrude if needed
    for (uint j = 0; j < 3; ++j)
    {
        uint v0 = j;
        uint v1 = (j + 1) % 3;
         // Check if it's a possible silhouette
        if (input[v0].Position.w < 1e-3 || input[v1].Position.w < 1e-3 || (faces_light != (dot(ns[j], d[j]) > 0)))
        {
            // Make sure sides are oriented correctly
            uint j0 = faces_light ? v0 : v1;
            uint j1 = faces_light ? v1 : v0;

            // Extrude the edge
            VS_OUTPUT extrudedEdge[2];
            extrudedEdge[0] = input[j0];
            extrudedEdge[1].Position = mul(input[j0].Position, WorldMatrix);
            extrudedEdge[1].Position = mul(extrudedEdge[1].Position, ViewMatrix);
            extrudedEdge[1].Position = mul(extrudedEdge[1].Position, ProjectionMatrix);
            extrudedEdge[1].Position += float4(LightPosition.w * input[j0].Position.xyz - LightPosition.xyz, 0);
            extrudedEdge[1].Normal = input[j0].Normal; // Keep the normal unchanged
            extrudedEdge[1].TextureCoordinate = input[j0].TextureCoordinate; // Keep the texture coordinate unchanged
            OutputStream.Append(extrudedEdge[0]);
            OutputStream.Append(extrudedEdge[1]); // No error should occur here

            extrudedEdge[0] = input[j1];
            extrudedEdge[1].Position = mul(input[j1].Position, WorldMatrix);
            extrudedEdge[1].Position = mul(extrudedEdge[1].Position, ViewMatrix);
            extrudedEdge[1].Position = mul(extrudedEdge[1].Position, ProjectionMatrix);
            extrudedEdge[1].Position += float4(LightPosition.w * input[j1].Position.xyz - LightPosition.xyz, 0);
            extrudedEdge[1].Normal = input[j1].Normal; // Keep the normal unchanged
            extrudedEdge[1].TextureCoordinate = input[j1].TextureCoordinate; // Keep the texture coordinate unchanged
            OutputStream.Append(extrudedEdge[0]);
            OutputStream.Append(extrudedEdge[1]); // No error should occur here
        }
    }
}

technique PhongShadowVolumes
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 PhongVertexShader();
        GeometryShader = compile gs_4_0 PhongGeometryShader();
        PixelShader = compile ps_4_0 PhongPixelShader();
    }
}
