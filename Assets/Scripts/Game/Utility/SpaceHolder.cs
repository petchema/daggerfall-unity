using System;
using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
    public class SpaceHolder : MonoBehaviour
    {
        public DiscretizedSpace Space = null;

        public readonly Vector3 Origin = Vector3.zero;
        public readonly Vector3 Step = new Vector3(0.75f, 0.75f, 0.75f);

        static SpaceHolder instance = null;
#if DEBUG_HEARING
        private float HearingCyclesUsedTimer = 0f;
        private int MaxHearingCyclesUsedInAFrame = 0;
        private int MaxHearingRaycastsUsedInAFrame = 0;
#endif
        public static SpaceHolder Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject();
                    go.name = "SpaceHolder";
                    instance = go.AddComponent<SpaceHolder>();
                }
                return instance;
            }
        }

        public DiscretizedSpace GetSpace() { return Space ?? (Space = BuildDiscretizedSpace()); }

        public void Start()
        {

        }

        protected DiscretizedSpace BuildDiscretizedSpace()
        {
            Space = new DiscretizedSpace(Origin, Step);
            Space.SetRaycastBudget(0);
            return Space;
        }

        public void OnEnable()
        {
            PlayerEnterExit.OnTransitionInterior += OnTransition;
            PlayerEnterExit.OnTransitionDungeonInterior += OnTransition;
            PlayerEnterExit.OnTransitionExterior += OnTransition;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransition;
            StartGameBehaviour.OnNewGame += OnNewGame;
            SaveLoadManager.OnLoad += OnLoad;
            StreamingWorld.OnFloatingOriginChange += OnFloatingOriginChange;
        }

        public void OnDisable()
        {
            PlayerEnterExit.OnTransitionInterior -= OnTransition;
            PlayerEnterExit.OnTransitionDungeonInterior -= OnTransition;
            PlayerEnterExit.OnTransitionExterior -= OnTransition;
            PlayerEnterExit.OnTransitionDungeonExterior -= OnTransition;
            StartGameBehaviour.OnNewGame -= OnNewGame;
            SaveLoadManager.OnLoad -= OnLoad;
            StreamingWorld.OnFloatingOriginChange -= OnFloatingOriginChange;
        }

        public void Update()
        {
            if (DaggerfallUnity.Settings.HearingMaxCyclesPerFrame > 0 &&
                DaggerfallUnity.Settings.HearingMaxRaycastsPerFrame > 0)
            {
#if DEBUG_HEARING
                int hearingCyclesUsed = DaggerfallUnity.Settings.HearingMaxCyclesPerFrame - PathFinding.GetCyclesBudget();
                if (hearingCyclesUsed > MaxHearingCyclesUsedInAFrame)
                    MaxHearingCyclesUsedInAFrame = hearingCyclesUsed;
                int hearingRaycastsUsed = Space == null ? 0 : DaggerfallUnity.Settings.HearingMaxRaycastsPerFrame - Space.GetRaycastBudget();
                if (hearingRaycastsUsed > MaxHearingRaycastsUsedInAFrame)
                    MaxHearingRaycastsUsedInAFrame = hearingRaycastsUsed;
                if (Time.time > HearingCyclesUsedTimer)
                {
                    DaggerfallUI.AddHUDText(String.Format("Max raycasts/frame {0} cycles/frame {1} cache {2} cubes", MaxHearingRaycastsUsedInAFrame, MaxHearingCyclesUsedInAFrame, Space?.GetCacheCount()));
                    MaxHearingCyclesUsedInAFrame = 0;
                    MaxHearingRaycastsUsedInAFrame = 0;
                    HearingCyclesUsedTimer = Time.time + 2f;
                }
#endif
                PathFinding.SetCyclesBudget(DaggerfallUnity.Settings.HearingMaxCyclesPerFrame);
                Space?.SetRaycastBudget(DaggerfallUnity.Settings.HearingMaxRaycastsPerFrame);
            }
        }

        private void OnTransition(PlayerEnterExit.TransitionEventArgs args)
        {
            Space = null;
        }

        private void OnLoad(SaveData_v1 saveData)
        {
            Space = null;
        }

        private void OnNewGame()
        {
            Space = null;
        }

        private void OnFloatingOriginChange()
        {
            Space = null;
        }
    }
}