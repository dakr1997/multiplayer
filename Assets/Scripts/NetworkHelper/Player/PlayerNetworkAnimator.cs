using Unity.Netcode.Components;

public class PlayerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // This is a client-side authority setup
    }
}