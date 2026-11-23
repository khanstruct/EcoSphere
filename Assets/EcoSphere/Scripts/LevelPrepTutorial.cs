using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSphere
{
    public sealed partial class LevelPrepView
    {
        TutorialPopup setupPopup;
        IReadOnlyList<SetupTutorialStep> setupSteps;
        int dialogDismissedFrame=-1;
        public TutorialPopup SetupPopup => setupPopup;
        bool SetupInputBlocked => (setupPopup && setupPopup.IsOpen) || Time.frameCount==dialogDismissedFrame;
        SetupTutorialStep CurrentSetupStep
        {
            get
            {
                int index=world!=null ? GameServices.Progress.SetupTutorialStep(world.id) : 0;
                return setupSteps!=null && index<setupSteps.Count ? setupSteps[index] : null;
            }
        }
        void InitializeSetupTutorial()
        {
            if(DevelopmentMode.Enabled)return;
            var definition=WorldCatalog.Current.Find(world.predefinedId);
            if(!definition || definition.setupTutorial==null || definition.setupTutorial.Count==0)return;
            setupSteps=definition.setupTutorial;
            setupPopup=new GameObject("Setup tutorial").AddComponent<TutorialPopup>();
            setupPopup.transform.SetParent(transform,false);
            setupPopup.Initialize(frame,buttonPrefab,font);
            UpdateSetupTutorial();
        }
        void UpdateSetupTutorial()
        {
            if(!setupPopup || setupPopup.IsOpen)return;
            var step=CurrentSetupStep;
            if(step==null || (step.waitForFullLoadout && Selected.Count!=RequiredSpheres))return;
            var target=step.target==SetupTutorialTarget.RequiredSlots ? slots[0] :
                step.target==SetupTutorialTarget.OwnedSpheres ? (RectTransform)root.Find("Discovered Spheres/Collection viewport") : Begin.GetComponent<RectTransform>();
            setupPopup.Show(step.message,target,AdvanceSetupTutorial,step.target==SetupTutorialTarget.Begin);
        }
        void AdvanceSetupTutorial()
        {
            GameServices.Progress.AdvanceSetupTutorial(world.id); dialogDismissedFrame=Time.frameCount;
            UpdateSetupTutorial();
        }
        bool CanBeginDuringTutorial => setupPopup && setupPopup.IsOpen && CurrentSetupStep!=null && CurrentSetupStep.target==SetupTutorialTarget.Begin;
        void FinishBeginTutorial()
        {
            if(!CanBeginDuringTutorial)return;
            setupPopup.Close(); GameServices.Progress.AdvanceSetupTutorial(world.id);
        }
        internal void DismissSetupTutorialForSmoke()
        {
            if(!Array.Exists(Environment.GetCommandLineArgs(),arg=>arg.IndexOf("Smoke",StringComparison.OrdinalIgnoreCase)>=0))
                throw new InvalidOperationException("Test helper requires a smoke run");
            if(setupSteps==null)return;
            setupPopup.Close();
            while(CurrentSetupStep!=null)GameServices.Progress.AdvanceSetupTutorial(world.id);
            dialogDismissedFrame=-1;
        }
    }
}
