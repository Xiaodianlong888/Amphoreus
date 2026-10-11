// 屏幕紫色光芒着色器
sampler uImage0 : register(s0);
float4 uOpacity : register(c0);
float2 uTargetPosition : register(c1);

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, texCoord);
    
    // 计算与屏幕中心的距离
    float2 screenCenter = float2(0.5, 0.5);
    float2 uvOffset = texCoord - screenCenter;
    float distance = length(uvOffset);
    
    // 创建紫色光芒效果
    float glow = max(0.0, 1.0 - distance * 2.0);
    float4 purpleGlow = float4(0.6, 0.2, 0.8, 1.0) * glow * uOpacity.x;
    
    // 混合原色和紫色光芒
    color.rgb = lerp(color.rgb, float3(0.6, 0.2, 0.8), glow * uOpacity.x * 0.3);
    color.rgb += purpleGlow.rgb * 0.5;
    
    return color;
}

technique PurpleGlow
{
    pass P0
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}