using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon_Rapier : BaseItem<SO_RapierWeapon>
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Damageable"))
        {
            var characterStatsControl = other.gameObject.GetComponentInParent<Character>().StatsControl;
            var source = Owner != null ? Owner.StatsControl : null;
            if (characterStatsControl != null && characterStatsControl != source)
                UseItem(characterStatsControl, source);
        }
    }
}
