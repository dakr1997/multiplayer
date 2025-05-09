// Location: Core/Player/Data/PlayerMetaData.cs
using UnityEngine;

namespace Core.Player.Data
{
    /// <summary>
    /// Stores player progression data and stats
    /// </summary>
    [System.Serializable]
    public class PlayerMetaData
    {
        // Basic stats
        public string playerName = "Player";
        public int playerLevel = 1;
        public float totalExperience = 0f;
        
        // Core stats
        public float baseHealth = 100f;
        public float baseDamage = 10f;
        public float baseSpeed = 5f;
        
        // Defense stats
        public float damageResistance = 0f;
        
        // Experience factors
        public float expGainMultiplier = 1f;
        
        // Class info
        public PlayerClassType classType = PlayerClassType.Warrior;
        
        // Player preferences
        public Color playerColor = Color.blue;
        
        // Match statistics
        public int totalKills = 0;
        public int totalDeaths = 0;
        public int totalWaves = 0;
        public bool hasWonGame = false;
    }
    
    public enum PlayerClassType
    {
        Warrior,
        Archer,
        Mage
    }
}