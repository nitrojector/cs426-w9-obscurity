using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Perfect maze on a cell grid: true = wall, false = walkable.
/// Coordinates are integer cell centers; outer ring stays wall.
/// </summary>
public static class MazeGenerator
{
    public static (bool[,] walls, int startX, int startZ, int goalX, int goalZ, int shortestPathLength) Build(
        int gridSize,
        System.Random rng)
    {
        gridSize = Mathf.Max(7, gridSize | 1);

        var map = new bool[gridSize, gridSize];
        for (int z = 0; z < gridSize; z++)
        for (int x = 0; x < gridSize; x++)
            map[x, z] = true;

        void Carve(int x, int z)
        {
            map[x, z] = false;
            var dirs = new (int dx, int dz)[] { (0, 2), (0, -2), (2, 0), (-2, 0) };
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            foreach (var (dx, dz) in dirs)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (nx <= 0 || nz <= 0 || nx >= gridSize - 1 || nz >= gridSize - 1)
                    continue;
                if (!map[nx, nz])
                    continue;
                map[x + dx / 2, z + dz / 2] = false;
                Carve(nx, nz);
            }
        }

        Carve(1, 1);
        map[1, 1] = false;

        int gx = gridSize - 2;
        int gz = gridSize - 2;
        if (map[gx, gz])
            (gx, gz) = FindFarthestOpen(map, 1, 1, gridSize);

        int shortest = ShortestPathLength(map, 1, 1, gx, gz, gridSize);
        return (map, 1, 1, gx, gz, shortest);
    }

    static (int x, int z) FindFarthestOpen(bool[,] map, int sx, int sz, int n)
    {
        var q = new Queue<(int x, int z)>();
        var dist = new Dictionary<(int, int), int>();
        q.Enqueue((sx, sz));
        dist[(sx, sz)] = 0;
        (int bx, int bz) best = (sx, sz);
        int bestD = 0;

        while (q.Count > 0)
        {
            var (x, z) = q.Dequeue();
            int d = dist[(x, z)];
            if (d > bestD)
            {
                bestD = d;
                best = (x, z);
            }

            Try(x + 1, z);
            Try(x - 1, z);
            Try(x, z + 1);
            Try(x, z - 1);

            void Try(int nx, int nz)
            {
                if (nx < 0 || nz < 0 || nx >= n || nz >= n) return;
                if (map[nx, nz]) return;
                var key = (nx, nz);
                if (dist.ContainsKey(key)) return;
                dist[key] = d + 1;
                q.Enqueue((nx, nz));
            }
        }

        return best;
    }

    static int ShortestPathLength(bool[,] map, int sx, int sz, int gx, int gz, int n)
    {
        var q = new Queue<(int x, int z)>();
        var seen = new HashSet<(int, int)>();
        q.Enqueue((sx, sz));
        seen.Add((sx, sz));
        int steps = 0;

        while (q.Count > 0)
        {
            int count = q.Count;
            for (int i = 0; i < count; i++)
            {
                var (x, z) = q.Dequeue();
                if (x == gx && z == gz)
                    return steps;

                Enq(x + 1, z);
                Enq(x - 1, z);
                Enq(x, z + 1);
                Enq(x, z - 1);
            }

            steps++;
        }

        return 0;

        void Enq(int x, int z)
        {
            if (x < 0 || z < 0 || x >= n || z >= n) return;
            if (map[x, z]) return;
            var key = (x, z);
            if (!seen.Add(key)) return;
            q.Enqueue((x, z));
        }
    }
}
