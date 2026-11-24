using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EcoSphere
{
    public enum GamePhase { System, Setup, Playing, Completing, Rating, Results, Failed }
    public sealed class GameSession
    {
        public int SystemNumber=1,PlanetIndex;
        public bool ReturningToSystem;
        public GamePhase Phase {get;internal set;}
        public WorldResult PendingResults {get;internal set;}
        // DEV-only composition readouts, keyed by world, handed from the level to the Results screen.
        readonly Dictionary<string,string> devReports=new Dictionary<string,string>();
        string DevReportKey=>SystemNumber+":"+PlanetIndex;
        internal void SetDevReport(string report)=>devReports[DevReportKey]=report;
        internal bool TryGetDevReport(out string report)=>devReports.TryGetValue(DevReportKey,out report);
        int[] pendingLoadout,activeLoadout;
        public WorldRecord Planet=>GameServices.Profile.GetPlanet(SystemNumber,PlanetIndex);
        public void PrepareLoadout(int[] selected){pendingLoadout=(int[])selected.Clone();}
        public int[] TakeLoadout(){var result=pendingLoadout;pendingLoadout=null;if(result!=null)activeLoadout=(int[])result.Clone();return result;}
        public void PrepareRetry(){if(activeLoadout!=null)pendingLoadout=(int[])activeLoadout.Clone();}
    }
    public sealed class PauseService
    {
        readonly HashSet<object> requests=new HashSet<object>();
        float previousScale=1;
        public bool IsPaused=>requests.Count>0;
        public void Acquire(object owner){if(requests.Contains(owner))return;if(requests.Count==0)previousScale=Time.timeScale;requests.Add(owner);Time.timeScale=0;}
        public void Release(object owner){if(!requests.Remove(owner))return;if(requests.Count==0)Time.timeScale=previousScale;}
        public void Reset(){requests.Clear();previousScale=1;Time.timeScale=1;}
    }
    public sealed class SceneNavigation
    {
        public void ShowSystem(){GameServices.Session.ReturningToSystem=true;Load("System",GamePhase.System);}
        public void ShowSetup(){Load("LevelPrep",GamePhase.Setup);}
        public void ShowResults(){Load("Results",GamePhase.Results);}
        public void Play(string scene){Load(scene,GamePhase.Playing);}
        public void Retry(){GameServices.Session.PrepareRetry();Play(SceneManager.GetActiveScene().name);}
        void Load(string scene,GamePhase phase){GameServices.Pause.Reset();GameServices.Session.Phase=phase;SceneManager.LoadScene(scene);}
    }
    // One composition root. Services own state; scene controllers only present/use it.
    public static class GameServices
    {
        public static ProfileService Profile {get;private set;}=new ProfileService();
        public static GameSession Session {get;private set;}=new GameSession();
        public static PauseService Pause {get;private set;}=new PauseService();
        public static SceneNavigation UI {get;private set;}=new SceneNavigation();
        public static PlanetProgressService Progress {get;private set;}=new PlanetProgressService();
        public static AudioService Audio {get;private set;}=new AudioService();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Pause.Reset();Profile=new ProfileService();Session=new GameSession();Progress=new PlanetProgressService();UI=new SceneNavigation();Audio=new AudioService();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap(){var root=new GameObject("Game services");UnityEngine.Object.DontDestroyOnLoad(root);root.AddComponent<GameServicesHost>();}
    }
}
