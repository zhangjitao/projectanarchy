/*
 *
 * Confidential Information of Telekinesys Research Limited (t/a Havok). Not for disclosure or distribution without Havok's
 * prior written consent. This software contains code, techniques and know-how which is confidential and proprietary to Havok.
 * Product and Trade Secret source code contains trade secrets of Havok. Havok Software (C) Copyright 1999-2014 Telekinesys Research Limited t/a Havok. All Rights Reserved. Use of this software is subject to the terms of an end user license agreement.
 *
 */

// WARNING: THIS FILE IS GENERATED. EDITS WILL BE LOST.
// Generated from 'Behavior/AiBridge/Modifier/hkbAiSteeringModifier.h'

#ifndef HKB_AI_STEERING_MODIFIER_INTERNAL_STATE_H
#define HKB_AI_STEERING_MODIFIER_INTERNAL_STATE_H

#include <Behavior/AiBridge/Modifier/hkbAiSteeringModifier.h>

class hkbAiSteeringModifierInternalState : public hkReferencedObject
{
	//+vtable(1)
	//+version(0)
	public:

		HK_DECLARE_REFLECTION();
		HK_DECLARE_CLASS_ALLOCATOR( HK_MEMORY_CLASS_BEHAVIOR );

		hkbAiSteeringModifierInternalState() {}
		hkbAiSteeringModifierInternalState( hkFinishLoadedObjectFlag flag )
			: hkReferencedObject(flag)
		{
		}

		hkVector4 m_requestedVelocityInternal;
		hkBool m_hasLastUserEdgeEntry;
		hkbAiSteeringModifier::InternalUserEdgeInfo m_lastUserEdgeEntry;
};

#endif

/*
 * Havok SDK - Base file, BUILD(#20140618)
 * 
 * Confidential Information of Havok.  (C) Copyright 1999-2014
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
