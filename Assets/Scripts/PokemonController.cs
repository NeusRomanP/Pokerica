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

public class PokemonController : MonoBehaviour
{

    private HttpClient client = new HttpClient();
    private string apiPath = "https://pokeapi.co/api/v2/pokemon/";
    int pokemonNumber = 1;

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
        pokemonNumber++;
    }

    IEnumerator FetchPokemonFromApi(int number)
    {
        /*string prova = "";
        HttpResponseMessage response = await client.GetAsync(apiPath+number);

        if (response.IsSuccessStatusCode)
        {
            prova = await response.Content.ReadAsStringAsync();
        }

        return "Hola";*/

        UnityWebRequest pokemonInfo = UnityWebRequest.Get(apiPath+number);

        yield return pokemonInfo.SendWebRequest();

        if(pokemonInfo.result == UnityWebRequest.Result.ConnectionError || pokemonInfo.result == UnityWebRequest.Result.ProtocolError){
            Debug.LogError(pokemonInfo.error);
            yield break;
        }
        yield return new WaitForSeconds(1);

        Debug.Log(pokemonInfo.downloadHandler.text);

        //yield return pokemonInfo.downloadHandler.text;

        currentPokemon = JsonUtility.FromJson<Pokemon>(pokemonInfo.downloadHandler.text);

        yield return new WaitForSeconds(1);

        Debug.Log(currentPokemon.name);
        Debug.Log(currentPokemon.sprites.front_default);

        yield return new WaitForSeconds(0);

    }
}

[System.Serializable]
class Pokemon{
    public string name;
    public Sprite sprites;

    public Pokemon(string name, Sprite sprites){
        this.name = name;
        this.sprites = sprites;
    }
}

[System.Serializable]
class Sprite{
    public string front_default;

    public Sprite(string front_default){
        this.front_default = front_default;
    }
}
