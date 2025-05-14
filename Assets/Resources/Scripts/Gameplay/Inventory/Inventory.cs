using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // Inventory values
    public int Ammo { get; private set; } = 0;

    public int Medkits { get; private set; } = 0;

    List<Gun> ownedGuns = new List<Gun>();

    public Gun CurrentGun { get; private set; }

    public void AddAmmo(int amount) => Ammo += amount;

    public void AddMedkit() => Medkits += 1;

    public void UseMedkit()
    {
        if (Medkits > 0)
        {
            Medkits--;
        }
    }

    public void AddGun(Gun gun)
    {
        if (!ownedGuns.Contains(gun))
        {
            ownedGuns.Add(gun);
        }

        CurrentGun = gun;
    }

    public bool HasGun(Gun gun) => ownedGuns.Contains(gun);
}
