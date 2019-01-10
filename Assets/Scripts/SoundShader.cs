using UnityEngine;
using Random = System.Random;

[RequireComponent(typeof(AudioSource))]
public class SoundShader : MonoBehaviour
{
    const int Width = 128; // texture width
    const int Height = 64; // texture height

    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.05f;
    [SerializeField] private Material soundMaterial; // Material for audio synthesis
    RenderTexture soundRT; // シェーダーで加工した結果を保持するためのRenderTexture
    Texture2D soundTexture; 
    float[] textureBuffer; // オーディオに渡すためのデータ
    int bufferReadPos = 0; // データの読み取り位置

    void Start()
    {
        soundRT = new RenderTexture(Width, Height, 0);
        soundRT.Create();
        textureBuffer = new float[soundRT.width * soundRT.height];
        soundTexture = new Texture2D(soundRT.width, soundRT.height);
    }

    void Update()
    {
        UpdateBuffer();
    }
    
    private void UpdateBuffer()
    {
        // シェーダーでテクスチャを加工し、結果をsoundRTに保存
        Graphics.Blit(soundRT, soundRT, soundMaterial, 0);

        // RenderTextureはそのままではピクセルにアクセスできないのでTexture2Dに変換
        RenderTexture.active = soundRT;
        soundTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); // RenderTexture -> Texture2D

        // Texture2D -> float[]
        // Texture2D.GetPixel()をOnAudioFilterRead()の中で使うと怒られるので注意
        int dst = 0;
        for (int y = 0; y < soundRT.height; y++)
        {
            for (int x = 0; x < soundRT.width; x++)
            {
                // とりあえず、テクスチャのrチャンネルをオーディオに渡す
                textureBuffer[dst++] = soundTexture.GetPixel(x, y).r * 2f - 1f; // [0:1] -> [-1:1]
            }
        }
    }


    void OnDestroy()
    {
        soundRT.Release();
        DestroyImmediate(soundTexture);
    }
    
    void OnAudioFilterRead(float[] data, int channels)
    {
        int dst = 0;
        while (dst < data.Length)
        {
            float value = textureBuffer[bufferReadPos] * soundVolume;
            for (int i = 0; i < channels; i++)
            {
                data[dst + i] = value; // write
            }

            dst += channels;
            bufferReadPos ++;
            if (bufferReadPos == textureBuffer.Length)
            {
                bufferReadPos = 0;
            }
        }
    }
}