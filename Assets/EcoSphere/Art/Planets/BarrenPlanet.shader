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
        _Soil ("Soil",Range(0,1))=0
        _Moisture ("Moisture",Range(0,1))=0
        _Vegetation ("Vegetation growth",Range(0,1))=0
        _WaterGreenness ("Water biological tint",Range(0,1))=0
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
                float _Vegetation, _WaterGreenness;
                float4 _TerrainBounds;
            CBUFFER_END
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
                half drySand=_Sediment*(1-_Moisture)*(.7+.3*(1-exposed));
                rock=lerp(rock,half3(.7,.5,.28)*(.65+rock.r),drySand);
                half heat=saturate(_Temperature);
                half scorch=heat*.15 + smoothstep(.25,.95,heat)*.85;
                rock=lerp(rock,rock*half3(1.16,.77,.56),heat*.65);
                rock=lerp(rock,rock*half3(.25,.20,.19),scorch*.8);
                half water = 1 - smoothstep(_SeaLevel - .008, _SeaLevel + .008, elevation);
                half depth = saturate((_SeaLevel - elevation) * 9);
                half3 ocean = lerp(_WaterColor.rgb * half3(1.6, 1.8, 1.65), _WaterColor.rgb, depth);
                ocean=lerp(ocean,half3(.04,.23,.25),saturate(_WaterGreenness));
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
                half3 illumination = max(SampleSH(normal), half3(.18, .18, .18)) + light.color * diffuse * .65;
                half3 halfDirection = normalize(light.direction + GetWorldSpaceNormalizeViewDir(input.positionWS));
                half glint = pow(saturate(dot(normal, halfDirection)), 64) * water * diffuse * (1-ice*.8);
                return half4(albedo * illumination + light.color * glint * .45 + lavaColor*lava*1.6, 1);
            }
            ENDHLSL
        }
    }
}
