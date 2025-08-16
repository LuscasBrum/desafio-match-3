using System;
using System.Collections.Generic;
using Gazeus.DesafioMatch3.Models;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Core
{
    public class GameService
    {
        public event Action<int> OnPointMatched;

        private List<List<Tile>> _boardTiles;
        private List<int> _tilesTypes;
        private int _tileCount;

        public bool IsValidMovement(int fromX, int fromY, int toX, int toY)
        {
            List<List<Tile>> newBoard = CopyBoard(_boardTiles);

            (newBoard[toY][toX], newBoard[fromY][fromX]) = (newBoard[fromY][fromX], newBoard[toY][toX]);

            for (int y = 0; y < newBoard.Count; y++)
            {
                for (int x = 0; x < newBoard[y].Count; x++)
                {
                    if (x > 1 &&
                        newBoard[y][x].Type == newBoard[y][x - 1].Type &&
                        newBoard[y][x - 1].Type == newBoard[y][x - 2].Type)
                    {
                        return true;
                    }

                    if (y > 1 &&
                        newBoard[y][x].Type == newBoard[y - 1][x].Type &&
                        newBoard[y - 1][x].Type == newBoard[y - 2][x].Type)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<List<Tile>> StartGame(int boardWidth, int boardHeight)
        {
            _tilesTypes = new List<int> { 0, 1, 2, 3 };
            _boardTiles = CreateBoard(boardWidth, boardHeight, _tilesTypes);

            return _boardTiles;
        }

        public List<BoardSequence> SwapTile(int fromX, int fromY, int toX, int toY)
        {
            List<List<Tile>> newBoard = CopyBoard(_boardTiles);
            (newBoard[toY][toX], newBoard[fromY][fromX]) = (newBoard[fromY][fromX], newBoard[toY][toX]);

            List<BoardSequence> boardSequences = new();

            while (true)
            {
                List<MatchGroup> groups = FindMatchGroups(newBoard);
                if (groups.Count == 0) break;

                var toRemove = new HashSet<(int x, int y)>();
                var specialsToCreate = new List<(Vector2Int pos, SpecialKind kind)>();

                var membershipCount = new Dictionary<(int x, int y), (int hCount, int vCount)>();

                for (int gi = 0; gi < groups.Count; gi++)
                {
                    var g = groups[gi];
                    for (int i = 0; i < g.Cells.Count; i++)
                    {
                        var p = g.Cells[i];
                        var key = (p.x, p.y);
                        if (!membershipCount.ContainsKey(key)) membershipCount[key] = (0, 0);
                        var tup = membershipCount[key];
                        if (g.IsHorizontal) tup.hCount++; else tup.vCount++;
                        membershipCount[key] = tup;
                    }
                }

                for (int gi = 0; gi < groups.Count; gi++)
                {
                    var g = groups[gi];
                    int n = g.Cells.Count;

                    bool hasExistingSpecial = false;
                    for (int i = 0; i < g.Cells.Count && !hasExistingSpecial; i++)
                    {
                        var p = g.Cells[i];
                        var t = newBoard[p.y][p.x];
                        if (t.Special != SpecialKind.None) hasExistingSpecial = true;
                    }

                    if (hasExistingSpecial)
                    {
                        // Special Tiles 
                        for (int i = 0; i < g.Cells.Count; i++)
                        {
                            var p = g.Cells[i];
                            var t = newBoard[p.y][p.x];
                            switch (t.Special)
                            {
                                case SpecialKind.LineH:
                                    for (int x = 0; x < newBoard[p.y].Count; x++) toRemove.Add((x, p.y));
                                    break;
                                case SpecialKind.LineV:
                                    for (int y = 0; y < newBoard.Count; y++) toRemove.Add((p.x, y));
                                    break;
                                case SpecialKind.Bomb:
                                    for (int dy = -1; dy <= 1; dy++)
                                        for (int dx = -1; dx <= 1; dx++)
                                        {
                                            int nx = p.x + dx, ny = p.y + dy;
                                            if (ny >= 0 && ny < newBoard.Count && nx >= 0 && nx < newBoard[ny].Count)
                                                toRemove.Add((nx, ny));
                                        }
                                    break;
                                case SpecialKind.Color:
                                    int color = PickColorForColorBomb(g, newBoard, fromX, fromY, toX, toY);
                                    if (color > -1)
                                    {
                                        for (int y = 0; y < newBoard.Count; y++)
                                            for (int x = 0; x < newBoard[y].Count; x++)
                                                if (newBoard[y][x].Type == color)
                                                    toRemove.Add((x, y));
                                    }
                                    break;
                            }
                            toRemove.Add((p.x, p.y));
                        }
                    }
                    else
                    {
                        if (n >= 5)
                        {
                            var seed = PickSeedForGroup(g, fromX, fromY, toX, toY);
                            specialsToCreate.Add((seed, SpecialKind.Color));
                            for (int i = 0; i < g.Cells.Count; i++)
                            {
                                var p = g.Cells[i];
                                if (!(p.x == seed.x && p.y == seed.y)) toRemove.Add((p.x, p.y));
                            }
                        }
                        else if (n == 4)
                        {
                            var seed = PickSeedForGroup(g, fromX, fromY, toX, toY);
                            specialsToCreate.Add((seed, g.IsHorizontal ? SpecialKind.LineH : SpecialKind.LineV));
                            for (int i = 0; i < g.Cells.Count; i++)
                            {
                                var p = g.Cells[i];
                                if (!(p.x == seed.x && p.y == seed.y)) toRemove.Add((p.x, p.y));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < g.Cells.Count; i++)
                            {
                                var p = g.Cells[i];
                                toRemove.Add((p.x, p.y));
                            }
                        }
                    }
                }

                foreach (var kv in membershipCount)
                {
                    var key = kv.Key;
                    var counts = kv.Value;
                    if (counts.hCount > 0 && counts.vCount > 0)
                    {
                        var pos = new Vector2Int(key.x, key.y);

                        toRemove.Remove((key.x, key.y));

                        bool replaced = false;
                        for (int i = 0; i < specialsToCreate.Count; i++)
                        {
                            if (specialsToCreate[i].pos == pos)
                            {
                                specialsToCreate[i] = (pos, SpecialKind.Bomb);
                                replaced = true;
                                break;
                            }
                        }
                        if (!replaced) specialsToCreate.Add((pos, SpecialKind.Bomb));
                    }
                }

                if (toRemove.Count == 0 && specialsToCreate.Count == 0) break;

                var matchedPosition = new List<Vector2Int>();
                foreach (var pr in toRemove)
                {
                    matchedPosition.Add(new Vector2Int(pr.x, pr.y));
                    newBoard[pr.y][pr.x] = new Tile { Id = -1, Type = -1, Special = SpecialKind.None };
                    OnPointMatched?.Invoke(matchedPosition.Count);
                }

                for (int i = 0; i < specialsToCreate.Count; i++)
                {
                    var (pos, kind) = specialsToCreate[i];
                    var t = newBoard[pos.y][pos.x];
                    if (t.Type > -1)
                    {
                        t.Special = kind;
                        newBoard[pos.y][pos.x] = t;
                    }
                }

                var movedTiles = new Dictionary<int, MovedTileInfo>();
                var movedTilesList = new List<MovedTileInfo>();
                for (int x = 0; x < newBoard[0].Count; x++)
                {
                    for (int y = newBoard.Count - 1; y >= 0; y--)
                    {
                        if (newBoard[y][x].Type == -1)
                        {
                            for (int j = y; j > 0; j--)
                            {
                                Tile movedTile = newBoard[j - 1][x];
                                newBoard[j][x] = movedTile;
                                if (movedTile.Type > -1)
                                {
                                    if (movedTiles.ContainsKey(movedTile.Id))
                                    {
                                        movedTiles[movedTile.Id].To = new Vector2Int(x, j);
                                    }
                                    else
                                    {
                                        MovedTileInfo movedTileInfo = new()
                                        {
                                            From = new Vector2Int(x, j - 1),
                                            To = new Vector2Int(x, j)
                                        };
                                        movedTiles.Add(movedTile.Id, movedTileInfo);
                                        movedTilesList.Add(movedTileInfo);
                                    }
                                }
                            }

                            newBoard[0][x] = new Tile
                            {
                                Id = -1,
                                Type = -1,
                                Special = SpecialKind.None
                            };
                        }
                    }
                }

                List<AddedTileInfo> addedTiles = new();
                for (int y = 0; y < newBoard.Count; y++)
                {
                    for (int x = 0; x < newBoard[y].Count; x++)
                    {
                        if (newBoard[y][x].Type == -1)
                        {
                            int tileType = UnityEngine.Random.Range(0, _tilesTypes.Count);
                            Tile tile = newBoard[y][x];
                            tile.Id = _tileCount++;
                            tile.Type = _tilesTypes[tileType];
                            tile.Special = SpecialKind.None;

                            addedTiles.Add(new AddedTileInfo
                            {
                                Position = new Vector2Int(x, y),
                                Type = tile.Type
                            });
                        }
                    }
                }

                BoardSequence sequence = new()
                {
                    MatchedPosition = matchedPosition,
                    MovedTiles = movedTilesList,
                    AddedTiles = addedTiles
                };
                boardSequences.Add(sequence);
            }

            _boardTiles = newBoard;
            return boardSequences;
        }


        private static List<List<Tile>> CopyBoard(List<List<Tile>> boardToCopy)
        {
            List<List<Tile>> newBoard = new(boardToCopy.Count);
            for (int y = 0; y < boardToCopy.Count; y++)
            {
                newBoard.Add(new List<Tile>(boardToCopy[y].Count));
                for (int x = 0; x < boardToCopy[y].Count; x++)
                {
                    Tile tile = boardToCopy[y][x];
                    newBoard[y].Add(new Tile { Id = tile.Id, Type = tile.Type });
                }
            }

            return newBoard;
        }

        private List<List<Tile>> CreateBoard(int width, int height, List<int> tileTypes)
        {
            List<List<Tile>> board = new(height);
            _tileCount = 0;
            for (int y = 0; y < height; y++)
            {
                board.Add(new List<Tile>(width));
                for (int x = 0; x < width; x++)
                {
                    board[y].Add(new Tile { Id = -1, Type = -1 });
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    List<int> noMatchTypes = new(tileTypes.Count);
                    for (int i = 0; i < tileTypes.Count; i++)
                    {
                        noMatchTypes.Add(_tilesTypes[i]);
                    }

                    if (x > 1 &&
                        board[y][x - 1].Type == board[y][x - 2].Type)
                    {
                        noMatchTypes.Remove(board[y][x - 1].Type);
                    }

                    if (y > 1 &&
                        board[y - 1][x].Type == board[y - 2][x].Type)
                    {
                        noMatchTypes.Remove(board[y - 1][x].Type);
                    }

                    board[y][x].Id = _tileCount++;
                    board[y][x].Type = noMatchTypes[UnityEngine.Random.Range(0, noMatchTypes.Count)];
                }
            }

            return board;
        }

        private static List<MatchGroup> FindMatchGroups(List<List<Tile>> board)
        {
            var groups = new List<MatchGroup>();

            int h = board.Count;
            if (h == 0) return groups;
            int w = board[0].Count;

            // Horizontal
            for (int y = 0; y < h; y++)
            {
                int runStart = 0;
                for (int x = 1; x <= w; x++)
                {
                    bool same = x < w && board[y][x].Type > -1 && board[y][x].Type == board[y][x - 1].Type;
                    if (!same)
                    {
                        int len = x - runStart;
                        if (len >= 3 && board[y][runStart].Type > -1)
                        {
                            var cells = new List<Vector2Int>(len);
                            for (int k = runStart; k < x; k++) cells.Add(new Vector2Int(k, y));
                            groups.Add(new MatchGroup { Cells = cells, IsHorizontal = true });
                        }
                        runStart = x;
                    }
                }
            }

            // Vertical
            for (int x = 0; x < w; x++)
            {
                int runStart = 0;
                for (int y = 1; y <= h; y++)
                {
                    bool same = y < h && board[y][x].Type > -1 && board[y][x].Type == board[y - 1][x].Type;
                    if (!same)
                    {
                        int len = y - runStart;
                        if (len >= 3 && board[runStart][x].Type > -1)
                        {
                            var cells = new List<Vector2Int>(len);
                            for (int k = runStart; k < y; k++) cells.Add(new Vector2Int(x, k));
                            groups.Add(new MatchGroup { Cells = cells, IsHorizontal = false });
                        }
                        runStart = y;
                    }
                }
            }

            return groups;
        }

        private static int PickColorForColorBomb(MatchGroup g, List<List<Tile>> board, int fromX, int fromY, int toX, int toY)
        {
            for (int i = 0; i < g.Cells.Count; i++)
            {
                var p = g.Cells[i];
                var t = board[p.y][p.x];
                if (t.Special == SpecialKind.None && t.Type > -1)
                    return t.Type;
            }
            if (toY >= 0 && toY < board.Count && toX >= 0 && toX < board[toY].Count)
            {
                var t = board[toY][toX];
                if (t.Type > -1) return t.Type;
            }
            if (fromY >= 0 && fromY < board.Count && fromX >= 0 && fromX < board[fromY].Count)
            {
                var t = board[fromY][fromX];
                if (t.Type > -1) return t.Type;
            }
            return -1;
        }

        private static Vector2Int PickSeedForGroup(MatchGroup g, int fromX, int fromY, int toX, int toY)
        {
            for (int i = 0; i < g.Cells.Count; i++)
            {
                var p = g.Cells[i];
                if ((p.x == toX && p.y == toY) || (p.x == fromX && p.y == fromY))
                    return p;
            }
            return g.Cells[g.Cells.Count / 2];
        }

        private static List<List<bool>> FindMatches(List<List<Tile>> newBoard)
        {
            List<List<bool>> matchedTiles = new();
            for (int y = 0; y < newBoard.Count; y++)
            {
                matchedTiles.Add(new List<bool>(newBoard[y].Count));
                for (int x = 0; x < newBoard.Count; x++)
                {
                    matchedTiles[y].Add(false);
                }
            }

            for (int y = 0; y < newBoard.Count; y++)
            {
                for (int x = 0; x < newBoard[y].Count; x++)
                {
                    if (x > 1 &&
                        newBoard[y][x].Type == newBoard[y][x - 1].Type &&
                        newBoard[y][x - 1].Type == newBoard[y][x - 2].Type)
                    {
                        matchedTiles[y][x] = true;
                        matchedTiles[y][x - 1] = true;
                        matchedTiles[y][x - 2] = true;
                    }

                    if (y > 1 &&
                        newBoard[y][x].Type == newBoard[y - 1][x].Type &&
                        newBoard[y - 1][x].Type == newBoard[y - 2][x].Type)
                    {
                        matchedTiles[y][x] = true;
                        matchedTiles[y - 1][x] = true;
                        matchedTiles[y - 2][x] = true;
                    }
                }
            }

            return matchedTiles;
        }

        private static bool HasMatch(List<List<bool>> list)
        {
            for (int y = 0; y < list.Count; y++)
            {
                for (int x = 0; x < list[y].Count; x++)
                {
                    if (list[y][x])
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
