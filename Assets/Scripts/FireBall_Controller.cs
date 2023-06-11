using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBall_Controller : MonoBehaviour
{
    private bool hasHitEnemy = false;
    public float speed = 1.0f; 
    private Transform objectTransform;
    public Vector3 dir;
    public GameObject Explosion;
    private bool buffed = false;
    public AudioSource audioSource;
    public AudioClip soundClip;
    void Start()
    {
        buffed = false;
        hasHitEnemy = false;
        objectTransform = GetComponent<Transform>();
        audioSource.clip = soundClip;
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        objectTransform.Translate(dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitEnemy) return;
        if (!hasHitEnemy)
        {
            if(other.tag == "Enemy" || other.tag == "Castle")
            {

                if(other.tag == "Enemy")
                {
                    Enemy enemy = other.GetComponent<Enemy>();
                    if (enemy.isDead)
                        return;
                }

                hasHitEnemy = true;
                GameObject explosion = GameObject.Instantiate(Explosion, this.transform.position, Quaternion.identity) as GameObject;
                if (buffed)
                    explosion.GetComponent<Explosion_Controller>().Buffed();
                Destroy(gameObject);
            }
        }
    }

    public void Buffed()
    {
        buffed = true;
        Vector3 scale_act = this.transform.localScale;
        this.transform.localScale = new Vector3(scale_act.x * 2, scale_act.y * 2, scale_act.z * 2);
    }
}
