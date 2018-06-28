using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class PeriodicNoiseTest : MonoBehaviour
{
    const int kResolution = 0x200;

    [ComputeJobOptimization]
    struct NoiseJob : IJobParallelFor
    {
        [ReadOnly] public bool periodic;
        [ReadOnly] public bool derivative;
        [ReadOnly] public bool rotation;
        [ReadOnly] public int period;
        [ReadOnly] public float frequency;
        [ReadOnly] public float time;

        public NativeArray<Color32> pixels;

        float3 Noise(float2 pos, float rot)
        {
            if (periodic)
            {
                if (derivative)
                {
                    if (rotation)
                        return noise.psrdnoise(pos, 4, rot);
                    else
                        return noise.psrdnoise(pos, 4);
                }
                else
                {
                    if (rotation)
                        return noise.psrnoise(pos, 4, rot);
                    else
                        return noise.psrnoise(pos, 4);
                }
            }
            else
            {
                if (derivative)
                {
                    if (rotation)
                        return noise.srdnoise(pos, rot);
                    else
                        return noise.srdnoise(pos);
                }
                else
                {
                    if (rotation)
                        return noise.srnoise(pos, rot);
                    else
                        return noise.srnoise(pos);
                }
            }
        }

        public void Execute(int i)
        {
            var y = i / kResolution;
            var x = i - y * kResolution;

            var p = (new float2(x, y) - kResolution / 2) * 2 * frequency / kResolution;
            var c = math.saturate(Noise(p, time) * 0.5f + 0.5f);

            var r = (System.Byte)(c.x * 255);
            var g = (System.Byte)(c.y * 255);
            var b = (System.Byte)(c.z * 255);

            pixels[i] = new Color32(r, g, b, 0xff);
        }
    }

    [SerializeField] bool _periodic;
    [SerializeField] bool _derivative;
    [SerializeField] bool _rotation;
    [SerializeField] float _frequency = 10;

    Texture2D _texture;
    NativeArray<Color32> _pixels;

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
            periodic = _periodic,
            derivative = _derivative,
            rotation = _rotation,
            frequency = _frequency,
            time = Time.time,
            pixels = _pixels
        };

        job.Schedule(_pixels.Length, 64).Complete();

        _texture.LoadRawTextureData(_pixels);
        _texture.Apply();
    }
}
