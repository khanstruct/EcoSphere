using UnityEngine;
namespace EcoSphere
{
    public sealed class GameServicesHost:MonoBehaviour
    {
        public PlayerProfile Profile=>GameServices.Profile.Active;
        public GamePhase Phase=>GameServices.Session.Phase;
        public bool IsPaused=>GameServices.Pause.IsPaused;
        void OnApplicationQuit(){GameServices.Reset();}
    }
}
