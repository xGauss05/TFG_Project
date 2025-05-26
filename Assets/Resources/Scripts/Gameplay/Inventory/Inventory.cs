using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Inventory : NetworkBehaviour
{
    public NetworkVariable<int> Ammo = new NetworkVariable<int>(0);
    public NetworkVariable<int> Medkits = new NetworkVariable<int>(0);

    public GunBase currentGun { get; private set; }
    public Dictionary<GunBase, bool> availableGuns = new Dictionary<GunBase, bool>();

    public event Action<int> OnMedkitChanged;
    public event Action<GunBase> OnGunChanged;

    public void AddGun(GunBase gun)
    {
        GunBase gunToGet = CheckGun(gun);
        if (gunToGet != null)
        {
            List<GunBase> keysToEnable = new List<GunBase>();
            foreach (var gunKey in availableGuns.Keys)
            {
                if (gunKey.GetType() == gun.GetType())
                {
                    keysToEnable.Add(gunKey);
                    break;
                }
            }

            foreach (var key in keysToEnable)
            {
                availableGuns[key] = true;
            }
        }
    }

    GunBase CheckGun(GunBase gunToCheck)
    {
        GunBase ret = null;

        foreach (var kvp in availableGuns)
        {
            if (kvp.Key.GetType() == gunToCheck.GetType())
            {
                ret = kvp.Key;
                break;
            }
        }

        return ret;
    }

    public bool EquipGun(GunBase gun)
    {
        GunBase ret = CheckGun(gun);

        if (ret != null)
        {
            currentGun = ret;
            OnGunChanged?.Invoke(currentGun);
            return true;
        }

        return false;
    }

    public void ShootGun(Vector3 origin, Vector3 direction)
    {
        currentGun.Shoot(origin, direction);
        OnGunChanged.Invoke(currentGun);
    }

    public void ReloadGun()
    {
        currentGun.Reload();
        StartCoroutine(DelayReload());
    }

    IEnumerator DelayReload()
    {
        float seconds = currentGun.GetReloadLength();
        yield return new WaitForSeconds(seconds);

        OnGunChanged?.Invoke(currentGun);
    }

    public bool IsGunAvailable(GunBase gun)
    {
        if (availableGuns.TryGetValue(gun, out bool isAvailable))
        {
            return isAvailable;
        }

        return false;
    }


    public void AddAmmo(int amount) => Ammo.Value += amount;

    public void AddMedkit()
    {
        Medkits.Value += 1;
        OnMedkitChanged?.Invoke(Medkits.Value);
    }

    public bool UseMedkit()
    {
        if (Medkits.Value > 0)
        {
            Medkits.Value -= 1;
            OnMedkitChanged?.Invoke(Medkits.Value);
            //Debug.Log("Used Medkit!");
            return true;
        }

        return false;
    }

}
