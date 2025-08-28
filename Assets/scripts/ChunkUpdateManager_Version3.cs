using System.Collections.Generic;
using UnityEngine;

public class ChunkUpdateManager : MonoBehaviour
{
    public TileInfiniteCameraSpawner spawner;
    public int maxChunksPerFrame = 2; // Limit for performance
    public float updateDistance = 64f; // Only update chunks within this distance

    private Queue<ChunkRequest> chunkQueue = new Queue<ChunkRequest>();

    void Update()
    {
        Vector3 playerPos = spawner.playerTransform.position;

        // Remove queued chunks if too far from player
        int count = chunkQueue.Count;
        for (int i = 0; i < count; i++)
        {
            var req = chunkQueue.Dequeue();
            float dist = Vector3.Distance(playerPos, req.centerWorldPos);
            if (dist < updateDistance)
                chunkQueue.Enqueue(req);
        }

        // Process a few chunks per frame
        int processed = 0;
        while (chunkQueue.Count > 0 && processed < maxChunksPerFrame)
        {
            var req = chunkQueue.Dequeue();
            float dist = Vector3.Distance(playerPos, req.centerWorldPos);
            if (dist < updateDistance)
            {
                spawner.SpawnOrLoadChunk(req.chunkX, req.z, req.buildBottom, req.maxY, req.playerZ, req.render);
                processed++;
            }
        }
    }

    public void RequestChunk(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render, Vector3 centerWorldPos)
    {
        chunkQueue.Enqueue(new ChunkRequest
        {
            chunkX = chunkX,
            z = z,
            buildBottom = buildBottom,
            maxY = maxY,
            playerZ = playerZ,
            render = render,
            centerWorldPos = centerWorldPos
        });
    }

    private struct ChunkRequest
    {
        public int chunkX, z, buildBottom, maxY, playerZ;
        public bool render;
        public Vector3 centerWorldPos;
    }
}