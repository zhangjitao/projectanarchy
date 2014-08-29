/*
 *
 * Confidential Information of Telekinesys Research Limited (t/a Havok). Not for disclosure or distribution without Havok's
 * prior written consent. This software contains code, techniques and know-how which is confidential and proprietary to Havok.
 * Product and Trade Secret source code contains trade secrets of Havok. Havok Software (C) Copyright 1999-2014 Telekinesys Research Limited t/a Havok. All Rights Reserved. Use of this software is subject to the terms of an end user license agreement.
 *
 */

#include <Vision/Runtime/EnginePlugins/Havok/HavokAiEnginePlugin/HavokAiEnginePlugin.hpp>
#include <Vision/Runtime/EnginePlugins/Havok/HavokAiEnginePlugin/vHavokAiModule.hpp>
#include <Vision/Runtime/EnginePlugins/Havok/HavokPhysicsEnginePlugin/vHavokPhysicsModule.hpp>
#include <Vision/Runtime/EnginePlugins/Havok/HavokAiEnginePlugin/vHavokAiNavMeshInstance.hpp>
#include <Vision/Runtime/EnginePlugins/Havok/HavokAiEnginePlugin/vHavokAiNavMeshResourceManager.hpp>
#include <Vision/Runtime/EnginePlugins/Havok/HavokPhysicsEnginePlugin/vHavokSync.inl>
#include <Vision/Runtime/EnginePlugins/Havok/HavokAiEnginePlugin/vHavokAiObstacle.hpp>

#include <Common/Serialize/Util/hkBuiltinTypeRegistry.h>
#include <Common/Base/Reflection/Registry/hkDefaultClassNameRegistry.h>
#include <Common/Base/Reflection/Registry/hkTypeInfoRegistry.h>
#include <Common/Base/Reflection/Registry/hkVtableClassRegistry.h>

//AI Utility
#include <Common/Serialize/Version/hkVersionPatchManager.h>
#if defined(HAVOK_SDK_VERSION_MAJOR) && (HAVOK_SDK_VERSION_MAJOR >= 2012)
#include <Common/Compat/hkHavokVersions.h>
#define HK_SERIALIZE_MIN_COMPATIBLE_VERSION_INTERNAL_VALUE HK_HAVOK_VERSION_300
void HK_CALL registerAiPatches()
{
	hkVersionPatchManager& man = hkVersionPatchManager::getInstance();
	#define HK_EXCLUDE_COMMON_PATCHES
	#define HK_FEATURE_PRODUCT_AI
	#undef HK_FEATURE_PRODUCT_ANIMATION
	#undef HK_FEATURE_PRODUCT_CLOTH
	#undef HK_FEATURE_PRODUCT_DESTRUCTION_2012
	#undef HK_FEATURE_PRODUCT_BEHAVIOR
	#undef HK_FEATURE_PRODUCT_PHYSICS_2012
	#undef HK_FEATURE_PRODUCT_SIMULATION
	#undef HK_FEATURE_PRODUCT_PHYSICS
	#undef HK_FEATURE_PRODUCT_DESTRUCTION
	#undef HK_FEATURE_PRODUCT_SIMULATION
	#include <Common/Compat/Patches/hkRegisterPatches.cxx>
}
#else
extern void HK_CALL registerAiPatches();
#endif

VIMPORT IVisPlugin_cl* GetEnginePlugin_vHavok();

#ifdef VBASE_LIB
#include <Vision/Runtime/EnginePlugins/Havok/HavokAiEnginePlugin/vHavokAiClassReg.h>
#endif

//////////////////////////////////////////////////////////////////////////

class vHavokAiPlugin_cl : public IVisPlugin_cl
{
public:
	void OnInitEnginePlugin();
	void OnDeInitEnginePlugin();
  
	const char *GetPluginName()
	{
		return "vHavokAi";  //must match DLL name
	}

};

static vHavokAiPlugin_cl g_HavokAiPlugin;

//////////////////////////////////////////////////////////////////////////

// A module for the serialization
DECLARE_THIS_MODULE( g_vHavokAiModule, MAKE_VERSION(1,0),
					 "Havok AI Plugin", "Havok",
					 "Module for the Havok AI binding", &g_HavokAiPlugin );

//////////////////////////////////////////////////////////////////////////

// A listener for Havok AI module callbacks
class vHavokAiModuleCallbackHandler_cl : public IVisCallbackHandler_cl
{
public:

	vHavokAiModuleCallbackHandler_cl() {}

	virtual void OnHandleCallback(IVisCallbackDataObject_cl *pData) HKV_OVERRIDE
	{
		if (pData->m_pSender == &vHavokPhysicsModule::OnBeforeInitializePhysics)
		{
			hkDefaultClassNameRegistry& dcnReg	= hkDefaultClassNameRegistry::getInstance();
			hkTypeInfoRegistry&			tyReg	= hkTypeInfoRegistry::getInstance();
			hkVtableClassRegistry&		vtcReg	= hkVtableClassRegistry::getInstance();

			// Register AI classes
#ifndef VBASE_LIB  // DLL, so have a full set 
      dcnReg.registerList(hkBuiltinTypeRegistry::StaticLinkedClasses);
      tyReg.registerList(hkBuiltinTypeRegistry::StaticLinkedTypeInfos);
      vtcReg.registerList(hkBuiltinTypeRegistry::StaticLinkedTypeInfos, hkBuiltinTypeRegistry::StaticLinkedClasses);

#else // Static lib, just need to add Ai ones and reg the ai patches which would not have been done yet
      dcnReg.registerList(hkBuiltinAiTypeRegistry::StaticLinkedClasses);
      tyReg.registerList(hkBuiltinAiTypeRegistry::StaticLinkedTypeInfos);
      vtcReg.registerList(hkBuiltinAiTypeRegistry::StaticLinkedTypeInfos, hkBuiltinAiTypeRegistry::StaticLinkedClasses);

#endif
			registerAiPatches();
			hkVersionPatchManager::getInstance().recomputePatchDependencies();

      vHavokPhysicsModuleCallbackData* pHavokData = static_cast<vHavokPhysicsModuleCallbackData*>(pData);
      vHavokAiModule::GetInstance()->Init();
      pHavokData->GetHavokModule()->AddStepper(vHavokAiModule::GetInstance());
    }
		else if (pData->m_pSender == &vHavokPhysicsModule::OnAfterDeInitializePhysics)
		{
			vHavokPhysicsModuleCallbackData* pHavokData = static_cast<vHavokPhysicsModuleCallbackData*>(pData);
			pHavokData->GetHavokModule()->RemoveStepper(vHavokAiModule::GetInstance());

			// destroy all navmesh instances
			vHavokAiNavMeshInstance::ElementManagerDeleteAllUnRef();
			VASSERT(vHavokAiNavMeshInstance::ElementManagerGetSize()==0);

			// unload all resources
			vHavokAiNavMeshResourceManager::GetInstance()->PurgeUnusedResources();
			VASSERT(vHavokAiNavMeshResourceManager::GetInstance()->GetResourceCount() == 0);

			vHavokAiModule::GetInstance()->DeInit();
		}
		else if (pData->m_pSender == &Vision::Callbacks.OnAfterSceneUnloaded)
		{
			// we don't destroy the ai world, but we need delete all the unused instances and resources

			// destroy all unreferenced navmesh instances
			vHavokAiNavMeshInstance::ElementManagerDeleteAllUnRef();

			// unload all unused resources
			vHavokAiNavMeshResourceManager::GetInstance()->PurgeUnusedResources();
		}
	}

	virtual int64 GetCallbackSortingKey(VCallback *pCallback) HKV_OVERRIDE
	{
		return 0;
	}
};

//static handler
static vHavokAiModuleCallbackHandler_cl g_HavokAiModuleCallbackHandler;

//////////////////////////////////////////////////////////////////////////

// Initialize the plugin
void vHavokAiPlugin_cl::OnInitEnginePlugin()
{
  VISION_HAVOK_SYNC_STATICS();

	// This plugin depends on the main Havok Physics plugin being loaded
	VISION_PLUGIN_ENSURE_LOADED(vHavok);

	// Register to sync statics (hopefully) after the main Havok plugin
	vHavokPhysicsModule::OnBeforeInitializePhysics += &g_HavokAiModuleCallbackHandler;
	vHavokPhysicsModule::OnAfterDeInitializePhysics += &g_HavokAiModuleCallbackHandler;
  vHavokPhysicsModule::OnBeforeWorldCreated += &g_HavokAiModuleCallbackHandler;
	Vision::Callbacks.OnAfterSceneUnloaded += &g_HavokAiModuleCallbackHandler;

	//Register to sync after main Havok plugin

	Vision::RegisterModule( &g_vHavokAiModule );

	vHavokAiModule::GetInstance()->OneTimeInit();

	vHavokAiNavMeshResourceManager::GetInstance()->OneTimeInit();
	
	//vHavokAiUtil::GetInstance()->OneTimeInit();
  FORCE_LINKDYNCLASS(vHavokAiNavMeshInstance);
  FORCE_LINKDYNCLASS(vHavokAiObstacle);
}

// De-initialize the plugin
void vHavokAiPlugin_cl::OnDeInitEnginePlugin()
{
	//vHavokAiUtil::GetInstance()->OneTimeInit();

	vHavokAiNavMeshResourceManager::GetInstance()->OneTimeDeInit();

	vHavokAiModule::GetInstance()->OneTimeDeInit();

	Vision::UnregisterModule( &g_vHavokAiModule );

	vHavokPhysicsModule::OnBeforeInitializePhysics -= &g_HavokAiModuleCallbackHandler;
	vHavokPhysicsModule::OnAfterDeInitializePhysics -= &g_HavokAiModuleCallbackHandler;
	vHavokPhysicsModule::OnBeforeWorldCreated -= &g_HavokAiModuleCallbackHandler;
	Vision::Callbacks.OnAfterSceneUnloaded -= &g_HavokAiModuleCallbackHandler;

	// To match the VISION_PLUGIN_ENSURE_LOADED
	GetEnginePlugin_vHavok()->DeInitEnginePlugin();

  VISION_HAVOK_UNSYNC_STATICS();
}

//////////////////////////////////////////////////////////////////////////

// Use this to get and initialize the plugin when you link statically
VEXPORT IVisPlugin_cl* GetEnginePlugin_vHavokAi()
{
	return &g_HavokAiPlugin;
}

// The engine uses this to get and initialize the plugin dynamically
#if ((defined _DLL) || (defined _WINDLL)) && !defined(VBASE_LIB)
VEXPORT IVisPlugin_cl* GetEnginePlugin()
{
	return GetEnginePlugin_vHavokAi();
}
#endif // _DLL or _WINDLL

/*
 * Havok SDK - Base file, BUILD(#20140621)
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
