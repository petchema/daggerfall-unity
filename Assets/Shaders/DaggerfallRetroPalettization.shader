Shader "Daggerfall/RetroPalettization"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
       Lighting Off 
		Cull Off ZWrite Off ZTest Always
       Fog { Mode Off } 

		Pass
		{
			CGPROGRAM
           #pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
            
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

// Beginning of generated code
// See https://github.com/petchema/ocaml-palettisation-shader-generator

// ART_PAL+NIGHTSKY_blues.txt - 286 unique colors - max color cluster size 20
fixed3 targetColor;
fixed4 best;

void tryColor(fixed3 color)
{
  fixed dist = dot(targetColor - color, targetColor - color);
  if (dist < best.a) best = fixed4(color, dist);
}

// 18 colors in (0,0,0)-(33,46,52)
const static fixed3 paletteLLLL[18] = {
  fixed3(31.0/255,31.0/255,31.0/255),
  fixed3(27.0/255,27.0/255,27.0/255),
  fixed3(25.0/255,25.0/255,49.0/255),
  fixed3(24.0/255,24.0/255,46.0/255),
  fixed3(23.0/255,23.0/255,42.0/255),
  fixed3(23.0/255,23.0/255,38.0/255),
  fixed3(23.0/255,23.0/255,23.0/255),
  fixed3(22.0/255,22.0/255,35.0/255),
  fixed3(22.0/255,22.0/255,31.0/255),
  fixed3(21.0/255,21.0/255,27.0/255),
  fixed3(21.0/255,21.0/255,24.0/255),
  fixed3(20.0/255,20.0/255,20.0/255),
  fixed3(19.0/255,19.0/255,19.0/255),
  fixed3(15.0/255,15.0/255,15.0/255),
  fixed3(0.0/255,0.0/255,48.0/255),
  fixed3(0.0/255,0.0/255,44.0/255),
  fixed3(0.0/255,0.0/255,39.0/255),
  fixed3(0.0/255,0.0/255,0.0/255),
};

void findColorLLLL()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteLLLL[i]);
}

// 18 colors in (34,0,0)-(110,46,52)
const static fixed3 paletteLLLR[18] = {
  fixed3(35.0/255,35.0/255,35.0/255),
  fixed3(35.0/255,23.0/255,11.0/255),
  fixed3(38.0/255,38.0/255,38.0/255),
  fixed3(40.0/255,40.0/255,40.0/255),
  fixed3(43.0/255,46.0/255,45.0/255),
  fixed3(44.0/255,44.0/255,44.0/255),
  fixed3(44.0/255,44.0/255,45.0/255),
  fixed3(46.0/255,44.0/255,46.0/255),
  fixed3(47.0/255,31.0/255,15.0/255),
  fixed3(50.0/255,45.0/255,34.0/255),
  fixed3(56.0/255,45.0/255,52.0/255),
  fixed3(57.0/255,43.0/255,39.0/255),
  fixed3(59.0/255,39.0/255,19.0/255),
  fixed3(65.0/255,41.0/255,33.0/255),
  fixed3(74.0/255,39.0/255,27.0/255),
  fixed3(83.0/255,32.0/255,10.0/255),
  fixed3(92.0/255,33.0/255,3.0/255),
  fixed3(102.0/255,33.0/255,1.0/255),
};

void findColorLLLR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteLLLR[i]);
}

void findColorLLL() // 36 colors in (0,0,0)-(110,46,52)
{
  fixed diff = targetColor.r - 34.0/255;
  if (diff >= 0)
  {
    findColorLLLR();
    if (best.a > diff * diff) findColorLLLL();
  }
  else
  {
    findColorLLLL();
    if (best.a > diff * diff) findColorLLLR();
  }
}

// 17 colors in (0,47,0)-(110,61,52)
const static fixed3 paletteLLRL[17] = {
  fixed3(69.0/255,60.0/255,21.0/255),
  fixed3(39.0/255,60.0/255,39.0/255),
  fixed3(43.0/255,56.0/255,39.0/255),
  fixed3(87.0/255,55.0/255,27.0/255),
  fixed3(30.0/255,55.0/255,30.0/255),
  fixed3(61.0/255,54.0/255,38.0/255),
  fixed3(66.0/255,54.0/255,41.0/255),
  fixed3(58.0/255,51.0/255,25.0/255),
  fixed3(34.0/255,51.0/255,34.0/255),
  fixed3(38.0/255,51.0/255,40.0/255),
  fixed3(51.0/255,51.0/255,51.0/255),
  fixed3(54.0/255,50.0/255,40.0/255),
  fixed3(45.0/255,48.0/255,48.0/255),
  fixed3(44.0/255,48.0/255,49.0/255),
  fixed3(48.0/255,48.0/255,50.0/255),
  fixed3(75.0/255,47.0/255,23.0/255),
  fixed3(40.0/255,47.0/255,40.0/255),
};

void findColorLLRL()
{
  for (int i = 0; i < 17; i++)
    tryColor(paletteLLRL[i]);
}

// 18 colors in (0,62,0)-(110,255,52)
const static fixed3 paletteLLRR[18] = {
  fixed3(79.0/255,63.0/255,43.0/255),
  fixed3(69.0/255,63.0/255,42.0/255),
  fixed3(99.0/255,63.0/255,31.0/255),
  fixed3(103.0/255,64.0/255,39.0/255),
  fixed3(45.0/255,64.0/255,37.0/255),
  fixed3(91.0/255,67.0/255,38.0/255),
  fixed3(46.0/255,68.0/255,37.0/255),
  fixed3(81.0/255,69.0/255,18.0/255),
  fixed3(83.0/255,71.0/255,44.0/255),
  fixed3(51.0/255,75.0/255,35.0/255),
  fixed3(96.0/255,76.0/255,51.0/255),
  fixed3(52.0/255,77.0/255,45.0/255),
  fixed3(93.0/255,78.0/255,14.0/255),
  fixed3(53.0/255,87.0/255,34.0/255),
  fixed3(104.0/255,87.0/255,11.0/255),
  fixed3(65.0/255,98.0/255,37.0/255),
  fixed3(76.0/255,108.0/255,42.0/255),
  fixed3(84.0/255,118.0/255,48.0/255),
};

void findColorLLRR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteLLRR[i]);
}

void findColorLLR() // 35 colors in (0,47,0)-(110,255,52)
{
  fixed diff = targetColor.g - 62.0/255;
  if (diff >= 0)
  {
    findColorLLRR();
    if (best.a > diff * diff) findColorLLRL();
  }
  else
  {
    findColorLLRL();
    if (best.a > diff * diff) findColorLLRR();
  }
}

void findColorLL() // 71 colors in (0,0,0)-(110,255,52)
{
  fixed diff = targetColor.g - 47.0/255;
  if (diff >= 0)
  {
    findColorLLR();
    if (best.a > diff * diff) findColorLLL();
  }
  else
  {
    findColorLLL();
    if (best.a > diff * diff) findColorLLR();
  }
}

// 18 colors in (0,0,53)-(110,27,255)
const static fixed3 paletteLRLL[18] = {
  fixed3(27.0/255,27.0/255,64.0/255),
  fixed3(27.0/255,27.0/255,67.0/255),
  fixed3(26.0/255,26.0/255,56.0/255),
  fixed3(26.0/255,26.0/255,60.0/255),
  fixed3(25.0/255,25.0/255,53.0/255),
  fixed3(0.0/255,0.0/255,53.0/255),
  fixed3(0.0/255,0.0/255,57.0/255),
  fixed3(0.0/255,0.0/255,62.0/255),
  fixed3(0.0/255,0.0/255,67.0/255),
  fixed3(0.0/255,0.0/255,71.0/255),
  fixed3(0.0/255,0.0/255,76.0/255),
  fixed3(0.0/255,0.0/255,80.0/255),
  fixed3(0.0/255,0.0/255,85.0/255),
  fixed3(0.0/255,0.0/255,90.0/255),
  fixed3(0.0/255,0.0/255,94.0/255),
  fixed3(0.0/255,0.0/255,99.0/255),
  fixed3(0.0/255,0.0/255,103.0/255),
  fixed3(0.0/255,0.0/255,108.0/255),
};

void findColorLRLL()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteLRLL[i]);
}

// 18 colors in (0,28,53)-(110,73,255)
const static fixed3 paletteLRLR[18] = {
  fixed3(28.0/255,28.0/255,71.0/255),
  fixed3(63.0/255,47.0/255,56.0/255),
  fixed3(67.0/255,51.0/255,63.0/255),
  fixed3(75.0/255,52.0/255,71.0/255),
  fixed3(53.0/255,53.0/255,59.0/255),
  fixed3(50.0/255,55.0/255,55.0/255),
  fixed3(86.0/255,58.0/255,77.0/255),
  fixed3(58.0/255,58.0/255,58.0/255),
  fixed3(47.0/255,59.0/255,60.0/255),
  fixed3(61.0/255,61.0/255,67.0/255),
  fixed3(50.0/255,62.0/255,73.0/255),
  fixed3(47.0/255,63.0/255,63.0/255),
  fixed3(101.0/255,65.0/255,96.0/255),
  fixed3(67.0/255,67.0/255,67.0/255),
  fixed3(68.0/255,68.0/255,80.0/255),
  fixed3(109.0/255,69.0/255,102.0/255),
  fixed3(52.0/255,69.0/255,87.0/255),
  fixed3(45.0/255,72.0/255,72.0/255),
};

void findColorLRLR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteLRLR[i]);
}

void findColorLRL() // 36 colors in (0,0,53)-(110,73,255)
{
  fixed diff = targetColor.g - 28.0/255;
  if (diff >= 0)
  {
    findColorLRLR();
    if (best.a > diff * diff) findColorLRLL();
  }
  else
  {
    findColorLRLL();
    if (best.a > diff * diff) findColorLRLR();
  }
}

// 18 colors in (0,74,53)-(110,255,105)
const static fixed3 paletteLRRL[18] = {
  fixed3(46.0/255,103.0/255,103.0/255),
  fixed3(51.0/255,77.0/255,102.0/255),
  fixed3(99.0/255,99.0/255,99.0/255),
  fixed3(85.0/255,85.0/255,96.0/255),
  fixed3(93.0/255,130.0/255,94.0/255),
  fixed3(39.0/255,91.0/255,91.0/255),
  fixed3(87.0/255,87.0/255,87.0/255),
  fixed3(82.0/255,116.0/255,86.0/255),
  fixed3(75.0/255,75.0/255,85.0/255),
  fixed3(40.0/255,83.0/255,83.0/255),
  fixed3(77.0/255,110.0/255,78.0/255),
  fixed3(78.0/255,78.0/255,78.0/255),
  fixed3(68.0/255,99.0/255,67.0/255),
  fixed3(109.0/255,146.0/255,66.0/255),
  fixed3(101.0/255,137.0/255,60.0/255),
  fixed3(92.0/255,127.0/255,54.0/255),
  fixed3(109.0/255,85.0/255,54.0/255),
  fixed3(61.0/255,89.0/255,53.0/255),
};

void findColorLRRL()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteLRRL[i]);
}

// 18 colors in (0,74,106)-(110,255,255)
const static fixed3 paletteLRRR[18] = {
  fixed3(94.0/255,94.0/255,109.0/255),
  fixed3(107.0/255,144.0/255,109.0/255),
  fixed3(110.0/255,110.0/255,110.0/255),
  fixed3(54.0/255,112.0/255,112.0/255),
  fixed3(103.0/255,103.0/255,116.0/255),
  fixed3(45.0/255,82.0/255,122.0/255),
  fixed3(62.0/255,124.0/255,124.0/255),
  fixed3(70.0/255,135.0/255,135.0/255),
  fixed3(49.0/255,90.0/255,142.0/255),
  fixed3(77.0/255,142.0/255,142.0/255),
  fixed3(55.0/255,97.0/255,154.0/255),
  fixed3(87.0/255,154.0/255,154.0/255),
  fixed3(62.0/255,105.0/255,167.0/255),
  fixed3(109.0/255,170.0/255,170.0/255),
  fixed3(68.0/255,112.0/255,179.0/255),
  fixed3(68.0/255,124.0/255,192.0/255),
  fixed3(87.0/255,137.0/255,205.0/255),
  fixed3(104.0/255,152.0/255,217.0/255),
};

void findColorLRRR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteLRRR[i]);
}

void findColorLRR() // 36 colors in (0,74,53)-(110,255,255)
{
  fixed diff = targetColor.b - 106.0/255;
  if (diff >= 0)
  {
    findColorLRRR();
    if (best.a > diff * diff) findColorLRRL();
  }
  else
  {
    findColorLRRL();
    if (best.a > diff * diff) findColorLRRR();
  }
}

void findColorLR() // 72 colors in (0,0,53)-(110,255,255)
{
  fixed diff = targetColor.g - 74.0/255;
  if (diff >= 0)
  {
    findColorLRR();
    if (best.a > diff * diff) findColorLRL();
  }
  else
  {
    findColorLRL();
    if (best.a > diff * diff) findColorLRR();
  }
}

void findColorL() // 143 colors in (0,0,0)-(110,255,255)
{
  fixed diff = targetColor.b - 53.0/255;
  if (diff >= 0)
  {
    findColorLR();
    if (best.a > diff * diff) findColorLL();
  }
  else
  {
    findColorLL();
    if (best.a > diff * diff) findColorLR();
  }
}

// 18 colors in (111,0,0)-(148,106,94)
const static fixed3 paletteRLLL[18] = {
  fixed3(147.0/255,12.0/255,4.0/255),
  fixed3(147.0/255,71.0/255,47.0/255),
  fixed3(143.0/255,87.0/255,51.0/255),
  fixed3(140.0/255,86.0/255,55.0/255),
  fixed3(139.0/255,83.0/255,43.0/255),
  fixed3(134.0/255,103.0/255,65.0/255),
  fixed3(130.0/255,22.0/255,0.0/255),
  fixed3(129.0/255,79.0/255,48.0/255),
  fixed3(127.0/255,75.0/255,39.0/255),
  fixed3(127.0/255,106.0/255,4.0/255),
  fixed3(123.0/255,92.0/255,60.0/255),
  fixed3(122.0/255,75.0/255,43.0/255),
  fixed3(118.0/255,105.0/255,93.0/255),
  fixed3(116.0/255,97.0/255,7.0/255),
  fixed3(115.0/255,67.0/255,35.0/255),
  fixed3(112.0/255,70.0/255,40.0/255),
  fixed3(112.0/255,94.0/255,72.0/255),
  fixed3(111.0/255,34.0/255,0.0/255),
};

void findColorRLLL()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteRLLL[i]);
}

// 18 colors in (149,0,0)-(255,106,94)
const static fixed3 paletteRLLR[18] = {
  fixed3(151.0/255,91.0/255,55.0/255),
  fixed3(152.0/255,93.0/255,63.0/255),
  fixed3(154.0/255,24.0/255,8.0/255),
  fixed3(155.0/255,95.0/255,59.0/255),
  fixed3(155.0/255,91.0/255,47.0/255),
  fixed3(155.0/255,75.0/255,51.0/255),
  fixed3(159.0/255,99.0/255,63.0/255),
  fixed3(162.0/255,36.0/255,12.0/255),
  fixed3(163.0/255,79.0/255,59.0/255),
  fixed3(165.0/255,100.0/255,70.0/255),
  fixed3(167.0/255,103.0/255,67.0/255),
  fixed3(169.0/255,48.0/255,15.0/255),
  fixed3(175.0/255,95.0/255,75.0/255),
  fixed3(175.0/255,87.0/255,67.0/255),
  fixed3(176.0/255,60.0/255,19.0/255),
  fixed3(183.0/255,72.0/255,23.0/255),
  fixed3(190.0/255,84.0/255,27.0/255),
  fixed3(198.0/255,95.0/255,31.0/255),
};

void findColorRLLR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteRLLR[i]);
}

void findColorRLL() // 36 colors in (111,0,0)-(255,106,94)
{
  fixed diff = targetColor.r - 149.0/255;
  if (diff >= 0)
  {
    findColorRLLR();
    if (best.a > diff * diff) findColorRLLL();
  }
  else
  {
    findColorRLLL();
    if (best.a > diff * diff) findColorRLLR();
  }
}

// 18 colors in (111,107,0)-(255,140,94)
const static fixed3 paletteRLRL[18] = {
  fixed3(183.0/255,140.0/255,88.0/255),
  fixed3(154.0/255,133.0/255,0.0/255),
  fixed3(219.0/255,131.0/255,43.0/255),
  fixed3(164.0/255,130.0/255,67.0/255),
  fixed3(173.0/255,127.0/255,78.0/255),
  fixed3(137.0/255,121.0/255,94.0/255),
  fixed3(212.0/255,119.0/255,39.0/255),
  fixed3(160.0/255,118.0/255,74.0/255),
  fixed3(139.0/255,115.0/255,0.0/255),
  fixed3(179.0/255,115.0/255,79.0/255),
  fixed3(132.0/255,114.0/255,82.0/255),
  fixed3(180.0/255,113.0/255,80.0/255),
  fixed3(137.0/255,112.0/255,66.0/255),
  fixed3(175.0/255,111.0/255,75.0/255),
  fixed3(151.0/255,110.0/255,69.0/255),
  fixed3(205.0/255,107.0/255,35.0/255),
  fixed3(171.0/255,107.0/255,71.0/255),
  fixed3(179.0/255,107.0/255,83.0/255),
};

void findColorRLRL()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteRLRL[i]);
}

// 18 colors in (111,141,0)-(255,255,94)
const static fixed3 paletteRLRR[18] = {
  fixed3(164.0/255,141.0/255,94.0/255),
  fixed3(226.0/255,143.0/255,46.0/255),
  fixed3(185.0/255,148.0/255,76.0/255),
  fixed3(168.0/255,150.0/255,0.0/255),
  fixed3(234.0/255,155.0/255,50.0/255),
  fixed3(206.0/255,159.0/255,73.0/255),
  fixed3(130.0/255,162.0/255,77.0/255),
  fixed3(241.0/255,167.0/255,54.0/255),
  fixed3(183.0/255,168.0/255,0.0/255),
  fixed3(255.0/255,175.0/255,83.0/255),
  fixed3(228.0/255,178.0/255,80.0/255),
  fixed3(197.0/255,185.0/255,0.0/255),
  fixed3(247.0/255,189.0/255,79.0/255),
  fixed3(255.0/255,197.0/255,86.0/255),
  fixed3(212.0/255,203.0/255,0.0/255),
  fixed3(255.0/255,206.0/255,90.0/255),
  fixed3(226.0/255,220.0/255,0.0/255),
  fixed3(241.0/255,238.0/255,45.0/255),
};

void findColorRLRR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteRLRR[i]);
}

void findColorRLR() // 36 colors in (111,107,0)-(255,255,94)
{
  fixed diff = targetColor.g - 141.0/255;
  if (diff >= 0)
  {
    findColorRLRR();
    if (best.a > diff * diff) findColorRLRL();
  }
  else
  {
    findColorRLRL();
    if (best.a > diff * diff) findColorRLRR();
  }
}

void findColorRL() // 72 colors in (111,0,0)-(255,255,94)
{
  fixed diff = targetColor.g - 107.0/255;
  if (diff >= 0)
  {
    findColorRLR();
    if (best.a > diff * diff) findColorRLL();
  }
  else
  {
    findColorRLL();
    if (best.a > diff * diff) findColorRLR();
  }
}

// 17 colors in (111,0,95)-(191,158,255)
const static fixed3 paletteRRLL[17] = {
  fixed3(123.0/255,156.0/255,118.0/255),
  fixed3(165.0/255,156.0/255,156.0/255),
  fixed3(147.0/255,147.0/255,147.0/255),
  fixed3(161.0/255,147.0/255,125.0/255),
  fixed3(145.0/255,145.0/255,159.0/255),
  fixed3(135.0/255,135.0/255,149.0/255),
  fixed3(132.0/255,132.0/255,132.0/255),
  fixed3(140.0/255,129.0/255,119.0/255),
  fixed3(188.0/255,127.0/255,158.0/255),
  fixed3(122.0/255,122.0/255,137.0/255),
  fixed3(119.0/255,119.0/255,119.0/255),
  fixed3(132.0/255,119.0/255,107.0/255),
  fixed3(114.0/255,114.0/255,127.0/255),
  fixed3(175.0/255,111.0/255,144.0/255),
  fixed3(155.0/255,98.0/255,130.0/255),
  fixed3(143.0/255,84.0/255,119.0/255),
  fixed3(127.0/255,77.0/255,106.0/255),
};

void findColorRRLL()
{
  for (int i = 0; i < 17; i++)
    tryColor(paletteRRLL[i]);
}

// 18 colors in (111,159,95)-(191,255,255)
const static fixed3 paletteRRLR[18] = {
  fixed3(179.0/255,160.0/255,121.0/255),
  fixed3(162.0/255,162.0/255,162.0/255),
  fixed3(176.0/255,164.0/255,148.0/255),
  fixed3(123.0/255,164.0/255,230.0/255),
  fixed3(165.0/255,165.0/255,174.0/255),
  fixed3(186.0/255,174.0/255,147.0/255),
  fixed3(174.0/255,174.0/255,174.0/255),
  fixed3(148.0/255,176.0/255,141.0/255),
  fixed3(159.0/255,183.0/255,101.0/255),
  fixed3(185.0/255,185.0/255,185.0/255),
  fixed3(147.0/255,185.0/255,244.0/255),
  fixed3(134.0/255,187.0/255,187.0/255),
  fixed3(188.0/255,188.0/255,199.0/255),
  fixed3(175.0/255,200.0/255,168.0/255),
  fixed3(158.0/255,202.0/255,202.0/255),
  fixed3(185.0/255,205.0/255,127.0/255),
  fixed3(176.0/255,205.0/255,255.0/255),
  fixed3(182.0/255,218.0/255,227.0/255),
};

void findColorRRLR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteRRLR[i]);
}

void findColorRRL() // 35 colors in (111,0,95)-(191,255,255)
{
  fixed diff = targetColor.g - 159.0/255;
  if (diff >= 0)
  {
    findColorRRLR();
    if (best.a > diff * diff) findColorRRLL();
  }
  else
  {
    findColorRRLL();
    if (best.a > diff * diff) findColorRRLR();
  }
}

// 18 colors in (192,0,95)-(255,255,132)
const static fixed3 paletteRRRL[18] = {
  fixed3(255.0/255,229.0/255,129.0/255),
  fixed3(222.0/255,198.0/255,128.0/255),
  fixed3(213.0/255,174.0/255,128.0/255),
  fixed3(255.0/255,205.0/255,127.0/255),
  fixed3(231.0/255,206.0/255,123.0/255),
  fixed3(231.0/255,198.0/255,122.0/255),
  fixed3(207.0/255,152.0/255,118.0/255),
  fixed3(247.0/255,206.0/255,115.0/255),
  fixed3(239.0/255,206.0/255,115.0/255),
  fixed3(255.0/255,195.0/255,112.0/255),
  fixed3(255.0/255,206.0/255,107.0/255),
  fixed3(247.0/255,206.0/255,107.0/255),
  fixed3(196.0/255,154.0/255,105.0/255),
  fixed3(255.0/255,246.0/255,103.0/255),
  fixed3(193.0/255,133.0/255,100.0/255),
  fixed3(255.0/255,206.0/255,99.0/255),
  fixed3(255.0/255,198.0/255,99.0/255),
  fixed3(255.0/255,185.0/255,98.0/255),
};

void findColorRRRL()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteRRRL[i]);
}

// 18 colors in (192,0,133)-(255,255,255)
const static fixed3 paletteRRRR[18] = {
  fixed3(208.0/255,185.0/255,134.0/255),
  fixed3(255.0/255,215.0/255,141.0/255),
  fixed3(227.0/255,180.0/255,144.0/255),
  fixed3(229.0/255,193.0/255,150.0/255),
  fixed3(254.0/255,225.0/255,156.0/255),
  fixed3(216.0/255,227.0/255,162.0/255),
  fixed3(244.0/255,202.0/255,167.0/255),
  fixed3(204.0/255,146.0/255,170.0/255),
  fixed3(254.0/255,235.0/255,170.0/255),
  fixed3(245.0/255,212.0/255,172.0/255),
  fixed3(254.0/255,245.0/255,185.0/255),
  fixed3(220.0/255,166.0/255,188.0/255),
  fixed3(202.0/255,221.0/255,196.0/255),
  fixed3(197.0/255,197.0/255,197.0/255),
  fixed3(254.0/255,255.0/255,199.0/255),
  fixed3(232.0/255,188.0/255,200.0/255),
  fixed3(220.0/255,220.0/255,220.0/255),
  fixed3(205.0/255,205.0/255,224.0/255),
};

void findColorRRRR()
{
  for (int i = 0; i < 18; i++)
    tryColor(paletteRRRR[i]);
}

void findColorRRR() // 36 colors in (192,0,95)-(255,255,255)
{
  fixed diff = targetColor.b - 133.0/255;
  if (diff >= 0)
  {
    findColorRRRR();
    if (best.a > diff * diff) findColorRRRL();
  }
  else
  {
    findColorRRRL();
    if (best.a > diff * diff) findColorRRRR();
  }
}

void findColorRR() // 71 colors in (111,0,95)-(255,255,255)
{
  fixed diff = targetColor.r - 192.0/255;
  if (diff >= 0)
  {
    findColorRRR();
    if (best.a > diff * diff) findColorRRL();
  }
  else
  {
    findColorRRL();
    if (best.a > diff * diff) findColorRRR();
  }
}

void findColorR() // 143 colors in (111,0,0)-(255,255,255)
{
  fixed diff = targetColor.b - 95.0/255;
  if (diff >= 0)
  {
    findColorRR();
    if (best.a > diff * diff) findColorRL();
  }
  else
  {
    findColorRL();
    if (best.a > diff * diff) findColorRR();
  }
}

void findColor() // 286 colors in (0,0,0)-(255,255,255)
{
  fixed diff = targetColor.r - 111.0/255;
  if (diff >= 0)
  {
    findColorR();
    if (best.a > diff * diff) findColorL();
  }
  else
  {
    findColorL();
    if (best.a > diff * diff) findColorR();
  }
}

fixed4 nearestColor(fixed3 color)
{
  targetColor = color;
  best = fixed4(0.0, 0.0, 0.0, 2.0);
  findColor();
  best.a = 1.0;
  return best;
}

// End of generated code

	        fixed4 frag (v2f i) : SV_Target
	        {
                fixed4 target = tex2D(_MainTex, i.uv);
                
                // ART_PAL.COL
                return nearestColor(target);
            }
			ENDCG
		}
	}
}
