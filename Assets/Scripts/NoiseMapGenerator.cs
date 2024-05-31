using System;
using UnityEngine;

public class NoiseMapGenerator
{
    //To do:
    //Add seed

    private delegate float GetNoiseHeight(float sampledX, float sampledY);
    private FastNoiseLite fastNoiseLite = new();


    public float[,] GetNoiseMap(int width, int height, int seed, Vector2 offset, NoiseSetting noiseSetting)
    {

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[noiseSetting.octaves];
        for (int i = 0; i < noiseSetting.octaves; i++)
        {
            float offSetX = prng.Next(-100000, 100000) + offset.x;
            float offSetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offSetX, offSetY);
        }


        GetNoiseHeight getNoiseHeight;
        switch (noiseSetting.noiseKind)
        {
            case NoiseKind.Perlin:
                getNoiseHeight = GetNoiseHeightOnlyPositives;
                break;
            case NoiseKind.PerlinNegative:
                getNoiseHeight = GetNoiseHeightWithNegatives;
                break;
            case NoiseKind.OpenSimplex:
                getNoiseHeight = GetNoiseHeightOpenSimplex;
                break;
            case NoiseKind.Cellular:
                getNoiseHeight = GetNoiseHeightCellular;
                break;
            case NoiseKind.Value:
                getNoiseHeight = GetNoiseHeightValueNoise;
                break;
            default:
                getNoiseHeight = GetNoiseHeightOnlyPositives;
                break;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        //Scale towards the middle 
        float halfSizeX = width / 2f;
        float halfSizeY = height / 2f;

        float[,] noiseMap = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeigh = 0;

                for (int i = 0; i < noiseSetting.octaves; i++)
                {
                    float sampledX = (x - halfSizeX - octaveOffsets[i].x) * noiseSetting.scale * frequency;
                    float sampledY = (y - halfSizeY + octaveOffsets[i].y) * noiseSetting.scale * frequency;

                    float perlinValue = getNoiseHeight(sampledX, sampledY);
                    noiseHeigh += perlinValue * amplitude;

                    amplitude *= noiseSetting.persistance;
                    frequency *= noiseSetting.lacunarity;
                }

                //Get min and max noise heights
                if(noiseHeigh > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeigh;
                }else if(noiseHeigh < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeigh;
                }

                noiseMap[x, y] = noiseHeigh;
            }
        }

        if (noiseSetting.noiseKind == NoiseKind.PerlinNegative || noiseSetting.normalize) 
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }
        }
        return noiseMap;
    }

    private float GetNoiseHeightWithNegatives(float sampledX, float sampledY)
    {
        fastNoiseLite.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        float height = fastNoiseLite.GetNoise(sampledX, sampledY);
        return height;
    }

    private float GetNoiseHeightOnlyPositives(float sampledX, float sampledY)
    {
        fastNoiseLite.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        float height = fastNoiseLite.GetNoise(sampledX, sampledY);
        height = Mathf.InverseLerp(-1, 1, height);
        return height;
    }

    private float GetNoiseHeightOpenSimplex(float sampledX, float sampledY)
    {
        fastNoiseLite.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
        float height = fastNoiseLite.GetNoise(sampledX, sampledY);
        height = Mathf.InverseLerp(-1, 1, height);
        return height;
    }

    private float GetNoiseHeightCellular(float sampledX, float sampledY)
    {
        fastNoiseLite.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        float height = fastNoiseLite.GetNoise(sampledX, sampledY);
        height = Mathf.InverseLerp(-1, 1, height);
        return height;
    }

    private float GetNoiseHeightValueNoise(float sampledX, float sampledY)
    {
        fastNoiseLite.SetNoiseType(FastNoiseLite.NoiseType.Value);
        float height = fastNoiseLite.GetNoise(sampledX, sampledY);
        height = Mathf.InverseLerp(-1, 1, height);
        return height;
    }

}

public enum NoiseKind
{
    Perlin,
    PerlinNegative,
    OpenSimplex,
    Cellular,
    Value
}