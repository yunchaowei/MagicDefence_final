using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Hit : MonoBehaviour
{
    public GameObject EnemyTarget;
    public int Creature_Damage = 10;
    private int disappearance_time = 3;
    private WaitForSeconds countdownInterval = new WaitForSeconds(1f);
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(StartCountdown());
    }
    private IEnumerator StartCountdown()
    {
        while (disappearance_time > 0)
        {
            yield return countdownInterval;
            disappearance_time--;
        }

        Destroy(gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Castle")
        {
            EnemyTarget.GetComponent<CastleController>().Get_Damage(Creature_Damage);
            Destroy(gameObject);
        }
    }
}
