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
    GunBase currentGunInstance;

    void OnDisable()
    {
        if (player != null && player.IsOwner) UnsubscribeEvents();
    }

    public void SetPlayer(Player p)
    {
        //Debug.Log($"Setting player to UI. Player name: {p.steamName.Value}");

        if (player != null) UnsubscribeEvents();

        player = p;

        if (player == null) return;

        //Debug.Log($"Subscribing {p.steamName.Value} to UI events.");

        player.currentHealth.OnValueChanged += (prev, curr) => OnHealthChanged(curr);
        player.inventory.Medkits.OnValueChanged += (prev, curr) => OnMedkitChanged(curr);
        player.inventory.currentGunType.OnValueChanged += (prev, curr) => OnGunChanged(curr);
        StartCoroutine(WaitForGun());
        //player.inventory.OnGunChanged += OnGunChanged;

        healthSlider.maxValue = 100;

        OnHealthChanged(player.currentHealth.Value);
        OnMedkitChanged(player.inventory.Medkits.Value);
        OnGunChanged(player.inventory.currentGunType.Value);
        //OnCurrentAmmoChanged(player.inventory.currentGun.currentAmmo.Value);
    }

    void UnsubscribeEvents()
    {
        player.currentHealth.OnValueChanged -= (prev, curr) => OnHealthChanged(curr);
        player.inventory.Medkits.OnValueChanged -= (prev, curr) => OnMedkitChanged(curr);
        player.inventory.currentGunType.OnValueChanged -= (prev, curr) => OnGunChanged(curr);
        UnsubscribeFromGun();
        //player.inventory.OnGunChanged -= OnGunChanged;
    }

    void SubscribeToGun(GunBase gun)
    {
        if (gun == null) return;
        currentGunInstance = gun;
        currentGunInstance.currentAmmo.OnValueChanged += (prev, curr) => OnCurrentAmmoChanged(curr);
        OnCurrentAmmoChanged(currentGunInstance.currentAmmo.Value);
    }

    void UnsubscribeFromGun()
    {
        if (currentGunInstance == null) return;
        currentGunInstance.currentAmmo.OnValueChanged -= (prev, curr) => OnCurrentAmmoChanged(curr);
        currentGunInstance = null;
    }

    IEnumerator WaitForGun()
    {
        while (player.inventory.currentGun == null)
        {
            yield return null;
        }

        //Debug.Log("No longer waiting for Gun.");
        SubscribeToGun(player.inventory.currentGun);
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
            case GunBase.Type.None:
            default:
                weaponIcon.sprite = pistolSprite;
                break;
        }

        UnsubscribeFromGun();
        StartCoroutine(WaitForGun());
    }

    void OnCurrentAmmoChanged(int newValue)
    {
        currentAmmoText.text = $"{newValue}";
        if (currentGunInstance) maxAmmoText.text = "∞";
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
