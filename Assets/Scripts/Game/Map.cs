using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class TileChanger : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase[] tiles; // Drag a tile from your Tile Palette here
    private bool winner = false;
    private int number = 1;

    void Update()
    {
        if (!winner)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int cellPos = tilemap.WorldToCell(mouseWorldPos);

                TileBase clickedTile = tilemap.GetTile(cellPos);


                if (tilemap.HasTile(cellPos))
                {
                    if (clickedTile == tiles[0])
                    {
                        tilemap.SetTile(cellPos, tiles[number]);
                        //Debug.Log($"Changed tile at {cellPos} to new tile.");
                        clickedTile = tilemap.GetTile(cellPos);

                        winner = DidItWin(number, 5, 8);
                        if (winner)
                        {
                            print($"Player {number} won!");
                        }

                        if (number == 1) number = 2;
                        else number = 1;
                    }

                }
            }
        }
            
        /* for (int i = 0; i < 3; i++)
        {
            tilemap.SetTile(new Vector3Int(i, 0, 0), tiles[3]);
        } */

    }

    private Vector3Int vec(int n1, int n2, int n3)
    {
        return new Vector3Int(n1,n2,n3);
    }

    private bool DidItWin(int tilepos, int timesToRepeat, int size)
    {
        bool leave = false;
        for (int a = 0; a < size; a++)
        {
            for (int b = 0; b < size; b++)
            {
                Vector3Int pos = vec(a, b, 0);
                //Left to right
                for (int i = 0; i < timesToRepeat; i++)
                {
                    if (tilemap.GetTile(pos + vec(i, 0, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i + 1 == timesToRepeat)
                    {
                        for (int j = 0; j < timesToRepeat; j++)
                            tilemap.SetTile(pos + vec(j, 0, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }

                //Right to left
                for (int i = 0; i > -timesToRepeat; i--)
                {
                    if (tilemap.GetTile(pos + vec(i, 0, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i - 1 == -timesToRepeat)
                    {
                        for (int j = 0; j > -timesToRepeat; j--)
                            tilemap.SetTile(pos + vec(j, 0, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }

                //Down to up
                for (int i = 0; i < timesToRepeat; i++)
                {
                    if (tilemap.GetTile(pos + vec(0, i, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i + 1 == timesToRepeat)
                    {
                        for (int j = 0; j < timesToRepeat; j++)
                            tilemap.SetTile(pos + vec(0, j, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }

                //Up to Down
                for (int i = 0; i > -timesToRepeat; i--)
                {
                    if (tilemap.GetTile(pos + vec(0, i, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i - 1 == -timesToRepeat)
                    {
                        for (int j = 0; j > -timesToRepeat; j--)
                            tilemap.SetTile(pos + vec(0, j, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }

                //Left down to right up
                for (int i = 0; i < timesToRepeat; i++)
                {
                    if (tilemap.GetTile(pos + vec(i, i, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i + 1 == timesToRepeat)
                    {
                        for (int j = 0; j < timesToRepeat; j++)
                            tilemap.SetTile(pos + vec(j, j, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }

                //Right up to left down
                for (int i = 0; i > -timesToRepeat; i--)
                {
                    if (tilemap.GetTile(pos + vec(i, i, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i - 1 == -timesToRepeat)
                    {
                        for (int j = 0; j > -timesToRepeat; j--)
                            tilemap.SetTile(pos + vec(j, j, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }

                //Right down to left up
                for (int i = 0; i < timesToRepeat; i++)
                {
                    if (tilemap.GetTile(pos + vec(-i, i, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i + 1 == timesToRepeat)
                    {
                        for (int j = 0; j < timesToRepeat; j++)
                            tilemap.SetTile(pos + vec(-j, j, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }

                //Left up to right down
                for (int i = 0; i > -timesToRepeat; i--)
                {
                    if (tilemap.GetTile(pos + vec(i, -i, 0)) != tiles[tilepos])
                    {
                        leave = true;
                    }
                    if (leave)
                    {
                        leave = false;
                        break;
                    }
                    if (i - 1 == -timesToRepeat)
                    {
                        for (int j = 0; j > -timesToRepeat; j--)
                            tilemap.SetTile(pos + vec(j, -j, 0), tiles[tilepos + 2]);
                        return true;
                    }
                }
            }
        }

            return false;
    }
}