using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerUI : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;
    public TextMeshProUGUI medkitText;
    public TextMeshProUGUI currentAmmoText;
    public TextMeshProUGUI maxAmmoText;

    public Image weaponIcon;
    public Sprite pistolSprite;
    public Sprite assaultRifleSprite;
    public Sprite shotgunSprite;

    Player player;

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        TrySetLocalPlayer();
    }

    void OnDestroy()
    {
        if (player != null && player.IsOwner)
        {
            player.currentHealth.OnValueChanged -= OnHealthChanged;
            player.inventory.OnMedkitChanged -= OnMedkitChanged;
            player.inventory.OnGunChanged -= OnGunChanged;
        }

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            TrySetLocalPlayer();
        }
    }

    void TrySetLocalPlayer()
    {
        Player[] players = FindObjectsOfType<Player>();

        foreach (Player p in players)
        {
            if (p.IsOwner)
            {
                SetPlayer(p);
                break;
            }
        }
    }

    void SetPlayer(Player p)
    {
        if (player != null)
        {
            player.currentHealth.OnValueChanged -= OnHealthChanged;
            player.inventory.OnMedkitChanged -= OnMedkitChanged;
            player.inventory.OnGunChanged -= OnGunChanged;
        }

        player = p;

        if (player == null || !player.IsOwner) return;

        player.currentHealth.OnValueChanged += OnHealthChanged;
        player.inventory.OnMedkitChanged += OnMedkitChanged;
        player.inventory.OnGunChanged += OnGunChanged;

        healthSlider.maxValue = 100;
        healthSlider.value = player.currentHealth.Value;

        OnMedkitChanged(player.inventory.Medkits.Value);
        OnGunChanged(player.inventory.currentGun);
    }

    void OnMedkitChanged(int count)
    {
        medkitText.text = $"x{count}";
    }

    void OnGunChanged(GunBase gun)
    {
        if (gun == null) return;

        currentAmmoText.text = $"{gun.currentAmmo}";
        maxAmmoText.text = $"{gun.maxCapacity}";

        if (gun is Pistol)
        {
            weaponIcon.sprite = pistolSprite;
        }
        else if (gun is AssaultRifle)
        {
            weaponIcon.sprite = assaultRifleSprite;
        }
        else if (gun is Shotgun)
        {
            weaponIcon.sprite = shotgunSprite;
        }

    }

    void OnHealthChanged(int previousValue, int newValue)
    {
        healthSlider.value = newValue;
    }
}
