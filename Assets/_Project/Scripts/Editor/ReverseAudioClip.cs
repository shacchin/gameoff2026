using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Project ウィンドウで選んだ AudioClip を逆再生した WAV を、同じフォルダに「元の名前_Reverse.wav」で作る。
/// WebGL ではピッチをマイナスにした逆再生ができないので、逆再生した音は別のファイルとして用意する。
/// （エディター専用。ゲームの実行中には使わない）
/// </summary>
public static class ReverseAudioClip
{
    const string MenuPath = "Assets/逆再生の WAV を作る";

    // 前後の無音を削るときの、無音とみなす音の大きさ（0〜1）
    const float SilenceThreshold = 0.005f;

    [MenuItem(MenuPath)]
    static void CreateReversed()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is AudioClip clip) Create(clip);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem(MenuPath, true)]
    static bool Validate()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is AudioClip) return true;
        }
        return false;
    }

    static void Create(AudioClip clip)
    {
        string sourcePath = AssetDatabase.GetAssetPath(clip);

        // 波形を読めるのは Load Type が Decompress On Load のときだけ
        var importer = AssetImporter.GetAtPath(sourcePath) as AudioImporter;
        if (importer != null && importer.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            Debug.LogError($"{clip.name} の Load Type を Decompress On Load にしてから実行してください", clip);
            return;
        }

        clip.LoadAudioData();
        int channels = clip.channels;
        var samples = new float[clip.samples * channels];
        if (!clip.GetData(samples, 0))
        {
            Debug.LogError($"{clip.name} の波形を読めませんでした", clip);
            return;
        }

        // MP3 は前後に無音が入っていることがある。逆にすると頭の無音になり音が遅れて聞こえるので、前後の無音を削る
        int firstFrame = 0;
        int lastFrame = clip.samples - 1;
        while (firstFrame < lastFrame && IsSilent(samples, firstFrame, channels)) firstFrame++;
        while (lastFrame > firstFrame && IsSilent(samples, lastFrame, channels)) lastFrame--;
        int frameCount = lastFrame - firstFrame + 1;

        // フレーム（全チャンネルぶんの 1 サンプル）単位で並びを逆にする
        var reversed = new float[frameCount * channels];
        for (int i = 0; i < frameCount; i++)
        {
            int src = (lastFrame - i) * channels;
            for (int c = 0; c < channels; c++) reversed[i * channels + c] = samples[src + c];
        }

        string directory = Path.GetDirectoryName(sourcePath);
        string outputPath = Path.Combine(directory, clip.name + "_Reverse.wav").Replace('\\', '/');
        WriteWav(outputPath, reversed, channels, clip.frequency);
        Debug.Log($"逆再生の WAV を作りました: {outputPath}");
    }

    static bool IsSilent(float[] samples, int frame, int channels)
    {
        for (int c = 0; c < channels; c++)
        {
            if (Mathf.Abs(samples[frame * channels + c]) > SilenceThreshold) return false;
        }
        return true;
    }

    /// <summary>16bit PCM の WAV として書き出す。</summary>
    static void WriteWav(string path, float[] samples, int channels, int frequency)
    {
        const int bitsPerSample = 16;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = samples.Length * 2;

        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);                       // fmt チャンクの大きさ
            writer.Write((short)1);                 // PCM
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * blockAlign);   // 1 秒あたりのバイト数
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            foreach (float s in samples)
            {
                writer.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
