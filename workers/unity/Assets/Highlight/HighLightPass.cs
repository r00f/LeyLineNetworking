using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace FX.Highlight
{

    /*
#if UNITY_EDITOR
    [UnityEditor.Rendering.HighDefinition.CustomPass(typeof(HighlightPass))]
    public class HighlightPassEditor : CustomPass
    {
        // Remove the target / clear color fields, we don't need them
        protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
    }
#endif
*/
    public class HighlightPass : CustomPass
    {
        //Filter settings
        public LayerMask LayerMask = 1; // Layer mask Default enabled

        // [Header("Distance Settings")]
        [SerializeField]
        private float _maxDistance = 15f;

        [SerializeField]
        private float _lerpMaxDistance = 15f;

        // [Header("Outline Settings")]

        #region [Fields]
        [SerializeField, Range(1, 3)]
        private float _samplePrecision = 1;
    
        [SerializeField]
        private float _outlineWidth = 5;
    
        [SerializeField, ColorUsage(true, true)]
        private Color _outerColor = new Color(1, 1, 0, 0.5f);
    
        [SerializeField, Range(0, 1)]
        private float _behindFactor = 0.2f;

        [SerializeField, Range(0, 1)]
        private float _innerBehindFactor = 0.2f;

        // [Header("Object Settings")]

        [SerializeField, ColorUsage(true, true)]
        private Color _innerColor = new Color(1, 1, 0, 0.5f);
    
        [SerializeField]
        private Texture _texture = default;
    
        [SerializeField]
        private Vector2 _texturePixelSize = new Vector2(64,64);

        // [Header("Debug")]
    
        [SerializeField]
        private Shader _fullscreenShader;

        [SerializeField]
        private Shader _objectShader;

        private static ShaderTagId[] _forwardShaderTags;

        private static readonly int MaxDist = Shader.PropertyToID("_MaxDistance");
        private static readonly int LerpMaxDist = Shader.PropertyToID("_LerpMaxDistance");

        // Cache the shaderTagIds so we don't allocate a new array each frame
        private ShaderTagId[] _cachedShaderTagIDs;

        private Material _objectMaterial;
        private int _objectPass;
        
        private Material _fullscreenMaterial;
        private int _fullscreenPass;

        private int _fadeValueId;

        #endregion

        private static readonly int SamplePrecision = Shader.PropertyToID("_SamplePrecision");
        private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int InnerColor = Shader.PropertyToID("_InnerColor");
        private static readonly int OuterColor = Shader.PropertyToID("_OuterColor");
        private static readonly int Texture = Shader.PropertyToID("_Texture");
        private static readonly int TextureSize = Shader.PropertyToID("_TextureSize");
        private static readonly int BehindFactor = Shader.PropertyToID("_BehindFactor");
        private static readonly int InnerBehindFactor = Shader.PropertyToID("_InnerBehindFactor");

        ProfilingSampler outlineObjectsSampler = new ProfilingSampler("Render Outline Objects");
        ProfilingSampler fullscreenOutlineSampler = new ProfilingSampler("Fullscreen Outline Pass");

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _objectShader = Shader.Find("HDRP/PostFX/HighlightPassDrawRenderers");
            _fullscreenShader = Shader.Find("HDRP/PostFX/HighlightPassFullscreen");
            _objectMaterial = CoreUtils.CreateEngineMaterial(_objectShader);
            _fullscreenMaterial = CoreUtils.CreateEngineMaterial(_fullscreenShader);

            _objectPass = _objectMaterial.FindPass("FirstPass");
            _fullscreenPass = _fullscreenMaterial.FindPass("FirstPass");

            _fadeValueId = Shader.PropertyToID("_FadeValue");

            _forwardShaderTags = new[]
                                 {
                                 new ShaderTagId("Forward"), // HD Lit shader
                                 new ShaderTagId("ForwardOnly"), // HD Unlit shader
                                 new ShaderTagId("SRPDefaultUnlit"), // Cross SRP Unlit shader
                                 new ShaderTagId(""), // Add an empty slot for the override material
                              };
        }

        protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters,
                                                           HDCamera hdCamera)
        {
            cullingParameters.cullingMask |= (uint)(int)LayerMask;
        }

        /// <summary>
        /// Execute the DrawRenderers with parameters setup from the editor
        /// </summary>
        protected override void Execute(ScriptableRenderContext renderContext,
                                        CommandBuffer cmd,
                                        HDCamera hdCamera,
                                        CullingResults cullingResult)
        {
            // Render outline objects into the custom buffer
            using (new ProfilingScope(cmd, outlineObjectsSampler))
            {
                SetCustomRenderTarget(cmd, clearFlags: ClearFlag.All);
                RenderOutlineObjects(renderContext, cmd, hdCamera, cullingResult);
            }

            using (new ProfilingScope(cmd, fullscreenOutlineSampler))
            {
                ResolveMSAAColorBuffer(cmd, hdCamera);
                SetCameraRenderTarget(cmd);

                _fullscreenMaterial.SetFloat(_fadeValueId, fadeValue);
                _fullscreenMaterial.SetFloat(SamplePrecision, _samplePrecision);
                _fullscreenMaterial.SetFloat(OutlineWidth, _outlineWidth);
                _fullscreenMaterial.SetColor(InnerColor, _innerColor);
                _fullscreenMaterial.SetColor(OuterColor, _outerColor);
                _fullscreenMaterial.SetTexture(Texture, _texture);
                _fullscreenMaterial.SetVector(TextureSize, _texturePixelSize);
                _fullscreenMaterial.SetFloat(BehindFactor, _behindFactor);
                _fullscreenMaterial.SetFloat(InnerBehindFactor, _innerBehindFactor);

                // Don't forget shaderPassId: otherwise you call the override taking a `RenderTargetIdentifier` and it causes issues
                CoreUtils.DrawFullScreen(cmd, _fullscreenMaterial, shaderPassId: _fullscreenPass);
            }
        }

        void RenderOutlineObjects(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            var stateBlock = new RenderStateBlock();

            //PerObjectData renderConfig = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask)
                                            //? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask
                                            //: HDUtils.k_RendererConfigurationBakedLighting;

            var result = new RendererListDesc(_forwardShaderTags, cullingResult, hdCamera.camera)
            {
               // rendererConfiguration = renderConfig,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                overrideMaterial = _objectMaterial,
                overrideMaterialPassIndex = _objectPass,
                stateBlock = stateBlock,
                layerMask = LayerMask,
            };

            _objectMaterial.SetFloat(MaxDist, _maxDistance);
            _objectMaterial.SetFloat(LerpMaxDist, _lerpMaxDistance);

            HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(result));
        }

        protected override void Cleanup()
        {
            base.Cleanup();

            CoreUtils.Destroy(_objectMaterial);
        }
    }
}
