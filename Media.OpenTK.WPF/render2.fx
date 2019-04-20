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

struct OUTPUT
{
	float4 color0 : COLOR0; // Diffuse color
	float4 color1 : COLOR1; // Position
};

float4x4 worldViewProj;
int vpHeight;

texture texture0;
sampler texture0sampler = sampler_state { texture = <texture0>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(input.pos, worldViewProj);
	output.tex0 = input.tex0;

	return output;
}

OUTPUT deinterlace_split(PS_IN input) : SV_Target
{
	OUTPUT output;
	float d = .5 / vpHeight;
	float x = input.tex0.x, y = input.tex0.y;
	//return float4(input.tex0.xy,input.tex0.xy);
	output.color0 = float4(tex2D(texture0sampler, float2(x, y)).rgb, 1);
	output.color1 = float4(tex2D(texture0sampler, float2(x, y + d)).rgb, 1);
	return output;
}
float4 deinterlace_blend(PS_IN input) : SV_Target
{
	float d = .5 / vpHeight;
	float x = input.tex0.x, y = input.tex0.y;
	//return float4(input.tex0.xy,input.tex0.xy);
	float4 color0 = float4(tex2D(texture0sampler, float2(x, y)).rgb, 1);
	float4 color1 = float4(tex2D(texture0sampler, float2(x, y + d)).rgb, 1);
	return (color0 + color1) / 2;
}

technique Main {
	pass P0 {
		VertexShader = compile vs_2_0 VS();
		PixelShader = compile ps_2_0 deinterlace_split();
	}
	pass P1 {
		VertexShader = compile vs_2_0 VS();
		PixelShader = compile ps_2_0 deinterlace_blend();
	}
}
