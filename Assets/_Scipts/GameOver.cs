using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using TMPro;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [SerializeField]
    private GameObject gameOverCanvas;
    private int score = 0;
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private TextMeshProUGUI leaderBoardScoreText;
    [SerializeField]
    private TextMeshProUGUI leaderBoardNameText;
    [SerializeField]
    private TextMeshProUGUI scoreText;

    private string leaderBoardID = "14484";
    private int leaderBoardCount = 10;
    public void GameStop(int score)
    {
        gameOverCanvas.SetActive(true);
        this.score = score;
        scoreText.text = score.ToString();
        //SubmitScore();
        GetLeaderBoard();
    }
    public void SubmitScore()
    {
        StartCoroutine(SubmitScoretoLeaderboard());
    }  
    private IEnumerator SubmitScoretoLeaderboard()
    {
        bool? nameSet = null;
        LootLockerSDKManager.SetPlayerName(inputField.text, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Succesfully set player name");
                nameSet = true; 
            }
            else
            {
                Debug.Log("Unable to set player name");
                nameSet = false;
            }
        });
        yield return new WaitUntil(()=> nameSet.HasValue);
        
        bool? scoreSubmitted = null;
        LootLockerSDKManager.SubmitScore("", score, leaderBoardID, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Succesfully submitted score");
                scoreSubmitted = true;

            }
            else
            {
                Debug.Log("Unable to submit score");
                scoreSubmitted = false;
            }
        });
        yield return new WaitUntil(() => scoreSubmitted.HasValue);
        if(!scoreSubmitted.Value) { yield break; }
        GetLeaderBoard();
    }
    private void GetLeaderBoard()
    {
        LootLockerSDKManager.GetScoreList(leaderBoardID, leaderBoardCount, (response) =>
        {
            if(response.success)
            {
                Debug.Log("Succesfully fetched scores from leaderboard");
                string leaderBoardName = "";
                string leaderBoardScore = "";
                LootLockerLeaderboardMember[] members = response.items;
                for(int i=0; i<members.Length; i++)
                {
                    if (members[i].player == null) continue;

                    if (members[i].player.name != "")
                    {
                        leaderBoardName += members[i].player.name + '\n';
                        leaderBoardScore += members[i].score + "" + '\n';

                    }
                    else
                    {
                        leaderBoardName += members[i].player.id + '\n';
                        leaderBoardScore += members[i].score + ""+ '\n';
                    }
                    
                }
                leaderBoardNameText.SetText(leaderBoardName);
                leaderBoardScoreText.SetText(leaderBoardScore); 
            }
            else
            {
                Debug.Log("Failed to fetch scores from leaderboard");
            }
        });
    }
    public void AddXP(int score)
    {

    }
    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); 
    }
}
