using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading;
using System;

public class WorldGenerator : MonoBehaviour
{
    [Header("Preview settings")]
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private DrawMode _drawMode;
    [SerializeField] public int chunkSize;

    [Header("Noise settings")]
    [SerializeField] private NoiseSetting _noiseSetting;
    [SerializeField] private Vector2 _noiseOffset;
    [SerializeField] private int _seed;

    [Header("Terrain")]
    [SerializeField] private TerrainType[] _regions;
    [SerializeField] private NoiseSetting[] _terrainNoises;
    [SerializeField] private NoiseSetting[] _climateNoises;
    [SerializeField] private Biome[] _biomes;


    [Header("Dependencies")]
    private Tilemap _map;
    [SerializeField] private NoisePreview _noisePreview;

    [SerializeField] public bool _autoUpdate;
    private enum DrawMode { NoiseMap, ColorMap }

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        if (_drawMode == DrawMode.NoiseMap)
        {
            _noisePreview.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (_drawMode == DrawMode.ColorMap)
        {
            _noisePreview.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, _width, _height));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center ,callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    private void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.paramater);
            }
        }
    }

    private MapData GenerateMapData(Vector2 center)
    {
        NoiseMapGenerator noiseMapGenerator = new NoiseMapGenerator();
        float[,] noiseMap = noiseMapGenerator.GetNoiseMap(chunkSize, chunkSize, _seed,
            _noiseOffset + center, _noiseSetting);

        NoiseSetting cM = _terrainNoises[0];
        float[,] continentalnessMap = noiseMapGenerator.GetNoiseMap(chunkSize, chunkSize, _seed,
            _noiseOffset + center, _terrainNoises[0]);
        AnimationCurve continentalnessCurve = new AnimationCurve(cM.mapping.keys);

        NoiseSetting eM = _terrainNoises[1];
        float[,] erosionMap = noiseMapGenerator.GetNoiseMap(chunkSize, chunkSize, _seed,
            _noiseOffset + center, _terrainNoises[1]);
        AnimationCurve erosionCurve = new AnimationCurve(eM.mapping.keys);

        NoiseSetting tM = _climateNoises[0];
        float[,] temperatureMap = noiseMapGenerator.GetNoiseMap(chunkSize, chunkSize, _seed,
            _noiseOffset + center, _climateNoises[0]);

        NoiseSetting mM = _climateNoises[1];
        float[,] moistureMap = noiseMapGenerator.GetNoiseMap(chunkSize, chunkSize, _seed,
            _noiseOffset + center, _climateNoises[1]);

        Color[] colourMap = new Color[chunkSize * chunkSize];
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {

                noiseMap[x, y] *= continentalnessCurve.Evaluate(continentalnessMap[x, y]);
                noiseMap[x, y] *= erosionCurve.Evaluate(erosionMap[x, y]);
                float currentHeight = noiseMap[x, y];
                float currentTemp = temperatureMap[x, y];
                float currentMoist = moistureMap[x, y];
                for (int i = 0; i < _biomes.Length; i++)
                {
                    if (currentHeight <= _biomes[i].maxAlitude & currentTemp <= _biomes[i].maxTemperature & currentMoist <= _biomes[i].maxMoistere)
                    {     
                        Color color = _biomes[i].color;
                        color.r += currentHeight * 0.3f;
                        color.g += currentHeight * 0.3f;
                        color.b += currentHeight * 0.3f;


                        colourMap[y * chunkSize + x] = color;                   
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);

    }

    private void BuildTiles()
    {
        int offset = _width / 2;
        NoiseMapGenerator noiseMapGenerator = new NoiseMapGenerator();
        float[,] noiseMap = noiseMapGenerator.GetNoiseMap(_width, _height, _seed,
            _noiseOffset, _noiseSetting);
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float noiseValue = noiseMap[x, y];
                Vector3Int coords = new Vector3Int(x - offset, y - offset, 0);

                //_map.SetTile(coords, _tile);
                _map.SetTileFlags(coords, TileFlags.None);
                _map.SetColor(coords, Color.HSVToRGB(noiseValue, 1, 1));
            }
        }
    }


    private void OnValidate()
    {
        if(_width < 1)
        {
            _width = 1;
        }
        if (_height < 1)
        {
            _height = 1;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T paramater;

        public MapThreadInfo (Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.paramater = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

[System.Serializable]
public struct NoiseSetting : ISerializationCallbackReceiver
{
    public string name;
    public float scale;
    public int octaves;
    [Range(0, 1)] public float persistance;
    public float lacunarity;
    public NoiseKind noiseKind;
    public bool normalize;
    public AnimationCurve mapping;

    private void OnValidate()
    {
        if (octaves < 1)
        {
            octaves = 1;
        }
    }
    void ISerializationCallbackReceiver.OnBeforeSerialize() => this.OnValidate();
    void ISerializationCallbackReceiver.OnAfterDeserialize() { }
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}

[System.Serializable]
public struct Biome
{
    public string name;
    public Color color;
    public float maxMoistere;
    public float maxTemperature;
    public float maxAlitude;
}
