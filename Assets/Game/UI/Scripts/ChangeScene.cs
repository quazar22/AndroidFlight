using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public GameObject Networking;

	//not used anymore
    public void LoadScene(string scene)
    {
        DontDestroyOnLoad(Networking);
        SceneManager.LoadScene(scene);
    }
	
}
