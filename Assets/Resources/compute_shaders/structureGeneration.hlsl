#ifndef STRUCTURE_GENERATION_INC
#define STRUCTURE_GENERATION_INC

#define GENERATE_CONSTRUCTOR_2( NAME, T1, P1, T2, P2) \
	NAME new_##NAME( T1 P1, T2 P2){ \
		NAME output; \
		output.P1 = P1; \
		output.P2 = P2; \
		return output; \
	}

#define GENERATE_CONSTRUCTOR_3( NAME, T1, P1, T2, P2, T3, P3) \
	NAME new_##NAME( T1 P1, T2 P2, T3 P3){ \
		NAME output; \
		output.P1 = P1; \
		output.P2 = P2; \
		output.P3 = P3; \
		return output; \
	}

#define GENERATE_CONSTRUCTOR_4( NAME, T1, P1, T2, P2, T3, P3, T4, P4) \
	NAME new_##NAME( T1 P1, T2 P2, T3 P3, T4 P4){ \
		NAME output; \
		output.P1 = P1; \
		output.P2 = P2; \
		output.P3 = P3; \
		output.P4 = P4; \
		return output; \
	}

#define GENERATE_CLASS_2( NAME, T1, P1, T2, P2) \
	struct NAME { \
		T1 P1; T2 P2; }; \
	GENERATE_CONSTRUCTOR_2( NAME, T1, P1, T2, P2) ;

#define GENERATE_CLASS_3( NAME, T1, P1, T2, P2, T3, P3) \
	struct NAME { \
		T1 P1; T2 P2; T3 P3;  }; \
	GENERATE_CONSTRUCTOR_3( NAME, T1, P1, T2, P2, T3, P3) ;

#define GENERATE_CLASS_4( NAME, T1, P1, T2, P2, T3, P3, T4, P4) \
	struct NAME { \
		T1 P1; T2 P2; T3 P3; T4 P4; }; \
	GENERATE_CONSTRUCTOR_4( NAME, T1, P1, T2, P2, T3, P3, T4, P4) ;

#endif