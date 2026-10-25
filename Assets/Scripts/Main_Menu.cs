using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Main_Menu : MonoBehaviour
{
    public GameObject TransitionSwitch; // Reference to the main menu UI
    // Start is called before the first frame update
    public void Play()
    {
        //TransitionSwitch.SetActive(true); // Activate the transition switch
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);

    }

    // Update is called once per frame
    public void Quit()
    {
        Application.Quit();
        
    }
}
