using System.Collections;
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
    public TextMeshProUGUI nextPokemonTMP;

    public Sprite empty;

    private string apiPathSpecies = "https://pokeapi.co/api/v2/pokemon-species/";
    private string apiPathPokemon = "https://pokeapi.co/api/v2/pokemon/";
    int pokemonNumber = 1;
    public Image img;
    
    private int maxPokemon = 1010;

    private int highScore = 0;
    private string highScoreBy = "";
    private string shameOn = "";

    Texture2D downloadedTexture = null;

    [SerializeField]
    Pokemon currentPokemon;
    [SerializeField]
    Pokemon nextPokemon;

    [SerializeField]
    Pokemon extraPokemon1;
    [SerializeField]
    Pokemon extraPokemon2;

    string lastPokemonName = "";

    // Start is called before the first frame update
    void Start()
    {
        TwitchController.onTwitchMessageReceived += OnTwitchMessageReceived;

        Debug.Log("Enter start");
        nextPokemonTMP.text = "";
        shameOnTMP.text = shameOn;
        highScoreTMP.text = "High score: "+ highScore;
        highScoreByTMP.text = highScoreBy;

        if(OptionsController.difficultyLevel != null && (OptionsController.difficultyLevel.Equals("easy") || OptionsController.difficultyLevel.Equals("mid"))){
            StartCoroutine(FetchNextPokemon(1));
        }

        //ParseUserInput("PKMN: Bulbasaur");
    }

    // Update is called once per frame
    void Update()
    {
        

        
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

        PrintPokemon(chatter.message, chatter.tags.displayName);
    }

    public void PrintPokemon(string message, string user){
        //Debug.Log(apiPathSpecies+pokemonNumber);

        StartCoroutine(FetchPokemonFromApi(pokemonNumber, message, user));

        
        Debug.Log("Level");
        Debug.Log(OptionsController.difficultyLevel);
        string level = OptionsController.difficultyLevel;

        level ??= "hard";

        if(level.Equals("easy") || level.Equals("mid")){
            bool inputIsNextPokemon = CompareInputWithNextPokemon(message);
            if(OptionsController.restartOnFail && !inputIsNextPokemon){
                Debug.Log("1");
                StartCoroutine(FetchNextPokemon(1));
            }else if(!OptionsController.restartOnFail && !inputIsNextPokemon){
                Debug.Log(currentPokemon.name);
                Debug.Log("2");
                //StartCoroutine(FetchNextPokemon(pokemonNumber));
            }else{
                Debug.Log("3");
                StartCoroutine(FetchNextPokemon(pokemonNumber + 1));
            }
        }
    }

    IEnumerator FetchPokemonFromApi(int number, string message, string user)
    {

        UnityWebRequest specieInfo = UnityWebRequest.Get(apiPathSpecies+number);

        yield return specieInfo.SendWebRequest();

        if(specieInfo.result == UnityWebRequest.Result.ConnectionError || specieInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(specieInfo.error);
            yield break;
        }

        Specie currentSpecie = JsonUtility.FromJson<Specie>(specieInfo.downloadHandler.text);

        string currentSpecieName = currentSpecie.name;

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPathPokemon+currentSpecieName);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }

        currentPokemon = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        Debug.Log(currentPokemon.name);

        string input = ParseUserInput(message);

        bool isCorrectInput = CompareInputWithCurrentPokemon(message);

        if(isCorrectInput){
            ShowLastUsername(user);
            ShowLastPokemonName();
            StartCoroutine(FetchImageFromURL(currentPokemon.sprites.front_default));
            if(pokemonNumber > highScore){
                highScore = pokemonNumber;
                ShowHighScore();
                ShowHigScoreBy(user);
            }
        }else if(input != null){
            
            if(OptionsController.restartOnFail){
                pokemonNumber = 1;
                RestartOnFail();
                ShowShameOn(user);
            }
            
        }

        if(isCorrectInput){
            pokemonNumber++;
        }

        yield return new WaitForSeconds(0);

    }

    IEnumerator FetchNextPokemon(int number)
    {

        UnityWebRequest specieInfo = UnityWebRequest.Get(apiPathSpecies+number);

        yield return specieInfo.SendWebRequest();

        if(specieInfo.result == UnityWebRequest.Result.ConnectionError || specieInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(specieInfo.error);
            yield break;
        }

        Specie currentSpecie = JsonUtility.FromJson<Specie>(specieInfo.downloadHandler.text);

        string currentSpecieName = currentSpecie.name;

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPathPokemon+currentSpecieName);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }

        nextPokemon = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        if(OptionsController.difficultyLevel.Equals("mid")){
            StartCoroutine(FetchTwoExtraOptions(nextPokemon.name, number));
        }else{
            ShowNextPokemon(nextPokemon.name);
        }

        

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

        img.GetComponent<Image>().sprite = Sprite.Create(downloadedTexture, new Rect(0, 0, downloadedTexture.width, downloadedTexture.height), new Vector2(0, 0));


        yield return null;
    }

    IEnumerator FetchTwoExtraOptions(string correctOption, int number)
    {
        
        int[] numbers = {number};
        int option1 = GetRandomDifferentThan(numbers);
        int[] numbers2 = {number, option1};
        int option2 = GetRandomDifferentThan(numbers2);

        UnityWebRequest specieInfo1 = UnityWebRequest.Get(apiPathSpecies+option1);

        yield return specieInfo1.SendWebRequest();

        if(specieInfo1.result == UnityWebRequest.Result.ConnectionError || specieInfo1.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(specieInfo1.error);
            yield break;
        }

        Specie specie1 = JsonUtility.FromJson<Specie>(specieInfo1.downloadHandler.text);

        string specieName1 = specie1.name;

        UnityWebRequest specieInfo2 = UnityWebRequest.Get(apiPathSpecies+option2);

        yield return specieInfo2.SendWebRequest();

        if(specieInfo2.result == UnityWebRequest.Result.ConnectionError || specieInfo2.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(specieInfo2.error);
            yield break;
        }

        Specie specie2 = JsonUtility.FromJson<Specie>(specieInfo2.downloadHandler.text);

        string speciaName2 = specie2.name;

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPathPokemon+specieName1);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }

        extraPokemon1 = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        UnityWebRequest pokemonInfo2 = UnityWebRequest.Get(apiPathPokemon+speciaName2);

        yield return pokemonInfo2.SendWebRequest();

        if(pokemonInfo2.result == UnityWebRequest.Result.ConnectionError || pokemonInfo2.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo2.error);
            yield break;
        }

        extraPokemon2 = JsonUtility.FromJson<Pokemon>(pokemonInfo2.downloadHandler.text);

        string [] options = {correctOption, extraPokemon1.name, extraPokemon2.name};

        options = RandomizeArray(options);

        ShowExtraOptions(options[0], options[1], options[2]);


        yield return null;
    }

    public int GetRandomDifferentThan(int[] numbers){

        int option = Random.Range(1, maxPokemon);
        for(int i = 0; i < numbers.Length; i++){
            option = option;
            while(option == numbers[i]){
                option = Random.Range(1, maxPokemon);
            }
        }
        
        return option;
    }

    public string[] RandomizeArray(string [] array){
        for (int i = 0; i < array.Length; i++ )
        {
            string tmp = array[i];
            int r = Random.Range(i, array.Length);
            array[i] = array[r];
            array[r] = tmp;
        }

        return array;
    }



    public void RestartOnFail(){
        img.GetComponent<Image>().sprite = empty;
        lastPokemonNameTMP.text = "";
    }

    public bool CompareInputWithCurrentPokemon(string input)
    {

        Debug.Log(ParseUserInput(input) + " - " + ParseCurrentPokemonName());
        if(ParseUserInput(input) == ParseCurrentPokemonName()){
            return true;
        }
        

        return false;
    }

    public bool CompareInputWithNextPokemon(string input)
    {
        if(ParseUserInput(input) == ParseNextPokemonName()){
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

    public string ParseNextPokemonName()
    {
        string parsedNextPokemonName = nextPokemon.name.Replace("-", "").Replace(" ", "").Replace(".", "");

        return parsedNextPokemonName.ToLower();
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

    public void ShowNextPokemon(string pokemon){
        nextPokemonTMP.text = pokemon.Replace("-", " ").ToUpper();
    }

    public void ShowExtraOptions(string option1, string option2, string option3){
        option1 = option1.Replace("-", " ").ToUpper();
        option2 = option2.Replace("-", " ").ToUpper();
        option3 = option3.Replace("-", " ").ToUpper();
        nextPokemonTMP.text = option1 + " - " + option2 + " - " + option3;
    }
}

[System.Serializable]
class Specie{
    public string name;

    public Specie(string name){
        this.name = name;
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
