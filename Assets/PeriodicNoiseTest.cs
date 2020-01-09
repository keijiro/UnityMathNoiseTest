using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class PeriodicNoiseTest : MonoBehaviour
{
    const int kResolution = 0x200;

    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
    struct NoiseJob : IJobParallelFor
    {
        [ReadOnly] public bool periodic;
        [ReadOnly] public bool derivative;
        [ReadOnly] public bool rotation;
        [ReadOnly] public float2 frequency;
        [ReadOnly] public float2 period;
        [ReadOnly] public float2 offset;
        [ReadOnly] public float time;

        public NativeArray<Color32> pixels;

        float3 Noise(float2 pos, float rot)
        {
            if (periodic)
            {
                if (derivative)
                {
                    if (rotation)
                        return noise.psrdnoise(pos, period, rot);
                    else
                        return noise.psrdnoise(pos, period);
                }
                else
                {
                    if (rotation)
                        return noise.psrnoise(pos, period, rot);
                    else
                        return noise.psrnoise(pos, period);
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
            var c = math.saturate(Noise(p + offset, time) * 0.5f + 0.5f);

            var r = (System.Byte)(c.x * 255);
            var g = (System.Byte)(c.y * 255);
            var b = (System.Byte)(c.z * 255);

            pixels[i] = new Color32(r, g, b, 0xff);
        }
    }

    [SerializeField] bool _periodic = false;
    [SerializeField] bool _derivative = false;
    [SerializeField] bool _rotation = false;
    [SerializeField] float2 _frequency = 10;
    [SerializeField] float2 _period = 4;
    [SerializeField] float2 _offset = 10;

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
            period = _period,
            offset = _offset,
            time = Time.time,
            pixels = _pixels
        };

        job.Schedule(_pixels.Length, 64).Complete();

        _texture.LoadRawTextureData(_pixels);
        _texture.Apply();
    }
}
