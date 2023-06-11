using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CastleController : MonoBehaviour
{
    public int HP = 3000;
    private int HP_act = 0;
    public Slider Health_Bar; 
    private GameManager gameManager;
    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.SetActive(true);
        HP_act = HP;
        Health_Bar.maxValue = HP;
        Health_Bar.minValue = 0;
        Health_Bar.value = HP_act;
        gameManager = FindObjectOfType<GameManager>();
    }

    public void Get_Damage(int damage)
    {
        HP_act -= damage;
        if(HP_act < 0)
        {
            HP_act = 0;
        }

        Health_Bar.value = HP_act;

        if (HP_act == 0)
            GameOver();
    }

    private void GameOver()
    {
        gameManager.GameOver();
    }
}
