// Simplified Bumped Specular shader. Differences from regular Bumped Specular one:
// - no Main Color nor Specular Color
// - specular lighting directions are approximated per vertex
// - writes zero to alpha channel
// - Normalmap uses Tiling/Offset of the Base texture
// - no Deferred Lighting support
// - no Lightmap support
// - supports ONLY 1 directional light. Other lights are completely ignored.

// Simplified Specular shader. Differences from regular Specular one:
// - no Main Color nor Specular Color
// - specular lighting directions are approximated per vertex
// - writes zero to alpha channel
// - no Deferred Lighting support
// - no Lightmap support
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

/*
Shader "Mobile/SpecularX" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_SpecColor("Specular Color", Color) = (0.5,0.5,0.5,1)
		_Shininess("Shininess", Range(0.03, 1)) = 0.078125
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		//_BumpMap("Normalmap", 2D) = "bump" {}
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 250

		CGPROGRAM
#pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview

		inline fixed4 LightingMobileBlinnPhong(SurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
	{
		fixed diff = max(0, dot(s.Normal, lightDir));
		fixed nh = max(0, dot(s.Normal, halfDir));
		fixed spec = pow(nh, s.Specular * 128) * s.Gloss;

		fixed4 c;
		c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec * _SpecColor) * (atten * 2);
		c.a = 0.0;
		return c;
	}

	sampler2D _MainTex;
	//sampler2D _BumpMap;
	half _Shininess;
	half4 _Color;

	struct Input {
		float2 uv_MainTex;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = tex.rgb * _Color;
		o.Gloss = tex.a;
		o.Alpha = tex.a;
		o.Specular = _Shininess;
		//o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
	}
	ENDCG
	}

		FallBack "Mobile/VertexLit"
}

Shader "Mobile/Bumped Specular X (1 Directional Light)" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_Shininess("Shininess", Range(0.03, 1)) = 0.078125
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 250

		CGPROGRAM
#pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview

		inline fixed4 LightingMobileBlinnPhong(SurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
	{
		fixed diff = max(0, dot(s.Normal, lightDir));
		fixed nh = max(0, dot(s.Normal, halfDir));
		fixed spec = pow(nh, s.Specular * 128) * s.Gloss;

		fixed4 c;
		c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec * _SpecColor) * (atten*2);
		UNITY_OPAQUE_ALPHA(c.a);
		return c;
	}

	sampler2D _MainTex;
	sampler2D _BumpMap;
	half _Shininess;
	half4 _Color;

	struct Input {
		float2 uv_MainTex;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = tex.rgb * _Color.rgb;
		o.Gloss = tex.a;
		o.Alpha = tex.a;
		o.Specular = _Shininess;
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
	}
	ENDCG
	}

		FallBack "Mobile/Diffuse"
}
*/
/*
Shader "Surface/SpecularX" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_SpecMap("SpecMap(RGB) Illum(A)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_Shininess("Shininess", Range(0.01, 1)) = 0.078125
		_Color("Main Color", Color) = (1,1,1,1)
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf ColoredSpecular

	struct MySurfaceOutput {
		half3 Albedo;
		half3 Normal;
		half3 Emission;
		half Specular;
		half3 GlossColor;
		half Alpha;
	};


	inline half4 LightingColoredSpecular(MySurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
	{
		half3 h = normalize(lightDir + viewDir);

		half diff = max(0, dot(s.Normal, lightDir));

		float nh = max(0, dot(s.Normal, h));
		float spec = pow(nh, 32.0 * s.Specular);
		half3 specCol = spec * s.GlossColor;

		half4 c;
		c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * specCol) * (atten * 2);
		c.a = s.Alpha;
		return c;
	}

	inline half4 LightingColoredSpecular_PrePass(MySurfaceOutput s, half4 light)
	{
		half3 spec = light.a * s.GlossColor;

		half4 c;
		c.rgb = (s.Albedo * light.rgb + light.rgb * spec);
		c.a = s.Alpha + spec * _SpecColor.a;
		return c;
	}


	struct Input {
		float2 uv_MainTex;
		float2 uv_SpecMap;
		float2 uv_BumpMap;
	};

	sampler2D _MainTex;
	sampler2D _SpecMap;
	sampler2D _BumpMap;
	half4 _Color;
	half _Shininess;

	void surf(Input IN, inout MySurfaceOutput o)
	{
		half3 c = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
		o.Albedo = c;
		half4 spec = tex2D(_SpecMap, IN.uv_SpecMap);
		o.GlossColor = spec.rgb;
		o.Emission = c * spec.a;
		o.Specular = _Shininess;
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
	}
	ENDCG
	}
		Fallback "Diffuse"
}
*/
// Simplified Bumped Specular shader. Differences from regular Bumped Specular one:
// - no Main Color nor Specular Color
// - specular lighting directions are approximated per vertex
// - writes zero to alpha channel
// - Normalmap uses Tiling/Offset of the Base texture
// - no Deferred Lighting support
// - no Lightmap support
// - supports ONLY 1 directional light. Other lights are completely ignored.
/*
Shader "Mobile/SpecularX" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_Shininess("Shininess", Range(0.03, 1)) = 0.078125
		_MainTex("Base (RGB) Gloss (A)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 250

		CGPROGRAM
#pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview novertexlights

		inline fixed4 LightingMobileBlinnPhong(SurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
	{
		fixed diff = max(0, dot(s.Normal, lightDir));
		fixed nh = max(0, dot(s.Normal, halfDir));
		fixed spec = pow(nh, s.Specular * 128) * s.Gloss;

		fixed4 c;
		c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
		UNITY_OPAQUE_ALPHA(c.a);
		return c;
	}

	sampler2D _MainTex;
	sampler2D _BumpMap;
	half _Shininess;
	fixed4 _Color;

	struct Input {
		float2 uv_MainTex;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = tex.rgb * _Color.rgb;
		o.Gloss = tex.a;
		o.Alpha = tex.a;
		o.Specular = _Shininess;
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
	}
	ENDCG
	}

		FallBack "Mobile/VertexLit"
}
*/

Shader "Reflective/Bumped Specular" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_SpecColor("Specular Color", Color) = (0.5,0.5,0.5,1)
		_Shininess("Shininess", Range(0.01, 1)) = 0.078125
		_ReflectColor("Reflection Color", Color) = (1,1,1,0.5)
		_MainTex("Base (RGB) RefStrGloss (A)", 2D) = "white" {}
	_Cube("Reflection Cubemap", Cube) = "" { TexGen CubeReflect }
	_BumpMap("Normalmap", 2D) = "bump" {}
	}

		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 400
		CGPROGRAM
#pragma surface surf BlinnPhong
#pragma target 3.0

		sampler2D _MainTex;
	sampler2D _BumpMap;
	samplerCUBE _Cube;

	fixed4 _Color;
	fixed4 _ReflectColor;
	half _Shininess;

	struct Input {
		float2 uv_MainTex;
		float2 uv_BumpMap;
		float3 worldRefl;
		INTERNAL_DATA
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
		fixed4 c = tex * _Color;
		o.Albedo = c.rgb;

		o.Gloss = tex.a;
		o.Specular = _Shininess;

		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

		float3 worldRefl = WorldReflectionVector(IN, o.Normal);
		fixed4 reflcol = texCUBE(_Cube, worldRefl);
		reflcol *= tex.a;
		o.Emission = reflcol.rgb * _ReflectColor.rgb;
		o.Alpha = reflcol.a * _ReflectColor.a;
	}
	ENDCG
	}

		FallBack "Reflective/Bumped Diffuse"
}