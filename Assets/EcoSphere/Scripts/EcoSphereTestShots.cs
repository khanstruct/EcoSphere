using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EcoSphere
{
    // Shared by the gameplay smokes that need hand-built boards and real pointer shots (erosion, freed spheres…).
    public sealed partial class EcoSpherePrototype
    {
        // Replaces the board with exactly these cells. Infested cells are hazards for the rules only (no alien art).
        internal void SetTestBoard(string label,params (int x,int y,int type,bool infested)[] cells)
        {
            foreach(var model in visuals.Values)Destroy(model);visuals.Clear();board.Cells.Clear();
            foreach(var alien in aliens.Values)Destroy(alien);aliens.Clear();board.Infested.Clear();
            foreach(var c in cells)
            {
                var cell=new Vector2Int(c.x,c.y);board.Cells.Add(cell,c.type);visuals.Add(cell,MakeSphere(label+" "+cell,c.type,cluster,BoardRules.Position(cell)));
                if(c.infested)board.Infested.Add(cell);
            }
            RefreshFreedGlows();
        }

        // Loads `type`, aims at a board cell and fires through real Input System mouse events (rotation held at 0).
        internal IEnumerator RealShot(int type,Vector2Int aimCell)
        {
            var mouse=Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            while(!ready || projectile || armBusy)yield return null;
            rotationDegrees=0;
            if(loaded)Destroy(loaded);loadedType=type;loaded=MakeSphere("Loaded "+Definition(type).displayName,type,gun,LoadedLocal);
            yield return null;
            Vector2 point=view.WorldToScreenPoint(visuals[aimCell].transform.position);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
            if(ready)throw new Exception("Test shot: a real click did not fire");
            while(projectile)yield return null;
        }
    }
}
