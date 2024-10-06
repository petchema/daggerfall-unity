using System;
using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
    public class SpaceHolder : MonoBehaviour
    {
        private DiscretizedNavigableSpace Space = null;
        private DiscretizedSpace.SpaceCubeAllocator<UInt32> NavigableSpaceCubeAllocator = null;
        private DiscretizedSpace.SpaceCubeAllocator<byte> SearchSpaceCubeAllocator = null;

        public readonly Vector3 Origin = Vector3.zero;
        // Use Spherecasts on a grid, should be sufficient to pass thru gridResolution + 2 * Radius openings?
        // At least along axes
        public static readonly float GridResolution = 0.65f;
        public static readonly float Radius = 0.2f;

        static SpaceHolder instance = null;

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

        public DiscretizedNavigableSpace GetSpace() { return Space ?? (Space = BuildDiscretizedSpace()); }

        public DiscretizedSpace.SpaceCubeAllocator<byte> GetSearchSpaceCubeAllocator() { return SearchSpaceCubeAllocator ?? (SearchSpaceCubeAllocator = new DiscretizedSpace.SpaceCubeAllocator<byte>()); }

        public void Start()
        {

        }

        protected DiscretizedNavigableSpace BuildDiscretizedSpace()
        {
            Vector3 Step = new Vector3(GridResolution, GridResolution, GridResolution);
            NavigableSpaceCubeAllocator = new DiscretizedSpace.SpaceCubeAllocator<UInt32>();
            Space = new DiscretizedNavigableSpace(Origin, Step, Radius, NavigableSpaceCubeAllocator);
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

        public void FixedUpdate()
        {
            if (DaggerfallUnity.Settings.HearingMaxCycles > 0)
            {
                DiscretizedNavigableSpace.SetCyclesBudget(DaggerfallUnity.Settings.HearingMaxCycles);
                // Background activity, clean arrays for free lists
                NavigableSpaceCubeAllocator?.Update();
                SearchSpaceCubeAllocator?.Update();
            }
        }

        private void OnTransition(PlayerEnterExit.TransitionEventArgs args)
        {
            Space?.Clear();
        }

        private void OnLoad(SaveData_v1 saveData)
        {
            Space?.Clear();
        }

        private void OnNewGame()
        {
            Space?.Clear();
        }

        private void OnFloatingOriginChange()
        {
            Space?.Clear();
        }
   }
}