﻿using System.Text;
using AssetStudio;

namespace n3k0c4t.live2d.an1m4ti0n;

internal class CubismMotion3Converter
{
    private Dictionary<uint, string> bonePathHash = new();
    public List<ImportedKeyframedAnimation> AnimationList { get; protected set; } = new();

    public CubismMotion3Converter(List<string> paramNames, List<string> partsNames, AnimationClip[] animationClips)
    {
        CreateBonePathHash(paramNames, partsNames);
        ConvertAnimations(animationClips);
    }

    private void ConvertAnimations(AnimationClip[] animationClips)
    {
        foreach (var animationClip in animationClips)
        {
            var iAnim = new ImportedKeyframedAnimation();
            AnimationList.Add(iAnim);
            iAnim.Name = animationClip.m_Name;
            iAnim.SampleRate = animationClip.m_SampleRate;
            iAnim.Duration = animationClip.m_MuscleClip.m_StopTime;
            var m_Clip = animationClip.m_MuscleClip.m_Clip;
            var streamedFrames = m_Clip.m_StreamedClip.ReadData();
            var m_ClipBindingConstant = animationClip.m_ClipBindingConstant;
            for (var frameIndex = 1; frameIndex < streamedFrames.Count - 1; frameIndex++)
            {
                var frame = streamedFrames[frameIndex];
                for (var curveIndex = 0; curveIndex < frame.keyList.Count; curveIndex++)
                    ReadStreamedData(iAnim, m_ClipBindingConstant, frame.time, frame.keyList[curveIndex]);
            }

            var m_DenseClip = m_Clip.m_DenseClip;
            var streamCount = m_Clip.m_StreamedClip.curveCount;
            for (var frameIndex = 0; frameIndex < m_DenseClip.m_FrameCount; frameIndex++)
            {
                var time = m_DenseClip.m_BeginTime + frameIndex / m_DenseClip.m_SampleRate;
                var frameOffset = frameIndex * m_DenseClip.m_CurveCount;
                for (var curveIndex = 0; curveIndex < m_DenseClip.m_CurveCount; curveIndex++)
                {
                    var index = streamCount + curveIndex;
                    ReadCurveData(iAnim, m_ClipBindingConstant, (int)index, time, m_DenseClip.m_SampleArray,
                        (int)frameOffset, curveIndex);
                }
            }

            var m_ConstantClip = m_Clip.m_ConstantClip;
            var denseCount = m_Clip.m_DenseClip.m_CurveCount;
            var time2 = 0.0f;
            for (var i = 0; i < 2; i++)
            {
                for (var curveIndex = 0; curveIndex < m_ConstantClip.data.Length; curveIndex++)
                {
                    var index = streamCount + denseCount + curveIndex;
                    ReadCurveData(iAnim, m_ClipBindingConstant, (int)index, time2, m_ConstantClip.data, 0, curveIndex);
                }

                time2 = animationClip.m_MuscleClip.m_StopTime;
            }

            foreach (var m_Event in animationClip.m_Events)
                iAnim.Events.Add(new ImportedEvent
                {
                    time = m_Event.time,
                    value = m_Event.data
                });
        }
    }

    private void ReadStreamedData(ImportedKeyframedAnimation iAnim, AnimationClipBindingConstant m_ClipBindingConstant,
        float time, StreamedClip.StreamedCurveKey curveKey)
    {
        var binding = m_ClipBindingConstant.FindBinding(curveKey.index);
        GetLive2dPath(binding, out var target, out var boneName);
        if (!string.IsNullOrEmpty(boneName))
        {
            var track = iAnim.FindTrack(boneName);
            track.Target = target;
            track.Curve.Add(new ImportedKeyframe<float>(time, curveKey.value, curveKey.inSlope, curveKey.outSlope,
                curveKey.coeff));
        }
    }

    private void ReadCurveData(ImportedKeyframedAnimation iAnim, AnimationClipBindingConstant m_ClipBindingConstant,
        int index, float time, float[] data, int offset, int curveIndex)
    {
        var binding = m_ClipBindingConstant.FindBinding(index);
        GetLive2dPath(binding, out var target, out var boneName);
        if (!string.IsNullOrEmpty(boneName))
        {
            var track = iAnim.FindTrack(boneName);
            track.Target = target;
            var value = data[curveIndex];
            track.Curve.Add(new ImportedKeyframe<float>(time, value, 0, 0, null));
        }
    }

    private void GetLive2dPath(GenericBinding binding, out string target, out string id)
    {
        var path = binding.path;
        id = null;
        target = null;
        if (path != 0 && bonePathHash.TryGetValue(path, out var boneName))
        {
            // Console.WriteLine("bonePathHash {0} -> {1}", path, boneName);
            var index = boneName.LastIndexOf('/');
            id = boneName.Substring(index + 1);
            target = boneName.Substring(0, index);
            if (target == "Parameters")
            {
                if (!boneName.StartsWith("Param") && !boneName.StartsWith("PARAM"))
                    Console.WriteLine("bonePathHash {0} -> {1}", path, boneName);
                target = "Parameter";
            }
            else if (target == "Parts")
            {
                if (!boneName.StartsWith("Parts") && !boneName.StartsWith("PARTS"))
                    Console.WriteLine("bonePathHash {0} -> {1}", path, boneName);
                target = "PartOpacity";
            }
        }
        else
        {
            if (path != 0) Console.WriteLine("bonePathHash NOT FOUND! {0}", path);
            binding.script.TryGet(out MonoScript script);
            switch (script.m_ClassName)
            {
                case "CubismRenderController":
                    target = "Model";
                    id = "Opacity";
                    break;
                case "CubismEyeBlinkController":
                    target = "Model";
                    id = "EyeBlink";
                    break;
                case "CubismMouthController":
                    target = "Model";
                    id = "LipSync";
                    break;
            }
        }
    }

    private void CreateBonePathHash(List<string> paramNames, List<string> partsNames)
    {
        foreach (var name in paramNames)
        {
            var crc = new SevenZip.CRC();
            var bytes = Encoding.UTF8.GetBytes("Parameters/" + name);
            crc.Update(bytes, 0, (uint)bytes.Length);
            if (bonePathHash.ContainsKey(crc.GetDigest()))
                Console.WriteLine("DuplicateKey!");
            bonePathHash[crc.GetDigest()] = "Parameters/" + name;
        }

        foreach (var name in partsNames)
        {
            var crc = new SevenZip.CRC();
            var bytes = Encoding.UTF8.GetBytes("Parts/" + name);
            crc.Update(bytes, 0, (uint)bytes.Length);
            if (bonePathHash.ContainsKey(crc.GetDigest()))
                Console.WriteLine("DuplicateKey!");
            bonePathHash[crc.GetDigest()] = "Parts/" + name;
        }
    }

    private string GetTransformPath(Transform transform)
    {
        transform.m_GameObject.TryGet(out var m_GameObject);
        if (transform.m_Father.TryGet(out var father)) return GetTransformPath(father) + "/" + m_GameObject.m_Name;

        return m_GameObject.m_Name;
    }
}
