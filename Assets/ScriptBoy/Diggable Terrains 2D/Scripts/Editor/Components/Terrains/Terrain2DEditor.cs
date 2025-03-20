using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ScriptBoy.DiggableTerrains2D
{
    abstract class Terrain2DEditor : Editor
    {
        [InitializeOnLoadMethod]
        static void CheckEnterPlayModeOptions()
        {
            if (EditorSettings.enterPlayModeOptionsEnabled)
            {
                EditorSettings.enterPlayModeOptionsEnabled = false;
                Debug.Log("The [EnterPlayModeOptions] is disabled by [DiggableTerrains2D] to avoid errors.");
            }
        }

        static class GUIContents
        {
            public static readonly GUIContent buildOnAwake = new GUIContent("Build On Awake", "Build the terrain when the scene loads?");

            public static readonly GUIContent edge = new GUIContent("Edge", "The edge settings.");
            public static readonly GUIContent edgeHeight = new GUIContent("Height", "The height of edges.");
            public static readonly GUIContent edgeOffset = new GUIContent("Offset", "The offset of edges.");
            public static readonly GUIContent edgeCornerType = new GUIContent("Corner Type", "To handle sharp corners, you have three options:\nSimple: No changes are made to the corners.\nNormal: 2 points are added to the corners.\nRounded: N points are added to make the corners rounded.");
            public static readonly GUIContent edgeUVMapping = new GUIContent("UV Mapping", "Choose X for horizontal texture mapping, Y for vertical mapping, and XY for both axes.");

            public static readonly GUIContent isDiggable = new GUIContent("Is Diggable", "Is the terrain diggable in runtime?");
            public static readonly GUIContent isFillable = new GUIContent("Is Fillable", "Is the terrain fillable in runtime?");

            public static readonly GUIContent simplification = new GUIContent("Simplification", "The simplification threshold.");

            public static readonly GUIContent saveOptions = new GUIContent("Runtime Save Options", "Customize how terrain data is collected when you use the Save or GetData methods during runtime.");
            public static readonly GUIContent compressionMethod = new GUIContent("Compression Method", "Compression method used when saving data.");
            public static readonly GUIContent compressionLevel = new GUIContent("Compression Level", "Compression level used when saving data.");
            public static readonly GUIContent includeSplatMapInSave = new GUIContent("Include Splat Map", "Whether to include the splat map in the save data.");
            public static readonly GUIContent compressSplatMapInSave = new GUIContent("Compress Splat Map", "Whether to compress the splat map in the save data.");


            public static readonly GUIContent collider = new GUIContent("Collider", "The collider settings.");
            public static readonly GUIContent colliderOffset = new GUIContent("Offset", "The offset of colliders.");

            public static readonly GUIContent layers = new GUIContent("Layers", "You can set the colors and textures of terrains, with a maximum of 4 layers.");

            public static readonly GUIContent splatMap = new GUIContent("Splat Map", "The splat map settings.");
            public static readonly GUIContent splatMapTexture = new GUIContent("Texture", "The texture of splat map.");
            public static readonly GUIContent splatMapUVRect = new GUIContent("UV Rect", "The UV rect of splat map.");
        }

        SerializedProperty m_BuildOnAwakeProp;

        SerializedProperty m_CompressionMethodProp;
        SerializedProperty m_CompressionLevelProp;
        SerializedProperty m_IncludeSplatMapInSaveProp;
        SerializedProperty m_CompressSplatMapInSaveProp;

        [NonSerialized] int m_SaveDataSize = 0;
        [NonSerialized] float m_SavingDuration = 0;
        [NonSerialized] float m_LoadingDuration = 0;

        SerializedProperty m_IsDiggableProp;
        SerializedProperty m_IsFillableProp;

        SerializedProperty m_SortingLayerIDProp;
        SerializedProperty m_SortingOrderProp;
        SerializedProperty m_SimplificationProp;

        SerializedProperty m_EdgeHeightProp;
        SerializedProperty m_EdgeOffsetProp;
        SerializedProperty m_EdgeCornerTypeProp;
        SerializedProperty m_EdgeUVMappingProp;

        SerializedProperty m_ColliderOffsetProp;

        SerializedProperty m_LayersProp;

        SerializedProperty m_SplatMapTextureProp;
        SerializedProperty m_SplatMapUVRectProp;

        SerializedProperty m_UseDefaultCheckerTextureProp;
        SerializedProperty m_SplatMapEditorIDProp;

        [SerializeField] Texture m_DefaultCheckerTexture;
        protected Terrain2D m_Terrain;
        protected Tool m_ActiveTool;

        static bool s_FoldoutShapes;

        Tool m_SplatMapTool;
        Tool m_SplatMapTransformTool;

        TerrainLayerList m_LayerList;

        protected virtual void OnEnable()
        {
            this.FindProperties();
            serializedObject.Update();

            m_Terrain = target as Terrain2D;

            if (m_Terrain.name.StartsWith("GameObject"))
            {
                m_Terrain.name = m_Terrain.GetType().Name.AddWordSpaces();
            }

            m_LayerList = new TerrainLayerList(m_LayersProp);


            m_SplatMapTool = new Tool(() => SplatMapPainter.DoPaint(this), SaveSplatMapToDisk);
            m_SplatMapTransformTool = new Tool(DoSplatMapTransformTool);

            var map = m_SplatMapTextureProp.objectReferenceValue as Texture2D;

            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            if (m_UseDefaultCheckerTextureProp.boolValue)
            {
                m_UseDefaultCheckerTextureProp.boolValue = false;
                var layer = m_LayersProp.GetArrayElementAtIndex(0);
                layer.FindPropertyRelative("fillTexture").objectReferenceValue = m_DefaultCheckerTexture;
                layer.FindPropertyRelative("edgeTexture").objectReferenceValue = m_DefaultCheckerTexture;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;

            if (m_ActiveTool != null)
            {
                m_ActiveTool.OnDisable?.Invoke();
                Tools.hidden = false;
            }

            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            if (!Application.isPlaying)
            {
                TerrainTracker.SerDirty(m_Terrain);
            }
        }

        public override void OnInspectorGUI()
        {
            GUI.enabled = !Application.isPlaying;

            EditorGUI.BeginChangeCheck();


            OnShapesGUI();
            EditorGUILayout.Space();

            OnHeaderGUI();
            EditorGUILayout.Space();
            OnEdgeGUI();
            OnColliderGUI();
            OnLayersGUI();
            OnSplatMapGUI();
            OnFooterGUI();
        
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                TerrainTracker.SerDirty(m_Terrain);
            }

            GUI.enabled = true;

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Cannot change terrain settings during runtime.", MessageType.Warning);
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawSaveOptionsGUI();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        protected override void OnHeaderGUI()
        {
            SortingLayerUtility.RenderSortingLayerFields(m_SortingOrderProp, m_SortingLayerIDProp);
            EditorGUILayout.Slider(m_SimplificationProp, Terrain2D.MinSimplification, Terrain2D.MaxSimplification, GUIContents.simplification);

            EditorGUILayout.PropertyField(m_BuildOnAwakeProp, GUIContents.buildOnAwake);
            EditorGUILayout.PropertyField(m_IsDiggableProp, GUIContents.isDiggable);
            EditorGUILayout.PropertyField(m_IsFillableProp, GUIContents.isFillable);
        }

        void DrawSaveOptionsGUI()
        {
            bool foldout = m_CompressionMethodProp.isExpanded;
            foldout = EditorGUILayout.Foldout(foldout, GUIContents.saveOptions);
            m_CompressionMethodProp.isExpanded = foldout;
            if (!foldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_CompressionMethodProp, GUIContents.compressionMethod);
                EditorGUILayout.PropertyField(m_CompressionLevelProp, GUIContents.compressionLevel);

                if (m_SplatMapTextureProp.objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(m_IncludeSplatMapInSaveProp, GUIContents.includeSplatMapInSave);
                    if (m_IncludeSplatMapInSaveProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(m_CompressSplatMapInSaveProp, GUIContents.compressSplatMapInSave);
                    }
                }

                OnSavingOptionsGUI();

                if (m_Terrain.isActiveAndEnabled)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox($" Data Size : {m_SaveDataSize:n0} bytes\n Saving Duration: {m_SavingDuration:n0} ms\n Loading Duration: {m_LoadingDuration:n0} ms ", MessageType.Info);

                    if (GUILayout.Button("Test", GUILayout.ExpandHeight(true)))
                    {
                        if (!m_Terrain.isBuilt) m_Terrain.Build();

                        Timer.Start();
                        byte[] data = m_Terrain.GetData();
                        m_SaveDataSize = data.Length;
                        m_SavingDuration = Timer.Stop();

                        Timer.Start();
                        m_Terrain.LoadData(data);
                        m_LoadingDuration = Timer.Stop();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        protected virtual void OnSavingOptionsGUI()
        {

        }



        protected virtual void OnFooterGUI()
        {

        }

        protected void OnShapesGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New Shape", GUILayout.Height(20)))
            {
                var menu = new GenericMenu();
                var types = AssemblyUtility.FindSubclassOf<Shape2D>();
                foreach (var type in types)
                {
                    string name = type.Name.AddWordSpaces();
                    menu.AddItem(new GUIContent(type.Name.AddWordSpaces()), false, () =>
                    {
                        GameObject gameObject = new GameObject(name.Replace(" Shape 2D", ""), type);
                        gameObject.transform.SetParent(m_Terrain.transform, false);
                        Undo.RegisterCreatedObjectUndo(gameObject, "Create Shape");
                        TerrainTracker.SerDirty(m_Terrain);
                    });
                }
                menu.ShowAsContext();
            }

            if (m_Terrain.transform.childCount > 0 && GUILayout.Button("Clear", GUILayout.Height(20), GUILayout.Width(60)))
            {
                var children = m_Terrain.GetComponentsInChildren<Transform>();
                foreach (var child in children)
                {
                    if (child != m_Terrain.transform)
                        Undo.DestroyObjectImmediate(child.gameObject);
                }
                TerrainTracker.SerDirty(m_Terrain);
            }
            GUILayout.EndHorizontal();
        }


        protected void OnLayersGUI()
        {
            serializedObject.forceChildVisibility = true;
            bool foldout = m_LayersProp.isExpanded;
            foldout = EditorGUILayout.Foldout(foldout, GUIContents.layers);
            m_LayersProp.isExpanded = foldout;

            if (foldout)
            {
                EditorGUI.indentLevel++;
                m_LayerList.DoLayoutList();
                EditorGUI.indentLevel--;
            }
            serializedObject.forceChildVisibility = false;
        }

        protected void OnColliderGUI()
        {
            bool foldout = m_ColliderOffsetProp.isExpanded;
            foldout = EditorGUILayout.Foldout(foldout, GUIContents.collider);
            m_ColliderOffsetProp.isExpanded = foldout;
            if (!foldout) return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.Slider(m_ColliderOffsetProp, Terrain2D.MinColliderOffset, Terrain2D.MaxColliderOffset, GUIContents.colliderOffset);
            }
        }

        protected void OnEdgeGUI()
        {
            bool foldout = m_EdgeHeightProp.isExpanded;
            foldout = EditorGUILayout.Foldout(foldout, GUIContents.edge);
            m_EdgeHeightProp.isExpanded = foldout;
            if (foldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(m_EdgeHeightProp, Terrain2D.MinEdgeHeight, Terrain2D.MaxEdgeHeight, GUIContents.edgeHeight);
                EditorGUILayout.Slider(m_EdgeOffsetProp, Terrain2D.MinEdgeOffset, Terrain2D.MaxEdgeOffset, GUIContents.edgeOffset);
                EditorGUILayout.PropertyField(m_EdgeCornerTypeProp, GUIContents.edgeCornerType);
                EditorGUILayout.PropertyField(m_EdgeUVMappingProp, GUIContents.edgeUVMapping);
                EditorGUI.indentLevel--;
            }
        }

        protected void OnSplatMapGUI()
        {
            if (m_LayersProp.arraySize <= 1) return;

            bool foldout = m_SplatMapTextureProp.isExpanded;
            foldout = EditorGUILayout.Foldout(foldout, GUIContents.splatMap);
            m_SplatMapTextureProp.isExpanded = foldout;
            if (!foldout) return;


            using (new EditorGUI.IndentLevelScope())
            {
                Texture2D map = m_SplatMapTextureProp.objectReferenceValue as Texture2D;

                if (map != null)
                {
                    if (!map.isReadable)
                    {
                        EditorGUILayout.HelpBox("The texture Read/Write is disabled.", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        DrawToolButton(m_SplatMapTool, "Paint");
                        DrawToolButton(m_SplatMapTransformTool, "Transform");
                        EditorGUILayout.EndHorizontal();

                        if (m_ActiveTool == m_SplatMapTool)
                        {
                            SplatMapPainter.DrawBrushSettingsGUI(this);
                        }

                        EditorGUILayout.Space();
                    }
                }


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_SplatMapTextureProp, GUIContents.splatMapTexture);

                Rect rect = GUILayoutUtility.GetLastRect();
                rect = GUIUtility.GUIToScreenRect(rect);

                if (GUILayout.Button("New", GUILayout.Width(40)))
                {
                    SplatMapCreatorWindow.Open(rect, this);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(m_SplatMapUVRectProp, GUIContents.splatMapUVRect);
            }
        }

        protected void DrawToolButton(Tool tool, string lable)
        {
            bool changed = GUI.changed;
            EditorGUI.BeginChangeCheck();
            bool editMode = m_ActiveTool == tool;
            EditModeButton.Draw(ref editMode, lable);
            if (EditorGUI.EndChangeCheck())
            {
                if (m_ActiveTool != null)
                    m_ActiveTool.OnDisable?.Invoke();

                m_ActiveTool = editMode ? tool : null;
            }
            GUI.changed = changed;
        }

        protected virtual void OnSceneGUI(SceneView scene)
        {
            EditorGUI.BeginChangeCheck();

            if (Tools.hidden = m_ActiveTool != null)
            {
                Transform transform = m_Terrain.transform;
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    Handles.DoPositionHandle(transform.position, transform.rotation);
                }
            }

            if (m_ActiveTool != null)
            {
                int controlID = GUIUtility.GetControlID(FocusType.Passive); ;
                HandleUtility.AddDefaultControl(controlID);
                m_ActiveTool.OnSceneGUI();
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                TerrainTracker.SerDirty(m_Terrain);
                scene.Repaint();
            }
        }

        bool DoMoveHandle(ref Vector2 po, Quaternion q)
        {
            EditorGUI.BeginChangeCheck();
            float handleSize = HandleUtility.GetHandleSize(po) * 0.1f;
            po = Handles.FreeMoveHandle(po, handleSize, Vector3.zero, Handles.CubeHandleCap);

            if (Event.current.control)
                po = EditorGridUtility.SnapToGrid2D(po);
            return EditorGUI.EndChangeCheck();
        }

        bool DoPositionHandle(ref Vector2 po, Quaternion q)
        {
            EditorGUI.BeginChangeCheck();
            po = Handles.DoPositionHandle(po, q);
            if (Event.current.control)
                po = EditorGridUtility.SnapToGrid2D(po);
            return EditorGUI.EndChangeCheck();
        }

        void SaveSplatMapToDisk()
        {
            Texture2D map = m_SplatMapTextureProp.objectReferenceValue as Texture2D;
            int w = map.width;
            int h = map.height;

            var temp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.CopyTexture(map, temp);
            Rect r = new Rect(0, 0, w, h);
            RenderTexture.active = temp;
            map.ReadPixels(r, 0, 0);
            map.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temp);

            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
            AssetDatabase.Refresh();


            string path = AssetDatabase.GetAssetPath(map);
            path = FileUtility.GetAbsolutePath(path);
            // byte[] bytes = map.GetPixelData<btye>();
            // var data = map.GetPixelData<Color32>(0);
            //Debug.Log(data.Length);
            //  var bytes = ImageConversion.EncodeArrayToTGA(data, map.graphicsFormat, (uint)map.width, (uint)map.height);

            /*
            Debug.Log($"bytes IsCreated: {bytes.IsCreated}");
   
            Debug.Log($"bytes: {bytes == null}");
            Debug.Log($"path: {path == null}");
            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"i{i}: {bytes[i]}");
            }
            var b2 = bytes.ToArray();

            Debug.Log($"b2: {b2 == null}");
            */
            // if (bytes == null) return;
            // System.IO.File.WriteAllBytes(path, b2);

         //   map.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
         //   var bytes = ImageConversion.EncodeToTGA(map);
            var bytes = map.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);


            path = FileUtil.GetProjectRelativePath(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;


            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            map = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            m_SplatMapTextureProp.objectReferenceValue = map;
            m_SplatMapTextureProp.serializedObject.ApplyModifiedProperties();

            m_Terrain.Build();
        }



        void SaveSplatMap()
        {
            Texture2D map = m_SplatMapTextureProp.objectReferenceValue as Texture2D;
            int w = map.width;
            int h = map.height;

            var temp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.CopyTexture(map, temp);
            Rect r = new Rect(0, 0, w, h);
            RenderTexture.active = temp;
            map.ReadPixels(r, 0, 0);
            map.Apply();
            RenderTexture.ReleaseTemporary(temp);
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
            AssetDatabase.Refresh();
        }


        void DoSplatMapTransformTool()
        {
            if (m_LayersProp.arraySize <= 1)
            {
                if (m_ActiveTool == m_SplatMapTransformTool)
                {
                    m_ActiveTool = null;
                }
                return;
            }

            Rect rect = m_SplatMapUVRectProp.rectValue;

            Vector2 position = rect.position;
            Vector2 scale = rect.size;

            if (DoRectHandale(ref position, ref scale))
            {
                rect.position = position;
                rect.size = scale;
                m_SplatMapUVRectProp.rectValue = rect;
            }
        }

        void DrawRect(Rect rect, Matrix4x4 local2World)
        {
            Vector2 offset = rect.min;
            Vector2 scale = rect.size;
            Vector2 downL = offset;
            Vector2 downR = new Vector2(offset.x + scale.x, offset.y);
            Vector2 upR = new Vector2(offset.x + scale.x, offset.y + scale.y);
            Vector2 upL = new Vector2(offset.x, offset.y + scale.y);
            Vector2 center = new Vector2(offset.x + scale.x / 2, offset.y + scale.y / 2);

            downL = local2World.MultiplyPoint(downL);
            downR = local2World.MultiplyPoint(downR);
            upR = local2World.MultiplyPoint(upR);
            upL = local2World.MultiplyPoint(upL);
            center = local2World.MultiplyPoint(center);

            using (new Handles.DrawingScope(new Color32(0, 122, 200, 255)))
            {
                Handles.DrawAAPolyLine(1, downL, downR, upR, upL, downL);
            }
        }

        protected bool DoRectHandale(ref Vector2 position, ref Vector2 scale)
        {
            Matrix4x4 local2World = m_Terrain.transform.localToWorldMatrix;
            DrawRect(new Rect(position, scale), local2World);

            bool changed = false;
            using (new Handles.DrawingScope(new Color32(0, 122, 200, 255)))
            {
                Vector2 p = position;
                p = local2World.MultiplyPoint(p);
                if (DoPositionHandle(ref p, Quaternion.identity))
                {
                    p = local2World.inverse.MultiplyPoint(p);
                    position = p;
                    changed = true;
                }

                p = position + scale;
                p = local2World.MultiplyPoint(p);
                if (DoMoveHandle(ref p, Quaternion.identity))
                {
                    p = local2World.inverse.MultiplyPoint(p);
                    scale = p - position;
                    changed = true;
                }
            }

            return changed;
        }

        protected class Tool
        {
            public Action OnSceneGUI;
            public Action OnDisable;

            public Tool(Action onSceneGUI, Action onDisable = null)
            {
                this.OnSceneGUI = onSceneGUI;
                this.OnDisable = onDisable;
            }
        }

        static class SplatMapPainter
        {
            enum BrushColorSelector
            {
                Channel, Layer
            }

            enum BrushType
            {
                Circle, Texture
            }

            static float s_BrushRadius = 2;
            static float s_BrushSoftness = 1;
            static float s_BrushOpacity = 1;
            static float s_BrushColor = 1;
            static BrushType s_BrushType;
            static BrushColorSelector s_BrushColorSelector;
            static Texture s_BrushTexture;
            static Vector2 s_BrushPosition;

            public static void DoPaint(Terrain2DEditor terrainEditor)
            {
                if (terrainEditor.m_LayersProp.arraySize <= 1)
                {
                    if (terrainEditor.m_ActiveTool == terrainEditor.m_SplatMapTool)
                    {
                        terrainEditor.m_SplatMapTool.OnDisable();
                        terrainEditor.m_ActiveTool = null;
                    }
                    return;
                }

                Rect rect = terrainEditor.m_SplatMapUVRectProp.rectValue;

                terrainEditor.DrawRect(rect, terrainEditor.m_Terrain.transform.localToWorldMatrix);

                Event e = Event.current;
                Vector2 m = e.mousePosition;
                m = HandleUtility.GUIPointToWorldRay(m).origin;


                int controlID = GUIUtility.GetControlID(FocusType.Passive); ;
                //HandleUtility.AddDefaultControl(controlID);


                Event EVENT = Event.current;
                bool move = false;

                Vector2 newMousePo = HandleUtility.GUIPointToWorldRay(EVENT.mousePosition).origin;
                if (EVENT.alt)
                {
                    s_BrushRadius = (s_BrushPosition - newMousePo).magnitude;
                }
                else
                {
                    move = Vector2.Distance(s_BrushPosition, newMousePo) > s_BrushRadius * 0.2f;
                    if (GUIUtility.hotControl == controlID)
                    {
                        if (move)
                        {
                            float y = s_BrushPosition.y;
                            s_BrushPosition = newMousePo;
                            if (EVENT.control) s_BrushPosition.y = y;
                        }

                    }
                    else s_BrushPosition = newMousePo;
                }

                if (EVENT.type == EventType.MouseDown && EVENT.button == 0)
                {
                    s_BrushPosition = newMousePo;
                    move = true;
                    GUIUtility.hotControl = controlID;
                    EVENT.Use();
                }

                if (EVENT.type == EventType.MouseUp && EVENT.button == 0)
                {
                    var prop = terrainEditor.m_SplatMapEditorIDProp;
                    Undo.RegisterCompleteObjectUndo(terrainEditor.m_SplatMapTextureProp.objectReferenceValue, "Splat Map");
                    terrainEditor.SaveSplatMap();
                    GUIUtility.hotControl = 0;
                    EVENT.Use();
                }

                bool mouseButton = GUIUtility.hotControl == controlID;

                Color color = mouseButton ? Color.cyan : Color.white;
                Handles.color = color;
                Quaternion q = Quaternion.identity;

                if (s_BrushType == BrushType.Circle)
                {
                    Handles.CircleHandleCap(0, s_BrushPosition, q, s_BrushRadius, EVENT.type);
                    Handles.CircleHandleCap(0, s_BrushPosition, q, s_BrushRadius * (1 - s_BrushSoftness), EVENT.type);

                    Handles.color = color.Fade(0.1f);
                    Vector3[] circle = VectorUtility.ConvertToVector3(PolygonUtility.CreateCircle(s_BrushPosition, 20, s_BrushRadius));
                    Handles.DrawAAConvexPolygon(circle);
                }
                else
                {
                    Vector3[] box = VectorUtility.ConvertToVector3(PolygonUtility.CreateBox(s_BrushPosition, Vector2.one * s_BrushRadius * 2));
                    Handles.DrawLine(box[0], box[1]);
                    Handles.DrawLine(box[1], box[2]);
                    Handles.DrawLine(box[2], box[3]);
                    Handles.DrawLine(box[3], box[0]);
                    Handles.color = color.Fade(0.1f);
                    Handles.DrawAAConvexPolygon(box);
                }


                if (move && mouseButton && (EVENT.isMouse && EVENT.type != EventType.MouseUp || EVENT.type == EventType.Used))
                {
                    if (s_BrushType == BrushType.Texture)
                    {
                        if (s_BrushTexture != null)
                        {
                            terrainEditor.m_Terrain.PaintSplatMap(s_BrushPosition, s_BrushTexture, s_BrushRadius * 2, s_BrushOpacity, s_BrushColor);

                        }
                    }
                    else
                    {
                        terrainEditor.m_Terrain.PaintSplatMap(s_BrushPosition, s_BrushRadius, s_BrushSoftness, s_BrushOpacity, s_BrushColor);
                    }
                }
                SceneView.RepaintAll();
            }

            public static void DrawBrushSettingsGUI(Terrain2DEditor terrain2DEditor)
            {
                GUILayout.BeginVertical("Brush Settings", GUI.skin.window);
                s_BrushType = (BrushType)EditorGUILayout.EnumPopup("Paint By", s_BrushType);

                if (s_BrushType == BrushType.Texture)
                {
                    s_BrushTexture = (Texture)EditorGUILayout.ObjectField("Texture", s_BrushTexture, typeof(Texture), false);
                    s_BrushRadius = EditorGUILayout.FloatField("Size", s_BrushRadius * 2) / 2;

                }
                else
                {
                    s_BrushRadius = EditorGUILayout.FloatField("Radius", s_BrushRadius);
                    s_BrushSoftness = EditorGUILayout.Slider("Softness", s_BrushSoftness, 0, 1);
                }


                s_BrushColorSelector = (BrushColorSelector)EditorGUILayout.EnumPopup("Color By", s_BrushColorSelector);

                int n = terrain2DEditor.m_LayersProp.arraySize;
                if (s_BrushColorSelector == BrushColorSelector.Channel)
                {
                    string[] options = new string[n];

                    if (0 < n) options[0] = "Red (Layer 0)";
                    if (1 < n) options[1] = "Green (Layer 1)";
                    if (2 < n) options[2] = "Blue (Layer 2)";
                    if (3 < n) options[3] = "Alpha (Layer 3)";

                    s_BrushColor = EditorGUILayout.Popup("Channel", (int)s_BrushColor, options);
                }
                else
                {
                    s_BrushColor = EditorGUILayout.Slider("Layer", s_BrushColor, 0, n - 1);
                }

                s_BrushOpacity = EditorGUILayout.Slider("Opacity", s_BrushOpacity, 0, 1);
                GUILayout.EndVertical();


                HelpBox.Draw("To resize the brush, hold the <b>Alt</b> button.", 0);

            }
        }

        class SplatMapCreatorWindow : EditorWindow
        {
            Terrain2DEditor m_TerrainEditor;

            enum Res
            {
                _32 = 32,
                _64 = 64,
                _128 = 128,
                _256 = 256,
                _512 = 512,
                _1024 = 1024,
            }

            Res m_Width = Res._256;
            Res m_Height = Res._256;


            public static void Open(Rect rect, Terrain2DEditor terrainEditor)
            {
                EditorApplication.delayCall += () =>
                {
                    var win = GetWindow<SplatMapCreatorWindow>();
                    win.m_TerrainEditor = terrainEditor;
                    win.position = rect;
                    win.maxSize = win.minSize = new Vector2(Mathf.Max(rect.width, 200), 60);
                    win.ShowModal();
                };
            }

            void OnEnable()
            {
                titleContent = new GUIContent("New Splat Map");
            }

            void OnGUI()
            {
                if (m_TerrainEditor == null) Close();

                m_Width = (Res)EditorGUILayout.EnumPopup("Width", m_Width);
                m_Height = (Res)EditorGUILayout.EnumPopup("Height", m_Height);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create")) Create();
                if (GUILayout.Button("Cancel")) Close();
                GUILayout.EndHorizontal();
            }

            void Create()
            {
                Close();

                Texture2D map = m_TerrainEditor.m_SplatMapTextureProp.objectReferenceValue as Texture2D;
                string path = EditorUtility.SaveFilePanel("Create Splat Map", Application.dataPath, "Splat Map", "png");
                int width = (int)m_Width;
                int height = (int)m_Height;
                map = new Texture2D(width, height, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,0, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
                int n = width * height;
                Color32[] colors = new Color32[n];
                for (int i = 0; i < n; i++)
                {
                    colors[i] = new Color32(255, 0, 0, 0);
                }
                map.SetPixels32(colors);
                map.Apply();
                System.IO.File.WriteAllBytes(path, map.EncodeToPNG());
                path = FileUtil.GetProjectRelativePath(path);
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();
                map = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                m_TerrainEditor.m_SplatMapTextureProp.objectReferenceValue = map;
                TerrainTracker.SerDirty(m_TerrainEditor.m_Terrain);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
                map = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                m_TerrainEditor.serializedObject.ApplyModifiedProperties();
            }
        }

        class TerrainLayerList : ReorderableList
        {
            public TerrainLayerList(SerializedProperty elements) : base(elements.serializedObject, elements, true, false, true, true)
            {
                elementHeightCallback = ElementHeightCallback;
                drawElementCallback = DrawElementCallback;
                onCanAddCallback = OnCanAddCallback;
                onCanRemoveCallback = OnCanRemoveCallback;
                onAddCallback = OnAddCallback;
            }

            void OnAddCallback(ReorderableList list)
            {
                serializedProperty.arraySize++;
                var layer = serializedProperty.GetArrayElementAtIndex(serializedProperty.arraySize - 1);
            }

            bool OnCanAddCallback(ReorderableList list)
            {
                return count < 4;
            }

            bool OnCanRemoveCallback(ReorderableList list)
            {
                return count > 1;
            }

            void DrawHeaderCallback(Rect rect)
            {
                EditorGUI.LabelField(rect, "Layers");
            }

            float ElementHeightCallback(int index)
            {
                var prop = serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(prop, true);
            }

            void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                var prop = serializedProperty.GetArrayElementAtIndex(index);

                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(rect, prop, new GUIContent("Layer " + index), true);
                EditorGUI.indentLevel--;
            }
        }

        static class TerrainTracker
        {
            static List<Terrain2D> s_DirtyTerrains;
            static Dictionary<int, float> s_BuildTimes;

            [InitializeOnLoadMethod]
            static void InitEditor()
            {
                s_DirtyTerrains = new List<Terrain2D>();
                s_BuildTimes = new Dictionary<int, float>();
                EditorApplication.update += Update;
                ShapeTracker.onChanged += OnShapeChanged;
            }

            static void Update()
            {
             
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                bool delayHeavyTerrains = GUIUtility.hotControl != 0;

                foreach (var terrain in s_DirtyTerrains.ToArray())
                {
                    if (terrain == null)
                    {
                        s_DirtyTerrains.RemoveAll((t) => t == null);
                        return;
                    }
                    bool isHeavyTerrain = IsHeavyTerrain(terrain);
                    if (isHeavyTerrain && delayHeavyTerrains) continue;
                    Timer.Start();
                    terrain.Build();
                    UpdateBuildTime(terrain, Timer.Stop());
                    s_DirtyTerrains.Remove(terrain);
                }
            }

            static void OnShapeChanged(Shape2D shape)
            {
                Transform transform = shape.transform;
                foreach (var terrain in Terrain2D.instances)
                {
                    
                    if (transform.IsChildOf(terrain.transform))
                    {
                        SerDirty(terrain);
                    }
                }
            }

            static bool IsHeavyTerrain(Terrain2D terrain)
            {
                int id = terrain.GetInstanceID();
                if (s_BuildTimes.ContainsKey(id))
                {
                    return s_BuildTimes[id] > 40;
                }
                s_BuildTimes.Add(id, 0);
                return false;
            }


            static void UpdateBuildTime(Terrain2D terrain, float time)
            {
                int id = terrain.GetInstanceID();
                if (s_BuildTimes.TryGetValue(id, out float currentTime))
                {
                    s_BuildTimes[id] = (currentTime + time) / 2;
                }
                else
                {
                    s_BuildTimes.Add(id, time);
                }
            }

            public static void SerDirty(Terrain2D terrain)
            {
                if (s_DirtyTerrains.Contains(terrain)) return;

                s_DirtyTerrains.Add(terrain);
            }
        }

        static class Timer
        {
            private static float startTime;

            public static void Start()
            {
                startTime = Time.realtimeSinceStartup;
            }

            public static float Stop()
            {
                return (Time.realtimeSinceStartup - startTime) * 1000f;
            }
        }
    }
}