using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using VTools.Grid;

public struct Chunk
{
    public bool[,] isGround;
    public int size;

    public Chunk(int size)
    {
        this.size = size;
        isGround = new bool[size,size];
    }
}


[CreateAssetMenu(menuName = "Procedural Generation Method/CellularAutomata", fileName = "CA", order = 0)]
public class CA : ProceduralGenerationMethod
{
    [Header("Generation Parameters")]

    [SerializeField] [Range(0, 100)] [Tooltip("Percentage of stating ground")]
    private int noise = 10;

    [SerializeField] [Tooltip("Size of the chunk")]
    private int chunkSize = 10;

    [SerializeField] private int numberOfCellToChange = 4;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        var chunkNumber = new Vector2Int(Grid.Width / chunkSize, Grid.Lenght / chunkSize);
        var ChunkList = new Chunk[chunkSize,chunkSize];
        for (var index0 = 0; index0 < ChunkList.GetLength(0); index0++)
        for (var index1 = 0; index1 < ChunkList.GetLength(1); index1++)
        {
            ChunkList[index0, index1] = new Chunk(chunkSize);
        }

        var main = Camera.main;
        var value = math.max(Grid.Width, Grid.Lenght);
        if (main)
        {
            main.orthographicSize = value / 2;
            main.transform.position = new Vector3(value / 2, 20, value / 2);
        }

        //CreateNoiseChunkList(ChunkList);
        var isGround = new bool[Grid.Width, Grid.Lenght];
        CreateNoise();
        for (var i = 0; i < _maxSteps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            Change(isGround);
        }
    }

    private void CreateNoiseChunkList(Chunk[,] chunks)
    {
        for (var index0 = 0; index0 < chunks.GetLength(0); index0++)
        for (var index1 = 0; index1 < chunks.GetLength(1); index1++)
        {
            var VARIABLE = chunks[index0, index1];
            for (var x = 0; x < VARIABLE.size; x++)
            for (var y = 0; y < VARIABLE.size; y++)
            {
                //Debug.Log($"x{x} y{y}");
                var rnd = RandomService.Chance(noise / 100.0f);
                if (!Grid.TryGetCellByCoordinates(x + chunks.GetLength(0) * index0, y + chunks.GetLength(1) * index1, out var cell)) continue;
                AddTileToCell(cell, rnd ? GRASS_TILE_NAME : WATER_TILE_NAME, true);
            }
            Debug.Log($"chunk{index0 * chunks.GetLength(0) + index1} generated");
        }
    }

    private void CreateNoiseChunk(Chunk chunk)
    {

    }

    private void CreateNoise()
    {
        for (var x = 0; x < Grid.Width; x++)
        for (var y = 0; y < Grid.Lenght; y++)
        {
            var rnd = RandomService.Chance(noise / 100.0f);
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell)) continue;

            AddTileToCell(cell, rnd ? GRASS_TILE_NAME : WATER_TILE_NAME, true);
        }
    }

    private void Change(bool[,] isGround)
    {
        for (var x = 0; x < Grid.Width; x++)
        for (var y = 0; y < Grid.Lenght; y++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                Debug.LogError($"Unable to get cell on coordinates : ({x}, {y})");
            ChangeCell(cell, isGround);
        }

        for (var x = 0; x < Grid.Width; x++)
        for (var y = 0; y < Grid.Lenght; y++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                Debug.LogError($"Unable to get cell on coordinates : ({x}, {y})");
            AddTileToCell(cell, isGround[x, y] ? WATER_TILE_NAME : GRASS_TILE_NAME, true);
        }
    }

    private void ChangeCell(Cell cell, bool[,] isGround)
    {
        var countGrass = 0;
        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        {
            if (!Grid.TryGetCellByCoordinates(cell.Coordinates.x + x, cell.Coordinates.y + y, out var othercell))
                continue;

            if (cell != othercell)
                if (othercell.GridObject.Template.Name == GRASS_TILE_NAME)
                    countGrass++;
        }

        if (countGrass >= numberOfCellToChange)
            isGround[cell.Coordinates.x, cell.Coordinates.y] = false;
        else
            isGround[cell.Coordinates.x, cell.Coordinates.y] = true;
    }


}