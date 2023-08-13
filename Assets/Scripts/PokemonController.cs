using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine.Networking;
using TwitchChat;
using UnityEngine;
using UnityEngine.UI;

public class PokemonController : MonoBehaviour
{

    private HttpClient client = new HttpClient();
    private string apiPath = "https://pokeapi.co/api/v2/pokemon/";
    int pokemonNumber = 1;
    public Image img;

    [SerializeField]
    Pokemon currentPokemon;

    // Start is called before the first frame update
    void Start()
    {
        TwitchController.onTwitchMessageReceived += OnTwitchMessageReceived;
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
    }

    public void PrintPokemon(){
        Debug.Log(apiPath+pokemonNumber);
        StartCoroutine(FetchPokemonFromApi(pokemonNumber));
    }

    IEnumerator FetchPokemonFromApi(int number)
    {

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPath+number);
        pokemonNumber++;

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }
        //yield return new WaitForSeconds(1);

        Debug.Log(pokemonInfo.downloadHandler.text);

        currentPokemon = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        //yield return new WaitForSeconds(1);

        Debug.Log(currentPokemon.name);
        Debug.Log(currentPokemon.sprites.front_default);

        StartCoroutine(FetchImageFromURL(currentPokemon.sprites.front_default));

        yield return new WaitForSeconds(0);

    }

    IEnumerator FetchImageFromURL(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(request.error);
            yield break;
        }

        Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request) as Texture2D;

        GetComponent<Image>().sprite = Sprite.Create(downloadedTexture, new Rect(0, 0, downloadedTexture.width, downloadedTexture.height), new Vector2(25, -25));


        yield return null;
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
