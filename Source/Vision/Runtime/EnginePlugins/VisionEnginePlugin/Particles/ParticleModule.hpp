/*
 *
 * Confidential Information of Telekinesys Research Limited (t/a Havok). Not for disclosure or distribution without Havok's
 * prior written consent. This software contains code, techniques and know-how which is confidential and proprietary to Havok.
 * Product and Trade Secret source code contains trade secrets of Havok. Havok Software (C) Copyright 1999-2013 Telekinesys Research Limited t/a Havok. All Rights Reserved. Use of this software is subject to the terms of an end user license agreement.
 *
 */

/// \file ParticleModule.hpp

#if defined (V_DYNLINK_ENABLED)

  #ifdef PARTICLEMODULE_EXPORTS
    #define PARTICLE_IMPEXP V_DYNLINK_EXPORT
  #elif defined PARTICLEMODULE_IMPORTS
    #define PARTICLE_IMPEXP V_DYNLINK_IMPORT
  #else
    #define PARTICLE_IMPEXP V_DYNLINK_NONE
  #endif

#elif defined (V_STATICLINK_ENABLED)
  #define PARTICLE_IMPEXP
#else
  #error "Linkage mode not set!"
#endif

/*
 * Havok SDK - Base file, BUILD(#20130624)
 * 
 * Confidential Information of Havok.  (C) Copyright 1999-2013
 * Telekinesys Research Limited t/a Havok. All Rights Reserved. The Havok
 * Logo, and the Havok buzzsaw logo are trademarks of Havok.  Title, ownership
 * rights, and intellectual property rights in the Havok software remain in
 * Havok and/or its suppliers.
 * 
 * Use of this software for evaluation purposes is subject to and indicates
 * acceptance of the End User licence Agreement for this product. A copy of
 * the license is included with this software and is also available from salesteam@havok.com.
 * 
 */