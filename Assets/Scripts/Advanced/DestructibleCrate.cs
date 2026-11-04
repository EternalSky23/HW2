using UnityEngine;

public class DestructibleCrate : MonoBehaviour
{
    public string Id;
    public float Health = 20;
    private bool broken;

    public void TakeDamage(float damage)
    {
        if (broken || damage <= 0) return;
        Health -= damage;
        if (Health > 0) return;
        broken = true;
        AdvancedGame.Instance.RememberCrate(Id);
        // The intact collider is removed immediately; fragments disappear after two seconds.
        GetComponent<Collider>().enabled = false;
        for (int i = 0; i < 8; i++)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "Wood fragment";
            piece.transform.position = transform.position + Random.insideUnitSphere * 0.3f;
            piece.transform.localScale = new Vector3(0.22f, 0.14f, 0.32f);
            piece.GetComponent<Renderer>().sharedMaterial = GetComponent<Renderer>().sharedMaterial;
            piece.GetComponent<Collider>().enabled = false;
            var body = piece.AddComponent<Rigidbody>();
            body.AddForce((Random.insideUnitSphere + Vector3.up) * 3, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * 4, ForceMode.Impulse);
            Destroy(piece, 2);
        }
        var pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickup.name = "Ammo supply (+24)";
        pickup.transform.position = transform.position;
        pickup.transform.localScale = Vector3.one * 0.4f;
        pickup.GetComponent<Renderer>().sharedMaterial = AdvancedGame.Instance.AmmoMaterial;
        pickup.GetComponent<Collider>().isTrigger = true;
        var rb = pickup.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        pickup.AddComponent<AmmoPickup>().Id = Id;
        AdvancedGame.Instance.RegisterSupply(Id, pickup.transform.position);
        Destroy(gameObject);
    }
}
