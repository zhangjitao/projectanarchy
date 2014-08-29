/*
 *
 * Confidential Information of Telekinesys Research Limited (t/a Havok). Not for disclosure or distribution without Havok's
 * prior written consent. This software contains code, techniques and know-how which is confidential and proprietary to Havok.
 * Product and Trade Secret source code contains trade secrets of Havok. Havok Software (C) Copyright 1999-2014 Telekinesys Research Limited t/a Havok. All Rights Reserved. Use of this software is subject to the terms of an end user license agreement.
 *
 */


#ifndef HK_VISIONBEHAVIOR_MODULE_H
#define HK_VISIONBEHAVIOR_MODULE_H

#include <Vision/Runtime/EnginePlugins/Havok/HavokBehaviorEnginePlugin/vHavokBehaviorIncludes.hpp>
#include <Behavior/Physics2012Bridge/hkbpPhysicsInterface.h>
#include <Behavior/Behavior/Utils/hkbSceneModifierUtils.h>

class hkaiWorld;
class hkbCharacter;
class hkbWorld;
class hkbProjectAssetManager;
class hkbAssetLoader;
class hkbScriptAssetLoader;
class hkbBehaviorContext;
class hkProcessContext;
class vHavokBehaviorComponent;
class vHavokVisualDebugger;
class vHavokPhysicsModule;

class vHavokPhysicsStepper : public hkbpPhysicsInterface
{
public:
	vHavokPhysicsStepper( hkpWorld* world, hkJobQueue* jobQueue, hkThreadPool* threadPool );
	virtual ~vHavokPhysicsStepper() {}

	/// Advance the physics of the given world by the given timestep.
	virtual void step( hkReal timestep ) HK_OVERRIDE;
};

///
/// \brief
///   Module responsible for the behavior simulation.
///
class vHavokBehaviorModule : public IVisCallbackHandler_cl
{
	public:

		friend class vHavokBehaviorComponent;
		friend class vHavokBehaviorOnUpdateSceneFinishedHandler_cl;

    ///
    /// @name Constructor / Destructor
    /// @{
    ///

		vHavokBehaviorModule();

		VHAVOKBEHAVIOR_IMPEXP virtual ~vHavokBehaviorModule();

    ///
    /// @}
    ///

    /// \brief
		///   Should be called at plugin initialization time.
		VHAVOKBEHAVIOR_IMPEXP void OneTimeInit();

    /// \brief
		///   Should be called at plugin de-initialization time.
		VHAVOKBEHAVIOR_IMPEXP void OneTimeDeInit();

    /// \brief
		///   IVisCallbackHandler_cl implementation.
		VHAVOKBEHAVIOR_IMPEXP virtual void OnHandleCallback(IVisCallbackDataObject_cl* pData) HKV_OVERRIDE;

		VHAVOKBEHAVIOR_IMPEXP static vHavokBehaviorModule* GetInstance();

    /// \brief
		///   Add a character to the world.
		VHAVOKBEHAVIOR_IMPEXP hkbCharacter* addCharacter( vHavokBehaviorComponent* character );

    /// \brief
		///   Remove a character from the world.
		VHAVOKBEHAVIOR_IMPEXP void removeCharacter( vHavokBehaviorComponent* character );

    /// \brief
		///   Step the behavior world (including physics).
		VHAVOKBEHAVIOR_IMPEXP void stepModule();

    /// \brief
		///   Update properties if editor settings have changed.
		VHAVOKBEHAVIOR_IMPEXP void checkEditorModeChanged();

    /// \brief
		///   Returns the project asset manager associated with the behavior module.
		hkbProjectAssetManager* GetProjectAssetManager() const;

    ///
		/// @name Accessors
    /// @{
    ///

		inline hkbWorld* getBehaviorWorld() { return m_behaviorWorld; }
		inline hkbAssetLoader* getAssetLoader() { return m_assetLoader; }
		inline bool shouldStepWorld() { return m_stepWorld; }
		inline const hkArray<vHavokBehaviorComponent*>& getCharacters() { return m_visionCharacters; }

    ///
    /// @}
    ///

	private:
		/// Cleans up the state of the blend components and prepares them for the new frame of simulation
		void OnFrameStart();

    /// \brief
		///   Updates the pose of the entity.
		void UpdatePose();

		void InitWorld(vHavokPhysicsModule* physicsModule);

		void DeInitWorld();

	protected:

		/// Havok Physics module
		vHavokPhysicsModule* m_physicsModule;

		/// hkbWorld to hold the characters
		hkbWorld* m_behaviorWorld;

		/// Contexts used to manage viewers for VDB
		hkbBehaviorContext* m_behaviorContext;

		/// Utility to load behavior files
		hkbProjectAssetManager* m_projectAssetManager;

		/// Asset loader to load assets
		hkbAssetLoader* m_assetLoader;
        hkbScriptAssetLoader *m_scriptAssetLoader;

		/// Pointers to Vision behavior components
		hkArray< vHavokBehaviorComponent* > m_visionCharacters;

		/// Whether to step the world in the editor
		bool m_stepWorld;

		/// Whether the pose has been reinitialized (needs a behavior step to do so)
		bool m_reinitializedPoses;

		/// Physics Stepper - calls back into the Vision physics step function
		vHavokPhysicsStepper* m_physicsStepper;

	protected:
	  /// One global instance of our manager
		static vHavokBehaviorModule g_GlobalManager;
};


//Lua script registration helpers
void EnsureHavokBehaviorScriptRegistration();

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
