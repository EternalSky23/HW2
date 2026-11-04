using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject[] pages;
    private int index = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToNextPage()
    {
        pages[index].SetActive(false);
        index++;
        pages[index].SetActive(true);
    }

    public void GameStart()
    {
        AdvancedGame.Instance.NewGame();
    }
}
