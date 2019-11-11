#ifndef ETERRAIN_EPROPLOCALEHEIGHTACCESSING_INC
#define ETERRAIN_EPROPLOCALEHEIGHTACCESSING_INC

#include "eterrain_EPropLocaleCommon.hlsl"

#ifdef SHADER_API_D3D11
			StructuredBuffer<EPropLocale> _EPropLocaleBuffer;
			StructuredBuffer<EPropElevationId> _EPropIdsBuffer;


			float RetriveHeight() {
				uint pointerValue = asuint(UNITY_ACCESS_INSTANCED_PROP(Props, _Pointer));
				EPropElevationId  elevationId = _EPropIdsBuffer[pointerValue];
				uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength, elevationId.LocaleBufferScopeIndex, elevationId.InScopeIndex);
				return _EPropLocaleBuffer[localeIndex].Height;
			}

			float3 RetriveNormal() {
				uint pointerValue = asuint(UNITY_ACCESS_INSTANCED_PROP(Props, _Pointer));
				EPropElevationId  elevationId = _EPropIdsBuffer[pointerValue];
				uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength, elevationId.LocaleBufferScopeIndex, elevationId.InScopeIndex);
				return _EPropLocaleBuffer[localeIndex].Normal;
			}
#else
			float RetriveHeight() { return 0; }
			float3 RetriveNormal() { return 0; }
#endif

#endif
