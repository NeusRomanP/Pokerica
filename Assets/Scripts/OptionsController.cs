using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{

    public ToggleGroup toggleGroup;
    public Toggle restartToggle;

    public static string difficultyLevel;
    public static bool restartOnFail;
    
    // Start is called before the first frame update
    void Start()
    {
        if(difficultyLevel != null){
            Toggle toggleSelected = GameObject.FindGameObjectWithTag(difficultyLevel).GetComponent<Toggle>();
            toggleSelected.isOn = true;
        }

        restartToggle.isOn = restartOnFail;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeDifficultyLevel(){
        Toggle toggle = toggleGroup.ActiveToggles().FirstOrDefault();
        difficultyLevel = toggle.tag;
    }

    public void ToggleRestartOnFail(){
        restartOnFail = restartToggle.isOn;
    }
}
