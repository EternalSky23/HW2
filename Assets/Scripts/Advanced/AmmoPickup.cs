using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    public string Id;
    private void Update() { transform.Rotate(0, 70 * Time.deltaTime, 0); }
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        AdvancedGame.Instance.Ammo.AddReserve(24);
        AdvancedGame.Instance.CollectSupply(Id);
        AdvancedGame.Instance.Notify("Ammo +24");
        Destroy(gameObject);
    }
}
