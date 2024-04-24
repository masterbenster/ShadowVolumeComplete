cbuffer ConstantBuffer : register(b0)
{
    matrix WorldMatrix;
    matrix ViewMatrix;
    matrix ProjectionMatrix;
    float3 CameraPosition;
    float4 LightPosition; // Assuming this represents both position and intensity
    // Add additional parameters for shadow volume data
    float3 ShadowVolumeCenter;
    float ShadowVolumeRadius;
};

// Input structure for vertex shader
struct VS_INPUT
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TextureCoordinate : TEXCOORD0;
};

// Output structure from vertex shader
struct VS_OUTPUT
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float2 TextureCoordinate : TEXCOORD0;
    float4 ShadowCoord : TEXCOORD1; // Shadow coordinate for shadow mapping
};

// Vertex shader for scene rendering
VS_OUTPUT SceneVertexShader(VS_INPUT input)
{
    VS_OUTPUT output;
    // Transform vertex position to world space, then to view space, then to clip space
    output.Position = mul(input.Position, WorldMatrix);
    output.Position = mul(output.Position, ViewMatrix);
    output.Position = mul(output.Position, ProjectionMatrix);

    // Pass through normal and texture coordinate
    output.Normal = mul(input.Normal, (float3x3) WorldMatrix);
    output.TextureCoordinate = input.TextureCoordinate;

    // Calculate shadow coordinate for shadow mapping
    output.ShadowCoord = mul(input.Position, WorldMatrix);
    output.ShadowCoord = mul(output.ShadowCoord, ViewMatrix);
    output.ShadowCoord = mul(output.ShadowCoord, ProjectionMatrix);

    return output;
}
bool IsInsideShadowVolume(float4 position, float3 shadowVolumeCenter, float shadowVolumeRadius)
{
    // Check if the position is inside the shadow volume
    float distanceToCenter = length(position.xyz - shadowVolumeCenter);
    return distanceToCenter <= shadowVolumeRadius;
}

// Pixel shader for scene rendering
float4 ScenePixelShader(VS_OUTPUT input) : SV_TARGET
{
    // Sample texture or perform other material calculations here
    float3 ambientColor = float3(0.2, 0.2, 0.2); // Ambient color

    // Calculate lighting
    float3 lightDirection = normalize(LightPosition.xyz - input.Position.xyz);
    float diffuseFactor = max(0, dot(input.Normal, lightDirection));
    float3 diffuseColor = LightPosition.w * diffuseFactor * float3(1, 1, 1); // Assuming white light

    // Combine ambient and diffuse lighting
    float3 finalColor = ambientColor + diffuseColor;

    // Apply shadow mapping (pseudo code, actual implementation depends on shadow mapping technique)
    // Use input.ShadowCoord to sample from shadow map and calculate shadow factor

    // Apply shadow volume
    float shadowVolumeFactor = IsInsideShadowVolume(input.Position, ShadowVolumeCenter, ShadowVolumeRadius) ? 0.5 : 1.0;
    finalColor *= shadowVolumeFactor;

    return float4(finalColor, 1.0); // Output final color
}

// Technique for rendering the scene
technique RenderScene
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 SceneVertexShader();
        PixelShader = compile ps_4_0 ScenePixelShader();
    }
}
