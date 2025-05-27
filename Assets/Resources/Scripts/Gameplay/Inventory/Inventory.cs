using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Inventory : NetworkBehaviour
{
    public GunBase currentGun { get; private set; }

    [Header("Inventory Network variables")]
    public NetworkVariable<int> Ammo = new NetworkVariable<int>(0);
    public NetworkVariable<int> Medkits = new NetworkVariable<int>(0);
    public NetworkVariable<GunBase.Type> currentGunType = new NetworkVariable<GunBase.Type>(
        GunBase.Type.Pistol, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    public Dictionary<GunBase, bool> availableGuns = new Dictionary<GunBase, bool>();

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

            if (IsServer)
            {
                if (ret is Pistol) currentGunType.Value = GunBase.Type.Pistol;
                else if (ret is AssaultRifle) currentGunType.Value = GunBase.Type.AssaultRifle;
                else currentGunType.Value = GunBase.Type.Shotgun;
            }

            

            return true;
        }

        return false;
    }

    public void ShootGun(Vector3 origin, Vector3 direction)
    {
        currentGun.Shoot(origin, direction);
    }

    public void ReloadGun()
    {
        currentGun.Reload();
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
    }

    public bool UseMedkit()
    {
        if (Medkits.Value > 0)
        {
            Medkits.Value -= 1;
            //Debug.Log("Used Medkit!");
            return true;
        }

        return false;
    }

}
