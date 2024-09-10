using System;
using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
    public class SpaceHolder : MonoBehaviour
    {
        public DiscretizedSpace Space = null;

        public readonly Vector3 Origin = Vector3.zero;
        // Use Spherecasts on a grid, should be sufficient to pass thru gridResolution + 2 * Radius openings?
        // At least along axes
        public static readonly float GridResolution = 0.75f;
        public static readonly float Radius = 0.2f;

        static SpaceHolder instance = null;
#if DEBUG_HEARING
        private float HearingCyclesUsedTimer = 0f;
        private int MaxHearingCyclesUsedInAFrame = 0;
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
            Vector3 Step = new Vector3(GridResolution, GridResolution, GridResolution);
            Space = new DiscretizedSpace(Origin, Step, Radius);
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
            if (DaggerfallUnity.Settings.HearingMaxCyclesPerFrame > 0)
            {
#if DEBUG_HEARING
                int hearingCyclesUsed = DaggerfallUnity.Settings.HearingMaxCyclesPerFrame - PathFinding.GetCyclesBudget();
                if (hearingCyclesUsed > MaxHearingCyclesUsedInAFrame)
                    MaxHearingCyclesUsedInAFrame = hearingCyclesUsed;
                if (Time.time > HearingCyclesUsedTimer)
                {
                    DaggerfallUI.AddHUDText(String.Format("Max cycles/frame {1} cache {2} cubes", MaxHearingCyclesUsedInAFrame, Space?.GetCacheCount()));
                    MaxHearingCyclesUsedInAFrame = 0;
                    HearingCyclesUsedTimer = Time.time + 2f;
                }
#endif
                DiscretizedSpace.SetCyclesBudget(DaggerfallUnity.Settings.HearingMaxCyclesPerFrame);
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