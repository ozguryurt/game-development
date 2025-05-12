using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform playerSpawnPoint;
    public Transform botSpawnPoint;

    public HealthBar playerHealthBar;
    public HealthBar botHealthBar;

    public GameObject damageText;
    public GameObject wizardProjectile;
    public GameObject wizardStunObj;

    void Start()
    {
        // Seçilen karakteri al
        string selected = CharacterSelection.Instance.selectedCharacter;
        string[] allCharacters = { "Ninja_Player", "Wizard_Player", "Warrior_Player" };

        // Oyuncunun seçtiği karakteri sahneye yerleştir
        GameObject playerPrefab = Resources.Load<GameObject>("Characters/" + selected);
        GameObject player = Instantiate(playerPrefab, playerSpawnPoint.position, Quaternion.identity);
        player.AddComponent<PlayerController>();
        player.AddComponent<HealthManager>();

        // Bot karakterini rastgele seç (seçileni çıkar)
        string[] botCandidates = System.Array.FindAll(allCharacters, c => c != selected);
        string botCharacter = botCandidates[Random.Range(0, botCandidates.Length)];
        CharacterSelection.Instance.botCharacter = botCharacter;

        // Botu sahneye yerleştir
        GameObject botPrefab = Resources.Load<GameObject>("Characters/" + botCharacter);
        GameObject bot = Instantiate(botPrefab, botSpawnPoint.position, Quaternion.identity);
        bot.AddComponent<BotAI>();
        bot.AddComponent<HealthManager>();

        // Karşılıklı hedef atamaları
        player.GetComponent<PlayerController>().bot = bot.transform;
        bot.GetComponent<BotAI>().player = player.transform; 

        // Health bar referanslarını atama
        player.GetComponent<HealthManager>().healthbar = playerHealthBar;
        bot.GetComponent<HealthManager>().healthbar = botHealthBar;

        // Hasar yazısını ayarlama
        player.GetComponent<PlayerController>().damageText = damageText;
        bot.GetComponent<BotAI>().damageText = damageText;

        // Wizard projectile prefab'ini atama
        player.GetComponent<PlayerController>().wizardProjectilePrefab = wizardProjectile;
        bot.GetComponent<BotAI>().wizardProjectilePrefab = wizardProjectile;

        // Wizard stun object prefab'ini atama
        player.GetComponent<PlayerController>().wizardStunObjectPrefab = wizardStunObj;
        bot.GetComponent<BotAI>().wizardStunObjectPrefab = wizardStunObj;
    }
}