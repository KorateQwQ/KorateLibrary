sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);//消融材质
sampler2D uImage2 : register(s2);//可选：扰动材质
sampler2D uImage3 : register(s3);//可选：内部纹理



float4 iColor = float4(1, 1, 1, 1);

//=====消融效果=====//
float iTimeProgress;//消融进度
float2 iTimeDisolve;
float2 dissolveScale = float2(1.0, 1.0);//消融图大小缩放
float edgeWidth = 0.08;//剪切边缘宽度，越小则消融末端越“实”，越大则消融末端越“虚”
float noiseStrength = 0.22;
float curveStrength = 0.0;//裁切线弯曲强度，0为水平裁切，正值凸起，负值凹下去
float2 radialCenter = float2(0.5, 0.5);//裁切线弯曲中心，默认为图片中心
float2 sweepDirection = float2(0.0, 1.0);//消融方向

//=====扰动效果=====//
float2 iTimeDistort;
float distortStrength = 0.015;//整体扰动强度
float2 distortScale = float2(2.0, 2.0);//扰动图大小缩放

//=====内部纹理=====//
bool iUseInternalTexture = false;
float2 internalTextureScale = float2(1.0, 1.0);
float2 internalTextureOffset = float2(0.0, 0.0);

// 径向消融效果
float4 RadialDissolvePS(float2 uv : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
	float2 distortNoiseUV = uv * distortScale + iTimeDistort;
	float2 distortNoise = tex2D(uImage2, frac(distortNoiseUV)).rg * 2.0 - 1.0;
	float2 finalUV = saturate(uv + distortNoise * distortStrength);
	float4 mainTex = tex2D(uImage0, finalUV) * color;

	float2 dissolveUV = finalUV * dissolveScale + iTimeDisolve;
	float dissolveTex = tex2D(uImage1, frac(dissolveUV)).r;

	float2 dir = normalize(sweepDirection);
	float sweepValue = dot(finalUV - 0.5, dir) + 0.5;
	float2 curveOffset = finalUV - radialCenter;
	float tangentValue = dot(curveOffset, float2(dir.y, -dir.x));
	float curveValue = tangentValue * tangentValue * curveStrength;
	float dissolveField = saturate(sweepValue + curveValue + (dissolveTex - 0.5) * noiseStrength);
	float progress = frac(iTimeProgress) * (1.0 + edgeWidth * 2.0) - edgeWidth;

	float alpha = 1.0 - smoothstep(progress, progress + edgeWidth, dissolveField);
	float visible = 1.0 - alpha;
	mainTex.rgb *= visible;
	mainTex.a *= visible;
	
	if (iUseInternalTexture)
	{
		float2 internalUV = finalUV * internalTextureScale + internalTextureOffset;
		float4 internalTex = tex2D(uImage3, frac(internalUV));
		return mainTex * internalTex.r * iColor;
	}
	
	return mainTex * iColor;
}

technique Technique1
{
	pass Apply
	{
		PixelShader = compile ps_3_0 RadialDissolvePS();
	}
}
