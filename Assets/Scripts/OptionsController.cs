using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{

    public ToggleGroup toggleGroup;

    public static string difficultyLevel = "hard";
    public static bool restartOnFail = true;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeDifficultyLevel(){
        Toggle toggle = toggleGroup.ActiveToggles().FirstOrDefault();
        difficultyLevel = toggle.tag;
        //Debug.Log(difficultyLevel);
    }

    public void ToggleRestartOnFail(){
        restartOnFail = !restartOnFail;
        Debug.Log(restartOnFail);
    }
}
