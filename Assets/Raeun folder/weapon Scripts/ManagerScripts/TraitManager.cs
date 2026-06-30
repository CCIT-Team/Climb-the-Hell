//using NUnit.Framework;
//using UnityEngine;

//public class TraitManager : MonoBehaviour
//{
//    public PlayerStats playerstat;
//    public Player player;
//    public WeaponData weaponData;

//    public void ApplyTrait(TraitData trait, int level)
//    {
//        switch (trait.type)
//        {
//            case TraitType.StartGold:
//                playerstat.startGold += Mathf.RoundToInt(trait.valuePerLevel * level);
//                break;

//            case TraitType.MaxHp:
//                playerstat.playerhp += Mathf.RoundToInt(trait.valuePerLevel * level);
//                break;

//            case TraitType.Attack:
//                playerstat.attackMultiplier += trait.valuePerLevel * level;
//                //break;

//            case TraitType.DeathResist:
//                playerstat.deathResist = Mathf.RoundToInt(trait.valuePerLevel * level);
//                break;

//            case TraitType.CritChance:
//                playerstat.critChance += trait.valuePerLevel * level;
//                break;

//            case TraitType.CritDamage:
//                playerstat.critDamage += trait.valuePerLevel * level;
//                break;

//            case TraitType.ExtraDash:
//                playerstat.maxDash += Mathf.RoundToInt(trait.valuePerLevel * level);
//                break;

//            case TraitType.GoldMultiplier:
//                playerstat.goldMultiplier += trait.valuePerLevel * level;
//                break;

//            case TraitType.Reroll:
//                playerstat.rerollCount = Mathf.RoundToInt(trait.valuePerLevel * level);
//                break;
//        }
//    }

//    public int GetPrice(TraitData trait, int currentLevel)
//    {
//        return trait.levelPrices[currentLevel];
//    }
//}