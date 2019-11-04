#ifndef ETERRAIN_EPROPLOCALECOMMON_INC
#define ETERRAIN_EPROPLOCALECOMMON_INC

struct EPropLocale {
	float2 FlatPosition;
	float Height;
	float3 Normal;
};

struct EPropElevationId {
	uint LocaleBufferScopeIndex;
	uint InScopeIndex;
};

uint ComputeIndexInLocaleBuffer(uint scopeLength, uint scopeIndex, int indexInScope) {
	return scopeLength * scopeIndex + indexInScope;
}

#endif