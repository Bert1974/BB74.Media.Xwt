// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
struct VS_IN
{
	float4 pos : POSITION;
	float2 tex0 : TEXCOORD;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float2 tex0 : TEXCOORD;
};

float4x4 worldViewProj;
float alpha, fade;

texture texture0;
sampler texture0sampler = sampler_state { texture = <texture0>; magfilter = ANISOTROPIC; minfilter = ANISOTROPIC; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };
texture texture1;
sampler texture1sampler = sampler_state { texture = <texture1>; magfilter = ANISOTROPIC; minfilter = ANISOTROPIC; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(input.pos, worldViewProj);
	output.tex0 = input.tex0;

	return output;
}
PS_IN VS2(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(input.pos, worldViewProj);
	output.tex0 = input.tex0;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	//return float4(input.tex0.xy,input.tex0.xy);
	float4 c = tex2D(texture0sampler, input.tex0);
	return c*alpha;
}
float4 PS2(PS_IN input) : SV_Target
{
	//return input.color;// input.tex0.xy,input.tex0.xy);
	float4 c = tex2D(texture0sampler, input.tex0);
	return float4(c.rgb,1);
}

float4 GetColor(float4 c)
{
	if (c.a > 0)
	{
		return float4(c.rgb / c.a, c.a);

	}
	return float4(0, 0, 0, 0);
}

float4 GetColor1(PS_IN input)
{
	return GetColor(tex2D(texture0sampler, input.tex0));
}

float4 GetColor2(PS_IN input)
{
	return GetColor(tex2D(texture1sampler, input.tex0));
}

float4 crossefade(PS_IN input ) : SV_TARGET
{
	float4 c1 = GetColor1(input);
	float4 c2 = GetColor2(input);

	float a1 = c1.a*(1 - fade), a2 = c2.a*fade;
	float ta = a1 + a2;

	return float4(lerp(c1.rgb / c1.a, c2.rgb / c2.a, a2 / ta), 1)*ta;
}

technique Main {
	pass P0 {
		CullMode = NONE;
		VertexShader = compile vs_2_0 VS();
		PixelShader = compile ps_2_0 PS();
	}
	pass P1 {
		CullMode = NONE;
		ZEnable = false;
		AlphaBlendEnable = true;
		SEPARATEALPHABLENDENABLE = true;
		SrcBlend = invdestalpha;
		DestBlend = one;
		BlendOp = add;
		SrcBlendAlpha = invdestalpha;
		DestBlendAlpha = one;
		BlendOpAlpha = add;
		VertexShader = compile vs_2_0 VS2();
		PixelShader = compile ps_2_0 PS();
	}
	pass P2 {
		CullMode = NONE;
		VertexShader = compile vs_2_0 VS();
		PixelShader = compile ps_2_0 crossefade();
	}
}