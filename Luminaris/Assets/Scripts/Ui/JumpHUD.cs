using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class JumpHUD : MonoBehaviour
{
    //[Header("References")]
    // [SerializeField] private PlayerMovement player;
    //  [SerializeField] private Transform container;
    // [SerializeField] private GameObject jumpIconPrefab;
    //[SerializeField] private TextMeshProUGUI jumpText;

    private readonly List<Image> icons = new();

    private void OnEnable()
    {
        TurnControl.OnTurnEnded += RefreshHUD;
        //     player.OnJumpsChanged += RefreshHUD;
    }

    private void OnDisable()
    {
        TurnControl.OnTurnEnded -= RefreshHUD;
        // player.OnJumpsChanged -= RefreshHUD;
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        foreach (var icon in icons)
            Destroy(icon.gameObject);
        icons.Clear();

        // for (int i = 0; i < player.MaxJumps; i++)
        {
            //   var iconObj = Instantiate(jumpIconPrefab, container);
            //  var img = iconObj.GetComponent<Image>();
            //  img.color = i < player.JumpsUsed ? Color.gray : Color.white;
            // icons.Add(img);
        }

        // if (jumpText != null)
        // jumpText.text = $"{player.MaxJumps - player.JumpsUsed:00}x";
    }
}
//*