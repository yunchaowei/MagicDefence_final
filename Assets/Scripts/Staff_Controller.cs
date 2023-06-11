using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Staff_Controller : MonoBehaviour
{
    public GameObject Player;
    public GameObject Other_Player;
    public GameObject Fireballprefab;
    public GameObject Turretprefab;
    public Transform Spawn_Point;
    public Slider Mana_Bar;
    private Vector3 previousPosition;
    private float previousShootTime;
    private float previousManaRecoverTime;
    public float detect_Dis_first = 0.5f;
    public float detect_Dis_second = 1.0f;
    public int mana_Act = 0;
    public int Mana = 100;
    private int fire_ball_mana = 15;
    private int Turret_mana = 85;
    public int mana_Recovery_Amount = 1;
    private bool can_Shoot=true;
    // Start is called before the first frame update
    void Start()
    {
        //mana_Act = Mana;
        Mana_Bar.maxValue = Mana;
        Mana_Bar.minValue = 0;
        Mana_Bar.value = mana_Act;
        can_Shoot = true;
    }

    // Update is called once per frame
    void Update()
    {
        bool buffed = false;

        Vector2 pos_1 = new Vector2(this.transform.position.x, this.transform.position.z);
        Vector2 pos_2 = new Vector2(Other_Player.transform.position.x, Other_Player.transform.position.z);
        if (Vector2.Distance(pos_1, pos_2) < 5f/* && Other_Player.GetComponent<Staff_Controller>().can_Shoot*/)
            buffed = true;

        float moveDistanceY = Mathf.Abs(transform.position.y - previousPosition.y);

        //2nd spell
        if (moveDistanceY > detect_Dis_second && Time.time - previousShootTime > 1.0f)
        {
            if (mana_Act >= Turret_mana)
            {
                if (can_Shoot)
                {
                    mana_Act -= Turret_mana;
                    Mana_Bar.value = mana_Act;
                    Debug.Log(buffed);
                    launch_Turret(buffed);
                }
            }
        }
        else
        {
            //1st spell
            if (moveDistanceY > detect_Dis_first && moveDistanceY < detect_Dis_second && Time.time - previousShootTime > 1.0f)
            {
                float moveDistanceX = Mathf.Abs(transform.position.x - previousPosition.x);
                float moveDistanceZ = Mathf.Abs(transform.position.z - previousPosition.z);

                if (moveDistanceX > detect_Dis_first || moveDistanceZ > detect_Dis_first)
                {
                    if (mana_Act >= fire_ball_mana)
                    {
                        if (can_Shoot)
                        {
                            mana_Act -= fire_ball_mana;
                            Mana_Bar.value = mana_Act;
                            launch_FireBall(buffed);
                        }
                    }
                }
            }
        }

        previousPosition = transform.position; // last position
    }

    private void launch_Turret(bool buffed = false)
    {
        Vector3 pos = new Vector3(this.transform.position.x, -5, this.transform.position.z);
        GameObject Turret = Instantiate(Turretprefab, pos,
       Turretprefab.transform.rotation);
        if(buffed)
            Turret.GetComponent<Tower>().Buffed();
    }

    private void launch_FireBall(bool buffed = false)
    {
        Vector3 dir_act = new Vector3(transform.position.x - previousPosition.x, 0, transform.position.z - previousPosition.z);
        dir_act.Normalize();

        GameObject Fireball = Instantiate(Fireballprefab, Spawn_Point.position, Fireballprefab.transform.rotation);
        Fireball.GetComponent<FireBall_Controller>().dir = dir_act;
        if (buffed)
            Fireball.GetComponent<FireBall_Controller>().Buffed();

        previousShootTime = Time.time; // last launch time
        transform.Rotate(dir_act);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Recovery_Mana_Area")
        {
            can_Shoot = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Recovery_Mana_Area")
        {
            can_Shoot = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Recovery_Mana_Area")
        {
            if(Time.time - previousManaRecoverTime > 0.1f)
            {
                recover_Mana();
            }
        }
    }

    private void recover_Mana()
    {
        mana_Act += mana_Recovery_Amount;
        if (mana_Act > Mana)
            mana_Act = Mana;
        Mana_Bar.value = mana_Act;
        previousManaRecoverTime = Time.time;
    }
}
