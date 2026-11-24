using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSphere
{
    public sealed partial class EcoSpherePrototype
    {
        internal void CheckProceduralBoard(WorldRecord expected,int[] selected)
        {
            if(levelLayout!=LevelLayout.Procedural || preparedWorld!=expected || activeSphereTypes.Length!=expected.RequiredSpheres || board.Cells.Count!=GameplayGeometry.CellCount(InitialRings) || tutorialPaused)
                throw new Exception("Procedural gameplay did not apply its world or four-sphere loadout");
            var repeated=new BoardRules(); ProceduralCluster.Populate(repeated,selected,InitialRings,expected.seed,expected.rules.clustering);
            foreach(var cell in board.Cells)
                if(repeated.Cells[cell.Key]!=cell.Value || Array.IndexOf(selected,cell.Value)<0)throw new Exception("Gameplay discarded procedural seed, clustering or selected types");
        }
        internal IEnumerator ExerciseProceduralShot()
        {
            int before=shots; direction=Vector2.up; gun.up=direction; Fire();
            float deadline=Time.realtimeSinceStartup+15;
            while(!ready && !complete && !failed && Time.realtimeSinceStartup<deadline)yield return null;
            if(shots!=before+1 || failed || (!ready && !complete))throw new Exception("Procedural shot did not resolve using common gameplay");
        }
        internal void FinishProceduralForSmoke()
        {
            if(complete)return;
            RemoveCells(new HashSet<Vector2Int>(board.Cells.Keys),ElementScoring.Breakaway,true);
            if(stoneReleased)throw new Exception("A selected Stone type was treated as a scripted natural discovery");
            complete=true; ready=false;
            if(loaded) {Destroy(loaded);loaded=null;}
            StartCoroutine(FinishTurn());
        }
    }
    public sealed class ProceduralLevelSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void Launch()
        {
            if(LaunchFlags.Has("-proceduralSmoke"))
                new GameObject("Procedural gameplay checks").AddComponent<ProceduralLevelSmoke>();
        }
        static void Expect(bool value,string message) {if(!value)throw new Exception("Procedural level: "+message);}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); yield return null; yield return null;
            var world=WorldLevels.Get(1,2); world.rules.clustering=.9f;
            world.rules.requiredSpheres=5; world.displayName="Shared world record check";
            var system=FindAnyObjectByType<SystemView>(); system.RefreshSystemHeading();
            Expect(system.planets[2].name==world.displayName && system.planets[2].transform.Find("Sphere Requirement/Sphere Count").GetComponent<TMPro.TMP_Text>().text=="5","System did not read edited world traits");
            system.OpenPlanet(2); yield return null; yield return null;
            var prep=FindAnyObjectByType<LevelPrepView>();
            Expect(prep && prep.Title.text==world.displayName && LevelPrepView.RequiredSpheres==5 && !prep.Begin.interactable,"procedural setup context or requirement");
            int[] selected={SphereIds.Fire,SphereIds.Ice,SphereIds.Water,SphereIds.Stone,SphereIds.Void};
            for(int i=0;i<4;i++)prep.AddSphere(selected[i]);
            Expect(!prep.Begin.interactable,"Begin allowed fewer than the world's required spheres");
            prep.AddSphere(selected[4]); Expect(prep.Begin.interactable,"world-defined five-sphere Begin stayed disabled");
            prep.Begin.onClick.Invoke(); yield return null; yield return null;
            var game=FindAnyObjectByType<EcoSpherePrototype>();
            Expect(SceneManager.GetActiveScene().name==WorldLevels.ProceduralSceneName && game && Time.timeScale==1,"shared procedural scene did not open");
            game.CheckProceduralBoard(world,selected); Capture(game);
            yield return game.ExerciseProceduralShot();
            if(game && SceneManager.GetActiveScene().name==WorldLevels.ProceduralSceneName)game.FinishProceduralForSmoke();
            float deadline=Time.realtimeSinceStartup+25;
            while(SceneManager.GetActiveScene().name!="Results" && Time.realtimeSinceStartup<deadline) { var rating=FindAnyObjectByType<StarRatingPresentation>(); if(rating && rating.RevealComplete)rating.ContinueButton.onClick.Invoke(); yield return null; }
            Expect(SceneManager.GetActiveScene().name=="Results" && GameServices.Progress.Find(1,2)!=null,"procedural win did not persist and open results");
            Expect(!GameServices.Progress.Find(1,2).discoveries.Contains(SphereIds.Stone),"ordinary selected Stone unlocked a natural discovery");
            Expect(WorldLevels.Get(1,2).seed==world.seed && WorldLevels.Get(1,2).rules.clustering==world.rules.clustering,"world layout settings changed after completion");
            var book=SphereCatalog.Current.recipeBook;
            Expect(book.TryCombine(ElementId.Ice,ElementId.Fire,out var water) && water.id==SphereIds.Water,"Water recipe missing");
            var amounts=new double[Enum.GetValues(typeof(ElementId)).Length]; amounts[(int)ElementId.Fire]=amounts[(int)ElementId.Ice]=620;
            Expect(!GameServices.Progress.Complete(9,0,0,0,null,amounts).discoveries.Contains(SphereIds.Water),"DEV rediscovered Water");
            Debug.Log("ECOSPHERE_PROCEDURAL_GAMEPLAY_PASSED: shared world name and five-sphere requirement across System/setup/gameplay, selected seeded board, real shot, results/save, scripted-only natural unlocks and actual Ice+Fire Water award");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#endif
        }
        static void Capture(EcoSpherePrototype game)
        {
            foreach(var size in new[] {new Vector2Int(900,1600),new Vector2Int(1600,900)})
            {
                var target=new RenderTexture(size.x,size.y,24);target.Create();game.SetPreviewTarget(target);game.RenderPresentation();
                var old=RenderTexture.active;RenderTexture.active=target;
                var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Logs/Procedural-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                RenderTexture.active=old;game.SetPreviewTarget(null);target.Release();Destroy(target);Destroy(pixels);
            }
        }
    }
}
