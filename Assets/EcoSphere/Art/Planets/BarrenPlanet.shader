Shader "EcoSphere/Barren Planet"
{
    Properties
    {
        _BaseMap ("Barren terrain", 2D) = "white" {}
        _HeightMap ("Elevation (linear)", 2D) = "gray" {}
        _SeaLevel ("Sea level", Range(-0.02, 1.02)) = -0.02
        _WaterColor ("Deep water", Color) = (0.025, 0.16, 0.32, 1)
        _StoneRuggedness ("Stone ruggedness", Range(0,1)) = 0
        _IceCoverage ("Polar ice", Range(0,1)) = 0
        _FireHeat ("Fire heat", Range(0,1)) = 0
        _Temperature ("Temperature (cold to hot)",Range(-1,1))=0
        _FreezeStart ("Freezing onset",Range(-1,0))=0
        _FullyFrozen ("Fully frozen temperature",Range(-1,0))=-1
        _GeologicalActivity ("Geological activity",Range(0,1))=0
        _Sediment ("Sediment",Range(0,1))=0
        _SandColor ("Sand color (sediment)",Color)=(0.93,0.85,0.64,1)
        _SandReach ("Sand reach: x at first sediment, y at full (share of land height); z slope resistance",Vector)=(0.04,1.05,0.35,0)
        _SandEdge ("Sand edge softness",Range(0.005,0.3))=0.05
        _Soil ("Soil",Range(0,1))=0
        _Moisture ("Moisture",Range(0,1))=0
        _Vegetation ("Vegetation growth",Range(0,1))=0
        _WaterTint ("Water color trait (rgb color, a amount)",Color)=(0,0,0,0)
        _VegetationTint ("Vegetation color trait (rgb color, a amount)",Color)=(0,0,0,0)
        _LandTint ("Land color trait (rgb color, a amount)",Color)=(0,0,0,0)
        _CityLightsTint ("City lights color trait (rgb color, a amount)",Color)=(0,0,0,0)
        _CityLightsMask ("City lights, scattered settlements (linear)",2D)="black" {}
        _CityLightsDenseMask ("City lights, dense urban networks (linear)",2D)="black" {}
        _CityLightsColor ("Default city light color",Color)=(1,0.72,0.38,1)
        _CityLightsBrightness ("City light brightness (without technology, at full intensity)",Vector)=(0.6,3,0,0)
        _CityLightsDayVisibility ("City light visibility on the lit side",Range(0,1))=0.75
        _CityLightsUrbanShade ("Darkening of built-up land under the lights (daylight contrast)",Range(0,1))=0.35
        _CityLightsTiling ("City light mask repeats around the globe (x, y)",Vector)=(3,3,0,0)
        _CityLightsSharpness ("City light points (mask floor, gain, scattered settlement boost)",Vector)=(0.04,1.6,2.5,0)
        _Civilization ("Civilization (settlement coverage)",Range(0,1))=0
        _CityLightIntensity ("City light intensity",Range(0,1))=0
        _TerrainBounds ("Terrain elevation bounds",Vector)=(0,1,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);
            TEXTURE2D(_CityLightsMask); SAMPLER(sampler_CityLightsMask);
            TEXTURE2D(_CityLightsDenseMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _WaterColor;
                float _SeaLevel;
                float _StoneRuggedness;
                float _IceCoverage;
                float _FireHeat;
                float _Temperature, _GeologicalActivity;
                float _FreezeStart, _FullyFrozen;
                float _Sediment, _Soil, _Moisture;
                half4 _SandColor;
                float4 _SandReach;
                float _SandEdge;
                float _Vegetation;
                half4 _WaterTint, _VegetationTint, _LandTint, _CityLightsTint, _CityLightsColor;
                float4 _CityLightsBrightness;
                float _CityLightsDayVisibility, _CityLightsUrbanShade;
                float4 _CityLightsTiling, _CityLightsSharpness;
                float _Civilization, _CityLightIntensity;
                float4 _TerrainBounds;
            CBUFFER_END
            // Global, set by PlanetDayNight: 0 = day view, 1 = night view (luminescent effects at full night strength everywhere).
            float _PlanetNight, _PlanetNightAmbientDim;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; float3 positionWS : TEXCOORD2; half4 tangentWS : TEXCOORD3; float3 planetDirection : TEXCOORD4; };
            float IceHash(float3 p)
            {
                p = frac(p * .1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float IceNoise(float3 p)
            {
                float3 cell = floor(p), f = frac(p);
                f = f*f*(3-2*f);
                return lerp(lerp(lerp(IceHash(cell),IceHash(cell+float3(1,0,0)),f.x),
                    lerp(IceHash(cell+float3(0,1,0)),IceHash(cell+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(IceHash(cell+float3(0,0,1)),IceHash(cell+float3(1,0,1)),f.x),
                    lerp(IceHash(cell+float3(0,1,1)),IceHash(cell+float3(1,1,1)),f.x),f.y),f.z);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = half4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w * GetOddNegativeScale());
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.planetDirection = input.normalOS;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half elevation = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.uv).r;
                half3 rock = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                // Broad relief follows elevation; fine outcrops follow the existing rock texture.
                float2 stepUV = float2(1.0/256, 1.0/128);
                half east = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.uv + float2(stepUV.x,0)).r;
                half north = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.uv + float2(0,stepUV.y)).r;
                half fineEast = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + float2(stepUV.x,0)).r - rock.r;
                half fineNorth = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + float2(0,stepUV.y)).r - rock.r;
                half2 slope = half2(east-elevation,north-elevation);
                half exposed = saturate(length(slope)*16 + smoothstep(.45,.8,elevation)*.4);
                rock = lerp(rock, lerp(rock*.72, half3(.48,.45,.41), exposed), _StoneRuggedness*.7);
                // Biological soil values are modest: make small contributions visibly warm the land.
                // Recolor luminance rather than just darkening gray, retaining the source rock detail.
                half soilTint=(1-exp2(-8*saturate(_Soil)))*.9;
                half rockLuminance=dot(rock,half3(.2126,.7152,.0722));
                half3 soilColor=rockLuminance*half3(1.18,.72,.38);
                rock=lerp(rock,soilColor,soilTint);
                // Land color trait: recolor luminance toward the blended element hue. An sRGB mid-gray hue (linear .214) keeps brightness;
                // dark hues (coal) darken and pale ones (salt) brighten, while the rock detail survives.
                half landLuminance=dot(rock,half3(.2126,.7152,.0722));
                rock=lerp(rock,landLuminance*_LandTint.rgb*4.67,saturate(_LandTint.a));
                half heat=saturate(_Temperature);
                half scorch=heat*.15 + smoothstep(.25,.95,heat)*.85;
                rock=lerp(rock,rock*half3(1.16,.77,.56),heat*.65);
                rock=lerp(rock,rock*half3(.25,.20,.19),scorch*.8);
                // Sand (sediment) starts at the shoreline and climbs inland as sediment grows: a little gives beaches,
                // a lot gives sandy plains and deserts. Steep slopes and peaks hold out longest; a little noise keeps the
                // edge from tracing the contours exactly. Applied after heat so hot sandy worlds keep pale sand.
                float shoreLevel=max(_SeaLevel,_TerrainBounds.x);
                half shoreHeight=saturate((elevation-shoreLevel)/max(.05,_TerrainBounds.y-shoreLevel));
                half sediment=saturate(_Sediment);
                half sandReach=lerp(_SandReach.x,_SandReach.y,sediment);
                float3 sandDirection=normalize(input.planetDirection);
                half sandEdge=(IceNoise(sandDirection*17+2.3)-.5)*_SandEdge*2;
                half sand=(1-smoothstep(sandReach-_SandEdge,sandReach+_SandEdge,shoreHeight+exposed*_SandReach.z+sandEdge))*smoothstep(0,.02,sediment);
                half sandGrain=.86+.28*IceNoise(sandDirection*90+5.1);
                rock=lerp(rock,_SandColor.rgb*sandGrain,sand);
                half water = 1 - smoothstep(_SeaLevel - .008, _SeaLevel + .008, elevation);
                half depth = saturate((_SeaLevel - elevation) * 9);
                half3 ocean = lerp(_WaterColor.rgb * half3(1.6, 1.8, 1.65), _WaterColor.rgb, depth);
                // Water color trait keeps the shallow-to-deep variation of the base water.
                ocean=lerp(ocean,_WaterTint.rgb*lerp(1.5,1,depth),saturate(_WaterTint.a));
                float3 planetDirection = normalize(input.planetDirection);
                float lowland=max(_SeaLevel,_TerrainBounds.x);
                half altitude=saturate((elevation-lowland)/max(.05,_TerrainBounds.y-lowland));
                half middleTerrain=smoothstep(.05,.25,altitude)*(1-smoothstep(.6,.95,altitude));
                half elevationPreference=.08+.92*middleTerrain;
                half patch=IceNoise(planetDirection*7+14.2)*.75+IceNoise(planetDirection*23+5.7)*.25;
                half localGrowth=saturate(_Vegetation)*elevationPreference;
                half patchThreshold=.8-.6*sqrt(localGrowth);
                half foliage=smoothstep(patchThreshold-.035,patchThreshold+.035,patch)*smoothstep(0,.035,_Vegetation);
                half canopy=IceNoise(planetDirection*55+8.1);
                half3 foliageColor=lerp(half3(.035,.17,.055),half3(.16,.36,.08),canopy);
                // Vegetation color trait keeps the canopy light/dark variation.
                foliageColor=lerp(foliageColor,_VegetationTint.rgb*lerp(.45,1,canopy),saturate(_VegetationTint.a));
                rock=lerp(rock,foliageColor,foliage*.94);
                half3 albedo = lerp(rock, ocean, water);
                // Seam-free, planet-fixed variation breaks up the cap into terrain-following fingers.
                half effectiveIce=saturate((_FreezeStart-_Temperature)/max(.001,_FreezeStart-_FullyFrozen));
                half breakup = (IceNoise(planetDirection*11) - .5)*.20
                    + (IceNoise(planetDirection*31+7.3) - .5)*.09;
                half highland = smoothstep(.12,.65,elevation-max(_SeaLevel,0));
                half freezeBias = highland*.48 - (1-highland)*.12
                    - water*(.08+depth*.04) + breakup;
                // Water and valleys resist freezing; high ridges freeze farther toward the equator.
                // Bias diminishes continuously so full influence can still freeze the entire globe.
                half edgeVariation = freezeBias * (1-effectiveIce*.85);
                half capEdge = 1.015 - effectiveIce*1.11;
                half ice = smoothstep(capEdge-.012,capEdge+.012,abs(planetDirection.y)+edgeVariation)
                    * smoothstep(0,.025,effectiveIce);
                half3 snow = half3(.88,.93,.98) * lerp(.92,1,elevation);
                half3 seaIce = half3(.62,.81,.91);
                albedo = lerp(albedo,lerp(snow,seaIce,water),ice);
                // Warped contour networks produce thin, branching fissures without sprite textures.
                // Even a minority contribution leaves sparse, dim fissures; dominance expands them.
                half activity=saturate(_GeologicalActivity);
                half volcanic=sqrt(activity)*.5 + smoothstep(.35,1,activity)*.5;
                float3 fissurePosition=planetDirection*11 + (IceNoise(planetDirection*6)-.5)*2;
                half ridge=abs(IceNoise(fissurePosition)-.5);
                half branch=abs(IceNoise(fissurePosition*1.45+12.7)-.5);
                half fissure=min(ridge,branch*1.4);
                half edgeAA=max(fwidth(fissure)*.5,.002);
                half crack=1-smoothstep(.008-edgeAA,.018+volcanic*.025+edgeAA,fissure);
                half region=IceNoise(planetDirection*5.5+3.1)+exposed*.12;
                half volcanicRegion=smoothstep(.65-volcanic*.45,.8-volcanic*.45,region);
                // Keep minority fissures readable while their area still shrinks with heat.
                // Liquid oceans hide land flows, but frozen ocean crust can fracture too.
                half exposedCrust=1-water*(1-ice);
                half lava=crack*volcanicRegion*sqrt(volcanic)*exposedCrust;
                // Exposed flows cut a dark crust through snow instead of washing out against white ice.
                half crust=smoothstep(.01,.35,lava)*.9;
                albedo=lerp(albedo,half3(.08,.025,.012),crust);
                half3 lavaColor=lerp(half3(1,.045,.003),half3(1,.52,.045),volcanic*crack);
                half3 normal = normalize(input.normalWS);
                half3 tangent = normalize(input.tangentWS.xyz);
                half3 bitangent = cross(normal,tangent)*input.tangentWS.w;
                half2 relief = (slope*12 + half2(fineEast,fineNorth)*3)*_StoneRuggedness*(1-water);
                normal = normalize(normal - tangent*relief.x - bitangent*relief.y);
                Light light = GetMainLight();
                half diffuse = saturate(dot(normal, light.direction));
                // _PlanetNightAmbientDim (global, 0 by default) dims the ambient term in the day/night view.
                half3 illumination = max(SampleSH(normal), half3(.18, .18, .18))*(1-_PlanetNightAmbientDim) + light.color * diffuse * .65;
                half3 halfDirection = normalize(light.direction + GetWorldSpaceNormalizeViewDir(input.positionWS));
                half glint = pow(saturate(dot(normal, halfDirection)), 64) * water * diffuse * (1-ice*.8);
                // City lights. Civilization sets coverage: regions light up in a stable order with scattered settlements;
                // technology (intensity) brightens them and blends in dense urban networks. The CityLights color trait shifts the hue.
                // Lights sit on exposed land only, show on the night side, and twinkle per small cell rather than in unison.
                half civilization=saturate(_Civilization);
                half3 cityLight=0;
                if(civilization>0)
                {
                    // Tiled so each mask texel is about a screen pixel; the floor and gain turn soft blobs into points.
                    float2 lightsUV=input.uv*_CityLightsTiling.xy;
                    half scattered=saturate((SAMPLE_TEXTURE2D(_CityLightsMask,sampler_CityLightsMask,lightsUV).r-_CityLightsSharpness.x)*_CityLightsSharpness.y*max(1,_CityLightsSharpness.z));
                    half dense=saturate((SAMPLE_TEXTURE2D(_CityLightsDenseMask,sampler_CityLightsMask,lightsUV).r-_CityLightsSharpness.x)*_CityLightsSharpness.y);
                    half settledRegion=IceNoise(planetDirection*4+21.7)*.7+IceNoise(planetDirection*9+3.3)*.3;
                    half revealAt=.72-civilization*.9;
                    half reveal=smoothstep(revealAt-.08,revealAt+.08,settledRegion)*smoothstep(0,.03,civilization);
                    // Civilization only spreads scattered settlements; the dense urban network needs abundant technology (light intensity).
                    half urban=lerp(scattered,max(scattered,dense),smoothstep(.45,1,saturate(_CityLightIntensity)));
                    half onLand=(1-water)*(1-ice)*(1-crust);
                    // Full strength past the terminator; a dimmer floor keeps lights readable on the lit face the game cameras show.
                    half night=lerp(lerp(_CityLightsDayVisibility,1,smoothstep(.1,-.3,dot(normalize(input.normalWS),light.direction))),1,saturate(_PlanetNight));
                    float3 cell=floor(planetDirection*60);
                    half twinkle=.85+.15*sin(_Time.y*(1.5+IceHash(cell)*2)+IceHash(cell+7.1)*6.283);
                    half brightness=lerp(_CityLightsBrightness.x,_CityLightsBrightness.y,saturate(_CityLightIntensity));
                    half3 hue=lerp(_CityLightsColor.rgb,_CityLightsTint.rgb,saturate(_CityLightsTint.a));
                    half footprint=urban*reveal*onLand;
                    cityLight=hue*footprint*night*twinkle*brightness;
                    // Built-up land reads slightly darker, so the lights keep their contrast on the daylit face.
                    albedo*=1-_CityLightsUrbanShade*footprint;
                }
                return half4(albedo * illumination + light.color * glint * .45 + lavaColor*lava*1.6 + cityLight, 1);
            }
            ENDHLSL
        }
    }
}
