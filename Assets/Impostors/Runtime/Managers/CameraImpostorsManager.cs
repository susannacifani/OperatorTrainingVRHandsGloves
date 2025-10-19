using System;
using System.Collections.Generic;
using System.Linq;
using Impostors.Attributes;
using Impostors.Jobs;
using Impostors.Managers.QueueSortingMethods;
using Impostors.MemoryUsage;
using Impostors.Structs;
using Impostors.RenderPipelineProxy;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Impostors.Managers
{
    [DefaultExecutionOrder(-776)]
    public class CameraImpostorsManager : MonoBehaviour, IMemoryConsumer
    {
        [SerializeField, DisableAtRuntime]
        private AtlasResolution _atlasResolution = AtlasResolution._2048x2048;

        [Range(0.5f, 4f)]
        [SerializeField]
        public float textureSizeScale = 2f;

        [FormerlySerializedAs("_mainCamera")]
        [SerializeField, DisableAtRuntime]
        public Camera mainCamera = null;

        [FormerlySerializedAs("_directionalLight")]
        [SerializeField, DisableAtRuntime]
        public Light directionalLight = null;

        [SerializeField, DisableAtRuntime]
        private UpdateType _updateType = UpdateType.OnPreCull;

        [SerializeField]
        public Color backgroundClearColor = new Color(.5f, .5f, .5f, 0);

        [SerializeField, Layer]
        public int renderingLayer = 0;

        [FormerlySerializedAs("GraphicsApiProxy")]
        [SerializeField, DisableAtRuntime]
        internal RenderPipelineProxyBase _renderPipelineProxy = default;

        public QueueSortingMethodBase sortingService;

        // 0-realtime, 1-mixed, 2-baked
        [HideInInspector, SerializeField]
        private int _lightLightmapBakeType = 0;

        private enum UpdateType
        {
            OnPreCull,
            OnLateUpdate,
            Manual
        }

        [Header("DEBUG")]
        public bool debugModeEnabled = default;

        public bool debugCascadesModeEnabled = default;

        public bool debugFadingEnabled = default;

        public Color debugColor = Color.green;

        static readonly Gradient CascadeGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.green, 0),
                new GradientColorKey(Color.yellow, 128 / 512f),
                new GradientColorKey(new Color(1.0f, 0.64f, 0.0f), 256 / 512f),
                new GradientColorKey(Color.red, 1),
            }
        };

#if UNITY_EDITOR
        private InstanceData[] _debugListOfImpostorableObjects = null;
#endif

        [SerializeField]
        private ImpostorsChunkPool _chunkPool;

        SimplePlane[] simplePlanes = new SimplePlane[6];
        private NativeList<InstanceData> _instanceDataList;
        private NativeQueue<int> _updateQueue;
        private CommandBufferProxy _commandBufferProxy;

        private MaterialPropertyBlock _chunksRenderingPropertyBlock;
        private List<int> _updateQueueSortingList;
        private List<ImpostorsChunk> _renderedChunks;

        private bool _isDisposed = true;

        private void OnEnable()
        {
            AllocateNativeCollections();
            ImpostorLODGroupsManager.Instance.RegisterImpostorableObjectsManager(this);

            if (_renderPipelineProxy == null)
            {
                string error = "RenderPipelineProxy is not specified!\n\n" +
                               "Impostors won't work without specifying right RenderPipelineProxy. " +
                               "Please, add appropriate RenderPipelineProxy and place it in corresponding field.\n\n" +
                               $"Suggested proxy type:\n'{RenderPipelineProxyTypeProvider.Get().FullName}'";
                Debug.LogError("[IMPOSTORS] " + error, this);
                enabled = false;
#if UNITY_EDITOR
                UnityEditor.EditorGUIUtility.PingObject(this);
                UnityEditor.EditorUtility.DisplayDialog("Impostors Error", error, "Ok");
#endif
                return;
            }

            if (sortingService == null && (sortingService = GetComponent<QueueSortingMethodBase>()) == null)
            {
                string error = $"[IMPOSTORS] QueueSortingMethod is not specified!\n" +
                               $"Please, consider manually adding {nameof(DefaultQueueSortingMethod)} component to '{gameObject}'";
                sortingService = gameObject.AddComponent<DefaultQueueSortingMethod>();
                Debug.LogError(error, this);
            }
#if UNITY_EDITOR
            var currentProxyType = _renderPipelineProxy.GetType();
            var suggestedProxyType = RenderPipelineProxyTypeProvider.Get();
            if (RenderPipelineProxyTypeProvider.IsOneOfStandardProxy(currentProxyType) &&
                currentProxyType != suggestedProxyType)
            {
                string error = "Looks like you are using wrong RenderPipelineProxy!\n" +
                               $"Current: '{currentProxyType.FullName}'.\n" +
                               $"Suggested: '{suggestedProxyType.FullName}'.\n\n" +
                               $"Look at the Impostors documentation about setup for render pipelines.";
                Debug.LogError("[IMPOSTORS] " + error, this);
                UnityEditor.EditorGUIUtility.PingObject(this);
                UnityEditor.EditorUtility.DisplayDialog("Impostors Error", error, "Ok");
            }

            if (UnityEditor.PlayerSettings.graphicsJobs)
            {
                string error =
                    "Impostors doesn't work properly with Graphics Jobs enabled when running in build due to Unity bug.\n\nPlease, consider disabling Graphics Jobs setting:\n\nProject Settings/Player/Other Setting/Rendering/Graphics Jobs";
                Debug.LogError("[IMPOSTORS] " + error, this);

                if (UnityEditor.EditorPrefs.GetBool("Impostors.GraphicsJobWarning.DontShow") == false)
                {
                    bool response = UnityEditor.EditorUtility.DisplayDialog("Impostors Error", error, "Ok", "Don't show again");
                    if (response == false)
                    {
                        UnityEditor.EditorPrefs.SetBool("Impostors.GraphicsJobWarning.DontShow", true);
                    }
                }
            }
#endif

            if (_updateType == UpdateType.OnPreCull)
            {
                _renderPipelineProxy.PreCullCalled += OnPreCullCallback;
            }

            _renderPipelineProxy.PostRenderCalled += OnPostRenderCallback;
        }

        private void OnDisable()
        {
            DisposeNativeCollections();
            ImpostorLODGroupsManager.Instance.UnregisterImpostorableObjectsManager(this);
            if (_updateType == UpdateType.OnPreCull)
            {
                _renderPipelineProxy.PreCullCalled -= OnPreCullCallback;
            }

            _renderPipelineProxy.PostRenderCalled -= OnPostRenderCallback;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (debugModeEnabled)
            {
                if (_debugListOfImpostorableObjects != null &&
                    _debugListOfImpostorableObjects.Length == _instanceDataList.Length)
                {
                    NativeArray<InstanceData>.Copy(_instanceDataList, _debugListOfImpostorableObjects);
                }
                else
                {
                    _debugListOfImpostorableObjects = _instanceDataList.ToArray();
                }
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && _renderPipelineProxy == null)
            {
                if ((_renderPipelineProxy = GetComponent<RenderPipelineProxyBase>()) == null)
                {
                    Debug.LogError(
                        $"[IMPOSTORS] RenderPipelineProxy is required. Please, assign it in the inspector. " +
                        $"Suggested component '{RenderPipelineProxyTypeProvider.Get().Name}'", this);
                }
            }

            if (!Application.isPlaying && sortingService == null)
            {
                if ((sortingService = GetComponent<QueueSortingMethodBase>()) == null)
                {
                    void AddComponent()
                    {
                        UnityEditor.EditorApplication.update -= AddComponent;
                        UnityEditor.Undo.RegisterCompleteObjectUndo(gameObject, "Add DefaultQueueSortingMethod");
                        var sort = UnityEditor.Undo.AddComponent<DefaultQueueSortingMethod>(gameObject);
                        sortingService = sort;
                    }

                    UnityEditor.EditorApplication.update += AddComponent;
                }
            }

            _lightLightmapBakeType = GetLightLightmapBakeType();
        }
#endif

        private void LateUpdate()
        {
            if (_updateType == UpdateType.OnLateUpdate)
                OnPreCullCallback(mainCamera);
        }

        private void AllocateNativeCollections()
        {
            _isDisposed = false;
            _instanceDataList = new NativeList<InstanceData>(Allocator.Persistent);
            _updateQueue = new NativeQueue<int>(Allocator.Persistent);
            _commandBufferProxy = new CommandBufferProxy();
            _commandBufferProxy.CommandBuffer.name = $"ExecuteCommandBuffer Render Impostors for '{mainCamera.name}'";
            _chunksRenderingPropertyBlock = new MaterialPropertyBlock();
            _updateQueueSortingList = new List<int>();
            _renderedChunks = new List<ImpostorsChunk>();
            _chunkPool = new ImpostorsChunkPool(
                Enum.GetValues(typeof(TextureResolution)) as int[],
                (int)_atlasResolution,
                ImpostorLODGroupsManager.Instance.TimeProvider,
                ImpostorLODGroupsManager.Instance.RenderTexturePool,
                ImpostorLODGroupsManager.Instance.MaterialObjectPool);
        }

        private void DisposeNativeCollections()
        {
            _isDisposed = true;
            _instanceDataList.Dispose();
            _updateQueue.Dispose();
            _commandBufferProxy.Dispose();
            _commandBufferProxy = null;
            _chunkPool.Dispose();
            _chunkPool = null;

            _updateQueueSortingList.Clear();
            _updateQueueSortingList = null;

            _renderedChunks.Clear();
            _renderedChunks = null;
        }

        /// <summary>
        /// Adds impostors to rendering queue for specified camera.
        /// </summary>
        /// <param name="camera">Camera, where impostors will be rendered</param>
        public void DrawImpostorsForCamera(Camera camera)
        {
            _chunksRenderingPropertyBlock.SetVector(ShaderProperties._ImpostorsWorldSpaceCameraPosition,
                mainCamera.transform.position);
            _chunksRenderingPropertyBlock.SetColor(ShaderProperties._ImpostorsDebugColor,
                debugModeEnabled ? debugColor : Color.clear);

            if (debugModeEnabled && debugFadingEnabled)
                Shader.EnableKeyword(ShaderKeywords.IMPOSTORS_DEBUG_FADING);
            else
                Shader.DisableKeyword(ShaderKeywords.IMPOSTORS_DEBUG_FADING);

            var chunks = _chunkPool.Chunks;
            for (int i = 0, count = chunks.Count; i < count; i++)
            {
                ImpostorsChunk chunk = chunks[i];
                if (!chunk.IsEmpty)
                {
                    var materialPropertyBlock = _chunksRenderingPropertyBlock;
                    if (debugModeEnabled && debugCascadesModeEnabled)
                    {
                        var debugColor = CascadeGradient.Evaluate(chunk.TextureResolution / 512f) * 0.5f;
                        materialPropertyBlock.SetColor(ShaderProperties._ImpostorsDebugColor, debugColor);
                    }

                    _renderPipelineProxy.DrawMesh(chunk.GetMesh(), Vector3.zero, Quaternion.identity,
                        chunk.GetMaterial(),
                        renderingLayer,
                        camera,
                        0, materialPropertyBlock, castShadows: false, receiveShadows: false,
                        useLightProbes: false);
                }
            }
        }

        public void UpdateImpostorSystem()
        {
            JobHandle dependsOn = ImpostorLODGroupsManager.Instance.SyncTransformsJobHandle;
            ImpostorsCulling_ProfilerMarker.Begin();

            SchedulingCullingJobs_ProfilerMarker.Begin();
            JobHandle cullingJobHandle;
            int impostorsCount = _instanceDataList.Length;

            #region Objects Visibility

            SchedulingObjectsVisibilityJob_ProfilerMarker.Begin();
            {
                ImpostorsUtility.CalculateFrustumPlanes(mainCamera, simplePlanes, 1);
                NativeArray<SimplePlane> simplePlanesArray =
                    new NativeArray<SimplePlane>(simplePlanes, Allocator.TempJob);
                var jobImpostorableObjectsVisibility = new ImpostorableObjectsVisibilityJob
                {
                    cameraPlanes = simplePlanesArray,
                    impostors = _instanceDataList,
                    sharedDataArray = ImpostorLODGroupsManager.Instance.GetSharedDataArray()
                };

                cullingJobHandle =
                    jobImpostorableObjectsVisibility.Schedule(impostorsCount, 32, dependsOn);
                simplePlanesArray.Dispose(cullingJobHandle);
            }
            SchedulingObjectsVisibilityJob_ProfilerMarker.End();

            #endregion //Objects Visibility

            #region Is Need Update Impostors

            SchedulingIsNeedUpdateJob_ProfilerMarker.Begin();
            {
                var jobIsNeedUpdateImpostors = new IsNeedUpdateImpostorsJob()
                {
                    sharedDataArray = ImpostorLODGroupsManager.Instance.GetSharedDataArray(),
                    impostors = _instanceDataList,
                    multiplier =
                        2 * Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) / QualitySettings.lodBias,
                    cameraPosition = mainCamera.transform.position,
                    lightDirection = directionalLight ? directionalLight.transform.forward : Vector3.zero,
                    textureSizeMultiplier = textureSizeScale * Screen.height / QualitySettings.lodBias,
                    gameTime = ImpostorLODGroupsManager.Instance.TimeProvider.Time
                };
                cullingJobHandle = jobIsNeedUpdateImpostors.Schedule(impostorsCount, 32, cullingJobHandle);
            }
            SchedulingIsNeedUpdateJob_ProfilerMarker.End();

            #endregion //Is Need Update Impostors

            #region Add Impostorable Objects to update queue

            JobHandle sortJobHandle;
            SchedulingSortingUpdateQueueJobs_ProfilerMarker.Begin();
            {
                _updateQueue.Clear();
                sortJobHandle = sortingService.Sort(_instanceDataList, _updateQueue, cullingJobHandle);
                JobHandle.ScheduleBatchedJobs();
            }
            SchedulingSortingUpdateQueueJobs_ProfilerMarker.End();

            #endregion //Add Impostorable Objects to update queue

            var chunks = _chunkPool.Chunks;
            NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(chunks.Count, Allocator.Temp);

            #region Updating Impostors

            SchedulingUpdateImpostorsJob_ProfilerMarker.Begin();
            {
                var sharedDataArray = ImpostorLODGroupsManager.Instance.GetSharedDataArray();
                for (int i = 0, count = chunks.Count; i < count; i++)
                {
                    jobHandles.Add(chunks[i].ScheduleUpdateImpostors(cullingJobHandle, sharedDataArray));
                }
            }
            SchedulingUpdateImpostorsJob_ProfilerMarker.End();

            #endregion //Updating Impostors

            SchedulingCullingJobs_ProfilerMarker.End();

            JobHandle.CompleteAll(jobHandles);
            cullingJobHandle.Complete();
            jobHandles.Clear();
            sortJobHandle.Complete();

            ImpostorsCulling_ProfilerMarker.End();

            UpdateImpostorTextures();

            DestroyingEmptyChunks_ProfilerMarker.Begin();
            _chunkPool.DestroyEmpty();
            DestroyingEmptyChunks_ProfilerMarker.End();

            #region Impostors Mesh Creation

            ImpostorsMeshCreation_ProfilerMarker.Begin();
            {
                SchedulingMeshJobs_ProfilerMarker.Begin();
                for (int i = 0, count = chunks.Count; i < count; i++)
                {
                    if (chunks[i].NeedToRebuildMesh)
                    {
                        jobHandles.Add(chunks[i].ScheduleMeshCreation(default));
                    }
                }

                SchedulingMeshJobs_ProfilerMarker.End();

                JobHandle.CompleteAll(jobHandles);
                jobHandles.Dispose();
            }
            ImpostorsMeshCreation_ProfilerMarker.End();

            #endregion //Impostors Mesh Creation
        }

        private void OnPreCullCallback(Camera cam)
        {
#if UNITY_EDITOR
            if (debugModeEnabled && cam.cameraType == CameraType.SceneView)
            {
                ImpostorsSceneCameraRendering_ProfilerMarker.Begin();
                DrawImpostorsForCamera(cam);
                ImpostorsSceneCameraRendering_ProfilerMarker.End();
            }
#endif

            if (cam != mainCamera)
                return;

            ImpostorSystem_ProfilerMarker.Begin();

            if (!Application.isEditor || Time.deltaTime > 0) // if paused game in editor don't run update
                UpdateImpostorSystem();

            DrawImpostors_ProfilerMarker.Begin();
            DrawImpostorsForCamera(cam);
            DrawImpostors_ProfilerMarker.End();

            ImpostorSystem_ProfilerMarker.End();
        }

        private void OnPostRenderCallback(Camera camera)
        {
            if (camera != mainCamera)
                return;
            if (IsEditorAndPaused())
                return;
            _commandBufferProxy?.Clear();
        }

        private void UpdateImpostorTextures()
        {
            if (_updateQueue.Count <= 0)
                return;
            UpdateImpostorTextures_ProfilerMarker.Begin();

            _updateQueueSortingList.Clear();
            if (_updateQueueSortingList.Capacity < _updateQueue.Count)
                _updateQueueSortingList.Capacity = _updateQueue.Count;
            for (int i = 0, count = _updateQueue.Count; i < count; i++)
            {
                int id = _updateQueue.Dequeue();
                ProcessImpostorableObject(id, out bool requiresTextureUpdate);
                if (requiresTextureUpdate)
                    _updateQueueSortingList.Add(id);
            }

            SortingUpdateQueue_ProfilerMarker.Begin();
            if (_updateQueueSortingList.Count < 200) // ignore sorting when it's not optimal
            {
                InstanceDataArrayGroupByChunkId.Group(_instanceDataList.AsArray(), _updateQueueSortingList);
            }

            SortingUpdateQueue_ProfilerMarker.End();

            CalculateLightProbes_ProfilerMarker.Begin();
            var lightProbes =
                ImpostorsUtility.LightProbsUtility.GetLightProbes(_updateQueueSortingList,
                    ImpostorLODGroupsManager.Instance.GetSharedDataArray());
            CalculateLightProbes_ProfilerMarker.End();

            Vector3 cameraPosition = mainCamera.transform.position;
            var bufferProxy = _commandBufferProxy;
            bufferProxy.Clear();
            var cb = bufferProxy.CommandBuffer;
            _renderPipelineProxy.SetFogEnabled(false, cb);
            cb.SetGlobalVector( ShaderProperties.unity_LODFade, new Vector4(10,10));
            bufferProxy.DisableShaderKeyword(ShaderKeywords.SHADOWS_SCREEN);
            bufferProxy.DisableShaderKeyword(ShaderKeywords.SHADOWS_SHADOWMASK);

            if (directionalLight && directionalLight.gameObject.activeInHierarchy &&
                directionalLight.isActiveAndEnabled)
            {
                if (GetLightLightmapBakeType() == 1)
                    bufferProxy.EnableShaderKeyword(ShaderKeywords.SHADOWS_SHADOWMASK);

                Vector4 lightPos = directionalLight.transform.localToWorldMatrix.GetColumn(2);
                Vector4 lightDir = new Vector4(-lightPos.x, -lightPos.y, -lightPos.z, 0).normalized;
                var lightColor = directionalLight.color.linear * directionalLight.intensity;
                if (GetLightLightmapBakeType() == 2)
                    lightColor *= 0;
                cb.SetGlobalVector(ShaderProperties._WorldSpaceLightPos0, lightDir);
                // todo: test on URP ShaderProperties._MainLightColor
                cb.SetGlobalColor(ShaderProperties._MainLightColor, lightColor); // built-in
                cb.SetGlobalColor(ShaderProperties._LightColor0, lightColor); // urp
            }

            // this shitty algorithm is there to minimize SetRenderTarget commands
            _renderedChunks.Clear();
            ImpostorsChunk chunk = null;
            for (int i = 0; i < _updateQueueSortingList.Count; i++)
            {
                var id = _updateQueueSortingList[i];
                var instanceData = _instanceDataList[id];
                if (instanceData.requiredAction != InstanceData.RequiredAction.GoToImpostorMode &&
                    instanceData.requiredAction != InstanceData.RequiredAction.UpdateImpostorTexture)
                {
                    Debug.LogError("Unexpected behaviour. There must be only impostors that require texture update.");
                    continue;
                }

                if (chunk == null || chunk.Id != instanceData.ChunkId)
                {
                    chunk?.EndRendering(cb);
                    chunk = _chunkPool.GetById(instanceData.ChunkId);
                    chunk.BeginRendering(cb);
                    _renderedChunks.Add(chunk);
                }

                // set viewport, clear
                chunk.AddCommandBufferCommands(instanceData.PlaceInChunk, cb);
                cb.ClearRenderTarget(true, true, backgroundClearColor);
                // set V and P matrices, add renderers  
                var impostorLODGroup =
                    ImpostorLODGroupsManager.Instance.GetByInstanceId(instanceData.impostorLODGroupInstanceId);
                impostorLODGroup.AddCommandBufferCommands(bufferProxy, cameraPosition, instanceData.nowScreenSize,
                    lightProbes, i);
            }

            if (chunk != null)
                chunk.EndRendering(cb);

            _renderPipelineProxy.SetFogEnabled(true, cb);
            // restoring projection params to prevent problems with fog and the following rendering
            {
                float farClipPlane;
                cb.SetGlobalVector(ShaderProperties._ProjectionParams,
                    new Vector4(-1, mainCamera.nearClipPlane, (farClipPlane = mainCamera.farClipPlane),
                        1 / farClipPlane));
                cb.SetGlobalVector(ShaderProperties._WorldSpaceCameraPos, mainCamera.transform.position);
                cb.SetViewProjectionMatrices(mainCamera.worldToCameraMatrix, mainCamera.projectionMatrix);
            }

            SchedulingRenderImpostorTextures_ProfilerMarker.Begin();
            _renderPipelineProxy.ScheduleImpostorTextureRendering(cb);
            SchedulingRenderImpostorTextures_ProfilerMarker.End();

            _updateQueue.Clear();
            UpdateImpostorTextures_ProfilerMarker.End();
        }

        internal void AddImpostorableObject(ImpostorLODGroup impostorLODGroup)
        {
            if (_isDisposed)
                throw new AccessViolationException($"{GetType().Name} is disposed.");

            var instanceData = new InstanceData() { impostorLODGroupInstanceId = impostorLODGroup.GetInstanceID() };
            _instanceDataList.Add(instanceData);
        }

        internal void RemoveImpostorableObject(ImpostorLODGroup impostorLodGroup, int index)
        {
            if (_isDisposed)
                return;

            var instanceData = _instanceDataList[index];
            Assert.AreEqual(instanceData.impostorLODGroupInstanceId, impostorLodGroup.GetInstanceID());
            _instanceDataList.RemoveAtSwapBack(index);

            // If this was the last impostorLodGroup in the system then we don't need to replace anything.
            // By providing '-1' we ensure that ImpostorsChunk.RemoveSpecifiedImpostors job won't find any impostors to move around.
            int from = _instanceDataList.Length == 0 ? -1 : _instanceDataList.Length;
            int to = from == -1 ? -1 : index;

            var chunks = _chunkPool.Chunks;
            for (int i = 0, count = chunks.Count; i < count; i++)
            {
                chunks[i].RemoveAllImpostorsByInstanceId(instanceData.impostorLODGroupInstanceId, from, to);
            }
        }

        private void ProcessImpostorableObject(int index, out bool requiresTextureUpdate)
        {
            ProcessImpostorableObject_ProfilerMarker.Begin();
            InstanceData instanceData = _instanceDataList[index];
            SharedData sharedData = ImpostorLODGroupsManager.Instance.GetSharedData(index);
            requiresTextureUpdate = false;
            switch (instanceData.requiredAction)
            {
                case InstanceData.RequiredAction.GoToImpostorMode:
                case InstanceData.RequiredAction.UpdateImpostorTexture:
                    requiresTextureUpdate = true;
                    // mark last impostor as not relevant
                    if (instanceData.ChunkId > 0)
                    {
                        var c = _chunkPool.GetById(instanceData.ChunkId);
                        c.MarkPlaceAsNotRelevant(instanceData.PlaceInChunk,
                            sharedData.settings.fadeTransitionTime, true);
                    }

                    // create new impostor
                    int textureResolution =
                        (int)(instanceData.nowScreenSize * textureSizeScale * Screen.height / QualitySettings.lodBias);
                    textureResolution = math.ceilpow2(textureResolution);
                    textureResolution = Mathf.Clamp(textureResolution, sharedData.settings.minTextureResolution,
                        sharedData.settings.maxTextureResolution);

                    ChunkResolve_ProfilerMarker.Begin();
                    var chunk = _chunkPool.GetWithPlace(textureResolution);
                    int placeInChunk = chunk.GetPlace(in instanceData, in sharedData);
                    instanceData.SetChunk(chunk.Id, placeInChunk);
                    ChunkResolve_ProfilerMarker.End();

                    instanceData.lastUpdate.screenSize = instanceData.nowScreenSize;
                    instanceData.lastUpdate.cameraDirection = instanceData.nowDirection;
                    instanceData.lastUpdate.objectForwardDirection = sharedData.data.forward;
                    instanceData.lastUpdate.distance = instanceData.nowDistance;
                    instanceData.lastUpdate.textureResolution = textureResolution;
                    instanceData.lastUpdate.time = ImpostorLODGroupsManager.Instance.TimeProvider.Time;
                    instanceData.lastUpdate.lightDirection =
                        directionalLight ? directionalLight.transform.forward : Vector3.zero;
                    break;
                case InstanceData.RequiredAction.Cull:
                case InstanceData.RequiredAction.GoToNormalMode:
                    RemovingImpostor_ProfilerMarker.Begin();
                    if (instanceData.ChunkId > 0)
                    {
                        var c = _chunkPool.GetById(instanceData.ChunkId);
                        c.MarkPlaceAsNotRelevant(instanceData.PlaceInChunk,
                            sharedData.settings.fadeTransitionTime, false);
                    }

                    instanceData.SetChunk(0, -1);
                    RemovingImpostor_ProfilerMarker.End();
                    break;
            }

            _instanceDataList[index] = instanceData;
            ProcessImpostorableObject_ProfilerMarker.End();
        }

        public int GetUsedBytes()
        {
            int res = 0;
            res += MemoryUsageUtility.GetMemoryUsage(_instanceDataList);
            res += MemoryUsageUtility.GetMemoryUsage(_updateQueue);
            // todo chunk pool memory
            res += _commandBufferProxy.CommandBuffer.sizeInBytes;

            return res;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;
            if (!debugModeEnabled)
                return;

            var gradient = new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.green, 0),
                    new GradientColorKey(Color.blue, 1),
                }
            };
            var tempChunks = _chunkPool.Chunks.OrderByDescending(x => x.TextureResolution).ToList();
            for (int i = 0; i < tempChunks.Count; i++)
            {
                ImpostorsChunk chunk = tempChunks[i];
                Gizmos.color = gradient.Evaluate((float)i / tempChunks.Count);
                if (chunk.IsEmpty == false)
                {
                    var b = chunk.GetMesh().bounds;
                    Gizmos.DrawWireCube(b.center, b.size);
                }
            }
        }
#endif

        private static bool IsEditorAndPaused()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorApplication.isPaused;
#endif
            return false;
        }

        private static ProfilerMarker ImpostorsCulling_ProfilerMarker = new ProfilerMarker("Impostors Culling");

        private static ProfilerMarker SchedulingCullingJobs_ProfilerMarker =
            new ProfilerMarker("Scheduling Culling Jobs");

        private static ProfilerMarker SchedulingObjectsVisibilityJob_ProfilerMarker =
            new ProfilerMarker("Scheduling ObjectsVisibility Job");

        private static ProfilerMarker SchedulingIsNeedUpdateJob_ProfilerMarker =
            new ProfilerMarker("Scheduling IsNeedUpdate Job");

        private static ProfilerMarker SchedulingSortingUpdateQueueJobs_ProfilerMarker =
            new ProfilerMarker("Scheduling SortingUpdateQueue Jobs");

        private static ProfilerMarker SchedulingUpdateImpostorsJob_ProfilerMarker =
            new ProfilerMarker("Scheduling UpdateImpostors Job");

        private static ProfilerMarker DestroyingEmptyChunks_ProfilerMarker =
            new ProfilerMarker("Destroying Empty Chunks");

        private static ProfilerMarker ImpostorsMeshCreation_ProfilerMarker =
            new ProfilerMarker("Impostors Mesh Creation");

        private static ProfilerMarker SchedulingMeshJobs_ProfilerMarker = new ProfilerMarker("Scheduling Mesh Jobs");

        private static ProfilerMarker ImpostorsSceneCameraRendering_ProfilerMarker =
            new ProfilerMarker("Impostors Scene Camera Rendering");

        private static ProfilerMarker ImpostorSystem_ProfilerMarker = new ProfilerMarker("Impostor System");
        private static ProfilerMarker DrawImpostors_ProfilerMarker = new ProfilerMarker("Draw Impostors");

        private static ProfilerMarker UpdateImpostorTextures_ProfilerMarker =
            new ProfilerMarker("Update Impostor Textures");

        private static ProfilerMarker SortingUpdateQueue_ProfilerMarker = new ProfilerMarker("Sorting Update Queue");

        private static ProfilerMarker CalculateLightProbes_ProfilerMarker =
            new ProfilerMarker("Calculate Light Probes");

        private static ProfilerMarker SchedulingRenderImpostorTextures_ProfilerMarker =
            new ProfilerMarker("Scheduling Render Impostor Textures");

        private static ProfilerMarker ProcessImpostorableObject_ProfilerMarker =
            new ProfilerMarker("Process Impostorable Object");

        private static ProfilerMarker ChunkResolve_ProfilerMarker = new ProfilerMarker("Chunk Resolve");
        private static ProfilerMarker RemovingImpostor_ProfilerMarker = new ProfilerMarker("Removing Impostor");

        public void RequestImpostorTextureUpdate(ImpostorLODGroup impostorLODGroup)
        {
            var index = impostorLODGroup.IndexInImpostorsManager;
            var impostorableObject = _instanceDataList[index];
            impostorableObject.requiredAction = InstanceData.RequiredAction.ForcedUpdateImpostorTexture;
            _instanceDataList[index] = impostorableObject;
        }

        private int GetLightLightmapBakeType()
        {
            #if UNITY_EDITOR
            if (directionalLight == null)
                return 0;
            switch (directionalLight.lightmapBakeType)
            {
                case LightmapBakeType.Realtime:
                    return 0;
                case LightmapBakeType.Mixed:
                    return 1;
                case LightmapBakeType.Baked:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(LightmapBakeType));
            }
            #endif
            return _lightLightmapBakeType;
        }
    }
}