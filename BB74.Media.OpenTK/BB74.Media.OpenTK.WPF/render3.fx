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
	float2 tex0 : TEXCOORD0;
	float2 tex1 : TEXCOORD1;
};

float4x4 worldViewProj;
//float4x4 texturematrix;
int vpHeight;

texture texture0;
sampler texture0sampler = sampler_state { texture = <texture0>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

texture texture1;
sampler texture1sampler = sampler_state { texture = <texture1>; magfilter = POINT; minfilter = POINT; mipfilter = POINT; AddressU = clamp; AddressV = clamp; };

PS_IN VS(VS_IN input)
{
	PS_IN output;//  = (PS_IN)0;

	output.pos = mul(input.pos, worldViewProj);
	output.tex0 = input.tex0;
	output.tex1 = output.pos.xy;

	return output;
}

float4 combine(PS_IN input) : SV_Target
{
	float v = round(vpHeight*(-input.tex1.y+1)/2);
	float4 c;
	if (round(v /2) == v /2)
	{
		c = tex2D(texture1sampler, input.tex0);
	}
	else
	{
		c = tex2D(texture0sampler, input.tex0);
	}
	return c;
}

technique Main {
	pass P0 {
		VertexShader = compile vs_2_0 VS();
		PixelShader = compile ps_2_0 combine();
	}
}
