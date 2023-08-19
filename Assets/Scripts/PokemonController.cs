using System.Collections;
using UnityEngine.Networking;
using TMPro;
using TwitchChat;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    private int minPokemon = 1;
    private int highScore = 0;
    private string highScoreBy = "";
    private string shameOn = "";

    private bool inputIsNextPokemon;
    private bool isCorrectInput;

    Texture2D downloadedTexture = null;

    [SerializeField]
    Pokemon currentPokemon;
    [SerializeField]
    Pokemon nextPokemon;
    [SerializeField]
    Specie currentSpecie;
    [SerializeField]
    Specie nextSpecie;

    [SerializeField]
    Pokemon extraPokemon1;
    [SerializeField]
    Pokemon extraPokemon2;

    string lastPokemonName = "";

    // Start is called before the first frame update
    void Start()
    {
        TwitchController.onTwitchMessageReceived += OnTwitchMessageReceived;

        if(pokemonNumber == minPokemon){
            lastPokemonNameTMP.text = "";
            nextPokemonTMP.text = "";
            shameOnTMP.text = shameOn;
            highScoreTMP.text = "High score: "+ highScore;
            highScoreByTMP.text = highScoreBy;

            if(OptionsController.difficultyLevel != null && (OptionsController.difficultyLevel.Equals("easy") || OptionsController.difficultyLevel.Equals("mid"))){
                //StartCoroutine(FetchPokemonFromApi(minPokemon, "", ""));
                StartCoroutine(FetchNextPokemon(minPokemon -1));
            }
        }

        //ParseUserInput("PKMN: Bulbasaur");
    }

    // Update is called once per frame
    void Update()
    {
        

        
    }

    void OnDestroy()
    {
        TwitchController.onTwitchMessageReceived -= OnTwitchMessageReceived;
    }

    private void OnTwitchMessageReceived(Chatter chatter)
    {
        if(pokemonNumber == maxPokemon){
            SceneManager.LoadScene(3);
        }else{
            PrintPokemon(chatter.message, chatter.tags.displayName);
        }
        
    }

    public void PrintPokemon(string message, string user)
    {
        StartCoroutine(FetchPokemonFromApi(pokemonNumber, message, user));

        string level = OptionsController.difficultyLevel;

        level ??= "hard";

        if(level.Equals("easy") || level.Equals("mid")){
            inputIsNextPokemon = CompareInputWithNextPokemon(message);
            if(OptionsController.restartOnFail && !inputIsNextPokemon){
                StartCoroutine(FetchNextPokemon(minPokemon - 1));
            }else if(!OptionsController.restartOnFail && !inputIsNextPokemon){
                //StartCoroutine(FetchNextPokemon(pokemonNumber));
            }else{
                StartCoroutine(FetchNextPokemon(pokemonNumber));
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

        currentSpecie = JsonUtility.FromJson<Specie>(specieInfo.downloadHandler.text);

        string currentSpecieName = currentSpecie.name;

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPathPokemon+number);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }

        currentPokemon = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        string input = ParseUserInput(message);

        isCorrectInput = CompareInputWithCurrentPokemon(message);

        if(isCorrectInput){
            ShowLastUsername(user);
            ShowLastPokemonName();
            StartCoroutine(FetchImageFromURL(currentPokemon.sprites.front_default));
            if(pokemonNumber > highScore){
                highScore = pokemonNumber;
                ShowHighScore();
                ShowHighScoreBy(user);
            }
        }else if(input != null){
            
            if(OptionsController.restartOnFail){
                pokemonNumber = minPokemon;
                RestartOnFail();
                ShowShameOn(user);
            }
        }

        if(isCorrectInput){
            pokemonNumber++;
        }

        yield return null;

    }

    IEnumerator FetchNextPokemon(int number)
    {

        UnityWebRequest currentSpecieInfo = UnityWebRequest.Get(apiPathSpecies+number);

        yield return currentSpecieInfo.SendWebRequest();

        if(currentSpecieInfo.result == UnityWebRequest.Result.ConnectionError || currentSpecieInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(currentSpecieInfo.error);
            yield break;
        }

        currentSpecie = JsonUtility.FromJson<Specie>(currentSpecieInfo.downloadHandler.text);

        UnityWebRequest specieInfo = UnityWebRequest.Get(apiPathSpecies+(number+1));

        yield return specieInfo.SendWebRequest();

        if(specieInfo.result == UnityWebRequest.Result.ConnectionError || specieInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(specieInfo.error);
            yield break;
        }

        nextSpecie = JsonUtility.FromJson<Specie>(specieInfo.downloadHandler.text);
        
        string nextSpecieName = nextSpecie.name;

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPathPokemon+number);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }

        nextPokemon = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        //yield return nextPokemon;

        if(OptionsController.difficultyLevel.Equals("mid")){
            StartCoroutine(FetchTwoExtraOptions(nextSpecie.name, number));
        }else{
            ShowNextPokemon(nextSpecie.name);
        }

        

        yield return null;

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

    IEnumerator FetchTwoExtraOptions(string correct, int number)
    {
        string correctOption = correct;

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

        string specieName2 = specie2.name;

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPathPokemon+option1);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }

        extraPokemon1 = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        UnityWebRequest pokemonInfo2 = UnityWebRequest.Get(apiPathPokemon+option2);

        yield return pokemonInfo2.SendWebRequest();

        if(pokemonInfo2.result == UnityWebRequest.Result.ConnectionError || pokemonInfo2.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo2.error);
            yield break;
        }

        extraPokemon2 = JsonUtility.FromJson<Pokemon>(pokemonInfo2.downloadHandler.text);

        string [] options = {correctOption, specieName1, specieName2};

        options = RandomizeArray(options);

        yield return options;

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

            if(OptionsController.difficultyLevel != null && !OptionsController.difficultyLevel.Equals("easy") && !OptionsController.difficultyLevel.Equals("mid")){
                if((currentPokemon.name.EndsWith("-f") || currentPokemon.name.EndsWith("-m")) && !parsedInput.EndsWith("f") && !parsedInput.EndsWith("m")){
                    string sufix = currentPokemon.name[(currentPokemon.name.LastIndexOf("-")+1)..currentPokemon.name.Length];
                    if(!parsedInput.EndsWith(sufix)){
                        parsedInput += sufix;
                    }
                }
            }else{
                if((nextPokemon.name.EndsWith("-f") || nextPokemon.name.EndsWith("-m")) && !parsedInput.EndsWith("f") && !parsedInput.EndsWith("m")){
                    string sufix = nextPokemon.name[(nextPokemon.name.LastIndexOf("-")+1)..nextPokemon.name.Length];
                    if(!parsedInput.EndsWith(sufix)){
                        parsedInput += sufix;
                    }
                }
            }

            lastPokemonName = currentSpecie.name;

            return parsedInput;
        }

        return null;
    }

    public string ParseCurrentPokemonName()
    {
        string parsedCurrentPokemonName = currentSpecie.name.Replace("-", "").Replace(" ", "").Replace(".", "");

        return parsedCurrentPokemonName.ToLower();
    }

    public string ParseNextPokemonName()
    {
        string parsedNextPokemonName = nextSpecie.name.Replace("-", "").Replace(" ", "").Replace(".", "");

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

    public void ShowHighScoreBy(string username){
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
