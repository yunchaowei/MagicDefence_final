using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI gameOverText;
    private float elapsedTime = 0f;
    private bool setText = false;
    // Start is called before the first frame update
    void Start()
    {
        gameOverText.gameObject.SetActive(false);
        elapsedTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        elapsedTime += Time.deltaTime;
        //Debug.Log($"Sec: {Get_Seconds()}");
    }

    public void GameOver()
    {
        if (!setText)
        {
            gameOverText.text += "Game Over" + Environment.NewLine + $"Your score:"+ Environment.NewLine + $"{Get_Mins()}:{Get_Seconds()}";
            setText = true;
        }
        gameOverText.gameObject.SetActive(true);

        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(5f);

        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
    }

    public float Get_Seconds()
    {
        return Mathf.Floor(elapsedTime % 60);
    }

    public float Get_Mins()
    {
        return Mathf.Floor(elapsedTime / 60);
    }
}
