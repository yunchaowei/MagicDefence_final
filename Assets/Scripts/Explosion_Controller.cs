using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion_Controller : MonoBehaviour
{
    private HashSet<GameObject> damagedEnemies = new HashSet<GameObject>();

    public int damage = 50;
    public AudioSource audioSource;
    public AudioClip soundClip;
    // Start is called before the first frame update
    void Start()
    {
        audioSource.clip = soundClip;
        audioSource.Play();
        Destroy(this.gameObject, 2f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy" && !damagedEnemies.Contains(other.gameObject))
        {
            GameObject obj = other.gameObject;
            obj.GetComponent<Enemy>().Get_Damage(damage);

            damagedEnemies.Add(other.gameObject);
        }
    }

    public void Buffed()
    {
        Vector3 scale_act = this.transform.localScale;
        this.transform.localScale = new Vector3(scale_act.x * 2, scale_act.y * 2, scale_act.z * 2);
        damage = 75;
    }
}
