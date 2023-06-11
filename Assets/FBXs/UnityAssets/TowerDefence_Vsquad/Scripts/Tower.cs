using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class Tower : MonoBehaviour {


    public bool Catcher = false;
	public Transform shootElement;
    public Transform LookAtObj;    
    public GameObject bullet;
    public GameObject DestroyParticle;
    public Vector3 impactNormal_2;
    public Transform target;
    public int dmg = 30;
    public float shootDelay;
    bool isShoot;
    public Animator anim_2;
    public TowerHP TowerHp;    
    private float homeY;
    public Slider Health_Bar;
    public float HP = 1000;
    private float HP_act = 0;
    private float previousReduceHPTime = 0.0f;
    private int recovery_Amount = 1;

    void Start()
    {
        anim_2 = GetComponent<Animator>();
        homeY = LookAtObj.transform.localRotation.eulerAngles.y;

        HP_act = HP;
        Health_Bar.maxValue = HP;
        Health_Bar.minValue = 0;
        Health_Bar.value = HP_act;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy")
        {
            if(target == null)
            {
                Enemy enemy = other.GetComponent<Enemy>();
                if (enemy.isDead)
                {
                    target = null;
                    return;
                }
                GameObject obj = other.gameObject;
                target = obj.transform;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Enemy")
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy.isDead)
            {
                target = null;
                return;
            }
            if (target == null)
            {
                
                GameObject obj = other.gameObject;
                target = obj.transform;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Enemy")
        {
            GameObject obj = other.gameObject;
            if (target == obj)
                target = null;
        }
    }

    public void Buffed()
    {
        HP *= 1.5f;
        HP_act *= HP;
        Health_Bar.maxValue = HP;
        Health_Bar.value = HP_act;
        Vector3 scale_act = this.transform.localScale;
        this.transform.localScale = new Vector3(scale_act.x * 1.2f, scale_act.y * 1.2f, scale_act.z * 1.2f);
    }

    void Update () {

        if (target)
        {  
            Vector3 dir = target.transform.position - LookAtObj.transform.position;
                dir.y = 0; 
                Quaternion rot = Quaternion.LookRotation(dir);                
                LookAtObj.transform.rotation = Quaternion.Slerp( LookAtObj.transform.rotation, rot, 5 * Time.deltaTime);

        }
        else
        {
            Quaternion home = new Quaternion (0, homeY, 0, 1);
            
            LookAtObj.transform.rotation = Quaternion.Slerp(LookAtObj.transform.rotation, home, Time.deltaTime);                        
        }


        // Shooting
        if (!isShoot)
        {
            StartCoroutine(shoot());
        }

        if (Catcher == true)
        {
            if (!target || target.CompareTag("Dead"))
            {
                StopCatcherAttack();
            }

        }

        // Destroy
        if (Time.time - previousReduceHPTime > 0.1f)
        {
            reduce_HP();
        }
    }

    private void reduce_HP()
    {
        HP_act -= recovery_Amount;
        if (HP_act < 0)
            HP_act = 0;
        Health_Bar.value = HP_act;

        if (HP_act == 0)
            Destroy(this.gameObject, 0.1f);

        previousReduceHPTime = Time.time;
    }

    IEnumerator shoot()
	{
		isShoot = true;
		yield return new WaitForSeconds(shootDelay);

        if (target && Catcher == false)
        {
            GameObject b = GameObject.Instantiate(bullet, shootElement.position, Quaternion.identity) as GameObject;
            b.GetComponent<TowerBullet>().target = target;
            b.GetComponent<TowerBullet>().twr = this;
          
        }

        if (target && Catcher == true)
        {
            anim_2.SetBool("Attack", true);
            anim_2.SetBool("T_pose", false);
        }


        isShoot = false;
	}

    void StopCatcherAttack()
    {                
        target = null;
        anim_2.SetBool("Attack", false);
        anim_2.SetBool("T_pose", true);        
    } 
          

}



