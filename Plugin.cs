using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace DWNOFractalCode;

[BepInPlugin("SalemUnderwood.DWNO.FractalCode", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Digimon World Next Order.exe")]
public class Plugin : BasePlugin {

    private static float timer = 0f;
    private static float messageInterval = 10f;

    private static readonly HttpClient client = new HttpClient();
    private static string apiResponse;

    public override void Load(){
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        LoadApiDataAsync().Wait();
        
        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    private async Task LoadApiDataAsync(){
        try {
            // string apiUrl = "https://jsonplaceholder.typicode.com/posts?_limit=1";
            string apiUrl = "http://localhost:5001/api/v1/generate";
            var requestBody = new {
                prompt = "You are a BanchoLeomon from the franchise Digimon who just came to life. What do you say to your new tamer, Takuto?",
                max_length = 30,
                temperature = 0.7,
                top_p = 1.0,
                stop = new[] { "\n" } // You can add stop characters here, as needed
            };

            string jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode){
                string responseContent = await response.Content.ReadAsStringAsync();
                JsonDocument doc = JsonDocument.Parse(responseContent);
                string generatedText = doc.RootElement.GetProperty("results")[0].GetProperty("text").GetString();
                generatedText = CleanUpResponse(generatedText);
                apiResponse = generatedText;
            } else {
                Log.LogError($"Failed to get response from KoboldCPP: {response.StatusCode}");
            }
        }
        catch (Exception ex){
            Log.LogError($"Error loading API data: {ex.Message}");
        }
    }

    // Patch the MainGameManager Update method 
    [HarmonyPatch(typeof(MainGameManager), "Update")]
    [HarmonyPostfix] 
    public static void GameManager_Update_Postfix(MainGameManager __instance){
        PartnerCtrl _partnerCtrl1 = MainGameManager.GetPartnerCtrl(1);
        PartnerCtrl _partnerCtrl2 = MainGameManager.GetPartnerCtrl(0);

        // Make sure partners are active before sending messages
        if (_partnerCtrl1 && _partnerCtrl2){
            timer += Time.deltaTime; // timer to not overflow stack

            if (timer >= messageInterval){
                uFieldPanel.StartDigimonMessage((MainGameManager.UNITID)2, apiResponse, 5.0f);
                timer = 0f;
            }
        }
    }

    private string CleanUpResponse(string response){       
        response = response.TrimStart();
        response = System.Text.RegularExpressions.Regex.Replace(response, @"^\w+:", "").TrimStart(); // Remove any character name prefix (e.g., "BanchoLeomon: ")

        return response;
    }
}
