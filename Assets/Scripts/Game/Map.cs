using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileChanger : NetworkBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase[] tiles; // 0: empty, 1: player1, 2: player2, 3+: highlight tiles

    private NetworkVariable<int> currentPlayer = new NetworkVariable<int>();
    private bool winner = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentPlayer.Value = 1;
        }
    }

    void Update()
    {
        // Only the client owner should interact, and only if game isn't over
        if (winner || !IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject?.GetComponent<GameTag>();
            if (localPlayer == null || localPlayer.GetPlayerNumber() != currentPlayer.Value) return;

            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = tilemap.WorldToCell(mouseWorldPos);
            cellPos.z = 0;

            TileBase clickedTile = tilemap.GetTile(cellPos);

            if (tilemap.HasTile(cellPos) && clickedTile == tiles[0])
            {
                ChangeTileServerRpc(cellPos, currentPlayer.Value);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeTileServerRpc(Vector3Int cellPos, int tileIndex)
    {
        if (tilemap.GetTile(cellPos) != tiles[0] || winner) return;

        tilemap.SetTile(cellPos, tiles[tileIndex]);
        UpdateTileClientRpc(cellPos, tileIndex);

        if (DidItWin(tileIndex, 5, 8))
        {
            winner = true;
            Debug.Log($"Player {tileIndex} won!");
        }
        else
        {
            currentPlayer.Value = tileIndex == 1 ? 2 : 1;
        }
    }

    [ClientRpc]
    private void UpdateTileClientRpc(Vector3Int cellPos, int tileIndex)
    {
        tilemap.SetTile(cellPos, tiles[tileIndex]);
    }

    private Vector3Int vec(int x, int y, int z) => new Vector3Int(x, y, z);

    private bool DidItWin(int tileIndex, int toMatch, int boardSize)
    {
        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                Vector3Int origin = vec(x, y, 0);
                if (CheckDirection(origin, tileIndex, toMatch, vec(1, 0, 0)) ||  // Horizontal
                    CheckDirection(origin, tileIndex, toMatch, vec(0, 1, 0)) ||  // Vertical
                    CheckDirection(origin, tileIndex, toMatch, vec(1, 1, 0)) ||  // Diagonal \
                    CheckDirection(origin, tileIndex, toMatch, vec(-1, 1, 0)))   // Diagonal /
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CheckDirection(Vector3Int start, int tileIndex, int count, Vector3Int step)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3Int checkPos = start + step * i;
            if (tilemap.GetTile(checkPos) != tiles[tileIndex])
            {
                return false;
            }
        }

        // Highlight winning tiles
        for (int i = 0; i < count; i++)
        {
            Vector3Int winPos = start + step * i;
            tilemap.SetTile(winPos, tiles[tileIndex + 2]); // Assumes +2 is highlight version
        }

        return true;
    }
}