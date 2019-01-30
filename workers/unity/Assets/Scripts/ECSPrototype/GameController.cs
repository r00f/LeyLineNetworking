using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace LeyLineHybridECS
{
    public class GameController : MonoBehaviour
    {
        public void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void SetGameState(int state)
        {
            GameStateSystem.State stateToSet = (GameStateSystem.State)state;
            GameStateSystem.CurrentState = stateToSet;
        }

        public void SwapActivePlayer()
        {
            if(GameStateSystem.activePlayer == 0)
            {
                GameStateSystem.activePlayer = 1;
            }
            else
            {
                GameStateSystem.activePlayer = 0;
            }
            Debug.Log(GameStateSystem.activePlayer);
        }

    }

}

