using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine.Networking;
using TMPro;
using TwitchChat;
using UnityEngine;
using UnityEngine.UI;

public class PokemonController : MonoBehaviour
{

    public TextMeshProUGUI lastPokemonNameTMP;
    public TextMeshProUGUI highScoreTMP;
    public TextMeshProUGUI shameOnTMP;
    public TextMeshProUGUI highScoreByTMP;

    public Sprite empty;

    private HttpClient client = new HttpClient();
    private string apiPath = "https://pokeapi.co/api/v2/pokemon/";
    int pokemonNumber = 1;
    public Image img;

    private int highScore = 0;
    private string highScoreBy = "";
    private string shameOn = "";

    Texture2D downloadedTexture = null;

    [SerializeField]
    Pokemon currentPokemon;
    string lastPokemonName = "";

    // Start is called before the first frame update
    void Start()
    {
        TwitchController.onTwitchMessageReceived += OnTwitchMessageReceived;

        //ParseUserInput("PKMN: Bulbasaur");
    }

    // Update is called once per frame
    void Update()
    {
        

        ParseUserInput("!pkmn:bulbasaur");
        bool isCorrectInput = CompareInputWithCurrentPokemon("!pkmn:bulbasaur");

        
        if(isCorrectInput){
            Debug.Log(pokemonNumber);
            ShowLastUsername("neusr");
            ShowLastPokemonName();
            StartCoroutine(FetchImageFromURL(currentPokemon.sprites.front_default));
            if(pokemonNumber > highScore){
                highScore = pokemonNumber -1;
                ShowHighScore();
                ShowHigScoreBy("neusr");
            }
        }else{
            pokemonNumber = 1;
            RestartOnFail();
            ShowShameOn("neusr");
        }

        
    }

    void OnDestroy()
    {
        Debug.Log("Destroy");
        TwitchController.onTwitchMessageReceived -= OnTwitchMessageReceived;
    }

    private void OnTwitchMessageReceived(Chatter chatter)
    {
        Debug.Log("Entra");
        Debug.Log(chatter.message);
    }

    public void PrintPokemon(){
        Debug.Log(apiPath+pokemonNumber);
        StartCoroutine(FetchPokemonFromApi(pokemonNumber));
    }

    IEnumerator FetchPokemonFromApi(int number)
    {

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPath+number);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }

        currentPokemon = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        Debug.Log(currentPokemon.name);
        Debug.Log(currentPokemon.sprites.front_default);

        pokemonNumber++;

        yield return new WaitForSeconds(0);

    }

    IEnumerator FetchImageFromURL(string url)
    {

        //TODO: Dispose of this request
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(request.error);
            yield break;
        }

        if(downloadedTexture != null){
            Destroy(downloadedTexture);
        }

        downloadedTexture = DownloadHandlerTexture.GetContent(request) as Texture2D;

        request.Dispose();

        GetComponent<Image>().sprite = Sprite.Create(downloadedTexture, new Rect(0, 0, downloadedTexture.width, downloadedTexture.height), new Vector2(0, 0));


        yield return null;
    }

    public void RestartOnFail(){
        GetComponent<Image>().sprite = empty;
        lastPokemonNameTMP.text = "";
    }

    public bool CompareInputWithCurrentPokemon(string input)
    {
        if(ParseUserInput(input) == ParseCurrentPokemonName()){
            return true;
        }
        

        return false;
    }

    public string ParseUserInput(string input)
    {
        input = input.ToLower();

        if(input.StartsWith("!pkmn:"))
        {
            string name = input.Split("!pkmn:")[1];

            string parsedInput = name.Replace("-", "").Replace(" ", "").Replace(".", "").Replace("'", "");

            if((currentPokemon.name.EndsWith("-f") || currentPokemon.name.EndsWith("-m")) && !parsedInput.EndsWith("f") && !parsedInput.EndsWith("m")){
                string sufix = currentPokemon.name[(currentPokemon.name.LastIndexOf("-")+1)..currentPokemon.name.Length];
                if(!parsedInput.EndsWith(sufix)){
                    parsedInput += sufix;
                    Debug.Log("Parsed");
                    Debug.Log(parsedInput);
                }
            }

            lastPokemonName = currentPokemon.name;

            return parsedInput;
        }

        return null;
    }

    public string ParseCurrentPokemonName()
    {
        string parsedCurrentPokemonName = currentPokemon.name.Replace("-", "").Replace(" ", "").Replace(".", "");

        return parsedCurrentPokemonName.ToLower();
    }

    public void ShowLastPokemonName(){
        string nameToDisplay = lastPokemonName.Replace("-", " ").ToUpper();

        lastPokemonNameTMP.text = nameToDisplay;
    }

    public void ShowHighScore(){
        highScoreTMP.text = "High score: " + highScore;
    }

    public void ShowShameOn(string username){
        shameOnTMP.text = "Shame on "+ username;
    }

    public void ShowLastUsername(string username){
        shameOnTMP.text = username;
    }

    public void ShowHigScoreBy(string username){
        highScoreByTMP.text = "by " + username;
    }
}

[System.Serializable]
class Pokemon{
    public string name;
    public Sprites sprites;

    public Pokemon(string name, Sprites sprites){
        this.name = name;
        this.sprites = sprites;
    }
}

[System.Serializable]
class Sprites{
    public string front_default;

    public Sprites(string front_default){
        this.front_default = front_default;
    }
}
