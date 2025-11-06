using System.Threading;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Procedural Generation Method/Noise", fileName = "Noise", order = 0)]
public class Noise : ProceduralGenerationMethod
{
    [Header("Noise Param")] [SerializeField]
    private FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;

    [SerializeField] [Range(0, 1)] private float frequency = 0.01f;

    [Header("Fractal Param")] [SerializeField]
    private FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.Ridged;

    [SerializeField] [Min(0)] private int octaves = 3;

    [SerializeField] private float lunarity = 2;

    [SerializeField] [Tooltip("Persistance")] private float gain = 2;

    [SerializeField] private float weightedStrength;

    [SerializeField] private float pingpongStrength;

    [Header("DomainWarp Param")] [SerializeField]
    private FastNoiseLite.DomainWarpType domainWarpType;

    [SerializeField] private float domainWarpAmp = 2;

    [SerializeField] [Range(-1, 1)] private float water;
    [SerializeField] [Range(-1, 1)] private float sand;
    [SerializeField] [Range(-1, 1)] private float grass;
    [SerializeField] [Range(-1, 1)] private float rock;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var main = Camera.main;
        var value = math.max(Grid.Width, Grid.Lenght);
        if (main)
        {
            main.orthographicSize = value / 2;
            main.transform.position = new Vector3(value / 2, 20, value / 2);
        }

        var noise = new FastNoiseLite(RandomService.Seed);
        noise.SetNoiseType(noiseType);
        noise.SetFrequency(frequency);

        noise.SetFractalType(fractalType);
        noise.SetFractalOctaves(octaves);
        noise.SetFractalLacunarity(lunarity);
        noise.SetFractalGain(gain);
        noise.SetFractalWeightedStrength(weightedStrength);

        noise.SetFractalPingPongStrength(pingpongStrength);

        noise.SetDomainWarpType(domainWarpType);
        noise.SetDomainWarpAmp(domainWarpAmp);

        //var noiseData = new double[Grid.Width][];
        //for (var index = 0; index < Grid.Width; index++) noiseData[index] = new double[Grid.Lenght];

        for (var x = 0; x < Grid.Width; x++)
        for (var y = 0; y < Grid.Lenght; y++)
        {
            //noiseData[x][y] = noise.GetNoise(x, y);
            //if (!Grid.TryGetCellByCoordinates(x, y, out var cell)) continue;

            //if (noiseData[x][y] <= -0.5)
            //    AddTileToCell(cell, WATER_TILE_NAME, true);
            //else if (noiseData[x][y] is > -0.5 and <= -0.4)
            //    AddTileToCell(cell, SAND_TILE_NAME, true);
            //else if (noiseData[x][y] >= 0.5)
            //    AddTileToCell(cell, ROCK_TILE_NAME, true);
            //else
            //    AddTileToCell(cell, GRASS_TILE_NAME, true);
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell)) continue;
            var temp = noise.GetNoise(x, y);

            if (temp <= water)
                AddTileToCell(cell, WATER_TILE_NAME, true);
            else if (temp <= sand)
                AddTileToCell(cell, SAND_TILE_NAME, true);
            else if (temp <= grass)
                AddTileToCell(cell, GRASS_TILE_NAME, true);
            else if (temp <= rock)
                AddTileToCell(cell, ROCK_TILE_NAME, true);
            else
                AddTileToCell(cell, SNOW_TILE_NAME, true);
            
        }

        //await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    }
}