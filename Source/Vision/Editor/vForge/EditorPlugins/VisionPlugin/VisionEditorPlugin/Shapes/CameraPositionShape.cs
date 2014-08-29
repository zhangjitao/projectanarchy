/*
 *
 * Confidential Information of Telekinesys Research Limited (t/a Havok). Not for disclosure or distribution without Havok's
 * prior written consent. This software contains code, techniques and know-how which is confidential and proprietary to Havok.
 * Product and Trade Secret source code contains trade secrets of Havok. Havok Software (C) Copyright 1999-2014 Telekinesys Research Limited t/a Havok. All Rights Reserved. Use of this software is subject to the terms of an end user license agreement.
 *
 */

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Drawing.Design;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using CSharpFramework;
using ManagedFramework;
using CSharpFramework.Math;
using CSharpFramework.Shapes;
using CSharpFramework.PropertyEditors;
using CSharpFramework.DynamicProperties;
using CSharpFramework.UndoRedo;
using CSharpFramework.Actions;
using CSharpFramework.Scene;
using CSharpFramework.View;
using VisionManaged;
using CSharpFramework.Serialization;
using System.Drawing;
using System.Windows.Forms;
using CSharpFramework.ShortCuts;
using CSharpFramework.AssetManagement;
using CSharpFramework.Helper;


namespace VisionEditorPlugin.Shapes
{
  #region Class : CameraPositionShape

	/// <summary>
	/// Class that represents a camera position inside vForge. It uses shortcut keys to set the editor camera
	/// to the position of this shape
	/// </summary>
  [
    Serializable,
    ShapeTypeNiceName("Camera Position")
  ]
	public class CameraPositionShape : ShapeObject3D
	{
    #region Nested Class : CameraPositionShortCut
    
    /// <summary>
    /// Overridden shortcut class
    /// </summary>
    class CameraPositionShortCut : ShortCutAction
    {
      public CameraPositionShortCut(CameraPositionShape shape)
        : base(Keys.None)
      {
        iUniqueAdvance++;
        _shape = shape;
      }

      CameraPositionShape _shape;
      static int iUniqueAdvance = 0;

      public override string ActionName
      {
        get
        {
          return "CameraPositionShortCut#"+iUniqueAdvance.ToString(); // since shape name might not be unique
        }
      }

      public override bool Do()
      {
        _shape.SetEditorCameraPosition();
        return true;
      }

      public override string Category { get {return "Camera Positions";}}

      public override bool Configurable {get {return false;}}
      public override bool Hidden {get {return true;}}
    }

    #endregion

    #region Category Sorting Definitions

    /// <summary>
    /// Category string
    /// </summary>
    protected const string CAT_CAMERA   = "Camera";

    /// <summary>
    /// Category ID
    /// </summary>
    protected const int CATORDER_CAMERA = Shape3D.LAST_CATEGORY_ORDER_ID + 1;

    /// <summary>
    /// Category string
    /// </summary>
    protected const string CAT_VIEW = "Custom View Settings";

    /// <summary>
    /// Category ID
    /// </summary>
    protected const int CATORDER_VIEW = Shape3D.LAST_CATEGORY_ORDER_ID + 2;

    /// <summary>
    /// last used category ID
    /// </summary>
    public new const int LAST_CATEGORY_ORDER_ID = CATORDER_VIEW;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name"></param>
		public CameraPositionShape(string name) : base(name)
		{
			AddHint(HintFlags_e.NoScale);
		}
  
    #endregion
  
    #region Shape3D Overrides

    /// <summary>
    /// Overridden function
    /// </summary>
    public override void OnAddedToScene()
    {
      base.OnAddedToScene ();
      _shortCut = new CameraPositionShortCut(this);
      _shortCut.Shortcut = _key;
      EditorManager.ShortCuts.Add(_shortCut);
    }

    /// <summary>
    /// Overridden function
    /// </summary>
    public override void OnRemoveFromScene()
    {
      base.OnRemoveFromScene ();
      EditorManager.ShortCuts.Remove(_shortCut);
      _shortCut = null;
    }

    /// <summary>
    /// Overridden function
    /// </summary>
    public override bool GetLocalBoundingBox(ref BoundingBox bbox)
    {
      float r = 3.0f * EditorManager.Settings.GlobalUnitScaling;
      bbox = new BoundingBox(-r,-r,-r, r,r,r);
      return true;
    }

    /// <summary>
    /// Overridden function
    /// </summary>
    public override void RenderShape(VisionViewBase view, ShapeRenderMode mode)
    {
      base.RenderShape (view, mode);
      if (Selected) // the ShapeRenderMode does not help here since this shape does not have an engine instance
      {
        view.RenderLineFrustum(this.Position, this.RotationMatrix, 45.0f, 45.0f, 60.0f * EditorManager.Settings.GlobalUnitScaling, VisionColors.Blue, 1.0f);
      }
    }


    /// <summary>
    /// Overridden function
    /// </summary>
    public override void OnDoubleClick()
    {
      SetEditorCameraPosition();
    }

    static string RO_RESETVIEW = "Reset view settings";
    static string RO_BATCH_SCREENSHOTS = "Batch save screenshots for children";

    /// <summary>
    /// Overridden function
    /// </summary>
    public override ArrayList RelevantOperations
    {
      get
      {
        ArrayList op = base.RelevantOperations;
        if (op==null)
          op = new ArrayList(2);
        op.Add(@"Goto this Position");
        op.Add(@"Set to camera position");
        if (_fCustomFar > 0.0f || _fCustomNear > 0.0f || _fCustomFOV > 0 || _bApplyTimeOfDay)
          op.Add(RO_RESETVIEW);
        if (Modifiable && HasChildren())
          op.Add(RO_BATCH_SCREENSHOTS);

        return op;
      }
    }


    void BatchSaveScreenShots()
    {
      ShapeCollection shapes = new ShapeCollection();
      // step #1: filter shapes
      {
        foreach (ShapeBase shape in this.ChildCollection)
        {
          // filter?
          shapes.Add(shape);
        }
        if (shapes.Count == 0)
          return;
      }

      try
      {
        SetEditorCameraPosition();
        int iResX = EditorManager.ActiveView.Size.Width;
        int iResY = EditorManager.ActiveView.Size.Height;
        float fFOVX = 90;
        if (this.FOV > 0)
          fFOVX = this.FOV;

        // step #2: prepare the target dir
        string folder = EditorManager.Project.MakeAbsolute(ShapeName);
        Directory.CreateDirectory(folder);

        // step #3: turn them all to invisible
        {
          foreach (ShapeBase shape in shapes)
          {
            shape.SetVisible(false, true);
          }
        }

        // step #4: render each and save a screenshot
        foreach (ShapeBase shape in shapes)
        {
          shape.SetVisible(true, true);
          string filename = Path.Combine(folder, shape.ShapeName + ".jpg");

          // Render and save
          EditorManager.ActiveView.UpdateView(true);
          EditorManager.ActiveView.UpdateView(true);
          EditorManager.EngineManager.SaveScreenShot(filename, iResX, iResY, fFOVX, 0.0f);
          shape.SetVisible(false, true);
        }

      }
      catch (Exception ex)
      {
        EditorManager.DumpException(ex, true);
      }

      // #5: Cleanup
      {
        foreach (ShapeBase shape in shapes)
        {
          shape.SetVisible(shape.FinalVisibleState, true);
        }
      }

    }


    /// <summary>
    /// Overridden function
    /// </summary>
    public override void PerformRelevantOperation(string name, int iShapeIndex, int iShapeCount)
    {
      if (name==@"Goto this Position")
      {
        SetEditorCameraPosition();
        return;
      }
      if (name==@"Set to camera position")
      {
        EditorManager.Actions.StartGroup("Set to camera position");
        EditorManager.Actions.Add(new MoveShapeAction(this,Position,EditorManager.ActiveView.CameraPosition));
        EditorManager.Actions.Add(new RotateShapeAction(this, Orientation, EditorManager.ActiveView.CameraAngles));
        EditorManager.Actions.EndGroup();
        return;
      }
      if (name == RO_RESETVIEW)
      {
        EditorManager.ActiveView.SetViewSettings(EditorManager.ActiveView.DefaultViewSettings, false);

        // retrieve current time of day:
        GetCurrentSettingsCallbackArgs args = new GetCurrentSettingsCallbackArgs();
        EditorManager.ProfileManager.OnGetCurrentSettings(args);
        if (args._settings != null)
          EditorManager.RendererNodeManager.SetCurrentTime(args._settings._currentTime);

        EditorManager.ActiveView.UpdateView(false);
        return;
      }
      if (name == RO_BATCH_SCREENSHOTS)
      {
        BatchSaveScreenShots();
        return;
      }


      base.PerformRelevantOperation (name, iShapeIndex, iShapeCount);
    }


    public override bool OnExport(SceneExportInfo info)
    {     
      EngineEntity.SetVariable("FovX", _fCustomFOV.ToString());
      EngineEntity.SetVariable("NearClipDistance", NearClipDistance.ToString());
      EngineEntity.SetVariable("FarClipDistance", FarClipDistance.ToString());
      EngineEntity.SetVariable("m_fTimeOfDay", _bApplyTimeOfDay ? _fCustomTimeOfDay.ToString() : "-1.0"); // on the runtime side a negative value disables it
      
      bool bResult = base.OnExport(info);
      return bResult;
    }

    #endregion

    #region Engine Instance

    public override void CreateEngineInstance(bool bCreateChildren)
    {
      _engineInstance = new EngineInstanceEntity("CameraPositionEntity", null, this, null, true);
      base.CreateEngineInstance(bCreateChildren);
      SetEngineInstanceBaseProperties(); // sets the position etc.

      EngineEntity.SetVariable("FovX", _fCustomFOV.ToString());
      EngineEntity.SetVariable("NearClipDistance", NearClipDistance.ToString());
      EngineEntity.SetVariable("FarClipDistance", FarClipDistance.ToString());
    }


    #endregion


    ViewSettings _settings = null;

    /// <summary>
    /// Return the engine instance casted to EngineInstanceEntity
    /// </summary>
    [BrowsableAttribute(false)]
    public EngineInstanceEntity EngineEntity { get { return _engineInstance as EngineInstanceEntity; } }

    /// <summary>
    /// Modifies the editor camera position
    /// </summary>
    public void SetEditorCameraPosition()
    {
      EditorManager.ActiveView.SetCameraPosition(Position);
      EditorManager.ActiveView.SetCameraRotation(Orientation);
      ApplyViewSettings(false);
    }


    public void ApplyViewSettings(bool bCheckOwnership)
    {
      if (_settings != EditorManager.ActiveView.CurrentViewSettings)
      {
        if (bCheckOwnership)
          return;
        _settings = (ViewSettings)EditorManager.ActiveView.CurrentViewSettings.Clone(); // it is important that we have our own pointer here
      }

      ViewSettings defaultS = EditorManager.ActiveView.DefaultViewSettings;
      _settings.NearClipDistance = (_fCustomNear > 0.0f) ? _fCustomNear : defaultS.NearClipDistance;
      _settings.FarClipDistance = (_fCustomFar > 0.0f) ? _fCustomFar : defaultS.FarClipDistance;
      _settings.FOV.X = (_fCustomFOV > 0.0f) ? _fCustomFOV : defaultS.FOV.X;

      EditorManager.ActiveView.SetViewSettings(_settings, false);
      if (_bApplyTimeOfDay)
        EditorManager.RendererNodeManager.SetCurrentTime(_fCustomTimeOfDay);

      EditorManager.ActiveView.UpdateView(false);
    }

    [Browsable(false)]
    public object FOVLiveUpdate
    {
      set
      {
        if (_settings != EditorManager.ActiveView.CurrentViewSettings)
          return;
        float fFOV = _settings.FOV.X;
        if (value is float)
          fFOV = (float)value;
        if (fFOV > 0.0f)
          _settings.FOV.X = fFOV;
        else
          _settings.FOV.X = EditorManager.ActiveView.DefaultViewSettings.FOV.X;

        EditorManager.ActiveView.SetViewSettings(_settings, false);
        EditorManager.ActiveView.UpdateView(false);
      }
    }

    [Browsable(false)]
    public object TODLiveUpdate
    {
      set
      {
        if (!_bApplyTimeOfDay)
          return;
        float fTOD = _fCustomTimeOfDay;
        if (value is float)
          fTOD = (float)value;

        EditorManager.RendererNodeManager.SetCurrentTime(fTOD);
        EditorManager.ActiveView.UpdateView(false);
      }
    }

    #region ISerializable

    /// <summary>
    /// Called when deserializing
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected CameraPositionShape(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      AddHint(HintFlags_e.NoScale);

      if (SerializationHelper.HasElement(info,"key")) // new version
        _key = (Keys)info.GetValue("key", typeof(Keys));
      else if (SerializationHelper.HasElement(info, "_key")) // old version
        _key = (Keys)info.GetValue("_key", typeof(Shortcut));
      if (!CheckValidKey(_key,false))
        _key = Keys.None;

      if (SerializationHelper.HasElement(info, "_fCustomFOV"))
        _fCustomFOV = info.GetSingle("_fCustomFOV");
      if (SerializationHelper.HasElement(info, "_fCustomNear"))
        _fCustomNear = info.GetSingle("_fCustomNear");
      if (SerializationHelper.HasElement(info, "_fCustomFar"))
        _fCustomFar = info.GetSingle("_fCustomFar");
      if (SerializationHelper.HasElement(info, "_fCustomTimeOfDay"))
        _fCustomTimeOfDay = info.GetSingle("_fCustomTimeOfDay");
      if (SerializationHelper.HasElement(info, "_bApplyTimeOfDay"))
        _bApplyTimeOfDay = info.GetBoolean("_bApplyTimeOfDay");
      
    }

    /// <summary>
    /// Called when serializing
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context) 
    {
      base.GetObjectData(info,context);
      info.AddValue("key",_key); // old version was "_key"
      info.AddValue("_fCustomFOV", _fCustomFOV);
      info.AddValue("_fCustomNear", _fCustomNear);
      info.AddValue("_fCustomFar", _fCustomFar);
      info.AddValue("_fCustomTimeOfDay", _fCustomTimeOfDay);
      info.AddValue("_bApplyTimeOfDay", _bApplyTimeOfDay);
    }

    #endregion
  
    /// <summary>
    /// Overridden clone function
    /// </summary>
    /// <returns></returns>
    public override ShapeBase Clone()
    {
      CameraPositionShape copy = (CameraPositionShape)base.Clone ();
      copy._shortCut = null;
      copy._key = Keys.None;
      return copy;
    }

    #region GUI

    /// <summary>
    /// Overridden function to get the icon index
    /// </summary>
    /// <returns></returns>
    public override int GetIconIndex()
    {
      return EditorManager.GUI.ShapeTreeImages.AddBitmap(Path.Combine(EditorManager.AppDataDir, @"bitmaps\Shapes\VideoCamera.bmp"), Color.Magenta);
    }

    #endregion

    #region Properties

    CameraPositionShortCut _shortCut = null;
    Keys _key = Keys.None;

    float _fCustomFOV = 0.0f;
    float _fCustomNear = 0.0f;
    float _fCustomFar = 0.0f;
    bool _bApplyTimeOfDay = false;
    float _fCustomTimeOfDay = 0.5f;

    bool CheckValidKey(Keys newval, bool msgDlg)
    {
      if (newval != Keys.None)
      {
        ShortCutAction regAction = EditorManager.ShortCuts.IsRegistered(newval,_shortCut);
        if (regAction!=null)
        {
          if (msgDlg)
            EditorManager.ShowMessageBox("The shortcut '" + newval.ToString() + "' is already used by the action '" + regAction.ActionName + "'\nPlease use a different shortcut.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
          return false;
        }
      }
      return true;
    }

    [SortedCategory(CAT_CAMERA, CATORDER_CAMERA), PropertyOrder(2)]
    [Description("Optionally defines a keyboard hotkey to jump to this camera in the editor. The hotkey binding must not overlap with other hotkeys.")]
    public Keys ShortCut
    {
      get {return _key;}
      set
      {
        if (value==_key)
          return;
        if (!CheckValidKey(value,true))
          return;
        _key = value;
        if (_shortCut!=null)
          _shortCut.Shortcut = value;
      }
    }

    [SortedCategory(CAT_VIEW, CATORDER_VIEW), PropertyOrder(1)]
    [Description("Associates a custom FOV (field of view) with this camera that will be set upon double-clicking this shape.\nLeave to 0 for default.")]
    [EditorAttribute(typeof(SliderEditor), typeof(UITypeEditor)), SliderRangeAttribute(0.0f, 170.0f, 170), PropertyLiveUpdate("FOVLiveUpdate")]
    public float FOV
    {
      get { return _fCustomFOV; }
      set 
      {
        _fCustomFOV = value;
        if (_fCustomFOV < 0.0f)
          _fCustomFOV = 0.0f;
        if (_fCustomFOV > 170.0f)
          _fCustomFOV = 170.0f;

        if (HasEngineInstance())
          EngineEntity.SetVariable("FovX", _fCustomFOV.ToString());
        UpdateCustomViewSettings();
      }
    }

    [SortedCategory(CAT_VIEW, CATORDER_VIEW), PropertyOrder(2)]
    [Description("Associates a custom near clip distance with this camera that will be set upon double-clicking this shape.\nLeave to 0 for default.")]
    public float NearClipDistance
    {
      get { return _fCustomNear; }
      set
      {
        _fCustomNear = value;
        if (_fCustomNear < 0.0f)
          _fCustomNear = 0.0f;

        if (HasEngineInstance())
          EngineEntity.SetVariable("NearClipDistance", NearClipDistance.ToString());
        UpdateCustomViewSettings();
      }
    }

    [SortedCategory(CAT_VIEW, CATORDER_VIEW), PropertyOrder(3)]
    [Description("Associates a custom far clip distance with this camera that will be set upon double-clicking this shape.\nLeave to 0 for default.")]
    public float FarClipDistance
    {
      get { return _fCustomFar; }
      set
      {
        _fCustomFar = value;
        if (_fCustomFar < 0.0f)
          _fCustomFar = 0.0f;

        if (HasEngineInstance())
          EngineEntity.SetVariable("FarClipDistance", FarClipDistance.ToString());
        UpdateCustomViewSettings();
      }
    }

    [SortedCategory(CAT_VIEW, CATORDER_VIEW), PropertyOrder(10)]
    [Description("If enabled, a custom daytime (TimeOfDay property) is associated with this camera and set upon double clicking the camera shape")]
    public bool ApplyCustomTimeOfDay
    {
      get { return _bApplyTimeOfDay; }
      set
      {
        _bApplyTimeOfDay = value;
        if (_bApplyTimeOfDay)
          UpdateCustomViewSettings();
      }
    }


    [SortedCategory(CAT_VIEW, CATORDER_VIEW), PropertyOrder(11)]
    [Description("Associates a custom daytime with this camera that will be set upon double-clicking this shape. The ApplyCustomTimeOfDay flag has to be set, otherwise this property is ignored")]
    [TypeConverter(typeof(NormalizedFloat2TimeConverter))]
    [EditorAttribute(typeof(ToDSliderEditor), typeof(UITypeEditor)), SliderRange(0.0f, 1.0f, 1440), PropertyLiveUpdate("TODLiveUpdate"),DefaultValue(0.5f)]
    public float TimeOfDay
    {
      get { return _fCustomTimeOfDay; }
      set
      {
        _fCustomTimeOfDay = value;
        if (_fCustomTimeOfDay < 0.0f)
          _fCustomTimeOfDay = 0.0f;
        if (_fCustomTimeOfDay > 1.0f)
          _fCustomTimeOfDay = 1.0f;

        if (_bApplyTimeOfDay)
          UpdateCustomViewSettings();
      }
    }

    void UpdateCustomViewSettings()
    {
      ApplyViewSettings(true);
    }

    #endregion

    #region Plugin information

    /// <summary>
    /// Overridden function
    /// </summary>
    /// <returns></returns>
    public override EditorPluginInfo GetPluginInformation()
    {
      return EditorPlugin.EDITOR_PLUGIN_INFO;
    }

    #endregion

  }

  #endregion

  #region Class : CameraPositionShapeCreator

  class CameraPositionShapeCreator : CSharpFramework.IShapeCreatorPlugin
  {
    public CameraPositionShapeCreator()
    {
      IconIndex = EditorManager.GUI.ShapeTreeImages.AddBitmap(Path.Combine(EditorManager.AppDataDir, @"bitmaps\Shapes\VideoCamera.bmp"), Color.Magenta);
    }

    /// <summary>
    /// Get the name of the creator
    /// </summary>
    /// <returns></returns>
    public override string GetPluginName()
    {
      return "Camera Position";
    }


    /// <summary>
    /// Get the category's name
    /// </summary>
    /// <returns></returns>
    public override string GetPluginCategory()
    {
      return "Objects";
    }

    /// <summary>
    /// Returns a short description text
    /// </summary>
    /// <returns></returns>
    public override string GetPluginDescription()
    {
      return "Adds a camera position to the scene. "+
       "You can jump to this position via the hotkey assigned to the shape.\n" +
       "This shape is mainly used inside the vForge and vPlayer to improve workflow.";
    }


    /// <summary>
    /// Overridden function to create the shape instance
    /// </summary>
    /// <returns></returns>
    public override ShapeBase CreateShapeInstance()
    {
      CameraPositionShape shape = new CameraPositionShape("CameraPosition");
      shape.Position = EditorManager.Scene.CurrentShapeSpawnPosition;
      return shape;
    }

    public override Type GetShapeType()
    {
      return typeof(CameraPositionShape);
    }
  }


  #endregion

}

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
