// Orb.fx

#include "Lighthelper.fx"

// VARIABLES
float4x4 g_World;
float4   g_OrbColor;

// Common data
float3 g_EyePosW;
float4x4 g_View;
float4x4 g_Projection;
float4x4 g_LightView;
float4x4 g_LightProjection;

// Light data
float3 g_LightDir;
float4 g_LightAmbient;
float4 g_LightDiffuse;
float4 g_LightSpecular;

// Fog data
float  g_FogStart;
float  g_FogRange;
float4 g_SkyColor;
float  g_DarkSkyOffset;

// TEXTURES
Texture2D g_RefractionMap;

sampler g_RefractionMapSampler = sampler_state
{
	texture = <g_RefractionMap>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	addressU  = CLAMP;
	addressV  = CLAMP;
};

// SHADER I/O STRUCTS
struct OrbVS_IN
{
	float3 posL : POSITION;
	float3 normL : NORMAL;
};

struct OrbPS_IN
{
	float4 posH : SV_POSITION;
	float3 posW : TEXCOORD0;
	float3 normW : TEXCOORD1;
};

struct OrbRefractionPS_IN
{
	float4 posH : SV_POSITION;
	float4 refrTex : TEXCOORD0;
	float3 posW  : TEXCOORD1;
	float3 normW : TEXCOORD2;
};

struct OrbOcclusionPS_IN
{
	float4 posH : SV_POSITION;
};

struct OrbShadowPS_IN
{
	float4 posH : SV_POSITION;
	float4 lightPosH: TEXCOORD0;
};

struct OrbBloomPS_IN
{
	float4 posH : SV_POSITION;
};
	
// VERTEX SHADER
OrbPS_IN OrbVS(OrbVS_IN vIn)
{
	OrbPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);
	vOut.posW = mul(float4(vIn.posL, 1.0f), g_World).xyz;
	vOut.normW = mul(float4(vIn.normL, 0.0f), g_World).xyz;

	return vOut;
} // end OrbVS

OrbBloomPS_IN OrbBloomVS(OrbVS_IN vIn)
{
	OrbBloomPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL * 2.0f, 1.0f), WVP);

	return vOut;
} // end OrbBloomVS

OrbRefractionPS_IN OrbRefractionVS(OrbVS_IN vIn)
{
	OrbRefractionPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);

	vOut.refrTex = vOut.posH;
	vOut.refrTex.xy /= vOut.refrTex.w;
	vOut.refrTex.x = 0.5f * vOut.refrTex.x + 0.5f;
	vOut.refrTex.y = -0.5f * vOut.refrTex.y + 0.5f;

	vOut.posW = mul(float4(vIn.posL, 1.0f), g_World);
	vOut.normW = mul(float4(vIn.normL, 0.0f), g_World);

	return vOut;
} // end OrbRefractionVS

OrbOcclusionPS_IN OrbOcclusionVS(OrbVS_IN vIn)
{
	OrbOcclusionPS_IN vOut;

	float4x4 WVP = mul(g_World, g_View);
	WVP = mul(WVP, g_Projection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), WVP);

	return vOut;
} // end OrbOcclusionVS

OrbShadowPS_IN OrbShadowVS(OrbVS_IN vIn)
{
	OrbShadowPS_IN vOut;

	float4x4 lightWVP = mul(g_World, g_LightView);
	lightWVP = mul(lightWVP, g_LightProjection);

	vOut.posH = mul(float4(vIn.posL, 1.0f), lightWVP);
	vOut.lightPosH = vOut.posH;

	return vOut;
} // end OrbShadowVS

// PIXEL SHADER
float4 OrbPS(OrbPS_IN pIn) : SV_TARGET
{
	SurfaceInfo v = {pIn.posW, pIn.normW, g_OrbColor, float4(1.0f, 1.0f, 1.0f, 32.0f)};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};

	float3 litColor = DirectionalLight(v, light, g_EyePosW, 1.0f);

	// Calculate fog
	float3 toEye = pIn.posW - g_EyePosW;
	float dist = length(toEye);
	toEye = normalize(toEye);

	float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
	float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
	float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
			saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));
	
	litColor = lerp(litColor, fogColor, fogLerp);
	
	return float4(litColor, g_OrbColor.a);
} // end OrbPS

float4 OrbRefractionPS(OrbRefractionPS_IN pIn) : SV_TARGET
{
	float3 toEye = pIn.posW - g_EyePosW;
	float dist = length(toEye);
	toEye = normalize(toEye);

	// Disturb refrarction texture coordinates
	float3 R = reflect(toEye, pIn.normW);
	pIn.refrTex.xy += R.xy * 0.05f;

	float4 refrColor = float4(float3(tex2D(g_RefractionMapSampler, pIn.refrTex).rgb), 1.0f);
	float4 finalColor = lerp(refrColor, g_OrbColor, 0.35f);

	// Add specular lighting
	SurfaceInfo v = {pIn.posW, pIn.normW, g_OrbColor, float4(1.0f, 1.0f, 1.0f, 32.0f)};
	Light light = {g_LightDir, g_LightAmbient, g_LightDiffuse, g_LightSpecular};
	float3 spec = CalcSpecTerm(v, light, g_EyePosW);

	finalColor.rgb += spec;

	// Add sky fog
	float fogLerp = saturate((dist - g_FogStart) / g_FogRange);
	float4 darkSkyColor = float4(g_SkyColor.rgb * g_DarkSkyOffset, 1.0f);
	float4 fogColor = lerp(g_SkyColor, darkSkyColor, 
			saturate(dot(float3(0.0f, 1.0f, 0.0f), toEye) + 0.1f));

	return lerp(finalColor, fogColor, fogLerp);
} // end OrbRefractionPS

float4 OrbOcclusionPS(OrbOcclusionPS_IN pIn) : SV_TARGET
{
	return float4(0.0f, 0.0f, 0.0f, 1.0f);
} // end OrbOcclusionPS

float4 OrbShadowPS(OrbShadowPS_IN pIn) : SV_TARGET
{
	// Output the depth information
	float depth = 1.0f - (pIn.lightPosH.z / pIn.lightPosH.w);
	return float4(depth, 0.0f, 0.0f, 1.0f);
} // end OrbShadowPS

float4 OrbBloomPS(OrbBloomPS_IN pIn) : SV_TARGET
{
	return float4(lerp(g_OrbColor.rgb, float3(0.0f, 0.0f, 0.0f), 0.75f), 1.0f);
}

// TECHNIQUE
technique OrbTech
{
	Pass P0
	{
		VertexShader = compile vs_2_0 OrbVS();
		PixelShader  = compile ps_2_0 OrbPS();
	}
}

technique OrbRefractionTech
{
	Pass P0
	{
		VertexShader = compile vs_2_0 OrbRefractionVS();
		PixelShader  = compile ps_2_0 OrbRefractionPS();
	}
}

technique OrbOcclusionTech
{
	Pass P0
	{
		VertexShader = compile vs_2_0 OrbOcclusionVS();
		PixelShader  = compile ps_2_0 OrbOcclusionPS();
	}
}

technique OrbShadowTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 OrbShadowVS();
		PixelShader  = compile ps_2_0 OrbShadowPS();
	}
}

technique OrbBloomTech
{
	pass P0
	{
		VertexShader = compile vs_2_0 OrbBloomVS();
		PixelShader  = compile ps_2_0 OrbBloomPS();
	}
}