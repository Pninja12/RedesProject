using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameUI;
    [SerializeField] private TextMeshProUGUI detail;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase[] tiles;
    private int tileNumber;
    private int playerNumber;
    private string playerName;
    private int side;
    private float uiPosition;

    public void SetTile(int tile)
    {
        tileNumber = tile;
    }

    public void SetPlayerNumber(int number)
    {
        playerNumber = number;
    }

    public void SetPlayerName(string nameless)
    {

        playerName = nameless;
    }

    public void SetSide(int receivedSide)
    {

        side = receivedSide;
    }

    /* public override void OnNetworkSpawn()
    {
        playerNumber.OnValueChanged += OnPlayerNumberChanged;

        if (IsLocalPlayer)
        {
            Debug.Log($"I am Player {playerNumber} ({playerName})");
        }

        // Set initial value just in case it was set before spawn (not reliable without the OnValueChanged handler)
        UpdateDetailUI();
    } */
    private void OnPlayerNumberChanged(int previousValue, int newValue)
    {
        UpdateDetailUI();
    }

    public void Setup(int playerNumber, int side, int tileNumber, string playerName)
    {
        this.playerNumber = playerNumber;
        this.side = side;
        this.tileNumber = tileNumber;
        this.playerName = playerName;

        UpdateDetailUI();
    }

    private void UpdateDetailUI()
    {
        detail.SetText($"{playerNumber}");

        nameUI.rectTransform.anchoredPosition = new Vector2(uiPosition , 162f);
        detail.rectTransform.anchoredPosition = new Vector2(uiPosition , 50f);

        if (tilemap != null && tiles != null && tiles.Length > playerNumber)
        {
            tilemap.SetTile(new Vector3Int(side > 0 ? 3 : 0, 1, 0), tiles[playerNumber]);
        }
    }

    private void Start()
    {
        uiPosition = side > 0 ? 400f : -575f;

        /* if (!IsOwner)
            return;
        if (IsLocalPlayer)
        {
            Debug.Log($"Eu sou o Player {playerNumber.Value}");
        } */
        detail.SetText($"{playerNumber}");

        nameUI.rectTransform.anchoredPosition = new Vector2(uiPosition , 162f);
        detail.rectTransform.anchoredPosition = new Vector2(uiPosition, 50f);

        tilemap.SetTile(new Vector3Int(side > 0 ? 3 : 0, 1, 0), tiles[playerNumber]);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void GiveNameAndDetail()
    {

    }

    public int GetPlayerNumber()
    {
        return playerNumber;
    }
    
}
