using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI 연결")]
    public GameObject gameOverPanel;

    [Header("설정")]
    public Transform[] spawnPoints;
    public GameObject[] enemyPrefabs;
    public int totalRounds = 6;
    public int spawnAmount = 30;
    
    [Header("자원")] 
    public int money = 0;

    [Header("UI 연결")]
    public Text roundText;      
    public Text enemyCountText;
    public Text moneyText;

    [Header("상태")]
    public int currentRound = 0;
    public int remainingEnemies = 0; 
    public int currentWaveTotal = 0; 
    public bool isWaveInProgress = false;

    void Awake()
    {
        instance = this;
    }

    public void OnPlayerDead()
    {
        Debug.Log("게임 오버! 메뉴 띄움");

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void GoToTitle()
    {
        // Time.timeScale = 1;
        SceneManager.LoadScene("TitleScene"); 
    }

    void Start()
    {
        UpdateMoneyUI();
        StartCoroutine(StartNextRound());
        Time.timeScale = 1f;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateMoneyUI();
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "$ " + money;
        }
    }

    void UpdateEnemyUI()
    {
        if (enemyCountText != null)
        {
            enemyCountText.text = remainingEnemies + " / " + currentWaveTotal;
        }
    }

    IEnumerator StartNextRound()
    {
        currentRound++;

        if (roundText != null) roundText.text = "ROUND " + currentRound;

        if (currentRound > totalRounds)
        {
            if (roundText != null) roundText.text = "CLEAR!";
            if (enemyCountText != null) enemyCountText.text = "";
            yield break;
        }

        isWaveInProgress = true;

        int countToSpawn = spawnAmount;

        remainingEnemies = countToSpawn;
        currentWaveTotal = countToSpawn;

        UpdateEnemyUI(); 

        for (int i = 0; i < countToSpawn; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f)); 
        }
    }

    void SpawnEnemy()
    {
        int ranPoint = Random.Range(0, spawnPoints.Length);
        int enemyIndex = (currentRound - 1) % enemyPrefabs.Length;

        Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0, 0);
        Vector3 spawnPos = spawnPoints[ranPoint].position + randomOffset;

        Instantiate(enemyPrefabs[enemyIndex], spawnPos, Quaternion.identity);
    }

    public void OnEnemyDead()
    {
        remainingEnemies--;

        UpdateEnemyUI(); 

        if (remainingEnemies <= 0 && isWaveInProgress)
        {
            isWaveInProgress = false;
            Invoke("NextRoundDelay", 3f);
        }
    }

    void NextRoundDelay()
    {
        StartCoroutine(StartNextRound());
    }
}