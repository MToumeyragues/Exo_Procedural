using System.Threading;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;

[CreateAssetMenu(menuName = "Procedural Generation Method/BinarySpacePartition", fileName = "BSP", order = 0)]
public class BSP : ProceduralGenerationMethod
{
    [SerializeField] private int _cutnumber = 5;
    [SerializeField] private Vector2Int _minSize = new(2, 2);
    [SerializeField] private Vector2Int _maxSize = new(10, 10);
    [SerializeField] private Vector2 _cutRange = new(0.2f, 0.8f);

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        var allGrid = new RectInt(0, 0, Grid.Width, Grid.Lenght);
        var root = new Node(allGrid);
        Cut(root, _cutnumber);
        await Rooms(root);
        await Link(root);
        BuildGround();

        //await UniTask.Delay(GridGenerator.StepDelay, cancellationToken : cancellationToken);
    }

    private void Cut(Node node, int numberOfCut)
    {
        if (numberOfCut <= 0) return;

        //var where = RandomService.Range(_cutRange.x, _cutRange.y);
        const float where = 0.5f;
        node.Cut(where, numberOfCut % 2 != 0);
        Cut(node._child1, numberOfCut - 1);
        Cut(node._child2, numberOfCut - 1);
    }

    private async UniTask Rooms(Node node)
    {
        if (node._child1 == null && node._child2 == null)
        {
            PlaceRoomInNode(node);
            await UniTask.Delay(GridGenerator.StepDelay);
            Debug.Log(node);
        }
        else
        {
            await Rooms(node._child1);
            await Rooms(node._child2);
        }
    }

    private async UniTask Link(Node node)
    {
        if (node._child1 == null && node._child2 == null)
            return;
        LinkTwoChild(node);
        await Link(node._child1);
        await Link(node._child2);

        await UniTask.Delay(GridGenerator.StepDelay);
    }

    private void PlaceRoomInNode(Node node)
    {
        var sizex = 0;
        var sizey = 0;
        var iteration = 0;
        do
        {
            sizex = RandomService.Range(_minSize.x, _maxSize.x);
            iteration++;
            if (iteration == 50) return;
        } while (sizex >= node._rect.width);

        iteration = 0;
        do
        {
            sizey = RandomService.Range(_minSize.y, _maxSize.y);
            iteration++;
            if (iteration == 50) return;
        } while (sizey >= node._rect.height);

        var x = RandomService.Range(node._rect.xMin, node._rect.xMax - sizex);
        var y = RandomService.Range(node._rect.yMin, node._rect.yMax - sizey);
        var room = new RectInt(x, y, sizex, sizey);
        node._room = room;
        PlaceRoom(room);
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

    private void LinkTwoChild(Node node)
    {
        LinkTwoRooms(node._child1._room, node._child2._room, true);
    }

    private void LinkTwoRooms(RectInt left, RectInt right, bool overrideExistingObjects = false)
    {
        var leftcenter = left.GetCenter();
        var rigthcenter = right.GetCenter();
        CreateDogLegCorridor(leftcenter, rigthcenter);
        //var tempx = left.GetCenter().x;
        //if (left.center.x < rigthcenter.x)
        //{
        //    for (var x = leftcenter.x; x <= rigthcenter.x; x++)//left to right
        //    {
        //        if (!Grid.TryGetCellByCoordinates(x, leftcenter.y, out var cell)) continue;
        //        AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
        //        tempx = x;
        //    }
        //}
        //else
        //{
        //    for (var x = leftcenter.x; x >= rigthcenter.x; x--)//left to right
        //    {
        //        if (!Grid.TryGetCellByCoordinates(x, leftcenter.y, out var cell)) continue;
        //        AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
        //        tempx = x;
        //    }
        //}

        //if (leftcenter.y < rigthcenter.y)
        //{
        //    for (var y = leftcenter.y; y <= rigthcenter.y; y++)//bottom to top
        //    {
        //        if (!Grid.TryGetCellByCoordinates(tempx, y, out var cell)) continue;
        //        AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
        //    }
        //}
        //else
        //{
        //    for (var y = leftcenter.y; y >= rigthcenter.y; y--)//bottom to top
        //    {
        //        if (!Grid.TryGetCellByCoordinates(tempx, y, out var cell)) continue;
        //        AddTileToCell(cell, CORRIDOR_TILE_NAME, overrideExistingObjects);
        //    }
        //}
    }


    /// Creates an L-shaped corridor between two points, randomly choosing horizontal-first or vertical-first
    private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
    {
        var horizontalFirst = RandomService.Chance(0.5f);

        if (horizontalFirst)
        {
            // Draw horizontal line first, then vertical
            CreateHorizontalCorridor(start.x, end.x, start.y);
            CreateVerticalCorridor(start.y, end.y, end.x);
        }
        else
        {
            // Draw vertical line first, then horizontal
            CreateVerticalCorridor(start.y, end.y, start.x);
            CreateHorizontalCorridor(start.x, end.x, end.y);
        }
    }

    /// Creates a horizontal corridor from x1 to x2 at the given y coordinate
    private void CreateHorizontalCorridor(int x1, int x2, int y)
    {
        var xMin = Mathf.Min(x1, x2);
        var xMax = Mathf.Max(x1, x2);

        for (var x = xMin; x <= xMax; x++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;

            AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
        }
    }

    /// Creates a vertical corridor from y1 to y2 at the given x coordinate
    private void CreateVerticalCorridor(int y1, int y2, int x)
    {
        var yMin = Mathf.Min(y1, y2);
        var yMax = Mathf.Max(y1, y2);

        for (var y = yMin; y <= yMax; y++)
        {
            if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                continue;

            AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
        }
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
}

public class Node
{
    public Node(RectInt rect)
    {
        _rect = rect;
    }

    public RectInt _rect { get; }
    public RectInt _room { get; set; }
    public Node _parent { get; private set; }
    public Node _child1 { get; private set; }
    public Node _child2 { get; private set; }

    /// <summary>
    /// </summary>
    /// <param name="where">0 to 1</param>
    /// <param name="verticalCut"></param>
    public void Cut(float where, bool verticalCut = true)
    {
        if (where is < 0 or > 1)
            return;
        if (verticalCut)
        {
            var xCut = (_rect.xMin + _rect.xMax) * where;
            var c1 = new RectInt(_rect.xMin, _rect.yMin, (int)(_rect.width * where), _rect.height);
            var c2 = new RectInt((int)xCut, _rect.yMin, (int)(_rect.width * (1 - where)), _rect.height);
            _child1 = new Node(c1)
            {
                _parent = this
            };
            _child2 = new Node(c2)
            {
                _parent = this
            };
        }
        else
        {
            var yCut = (_rect.yMin + _rect.yMax) * where;
            var c1 = new RectInt(_rect.xMin, _rect.yMin, _rect.width, (int)(_rect.height * where));
            var c2 = new RectInt(_rect.xMin, (int)yCut, _rect.width, (int)(_rect.height * (1 - where)));
            _child1 = new Node(c1)
            {
                _parent = this
            };
            _child2 = new Node(c2)
            {
                _parent = this
            };
        }
    }

    public void CutChild(float where, float where2, bool verticalCut = true)
    {
        _child1.Cut(where, verticalCut);
        _child2.Cut(where2, verticalCut);
    }

    public override string ToString()
    {
        return $"{_rect}|{_room}";
    }
}