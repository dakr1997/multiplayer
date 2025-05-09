using Unity.Netcode.Components;


namespace Core.Player.Components
{
    /// <summary>
    /// PlayerNetworkTransform is a custom NetworkTransform component for player objects.
    /// It overrides the OnIsServerAuthoritative method to set client-side authority for movement.
    /// </summary>
public class PlayerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Client authoritative movement
    }
}
}