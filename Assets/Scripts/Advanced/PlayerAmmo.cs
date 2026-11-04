using System.Collections;
using UnityEngine;

// Ammunition belongs to the run, so changing stages does not refill it.
public class PlayerAmmo : MonoBehaviour
{
    public const int MagazineSize = 12;
    public int Loaded { get; private set; } = MagazineSize;
    public int Reserve { get; private set; } = 60;
    public bool Reloading { get; private set; }
    private float nextShot;

    public bool TryFire()
    {
        if (Reloading || Time.time < nextShot) return false;
        if (Loaded == 0) { Reload(); return false; }
        Loaded--;
        nextShot = Time.time + 0.18f;
        return true;
    }

    public void Reload()
    {
        if (Reloading || Loaded >= MagazineSize || Reserve <= 0) return;
        var player = FindObjectOfType<PlayerController>();
        if (player != null && player.Actions != null && !player.Actions.Reload()) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        Reloading = true;
        yield return new WaitForSeconds(PlayerActionAnimation.ReloadDuration);
        int amount = Mathf.Min(MagazineSize - Loaded, Reserve);
        Loaded += amount;
        Reserve -= amount;
        Reloading = false;
    }

    public void AddReserve(int amount) { Reserve += Mathf.Max(0, amount); }
    public void Restore(int loaded, int reserve)
    {
        StopAllCoroutines();
        Reloading = false;
        Loaded = Mathf.Clamp(loaded, 0, MagazineSize);
        Reserve = Mathf.Max(0, reserve);
        nextShot = 0;
    }
}
