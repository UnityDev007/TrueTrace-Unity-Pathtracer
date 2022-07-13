using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Denoiser {
    private ComputeShader SVGF;
    private ComputeShader AtrousDenoiser;
    private ComputeShader Bloom;
    private ComputeShader AutoExpose;
    private ComputeShader TAA;
    private ComputeShader Upscaler;

    private RenderTexture _ColorDirectIn;
    private RenderTexture _ColorIndirectIn;
    private RenderTexture _ColorDirectOut;
    private RenderTexture _ColorIndirectOut;
    private RenderTexture _PrevPosTex;
    private RenderTexture _ScreenPosPrev;
    private RenderTexture _HistoryDirect;
    private RenderTexture _HistoryIndirect;
    private RenderTexture _HistoryMoment;
    private RenderTexture _HistoryNormalDepth;
    private RenderTexture _NormalDepth;
    private RenderTexture _FrameMoment;
    private RenderTexture _History;
    private RenderTexture _TAAPrev;
    private RenderTexture Intermediate;
    private RenderTexture SuperIntermediate;
    private RenderTexture PrevDepthTex;
    private RenderTexture PrevOutputTex;

    private ComputeBuffer A;
    public ComputeBuffer B;

    private int ScreenWidth;
    private int ScreenHeight;

    private Camera _camera;
    private Matrix4x4 PrevViewProjection;

    private int threadGroupsX;
    private int threadGroupsY;

    private int threadGroupsX2;
    private int threadGroupsY2;

    private int VarianceKernel;
    private int CopyKernel;
    private int ReprojectKernel;
    private int FinalizeKernel;
    private int SVGFAtrousKernel;
    private int AtrousKernel;
    private int AtrousCopyKernel;
    private int AtrousFinalizeKernel;

    private int BloomKernel;

    private int ComputeHistogramKernel;
    private int CalcAverageKernel;
    private int ToneMapKernel;

    private int AutoExposeKernel;
    private int AutoExposeFinalizeKernel;

    private int TAAKernel;
    private int TAAFinalizeKernel;
    private int TAAPrepareKernel;

    private int UpsampleKernel;

    private int SourceWidth;
    private int SourceHeight;

    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB) {
        if(SRGB) {
        ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }


    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, int Width, int Height) {
        if(SRGB) {
        ThisTex = new RenderTexture(Width, Height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(Width, Height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }

    private void CreateRenderTexture2(ref RenderTexture ThisTex, bool SRGB) {
        if(SRGB) {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }


    private void InitRenderTexture() {
        if (_ColorDirectIn == null || _ColorDirectIn.width != SourceWidth || _ColorDirectIn.height != SourceHeight) {
            // Release render texture if we already have one
            if (_ColorDirectIn != null) {
                _ColorDirectIn.Release();
                _ColorIndirectIn.Release();
                _ColorDirectOut.Release();
                _ColorIndirectOut.Release();
                _ScreenPosPrev.Release();
                _PrevPosTex.Release();
                _HistoryDirect.Release();
                _HistoryIndirect.Release();
                _HistoryMoment.Release();
                _HistoryNormalDepth.Release();
                _NormalDepth.Release();
                _FrameMoment.Release();
                _History.Release();
                _TAAPrev.Release();
                PrevOutputTex.Release();
            }

         CreateRenderTexture(ref _ColorDirectIn, false);
         CreateRenderTexture(ref _ColorIndirectIn, false);
         CreateRenderTexture(ref _ColorDirectOut, false);
         CreateRenderTexture(ref _ColorIndirectOut, false);
         CreateRenderTexture(ref _ScreenPosPrev, false);
         CreateRenderTexture(ref _PrevPosTex, false);
         CreateRenderTexture(ref _HistoryDirect, false);
         CreateRenderTexture(ref _HistoryIndirect, false);
         CreateRenderTexture(ref _HistoryMoment, false);
         CreateRenderTexture(ref _HistoryNormalDepth, false);
         CreateRenderTexture(ref _NormalDepth, false);
         CreateRenderTexture(ref _FrameMoment, false);
         CreateRenderTexture(ref _History, false);
         CreateRenderTexture2(ref _TAAPrev, false);
         CreateRenderTexture2(ref PrevDepthTex, false);
         CreateRenderTexture2(ref PrevOutputTex, false);
        }
    }
    
    public Denoiser(Camera Cam, int SourceWidth, int SourceHeight) {
        this.SourceWidth = SourceWidth;
        this.SourceHeight = SourceHeight;
        _camera = Cam;
        if(SVGF == null) {SVGF = Resources.Load<ComputeShader>("Denoiser/SVGF");}
        if(AtrousDenoiser == null) {AtrousDenoiser = Resources.Load<ComputeShader>("Denoiser/Atrous");}
        if(AutoExpose == null) {AutoExpose = Resources.Load<ComputeShader>("Denoiser/AutoExpose");}
        if(Bloom == null) {Bloom = Resources.Load<ComputeShader>("Utility/Bloom");}
        if(TAA == null) {TAA = Resources.Load<ComputeShader>("Denoiser/TAA");}
        if(Upscaler == null) {Upscaler = Resources.Load<ComputeShader>("Denoiser/Upscaler");}

        VarianceKernel = SVGF.FindKernel("kernel_variance");
        CopyKernel = SVGF.FindKernel("kernel_copy");
        ReprojectKernel = SVGF.FindKernel("kernel_reproject");
        FinalizeKernel = SVGF.FindKernel("kernel_finalize");
        SVGFAtrousKernel = SVGF.FindKernel("kernel_atrous");
        AtrousKernel = AtrousDenoiser.FindKernel("Atrous");
        AtrousCopyKernel = AtrousDenoiser.FindKernel("kernel_copy");
        AtrousFinalizeKernel = AtrousDenoiser.FindKernel("kernel_finalize");

        BloomKernel = Bloom.FindKernel("Bloom");

        TAAKernel = TAA.FindKernel("kernel_taa");
        TAAFinalizeKernel = TAA.FindKernel("kernel_taa_finalize");
        TAAPrepareKernel = TAA.FindKernel("kernel_taa_prepare");
        
        UpsampleKernel = Upscaler.FindKernel("kernel_upsample");


        AutoExposeKernel = AutoExpose.FindKernel("AutoExpose");
        AutoExposeFinalizeKernel = AutoExpose.FindKernel("AutoExposeFinalize");
        List<float> TestBuffer = new List<float>();
        TestBuffer.Add(1);
        if(A == null) {A = new ComputeBuffer(1, sizeof(float)); A.SetData(TestBuffer);}
        SVGF.SetInt("screen_width", SourceWidth);
        SVGF.SetInt("screen_height", SourceHeight);
        SVGF.SetInt("TargetWidth", Screen.width);
        SVGF.SetInt("TargetHeight", Screen.height);

        Bloom.SetInt("screen_width", Screen.width);
        Bloom.SetInt("screen_width", Screen.height);

        AtrousDenoiser.SetInt("screen_width", Screen.width);
        AtrousDenoiser.SetInt("screen_height", Screen.height);

        AutoExpose.SetInt("screen_width", Screen.width);
        AutoExpose.SetInt("screen_height", Screen.height);
        AutoExpose.SetBuffer(AutoExposeKernel, "A", A);
        AutoExpose.SetBuffer(AutoExposeFinalizeKernel, "A", A);

        TAA.SetInt("screen_width", Screen.width);
        TAA.SetInt("screen_height", Screen.height);

        threadGroupsX = Mathf.CeilToInt(SourceWidth / 16.0f);
        threadGroupsY = Mathf.CeilToInt(SourceHeight / 16.0f);

        threadGroupsX2 = Mathf.CeilToInt(Screen.width / 16.0f);
        threadGroupsY2 = Mathf.CeilToInt(Screen.height / 16.0f);


        InitRenderTexture();
    }

    public void ExecuteSVGF(int CurrentSamples, int AtrousKernelSize, ref ComputeBuffer _ColorBuffer, ref RenderTexture _target, ref RenderTexture _Albedo, ref RenderTexture _NormTex, bool DiffRes) {
        InitRenderTexture();
        SVGF.SetBool("DiffRes", DiffRes);
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        var PrevMatrix = PrevViewProjection;
        SVGF.SetMatrix("viewprojection", viewprojmatrix);
        SVGF.SetMatrix("prevviewprojection", PrevMatrix);
        SVGF.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        SVGF.SetInt("Samples_Accumulated", CurrentSamples);
        PrevViewProjection = viewprojmatrix;
        SVGF.SetInt("AtrousIterations", AtrousKernelSize);
        bool OddAtrousIteration = (AtrousKernelSize % 2 == 1);
        UnityEngine.Profiling.Profiler.BeginSample("SVGFCopy");
        SVGF.SetBuffer(CopyKernel, "PerPixelRadiance", _ColorBuffer);
        SVGF.SetTexture(CopyKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(CopyKernel, "RWNormalAndDepth", _NormalDepth);
        SVGF.SetTexture(CopyKernel, "RWScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(CopyKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(CopyKernel, "ColorIndirectOut", _ColorIndirectOut);
        SVGF.SetFloat("FarPlane", _camera.farClipPlane);
        SVGF.SetTextureFromGlobal(CopyKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        SVGF.SetTextureFromGlobal(CopyKernel, "DepthTex", "_CameraDepthTexture");
        SVGF.SetTextureFromGlobal(CopyKernel, "PrevDepthTex", "_LastCameraDepthTexture");
        SVGF.SetTexture(CopyKernel, "_CameraNormalDepthTex", _NormTex);
        SVGF.Dispatch(CopyKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("SVGFReproject");
        SVGF.SetTexture(ReprojectKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(ReprojectKernel, "HistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(ReprojectKernel, "HistoryDirectTex", _HistoryDirect);
        SVGF.SetTexture(ReprojectKernel, "HistoryIndirectTex", _HistoryIndirect);
        SVGF.SetTexture(ReprojectKernel, "HistoryMomentTex", _HistoryMoment);
        SVGF.SetTexture(ReprojectKernel, "HistoryTex", _History);
        SVGF.SetTexture(ReprojectKernel, "ColorDirectIn", _ColorDirectOut);
        SVGF.SetTexture(ReprojectKernel, "ColorIndirectIn", _ColorIndirectOut);
        SVGF.SetTexture(ReprojectKernel, "ScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(ReprojectKernel, "ColorDirectOut", _ColorDirectIn);
        SVGF.SetTexture(ReprojectKernel, "ColorIndirectOut", _ColorIndirectIn);
        SVGF.SetTexture(ReprojectKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.Dispatch(ReprojectKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("SVGFVariance");
        SVGF.SetTexture(VarianceKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(VarianceKernel, "ColorIndirectOut", _ColorIndirectOut);
        SVGF.SetTexture(VarianceKernel, "ColorDirectIn", _ColorDirectIn);
        SVGF.SetTexture(VarianceKernel, "ColorIndirectIn", _ColorIndirectIn);
        SVGF.SetTexture(VarianceKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(VarianceKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.SetTexture(VarianceKernel, "HistoryTex", _History);
        SVGF.Dispatch(VarianceKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("SVGFAtrous");
        SVGF.SetTexture(SVGFAtrousKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(SVGFAtrousKernel, "HistoryDirectTex", _HistoryDirect);
        SVGF.SetTexture(SVGFAtrousKernel, "HistoryIndirectTex", _HistoryIndirect);
        for (int i = 0; i < AtrousKernelSize; i++) {
            int step_size = 1 << i;
            bool UseFlipped = (i % 2 == 1);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorDirectOut", (UseFlipped) ? _ColorDirectIn : _ColorDirectOut);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorIndirectOut", (UseFlipped) ? _ColorIndirectIn : _ColorIndirectOut);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorDirectIn", (UseFlipped) ? _ColorDirectOut : _ColorDirectIn);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorIndirectIn", (UseFlipped) ? _ColorIndirectOut : _ColorIndirectIn);
            var step2 = step_size;
            SVGF.SetInt("step_size", step2);
            SVGF.Dispatch(SVGFAtrousKernel, threadGroupsX, threadGroupsY, 1);
        }
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("SVGFFinalize");
        SVGF.SetBuffer(FinalizeKernel, "PerPixelRadiance", _ColorBuffer);
        SVGF.SetTexture(FinalizeKernel, "ColorDirectIn", (OddAtrousIteration) ? _ColorDirectOut : _ColorDirectIn);
        SVGF.SetTexture(FinalizeKernel, "ColorDirectOut", (OddAtrousIteration) ? _ColorDirectIn : _ColorDirectOut);
        SVGF.SetTexture(FinalizeKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(FinalizeKernel, "ColorIndirectIn", (OddAtrousIteration) ? _ColorIndirectOut : _ColorIndirectIn);
        SVGF.SetTexture(FinalizeKernel, "HistoryDirectTex", _HistoryDirect);
        SVGF.SetTexture(FinalizeKernel, "HistoryIndirectTex", _HistoryIndirect);
        SVGF.SetTexture(FinalizeKernel, "HistoryMomentTex", _HistoryMoment);
        SVGF.SetTexture(FinalizeKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(FinalizeKernel, "Result", _target);
        SVGF.SetTexture(FinalizeKernel, "HistoryTex", _History);
        SVGF.SetTexture(FinalizeKernel, "_Albedo", _Albedo);
        
        SVGF.SetTexture(FinalizeKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.Dispatch(FinalizeKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();

    }
    public void ExecuteAtrous(int AtrousKernelSize, float n_phi, float p_phi, float c_phi, ref RenderTexture _PosTex, ref RenderTexture _target, ref RenderTexture _converged, ref RenderTexture _Albedo, ref RenderTexture _NormTex) {
        InitRenderTexture();
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        AtrousDenoiser.SetMatrix("viewprojection", viewprojmatrix);
        AtrousDenoiser.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "PosTex", _PosTex);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "RWNormalAndDepth", _NormalDepth);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "_CameraNormalDepthTex", _NormTex);
        AtrousDenoiser.Dispatch(AtrousCopyKernel, threadGroupsX2, threadGroupsY2, 1);

        Graphics.CopyTexture(_converged, 0, 0, _ColorDirectIn, 0, 0);
            AtrousDenoiser.SetFloat("n_phi", n_phi);
            AtrousDenoiser.SetFloat("p_phi", p_phi);
            AtrousDenoiser.SetInt("KernelSize", AtrousKernelSize);
            AtrousDenoiser.SetTexture(AtrousKernel, "PosTex", _PosTex);
            AtrousDenoiser.SetTexture(AtrousKernel, "NormalAndDepth", _NormalDepth);
            int CurrentIteration = 0;
            for(int i = 1; i <= AtrousKernelSize; i *= 2) {
                var step_size = i;
                var c_phi2 = c_phi;
                bool UseFlipped = (CurrentIteration % 2 == 1);
                CurrentIteration++;
                AtrousDenoiser.SetTexture(AtrousKernel, "ResultIn", (UseFlipped) ? _ColorDirectOut : _ColorDirectIn);
                AtrousDenoiser.SetTexture(AtrousKernel, "Result", (UseFlipped) ? _ColorDirectIn : _ColorDirectOut);
                AtrousDenoiser.SetFloat("c_phi", c_phi2);
                AtrousDenoiser.SetInt("step_width", step_size);
                AtrousDenoiser.Dispatch(AtrousKernel, threadGroupsX2, threadGroupsY2, 1);
                c_phi /= 2.0f;
            }
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "ResultIn", (CurrentIteration % 2 == 1) ? _ColorDirectIn : _ColorDirectOut);
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "_Albedo", _Albedo);
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "Result", _target);
            AtrousDenoiser.Dispatch(AtrousFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);
    }

    public void ExecuteBloom(ref RenderTexture _target, ref RenderTexture _converged) {//need to fix this so it doesnt create new textures every time

        Bloom.SetInt("screen_width", Screen.width);
        Bloom.SetInt("screen_height", Screen.height);
        Bloom.SetTexture(BloomKernel, "OrigTex", _converged);
        Bloom.SetTexture(BloomKernel, "InputTex", _converged);
        Bloom.SetTexture(BloomKernel, "OutputTex", _target);
        Bloom.Dispatch(BloomKernel, threadGroupsX2, threadGroupsY2, 1);



    }


    public void ExecuteAutoExpose(ref RenderTexture _target, ref RenderTexture _converged) {//need to fix this so it doesnt create new textures every time
        AutoExpose.SetTexture(AutoExposeKernel, "InTex", _converged);
        AutoExpose.Dispatch(AutoExposeKernel, 1, 1, 1);
        AutoExpose.SetTexture(AutoExposeFinalizeKernel, "InTex", _converged);
        AutoExpose.SetTexture(AutoExposeFinalizeKernel, "OutTex", _target);
        AutoExpose.Dispatch(AutoExposeFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);


    }

    public void ExecuteTAA(ref RenderTexture Input, ref RenderTexture _Final, int CurrentSamples) {//need to fix this so it doesnt create new textures every time
        
        TAA.SetInt("Samples_Accumulated", CurrentSamples);

        RenderTexture TempTex = RenderTexture.GetTemporary(Input.descriptor);
        RenderTexture TempTex2 = RenderTexture.GetTemporary(Input.descriptor);

        UnityEngine.Profiling.Profiler.BeginSample("TAAKernel Prepare");
        TAA.SetFloat("FarPlane", _camera.farClipPlane);
        TAA.SetTextureFromGlobal(TAAPrepareKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        TAA.SetTextureFromGlobal(TAAPrepareKernel, "DepthTex", "_CameraDepthTexture");
        TAA.SetTexture(TAAPrepareKernel, "ColorIn", Input);
        TAA.SetTexture(TAAPrepareKernel, "ColorOut", TempTex);
        TAA.Dispatch(TAAPrepareKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();


        UnityEngine.Profiling.Profiler.BeginSample("TAAKernel");
        TAA.SetTexture(TAAKernel, "ColorIn", TempTex);
        TAA.SetTextureFromGlobal(TAAKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        TAA.SetTexture(TAAKernel, "TAAPrev", _TAAPrev);
        TAA.SetTexture(TAAKernel, "ColorOut", TempTex2);
        TAA.Dispatch(TAAKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("TAAFinalize");
        TAA.SetTexture(TAAFinalizeKernel, "TAAPrev", _TAAPrev);
        TAA.SetTexture(TAAFinalizeKernel, "ColorOut", _Final);
        TAA.SetTexture(TAAFinalizeKernel, "ColorIn", TempTex2);
        TAA.Dispatch(TAAFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        RenderTexture.ReleaseTemporary(TempTex);
        RenderTexture.ReleaseTemporary(TempTex2);
    }



    public void ExecuteUpsample(ref RenderTexture Input, ref RenderTexture Output, ref RenderTexture OrigPos, int curframe, int cursample) {//need to fix this so it doesnt create new textures every time
        UnityEngine.Profiling.Profiler.BeginSample("Upscale");
        Upscaler.SetInt("curframe", curframe);
        Upscaler.SetInt("cursam", cursample);
        Upscaler.SetInt("source_width", Input.width);
        Upscaler.SetInt("source_height", Input.height);
        Upscaler.SetInt("target_width", Output.width);
        Upscaler.SetInt("target_height", Output.height);
        Upscaler.SetTexture(UpsampleKernel, "PosTex", OrigPos);
        Upscaler.SetVector("CamPos", _camera.transform.position);
        Upscaler.SetFloat("FarPlane", _camera.farClipPlane);
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "Albedo", "_CameraGBufferTexture0");
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "DepthTex", "_CameraDepthTexture");
       // Upscaler.SetTextureFromGlobal(UpsampleKernel, "PrevDepthTex", "_LastCameraDepthTexture");
        Upscaler.SetTexture(UpsampleKernel, "PrevDepthTex", PrevDepthTex);
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "NormalTex", "_CameraGBufferTexture2");
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RenderTexture TempTex = RenderTexture.GetTemporary(Output.descriptor);
        RenderTexture TempTex2 = RenderTexture.GetTemporary(Output.descriptor);
        Upscaler.SetTexture(UpsampleKernel, "Input", Input);
        Upscaler.SetTexture(UpsampleKernel, "Output", Output);
        Upscaler.SetTexture(UpsampleKernel, "PrevOutput", PrevOutputTex);
        Upscaler.SetTexture(UpsampleKernel, "PrevDepthTexWrite", TempTex);
        Upscaler.Dispatch(UpsampleKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();
        Graphics.CopyTexture(TempTex, PrevDepthTex);
        Graphics.CopyTexture(Output, PrevOutputTex);
        RenderTexture.ReleaseTemporary(TempTex);
        RenderTexture.ReleaseTemporary(TempTex2);
//PrevDepthTex = Shader.GetGlobalTexture("_LastCameraDepthTexture");

    }

}


