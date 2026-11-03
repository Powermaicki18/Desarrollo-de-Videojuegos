using UnityEngine;

// Para conectar a eventos del EnemySpawner (ej. On Wave Cleared -> PlogHealPlayer.HealPercent(0.25)).
public class PlogHealPlayer : MonoBehaviour
{
    public void HealPercent(float percent)
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        var h = p ? p.GetComponent<Health>() : null;
        if (h != null && !h.IsDead) h.Heal(h.maxHealth * percent);
    }
}