﻿using UnityEngine;
using UnityEngine.Rendering;
using MessagePack;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using AIGraphics.Settings;
using static AIGraphics.Settings.CameraSettings;

namespace AIGraphics
{
    [MessagePackObject(true)]
    public struct SkyboxParams
    {
        public float exposure;
        public float rotation;
        public Color tint;
        public SkyboxParams(float exposure, float rotation, Color tint)
        {
            this.exposure = exposure;
            this.rotation = rotation;
            this.tint = tint;
        }
    };

    internal class SkyboxManager : MonoBehaviour
    {
        static readonly int _Exposure = Shader.PropertyToID("_Exposure");
        static readonly int _Rotation = Shader.PropertyToID("_Rotation");
        static readonly int _Tint = Shader.PropertyToID("_Tint");

        public SkyboxParams skyboxParams = new SkyboxParams(1f, 0f, new Color32(128, 128, 128, 128));
        public Material Skyboxbg { get; set; }
        public Material Skybox { get; set; }
        public Material MapSkybox { get; set; }

        internal List<string> CubemapPaths { get; set; }
        internal List<Texture2D> CubemapPreviewTextures { get; set; }

        internal static string noCubemap = "No skybox";
        private string selectedCubeMap = noCubemap;

        private string cubemapPath;
        private FolderAssist CubemapFolder;
        
        private GameObject probeParent;
        private ReflectionProbe probe;

        internal Camera Camera { get; set; }
        internal AIGraphics Parent { get; set; }

        public bool Update { get; set; }

        internal BepInEx.Logging.ManualLogSource Logger { get; set; }

        public void ApplySkybox()
        {
            Parent.LightingSettings.SkyboxSetting = Skybox;
            Parent.LightingSettings.AmbientModeSetting = LightingSettings.AIAmbientMode.Skybox;
            Parent.LightingSettings.ReflectionMode = DefaultReflectionMode.Skybox;
            Skybox sky = Camera.GetOrAddComponent<Skybox>();
            sky.enabled = true;
            sky.material = Skyboxbg;
            Parent.CameraSettings.ClearFlag = AICameraClearFlags.Skybox;            
        }
        public void ApplySkyboxParams()
        {
            Skyboxbg.SetFloat(_Exposure, skyboxParams.exposure);
            Skyboxbg.SetFloat(_Rotation, skyboxParams.rotation);
            Skyboxbg.SetColor(_Tint, skyboxParams.tint);
            Skybox.SetFloat(_Exposure, skyboxParams.exposure);
            Skybox.SetColor(_Tint, skyboxParams.tint);
            Skybox.SetFloat(_Rotation, skyboxParams.rotation);
        }
        public void SaveMapSkyBox()
        {
            //Skybox sky = camera.GetComponent<Skybox>();
            //MapSkybox = null == sky ? RenderSettings.skybox : sky.material;
            //MapSkybox = RenderSettings.skybox;
            MapSkybox = Parent.LightingSettings.SkyboxSetting;
        }
        public void SaveSkyboxParams()
        {
            skyboxParams.exposure = Exposure;
            skyboxParams.rotation = Rotation;
            skyboxParams.tint = Tint;
        }
        public void TurnOffCubeMap(Camera camera)
        {
            //RenderSettings.skybox = MapSkybox;
            Parent.LightingSettings.SkyboxSetting = MapSkybox;
            Skybox sky = camera.GetComponent<Skybox>();
            if (null != sky)
                Object.Destroy(sky);
            if (null == MapSkybox)
            {
                //Parent.LightingSettings.AmbientModeSetting = LightingSettings.AIAmbientMode.Trilight;
                Parent.CameraSettings.ClearFlag = AICameraClearFlags.Colour;
            }
            MapSkybox = null;
        }
        public float Exposure
        {
            get => Skybox.GetFloat(_Exposure);
            set
            {
                Skybox.SetFloat(_Exposure, value);
                Skyboxbg.SetFloat(_Exposure, value);
                skyboxParams.exposure = value;
            }
        }
        public Color Tint
        {
            get => Skybox.GetColor(_Tint);
            set
            {
                Skybox.SetColor(_Tint, value);
                Skyboxbg.SetColor(_Tint, value);
                skyboxParams.tint = value;
            }
        }
        public float Rotation
        {
            get => Skybox.GetFloat(_Rotation);
            set
            {
                Skyboxbg.SetFloat(_Rotation, value);
                Skybox.SetFloat(_Rotation, value);
                skyboxParams.rotation = value;
            }
        }

        public IEnumerator LoadCubemap(string filePath, Camera camera)
        {
            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);
            yield return assetBundleCreateRequest;
            AssetBundle cubemapbundle = assetBundleCreateRequest.assetBundle;
            AssetBundleRequest bundleRequest = assetBundleCreateRequest.assetBundle.LoadAssetAsync<Material>("skybox");
            yield return bundleRequest;
            Skybox = bundleRequest.asset as Material;
            AssetBundleRequest bundleRequestBG = assetBundleCreateRequest.assetBundle.LoadAssetAsync<Material>("skybox-bg");
            yield return bundleRequestBG;
            Skyboxbg = bundleRequestBG.asset as Material;
            if (Skyboxbg == null) Skyboxbg = Skybox;
            cubemapbundle.Unload(false);
            cubemapbundle = null;
            bundleRequestBG = null;
            bundleRequest = null;
            assetBundleCreateRequest = null;

            ApplySkybox();
            ApplySkyboxParams();
            Update = true;
            Resources.UnloadUnusedAssets();

            yield break;
        }

        internal string CurrentCubeMap
        {
            get => selectedCubeMap;
            set
            {
                //if cubemap is changed
                if (null != value && value != selectedCubeMap)
                {
                    //switch off cubemap
                    if (noCubemap == value)
                    {
                        this.TurnOffCubeMap(Camera);
                        if (KKAPI.GameMode.Maker == KKAPI.KoikatuAPI.GetCurrentGameMode()) ToggleCharaMakerBG(true);
                        this.Update = true;
                    }
                    else
                    {
                        //if current skybox isn't set to custom cubemap
                        if (noCubemap == selectedCubeMap)
                        {
                            //TODO - need to save cubemap from Map when Map changes too!
                            if (null != Parent.LightingSettings.SkyboxSetting && "skybox" != Parent.LightingSettings.SkyboxSetting.name )//CubeMapNames.IndexOf(RenderSettings.skybox.name))
                            {
                                //save the skybox in scene/map
                                this.SaveMapSkyBox();
                            }
                        }
                        if (KKAPI.GameMode.Maker == KKAPI.KoikatuAPI.GetCurrentGameMode()) ToggleCharaMakerBG(false);
                        StartCoroutine(LoadCubemap(value, Camera));
                    }
                    selectedCubeMap = value;
                }
            }
        }

        internal string CubemapPath {
            get => cubemapPath;
            set
            {
                cubemapPath = value;                
                LoadCubeMaps();
            }
        }

        private void LoadCubeMaps()
        {
            CubemapFolder = new FolderAssist();
            CubemapFolder.CreateFolderInfo(CubemapPath, "*.cube", true, true);            
            List<string> paths = CubemapFolder.lstFile.Select(file => file.FullPath).ToList<string>();
            CubemapPaths = new List<string>();
            CubemapPreviewTextures = new List<Texture2D>();
            foreach (string path in paths)
                StartCoroutine(LoadCubeMapPreview(path));
        }

        public IEnumerator LoadCubeMapPreview(string filePath)
        {
            AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);
            yield return assetBundleCreateRequest;
            AssetBundle cubemapbundle = assetBundleCreateRequest?.assetBundle;
            AssetBundleRequest bundleRequest = assetBundleCreateRequest?.assetBundle?.LoadAssetAsync<Cubemap>("skybox");
            yield return bundleRequest;
            if(null == bundleRequest || null == bundleRequest.asset)
                yield break;
            Cubemap cubemap = bundleRequest.asset as Cubemap;
            Texture2D texture = new Texture2D(cubemap.width, cubemap.height);
            Color[] CubeMapColors = cubemap.GetPixels(CubemapFace.PositiveX);
            texture.SetPixels(CubeMapColors);
            Scale(texture, 128, 128);
            FlipTextureVertically(texture);            
            CubemapPreviewTextures.Add(texture);
            CubemapPaths.Add(filePath);
            cubemapbundle.Unload(false);
            cubemapbundle = null;
            bundleRequest = null;
            assetBundleCreateRequest = null;
            CubeMapColors = null;
            texture = null;
            yield break;
        }

        internal void DefaultReflectionProbe()
        {
            probeParent = new GameObject("RealtimeReflectionProbe");
            probe = probeParent.AddComponent<ReflectionProbe>();
            probe.name = "Default Reflection Probe";
            probe.mode = ReflectionProbeMode.Realtime;
            probe.boxProjection = false;
            probe.intensity = 1f;
            probe.importance = 100;
            probe.resolution = 512;
            probe.backgroundColor = Color.white;
            probe.hdr = true;
            probe.cullingMask = 1 | ~Camera.cullingMask;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.size = new Vector3(10, 10, 10);
            probe.nearClipPlane = 1;
            probeParent.transform.position = new Vector3(0, 0, 0);
            probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        }

        internal void SetupDefaultReflectionProbe()
        {
            ReflectionProbe[] rps = GetReflectinProbes();
            //disable default realtime reflection probe if scene has realtime reflection probes.
            probe.intensity = (rps.Select(probe => probe.mode == ReflectionProbeMode.Realtime).ToArray().Length > 1) ? 0 : 1;
        }

        internal ReflectionProbe[] GetReflectinProbes()
        {
            return GameObject.FindObjectsOfType<ReflectionProbe>();
        }

        public IEnumerator UpdateEnvironment()//BepInEx.Logging.ManualLogSource logger)
        {
            while (true)
            {
                yield return null;
                if (Update)
                {
                    DynamicGI.UpdateEnvironment();
                    ReflectionProbe[] rps = GetReflectinProbes();
                    for (int i = 0; i < rps.Length; i++)
                    {
                        rps[i].RenderProbe();
                    }
                    Update = false;
                }
            }
        }

        //https://pastebin.com/qkkhWs2J
        private static void Scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
        {
            Rect texR = new Rect(0, 0, width, height);
            _gpu_scale(tex, width, height, mode);

            tex.Resize(width, height);
            tex.ReadPixels(texR, 0, 0, true);
            tex.Apply(true);
        }

        // Internal unility that renders the source texture into the RTT - the scaling method itself.
        //static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode)
        private static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode)
        {
            //We need the source texture in VRAM because we render with it
            src.filterMode = fmode;
            src.Apply(true);

            //Using RTT for best quality and performance. Thanks, Unity 5
            RenderTexture rtt = new RenderTexture(width, height, 32);

            //Set the RTT in order to render to it
            Graphics.SetRenderTarget(rtt);

            //Setup 2D matrix in range 0..1, so nobody needs to care about sized
            GL.LoadPixelMatrix(0, 1, 1, 0);

            //Then clear & draw the texture to fill the entire RTT.
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
            rtt = null;
        }

        private static void FlipTextureVertically(Texture2D original)
        {
            var originalPixels = original.GetPixels();

            Color[] newPixels = new Color[originalPixels.Length];

            int width = original.width;
            int rows = original.height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    newPixels[x + y * width] = originalPixels[x + (rows - y - 1) * width];
                }
            }

            original.SetPixels(newPixels);
            original.Apply();
        }
        
        internal static void ToggleCharaMakerBG(bool active)
        {
            CharaCustom.CharaCustom characustom = GameObject.FindObjectOfType<CharaCustom.CharaCustom>();
            if (null == characustom)
                return;
            Transform bgt = characustom.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "p_ai_mi_createBG00_00");
            if (null != bgt)
                bgt.gameObject.SetActive(active);
        }

        internal void Start()
        {
            DefaultReflectionProbe();
            StartCoroutine(UpdateEnvironment());
        }
    }
}