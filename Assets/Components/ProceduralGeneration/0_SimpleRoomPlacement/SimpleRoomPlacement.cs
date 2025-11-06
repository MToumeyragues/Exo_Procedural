using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Simple Room Placement")]
    public class SimpleRoomPlacement : ProceduralGenerationMethod
    {
        [Header("Room Parameters")] [SerializeField]
        private int _maxRooms = 5;

        [SerializeField] private int _minWidth = 5;
        [SerializeField] private int _MaxWidth = 10;
        [SerializeField] private int _minHeigth = 5;
        [SerializeField] private int _MaxHeigth = 10;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            var main = Camera.main;
            var value = math.max(Grid.Width, Grid.Lenght);
            if (main)
            {
                main.orthographicSize = value / 2;
                main.transform.position = new Vector3(value / 2, 20, value / 2);
            }

            List<RectInt> rooms = new();
            for (var i = 0; i < _maxSteps; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                var sizex = RandomService.Range(_minWidth, _MaxWidth);
                var sizey = RandomService.Range(_minHeigth, _MaxHeigth);


                var x = RandomService.Range(0, Grid.Width - sizex);
                var y = RandomService.Range(0, Grid.Lenght - sizey);


                var room = new RectInt(x, y, sizex, sizey);
                if (TryPlaceRoom(room, rooms)) rooms.Add(room);
                if (rooms.Count >= _maxRooms) break;

                // Waiting between steps to see the result.
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }

            for (var i = 0; i < rooms.Count - 1; i++) LinkTwoRooms(rooms[i], rooms[i + 1]);
            // Final ground building.
            BuildGround();
        }

        private void BuildGround()
        {
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");

            // Instantiate ground blocks
            for (var x = 0; x < Grid.Width; x++)
            for (var z = 0; z < Grid.Lenght; z++)
            {
                if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
                {
                    Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                    continue;
                }

                GridGenerator.AddGridObjectToCell(chosenCell, groundTemplate, false);
            }
        }

        private void PlaceRoom(RectInt room)
        {
            for (var x = room.xMin; x < room.xMax; x++)
            for (var y = room.yMin; y < room.yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell)) continue;
                AddTileToCell(cell, ROOM_TILE_NAME, false);
            }
        }

        private bool TryPlaceRoom(RectInt room, List<RectInt> _rooms)
        {
            var Placed = true;
            foreach (var rectInt in _rooms)
                for (var x = room.xMin; x < room.xMax; x++)
                for (var y = room.yMin; y < room.yMax; y++)
                    if (rectInt.Contains(new Vector2Int(x, y)))
                    {
                        Placed = false;
                        goto end;
                    }

            PlaceRoom(room);

            end:
            return Placed;
        }

        private void LinkTwoRooms(RectInt left, RectInt right, bool overrideExistingObjects = false)
        {
            var leftcenter = left.center;
            var rigthcenter = right.center;
            var tempx = (int)left.center.x;
            if (left.center.x < rigthcenter.x)
                for (var x = (int)leftcenter.x; x <= (int)rigthcenter.x; x++) //left to right
                {
                    if (!Grid.TryGetCellByCoordinates(x, (int)leftcenter.y, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
                    tempx = x;
                }
            else
                for (var x = (int)leftcenter.x; x >= (int)rigthcenter.x; x--) //left to right
                {
                    if (!Grid.TryGetCellByCoordinates(x, (int)leftcenter.y, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
                    tempx = x;
                }

            if (leftcenter.y < rigthcenter.y)
                for (var y = (int)leftcenter.y; y <= (int)rigthcenter.y; y++) //bottom to top
                {
                    if (!Grid.TryGetCellByCoordinates(tempx, y, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
                }
            else
                for (var y = (int)leftcenter.y; y >= (int)rigthcenter.y; y--) //bottom to top
                {
                    if (!Grid.TryGetCellByCoordinates(tempx, y, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
                }
        }
    }
}