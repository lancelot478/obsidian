https://indienova.com/indie-game-development/lighting-model-in-unity/

言
一篇多年前翻译的技术文章，算是上一篇 shader 文章的补充。

光照模型是用来决定模型表面光线的方法/算法。有许多利用着色器（shader）计算光照的方法。美术人员和程序员基于模型的真实程度/风格化程度/导入表现力来选择使用光照模型。下面我们会提到几个光照和着色模型。

Lambert
Lambertian 反射率，或者扩散光，是应用中最普遍的光照模型之一，但让不是最最普遍的。

这个光照模型独立出现，也就是说表面的亮度不会随着视点角度而改变。这是一个非常简单的模型，可以使用下面的伪代码（pseudocode）实现：

float3 SurfaceColor;    //objects color
float3 LightColor; //lights color * intensity
float LightAttenuation; //value of light at point  (shadow/falloff)

float NdotL = max(0.0,dot(normalDirection,lightDirection));
float LambertDiffuse = NdotL *  SurfaceColor;
float3 finalColor =  LambertDiffuse * LightAttenuation * LightColor;




Half-Lambert（扩散包裹 Diffuse Wrap）
Half-Lambert 光照模型最初是由 Valve 开发用在初代半条命中的技术。用来皮面物体后部变形而且看上去不立体。这是一个极其宽容的光照模型，也正因如此，此模型完全不具有物理特性。

Half-Lambert 光照模型的关键在于，Lambert 扩散除以 2，然后加上 0.5 然后再二次方。一种常见的插入方式，称作扩散包裹（diffuse wrap），不是使用 0.5，而是你可以使用 0.5 到 1 之间的所有数值，基于这样的理念运作。比如说，不用：

pow(NdotL * 0.5 + 0.5,2)
而还可以使用：

pow(NdotL * wrapValue + (1-wrapValue),2)
包裹数值可以是介于 0.5 到 1 之间的所有值。

下面是 Half-Lambert 光照模型的一个插入方式：

float3 SurfaceColor;    //objects color
float3 LightColor; //lights color * intensity
float LightAttenuation; //value of light at point  (shadow/falloff)

float NdotL =  max(0.0,dot(normalDirection,lightDirection));
float HalfLambertDiffuse =  pow(NdotL * 0.5 + 0.5,2.0) * SurfaceColor;
float3 finalColor =  HalfLambertDiffuse * LightAttenuation * LightColor;




Phong 光照
Phong 着色模型通常用来产生镜面效果。此函数基于这样的假设：表面反射的光是粗糙表面的漫反射和光滑表面的镜面反射的集合。

下面是 Phong 模型的一个常见插入方式：

float3 viewDirection =  normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
float3 lightReflectDirection =  reflect( -lightDirection, normalDirection );
float NdotL = max(0, dot(  normalDirection, lightDirection ));
float RdotV = max(0, dot(  lightReflectDirection, viewDirection ));
//Specular calculations
float3 specularity =  pow(RdotV,_SpecularGloss/4)*_SpecularPower *_SpecularColor.rgb ;

float3 lightingModel = NdotL *  diffuseColor + specularity;
float attenuation =  LIGHT_ATTENUATION(i);
float3 attenColor =  attenuation * _LightColor0.rgb;
float4 finalDiffuse =  float4(lightingModel* attenColor,1);




Band-Lighting
这不太像是一个光照模型，而更像是一个光照扭曲，来给你看你如何在标准光照模型上使用简单的数学操作。这种方法通过打断光照的方向然后将其变成一条一条的光带。这种方法可以变成其他任何光照模型，只要用 banded NdotL 替换 NdotL，正如下方所示的：

float NdotL = max(0.0, dot(  normalDirection, lightDirection ));

float lightBandsMultiplier =  _LightSteps/256;
float lightBandsAdditive = _LightSteps/2;
fixed bandedNdotL =  (floor((NdotL*256+lightBandsAdditive)/_LightSteps))* lightBandsMultiplier;

float3 lightingModel = bandedNdotL *  diffuseColor;
float attenuation = LIGHT_ATTENUATION(i);
float3 attenColor = attenuation *  _LightColor0.rgb;
float4 finalDiffuse = float4(lightingModel  * attenColor,1);
return finalDiffuse;




Minnaert 光照
Minnaert 光照模型最初设计用来复制月亮的着色，所以经常被称作月球着色器（shader）。Minnnaert 很适合用来模拟多孔和纤维表面，比如月亮和丝绒。这些表面可以在背散射（back-scatter）中造成很多光线。这在纤维垂直于表面时特别明显，比如丝绒，地毯等。

这种模拟提供的结果和 Oren-Nayar 十分接近，同样也经常被称作丝绒或月球着色器（shader）。

下面是 Minnaert 光照的一个近似例子：

float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz  - i.posWorld.xyz);
float NdotL = max(0, dot(  normalDirection, lightDirection ));
float NdotV = max(0, dot(  normalDirection, viewDirection ));

float3 minnaert =  saturate(NdotL * pow(NdotL*NdotV,_Roughness));

float3 lightingModel =  minnaert * diffuseColor;
float attenuation =  LIGHT_ATTENUATION(i);
float3 attenColor =  attenuation * _LightColor0.rgb;
float4 finalDiffuse =  float4(lightingModel * attenColor,1);
UNITY_APPLY_FOG(i.fogCoord, finalDiffuse);

return finalDiffuse;




Oren-Nayer Lighting
Oren-Nayer 反射率模型是一个反射模型，用在漫反射或粗糙表面上。此模型是一个简单的办法来近似模拟光线在粗糙但仍然具有 lambertian 特性的表面上的效果。

下面是一个简单的 Oren-Nayer 插入方式。

float roughness = _Roughness;
float roughnessSqr = roughness *  roughness;
float3 o_n_fraction = roughnessSqr /  (roughnessSqr + float3(0.33, 0.13, 0.09));
float3 oren_nayar = float3(1, 0, 0) +  float3(-0.5, 0.17, 0.45) * o_n_fraction;
float cos_ndotl = saturate(dot(normalDirection, lightDirection));
float cos_ndotv =  saturate(dot(normalDirection, viewDirection));
float oren_nayar_s =  saturate(dot(lightDirection, viewDirection)) - cos_ndotl * cos_ndotv;
oren_nayar_s /= lerp(max(cos_ndotl,  cos_ndotv), 1,
step(oren_nayar_s, 0));

//lighting and final diffuse
float attenuation = LIGHT_ATTENUATION(i);
float3 lightingModel = diffuseColor *  cos_ndotl * (oren_nayar.x + diffuseColor * oren_nayar.y  + oren_nayar.z * oren_nayar_s);
float3 attenColor = attenuation *  _LightColor0.rgb;
float4 finalDiffuse = float4(lightingModel  * attenColor,1);




原帖地址：