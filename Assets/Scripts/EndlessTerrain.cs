using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    [SerializeField] public const float maxViewdst = 600;
    public Transform viewer;
    [SerializeField] public Material material;
    [SerializeField] public static Material staticMaterial;

    public enum DrawMode { NoiseMap, ColorMap }
    [SerializeField] private DrawMode drawMode;

    public static Vector2 viewerPosition;
    private static WorldGenerator worldGenerator;
    private int chunksize;
    private int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        staticMaterial = material;
        worldGenerator = FindAnyObjectByType<WorldGenerator>();
        chunksize = worldGenerator.chunkSize;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewdst / chunksize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.y);
        UpdateVisibleChunks();
    }


    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunksize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunksize);

        for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
        {
            for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrianChunk();
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }
                else 
                { 
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunksize, transform, drawMode)); 
                }

            }
        }
    }

    public class TerrainChunk
    {
        GameObject plane;
        Vector2 position;
        Bounds bounds;
        MeshRenderer meshRenderer;
        DrawMode _drawMode;
        public TerrainChunk(Vector2 coord, int size, Transform parent, DrawMode drawMode)
        {
            _drawMode = drawMode;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, position.y, 0);

            plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshRenderer = plane.GetComponent<MeshRenderer>();
            plane.transform.position = positionV3;
            plane.transform.localScale = Vector3.one * size/10;
            plane.transform.rotation = Quaternion.Euler(90,0,0);
            plane.transform.parent = parent;
            SetVisible(false);

            worldGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        private void OnMapDataRecieved(MapData mapData)
        {
            int width = mapData.heightMap.GetLength(0);
            int height = mapData.heightMap.GetLength(1);
            Texture texture;
            if (_drawMode == DrawMode.ColorMap)
            {
                texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, width, height);
            }
            else
            {
                texture = TextureGenerator.TextureFromHeightMap(mapData.heightMap);
            }
            meshRenderer.material = staticMaterial;
            meshRenderer.material.mainTexture = texture;
        }

        public void UpdateTerrianChunk()
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewdst;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            plane.SetActive(visible);
        }

        public bool IsVisible()
        {
            return plane.activeSelf;
        }

    }

}
