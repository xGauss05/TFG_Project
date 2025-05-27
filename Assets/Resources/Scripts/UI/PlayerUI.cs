using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class PlayerUI : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI medkitText;
    public TextMeshProUGUI currentAmmoText;
    public TextMeshProUGUI maxAmmoText;

    public Image weaponIcon;
    public Sprite pistolSprite;
    public Sprite assaultRifleSprite;
    public Sprite shotgunSprite;

    Player player;

    void OnDisable()
    {
        if (player != null && player.IsOwner)
        {
            player.currentHealth.OnValueChanged -= (prev, curr) => OnHealthChanged(curr);
            player.inventory.Medkits.OnValueChanged -= (prev, curr) => OnMedkitChanged(curr);
            player.inventory.currentGunType.OnValueChanged -= (prev, curr) => OnGunChanged(curr);
            //player.inventory.currentGun.currentAmmo.OnValueChanged -= (prev, curr) => OnCurrentAmmoChanged(curr);
            //player.inventory.OnGunChanged -= OnGunChanged;
        }
    }

    public void SetPlayer(Player p)
    {
        Debug.Log($"Setting player to UI. Player name: {p.steamName.Value}");

        if (player != null)
        {
            player.currentHealth.OnValueChanged -= (prev, curr) => OnHealthChanged(curr);
            player.inventory.Medkits.OnValueChanged -= (prev, curr) => OnMedkitChanged(curr);
            player.inventory.currentGunType.OnValueChanged -= (prev, curr) => OnGunChanged(curr);
            //player.inventory.currentGun.currentAmmo.OnValueChanged -= (prev, curr) => OnCurrentAmmoChanged(curr);
        }

        player = p;

        if (player == null) return;

        Debug.Log($"Subscribing {p.steamName.Value} to UI events.");

        player.currentHealth.OnValueChanged += (prev, curr) => OnHealthChanged(curr);
        player.inventory.Medkits.OnValueChanged += (prev, curr) => OnMedkitChanged(curr);
        player.inventory.currentGunType.OnValueChanged += (prev, curr) => OnGunChanged(curr);
        //player.inventory.currentGun.currentAmmo.OnValueChanged += (prev, curr) => OnCurrentAmmoChanged(curr);

        healthSlider.maxValue = 100;
        healthSlider.value = player.currentHealth.Value;
        healthText.text = $"{player.currentHealth.Value}";

        OnMedkitChanged(player.inventory.Medkits.Value);
        OnGunChanged(player.inventory.currentGunType.Value);
    }

    void OnMedkitChanged(int count)
    {
        medkitText.text = $"x{count}";
    }

    void OnGunChanged(GunBase.Type gunType)
    {
        switch (gunType)
        {
            case GunBase.Type.AssaultRifle:
                weaponIcon.sprite = assaultRifleSprite;
                break;
            case GunBase.Type.Shotgun:
                weaponIcon.sprite = shotgunSprite;
                break;
            case GunBase.Type.Pistol:
                weaponIcon.sprite = pistolSprite;
                break;
            case GunBase.Type.None:
            default:
                break;
        }
    }

    void OnCurrentAmmoChanged(int newValue)
    {
        currentAmmoText.text = $"{newValue}";
    }

    void OnHealthChanged(int newValue)
    {
        healthSlider.value = newValue;
        healthText.text = $"{player.currentHealth.Value}";

        float percentage = newValue / healthSlider.maxValue;

        if (percentage >= 0.7f)
        {
            healthSlider.fillRect.GetComponent<Image>().color = Color.green;
        }
        else if (percentage >= 0.3f)
        {
            healthSlider.fillRect.GetComponent<Image>().color = Color.yellow;
        }
        else
        {
            healthSlider.fillRect.GetComponent<Image>().color = Color.red;
        }
    }
}
