using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BasicNoiseTest : MonoBehaviour
{
    const int kResolution = 0x200;

    public enum NoiseType {
        Classic2D, Classic3D, Classic4D,
        Simplex2D, Simplex3D, Simplex4D
    }

    [ComputeJobOptimization]
    struct NoiseJob : IJobParallelFor
    {
        [ReadOnly] public NoiseType type;
        [ReadOnly] public float scale;
        [ReadOnly] public float4 offset;

        public NativeArray<Color32> pixels;

        float Noise(float4 p)
        {
            switch (type)
            {
                case NoiseType.Classic2D: return noise.cnoise(p.xy);
                case NoiseType.Classic3D: return noise.cnoise(p.xyz);
                case NoiseType.Classic4D: return noise.cnoise(p.xyzw);
                case NoiseType.Simplex2D: return noise.snoise(p.xy);
                case NoiseType.Simplex3D: return noise.snoise(p.xyz);
                case NoiseType.Simplex4D: return noise.snoise(p.xyzw);
            }
            return 0;
        }

        public void Execute(int i)
        {
            var y = i / kResolution;
            var x = i - y * kResolution;

            var p = new float4(x * scale, y * scale, 0, 0) + offset;
            var c = math.saturate(Noise(p) * 0.5f + 0.5f);

            var b = (System.Byte)(c * 255);
            pixels[i] = new Color32(b, b, b, 0xff);
        }
    }

    [SerializeField] NoiseType _noiseType = NoiseType.Classic2D;
    [SerializeField] float _frequency = 2;

    Texture2D _texture;
    NativeArray<Color32> _pixels;

    float4 CalculateOffset()
    {
        var t = Time.time;

        if (_noiseType == NoiseType.Classic2D || _noiseType == NoiseType.Simplex2D)
        {
            var amp = math.cos(t / 5) * _frequency * 2;
            return new float4(math.cos(t), math.sin(t), 0, 0) * amp;
        }
        else if (_noiseType == NoiseType.Classic3D || _noiseType == NoiseType.Simplex3D)
        {
            return new float4(-0.5f, -0.5f, t / 2, 0) * _frequency;
        }
        else // 4D
        {
            var amp = math.cos(t / 5) * _frequency * 2;
            return new float4(-0.5f * _frequency, -0.5f * _frequency, math.cos(t) * amp, math.sin(t) * amp);
        }
    }

    void Start()
    {
        _texture = new Texture2D(kResolution, kResolution, TextureFormat.RGBA32, false);
        _pixels = new NativeArray<Color32>(kResolution * kResolution, Allocator.Persistent);
        GetComponent<Renderer>().material.mainTexture = _texture;
    }

    void OnDestroy()
    {
        Destroy(_texture);
        _pixels.Dispose();
    }

    void Update()
    {
        var job = new NoiseJob{
            type = _noiseType,
            scale = _frequency / kResolution,
            offset = CalculateOffset(),
            pixels = _pixels
        };

        job.Schedule(_pixels.Length, 64).Complete();

        _texture.LoadRawTextureData(_pixels);
        _texture.Apply();
    }
}
